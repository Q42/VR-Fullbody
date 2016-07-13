using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Neuron;
using System;

namespace Retargeting
{
    [AddComponentMenu("Retargeting/Perception Neuron Retargeting")]
    public class PerceptionNeuronRetargeting : GeneralRetargeting
    {
        [SerializeField]
        public string Address = "127.0.0.1";

        [SerializeField]
        public int Port = 7001;

        [SerializeField]
        public int CommandServerPort = 7007;

        [SerializeField]
        public NeuronConnection.SocketType SocketType = NeuronConnection.SocketType.TCP;

        [SerializeField]
        public int ActorId = 0;

        private NeuronSource Source;
        private NeuronActor Actor;
        private float ScaleFactor;

        void OnStart()
        {
            ScaleFactor = CalculateScaleFactor();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateBoneRotations();
        }

        void OnEnable()
        {
            Source = NeuronConnection.Connect(Address, Port, CommandServerPort, SocketType);

            if (Source != null)
            {
                Actor = Source.AcquireActor(ActorId);
            }
        }

        void OnDisable()
        {
            if (Source != null)
            {
                NeuronConnection.Disconnect(Source);
            }

            Source = null;
            Actor = null;
        }

        private float CalculateScaleFactor() {
            const float baseHipsHeight = 1.113886f;

            float feetPosition = (SkeletalBones[HumanBodyBones.LeftFoot].position.y + SkeletalBones[HumanBodyBones.RightFoot].position.y) * 0.5f;
            feetPosition -= (CharacterAnimator.leftFeetBottomHeight + CharacterAnimator.leftFeetBottomHeight) * 0.5f;

            return (SkeletalBones[HumanBodyBones.Hips].position.y - feetPosition) / baseHipsHeight;
        }

        private void UpdateBoneRotations()
        {
            foreach (KeyValuePair<NeuronBones, HumanBodyBones> bone in BoneReferences.PerceptionNeuronBones)
            {
                SetBoneRotation(bone.Value, CalculateBoneRotation(bone.Value, bone.Key));
            }
        }

        private void UpdateRootPosition()
        {
            Vector3 position = Actor.GetReceivedPosition(NeuronBones.Hips);
            Quaternion rotation = Quaternion.Euler(Actor.GetReceivedRotation(NeuronBones.Hips));

            Quaternion defaultRotation = DefaultRotations[(int)HumanBodyBones.Hips];
            Quaternion resetRotation = ResetRotations[(int)HumanBodyBones.Hips];

            SetBonePosition(HumanBodyBones.Hips, resetRotation * position * ScaleFactor);
        }

        private Quaternion CalculateBoneRotation(HumanBodyBones humanBone, NeuronBones neuronBone)
        {
            Quaternion neuronBoneRotation = Quaternion.Euler(Actor.GetReceivedRotation(neuronBone));
            Quaternion defaultRotation = DefaultRotations[(int)humanBone];
            Quaternion resetRotation = ResetRotations[(int)humanBone];
            Quaternion resetRotationInversed = Quaternion.Inverse(resetRotation);

            return resetRotation * neuronBoneRotation * resetRotationInversed * defaultRotation;
        }
    }
}
