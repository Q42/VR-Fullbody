/************************************************************************************
 Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using UnityEngine;
using System.Collections;
using Neuron;

/********************************************************************************
* This is a very basic example how to get motion capture data from the network.
* In this example, we use NeuronSource and NeuronActor classes.
* We then apply the motion data to target skeleton using the animator component.
*********************************************************************************/

public class CustomAnimatorDriver : MonoBehaviour
{
	// We use NeuronSource to handle the connection
	NeuronSource								source = null;
	NeuronActor									actor = null;
	
	// This value is used for NeuronAnimatorMaster to interpolate between the different frames that we received.
	int											lastEvaluateTime;

	public Animator								animator = null;								// The animator component which receives the mocap data
	public string 								address = "127.0.0.1";							// Axis Neuron IP address
	public int 									port = 7001;									// Axis Neuron port
	public int									commandServerPort = 7001;						// Axis command server port
	
	public NeuronConnection.SocketType			socketType = NeuronConnection.SocketType.TCP;	// Socket type set in Axis Neuron( "File->Settings->Broadcasting" )

	void Awake()
	{
		// If no animator was assigned, try to get one
		if( animator == null )
		{
			animator = GetComponent<Animator>();
		}
	}
	
	void OnEnable()
	{
		// Connect to target machine
		source = NeuronConnection.Connect( address, port, commandServerPort, socketType );
		// Get first actor from source
		actor = source.AcquireActor( 0 );
	}
	
	void OnDisable()
	{
		// Disconnect from the target machine, because multiple instances that require mocap data might connect to the same source.
		// We use a simple reference counter to handle connections, so each connection must have be paired with a disconnection.
		NeuronConnection.Disconnect( source );
	}
	
	void Update()
	{
		if( animator != null && source != null )
		{
			// Here we use NeuronAnimatorMaster to apply our motion to the animator component.
			Vector3[] positionOffsets = new Vector3[(int)HumanBodyBones.LastBone];
			Vector3[] rotationOffsets = new Vector3[(int)HumanBodyBones.LastBone];
			NeuronAnimatorInstance.ApplyMotion( actor, animator, positionOffsets, rotationOffsets );
			
			// If you want to do some customization and set bones transforms in your own way, use the following methods.
			
			// Update Hips
			// Transform hips_t = animator.GetBoneTransform( HumanBodyBones.Hips );
			// hips_t.position = actor.GetReceivedPosition( NeuronBones.Hips );
			// hips_t.root = actor.GetReceivedRotation( NeuronBones.Hips );
			
			// Update RightUpLeg
			// Transform rightUpperLeg_t = animator.GetBoneTransform( HumanBodyBones.RightUpperLeg );
			// rightUpperLeg_t.position = actor.GetReceivedPosition( NeuronBones.RightUpLeg );
			// rightUpperLeg_t.rotation = actor.GetReceivedRotation( NeuronBones.RightUpLeg );
			
			// ... and so on for the other bones.
			
			// In this example we traverse the HumanBodyBones to get transforms components referenced in the animator component.
			// If you don't want to use the Animator component, you can assign the bones transforms references in your own way.
			// You can check NeuronHelper.Bind() to see how we bind bones using a naming convention with prefixs.
			// Just keep in mind that the Neuron default model might be rigged differently then yours.
			// For further info on this subject check the included documentation.
		}
	}
}
