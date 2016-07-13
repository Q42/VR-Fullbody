using UnityEngine;
using Neuron;
using System.Collections.Generic;

namespace Retargeting
{
    static class BoneReferences
    {
        public static Dictionary<NeuronBones, HumanBodyBones> PerceptionNeuronBones = new Dictionary<NeuronBones, HumanBodyBones>()
        {
            { NeuronBones.Hips , HumanBodyBones.Hips },
            { NeuronBones.RightUpLeg , HumanBodyBones.RightUpperLeg },
            { NeuronBones.RightLeg , HumanBodyBones.RightLowerLeg },
            { NeuronBones.RightFoot , HumanBodyBones.RightFoot },
            { NeuronBones.LeftUpLeg , HumanBodyBones.LeftUpperLeg },
            { NeuronBones.LeftLeg , HumanBodyBones.LeftLowerLeg },
            { NeuronBones.LeftFoot , HumanBodyBones.LeftFoot },
            { NeuronBones.Spine , HumanBodyBones.Spine },
            { NeuronBones.Spine1 , HumanBodyBones.Chest },
            { NeuronBones.Spine2 , HumanBodyBones.Chest },
            { NeuronBones.Spine3 , HumanBodyBones.Chest },
            { NeuronBones.Neck , HumanBodyBones.Neck },
            { NeuronBones.Head , HumanBodyBones.Head },
            { NeuronBones.RightShoulder , HumanBodyBones.RightShoulder },
            { NeuronBones.RightArm , HumanBodyBones.RightUpperArm },
            { NeuronBones.RightForeArm , HumanBodyBones.RightLowerArm },
            { NeuronBones.RightHand , HumanBodyBones.RightHand },
            { NeuronBones.RightHandThumb1 , HumanBodyBones.RightThumbProximal },
            { NeuronBones.RightHandThumb2 , HumanBodyBones.RightThumbIntermediate },
            { NeuronBones.RightHandThumb3 , HumanBodyBones.RightThumbDistal },
            { NeuronBones.RightHandIndex1 , HumanBodyBones.RightIndexProximal },
            { NeuronBones.RightHandIndex2 , HumanBodyBones.RightIndexIntermediate },
            { NeuronBones.RightHandIndex3 , HumanBodyBones.RightIndexDistal },
            { NeuronBones.RightHandMiddle1 , HumanBodyBones.RightMiddleProximal },
            { NeuronBones.RightHandMiddle2 , HumanBodyBones.RightMiddleIntermediate },
            { NeuronBones.RightHandMiddle3 , HumanBodyBones.RightMiddleDistal },
            { NeuronBones.RightHandRing1 , HumanBodyBones.RightRingProximal },
            { NeuronBones.RightHandRing2 , HumanBodyBones.RightRingIntermediate },
            { NeuronBones.RightHandRing3 , HumanBodyBones.RightRingDistal },
            { NeuronBones.RightHandPinky1 , HumanBodyBones.RightLittleProximal },
            { NeuronBones.RightHandPinky2 , HumanBodyBones.RightLittleIntermediate },
            { NeuronBones.RightHandPinky3 , HumanBodyBones.RightLittleDistal },
            { NeuronBones.LeftShoulder , HumanBodyBones.LeftShoulder },
            { NeuronBones.LeftArm , HumanBodyBones.LeftUpperArm },
            { NeuronBones.LeftForeArm , HumanBodyBones.LeftLowerArm },
            { NeuronBones.LeftHand , HumanBodyBones.LeftHand },
            { NeuronBones.LeftHandThumb1 , HumanBodyBones.LeftThumbProximal },
            { NeuronBones.LeftHandThumb2 , HumanBodyBones.LeftThumbIntermediate },
            { NeuronBones.LeftHandThumb3 , HumanBodyBones.LeftThumbProximal },
            { NeuronBones.LeftHandIndex1 , HumanBodyBones.LeftIndexProximal },
            { NeuronBones.LeftHandIndex2 , HumanBodyBones.LeftIndexIntermediate },
            { NeuronBones.LeftHandIndex3 , HumanBodyBones.LeftIndexDistal },
            { NeuronBones.LeftHandMiddle1 , HumanBodyBones.LeftMiddleProximal },
            { NeuronBones.LeftHandMiddle2 , HumanBodyBones.LeftMiddleIntermediate },
            { NeuronBones.LeftHandMiddle3 , HumanBodyBones.LeftMiddleDistal },
            { NeuronBones.LeftHandRing1 , HumanBodyBones.LeftRingProximal },
            { NeuronBones.LeftHandRing2 , HumanBodyBones.LeftRingIntermediate },
            { NeuronBones.LeftHandRing3 , HumanBodyBones.LeftRingDistal },
            { NeuronBones.LeftHandPinky1 , HumanBodyBones.LeftLittleProximal },
            { NeuronBones.LeftHandPinky2 , HumanBodyBones.LeftLittleIntermediate },
            { NeuronBones.LeftHandPinky3 , HumanBodyBones.LeftLittleDistal }
        };
    }
}