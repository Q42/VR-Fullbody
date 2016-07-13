# Hair Rendering

Hair rendering is a combination of custom shader and render component developed for rendering the characters hair in The Blacksmith.


### How does it work?

There are two parts to the hair rendering used in The Blacksmith: a custom hair shader implementing an anisotropic reflection model, and a rendering component setting up shading input and managing depth sorting.

#### The shader part

The shader is just like any other shader, you assign it to a material and configure the properties until you are happy with the way it looks.

The basis of the shading model is the fairly standard approach of using two individually shifted Kajiya Kay (KK) anisotropic highlights (primary and secondary) to approximate the reflected and sub-scattered components identified by Marschner's research papers. The BRDF is currently a mix between the builtin PBS and KK with wrapped lambert (a.la. Valve/Pixar) as occlusion term. It uses a tangent space flow map (XY-directions major as opposed to Z-direction major of tangent space normal maps) dictating the comb of the hair and thus controlling highlight direction.

Both primary and secondary highlights have a direct and an indirect component. The direct is attenuated by directional shadows and a back-facing factor and scaled by the light's color. Indirect color is sampled from light probes. There are (non-physical) artistic options for controlling the mixing of the primary/secondard direct/indirect terms. In addition, a hair 'grayness' level is read from the mesh vertex color alpha channel.

#### The component part

The Hair Renderer component targets a single piece of hair geometry, thus the example character has one for hair, and another one for the beard. 

The renderer component tries to classify triangles into continuous strips of hair (patches), which are then sorted based on a chosen heuristic. In fairness, it doesn't try very hard to do this, so it really works best if the source mesh is exported with fairly 'continuous patches' as opposed to random triangle soups. We experimented with both view-dependent and view-independent sorting methods, and landed on something as simple as view-independently sorting the patches spatially bottom-up (i.e. by increasing "world-space" Y value). This works reasonably well as a starting point, as patches naturally flow on top of lower ones. There's some additional runtime rendering setup to ensure sorting is of sufficient quality from all view directions (see below).

Ambient occlusion is generated as an exponential factor from patch points identified as 'start' through 'end'. The component stores three different occlusion intensities in vertex color rgb (next to the optional vertex alpha grayness). The shader can pick either of these; our uses the darkes of the occlusion variants.

In playmode, the debugmode drop-down in the inspector helps shed some light on the different components making up the combined shading.

Note that since the mesh API doesn't currently expose access to blendshapes, any corrective hair blends will get lost in mesh processing. If your project needs corrective blends, you would have to use bone animation to solve those issues.

#### Runtime rendering 

At runtime, the hair is rendered in three separate passes to ensure best possible depth sorting (some of these can be toggled or modified in component options, though):
- Pass 0: Renders both back- and front-faces alpha-TESTED. Writes depth. Tests depth comparing LESSEQUAL. Clips based on a alpha cutoff (typically isolating opaque parts only).
- Pass 1: Renders back-faces alpha-BLENDED. Does NOT write depth. Tests depth comparing LESS. Clips based on alpha cutoff (typically clipping only fully transparent pixels).
- Pass 2: Renders front-faces alpha-BLENDED. Does not write depth. Tests depth comparing LESS. Clips based on alpha cutoff (typically clipping only fully transparent pixels).

The 3-pass rendering and ambient occlusion components are ONLY available in PLAYMODE, but the shader and its reflectance model work even in non-play mode.


### Setting up your own

Examining the included example scene is the easiest way to get an impression of how the setup looks. In particular, the game objects you should be looking at are challenger_beard_renderer and challenger_hair_renderer. The following steps are the minimum required to setup a new hair rendering component:
- Create a new GameObject as a child of the hair mesh you're targeting, make sure it doesn't have any local transformation (it doesn't strictly need to be a child, but depending on how your project animates things, it might need to be in the hierarchy)
- Add a HairRenderer component and populate the Source Renderer field (the renderer of the object's parent if you made it a child)
- Make sure the material of the renderer you're targeting uses the hair shader (or a custom, equivalent variant)

Press 'play' and notice how the source renderer gets disabled and three new children are added dynamically to the game object you just created; these are the object rendering the three passes mentioned above. There are no dynamic updates being done on these, they are created once, and then just sorted and rendered as any other Unity renderers.


### Shader options

**KK Flow Map**: The directional comb texture for anisotropic reflectance.

**KK Reflective Smoothness**: The smoothness with which to sample reflection probes.

**KK Reflective Gray Scale**: A measure of how much the hair's diffuse luminance tones down the reflective contribution.

**KK Primary Specular Color**: Specular color for the primary highlight.

**KK Primary Exponent**: Specular exponent for the primary highlight.

**KK Primary Root Shift**: Highlight shifting along the reflection tangent for the primary highlight. Use in conjuction with secondary shift to offset the two highlights.

**KK Secondary Specular Color**: Specular color for the secondary highlight.

**KK Secondary Exponent**: Specular exponent for the secondary highlight.

**KK Secondary Root Shift**: Highlight shifting along the reflection tangent for the secondary highlight. 

**KK Spec Mix Direct Factors**: Scaling factors for controlling the mix of the direct specular contributions. X=Primary KK, Y=Secondary KK, Z=Blinn

**KK Spec Mix Indirect Factors**: Scaling factors for controlling the mix of the indirect specular contributions. X=Primary KK, Y=Secondary KK, Z=Blinn


### Hair Renderer options

**Source Renderer**: The renderer to replace with three-pass rendering.

**Mode**: How to generate the 'sorted' and occluded mesh. Normally you'd just leave this at StaticHeightBased.

**Use Opaque Pass**: Whether to use a base opqaue pass before blending front and back edges on top of it. You normally need this to get shadows and depth information for the hair.

**Opaque Pass Ref**: Alpha cutoff for the opaque pass. Should be set high enough that only perceptively opaque portions are rendered in this pass.

**Front Write Depth**: Whether the front facing pass writes depth. Essentially sacrifices softness for more accurate depth information.

**Front Back Alpha Ref**: Alpha cutoff for the front and back pass. Used as an optimization to clip pixels that contribute almost nothing to the blended output.

**Debug Mode**: Various component visualizations.
