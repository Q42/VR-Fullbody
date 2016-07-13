using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Easy automated character retargeting based on Unity's Mecanim system.
/// The Unity Mecanim system provides an easy to use (semi-)automated character rig creator.
/// To use this script an Animator component is required, with a Humanoid Mecanim character rig setup inside of it.
///
/// By Ricardo Snoek, Q42 © 2016-07-12
/// </summary>
namespace Retargeting
{
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Retargeting/General Retargeting")]
    public class GeneralRetargeting : MonoBehaviour
    {

        // Animator containing character rig, used for referencing bones
        [SerializeField]
        public Animator CharacterAnimator;

        // Dictionary containing retargetable bone transforms, these transforms can be edited 
        protected Dictionary<HumanBodyBones, Transform> SkeletalBones;
		// Calculate all default Quaternion values for each bone
    	protected Quaternion[] DefaultRotations;
		// Calculate the default relative Quaternion values for each bone
		protected Quaternion[] ResetRotations;

        /// <summary>
        /// This function is executed while in editor.
        /// Checks if all values are actually filled in and correct.
        /// </summary>
        void OnValidate()
        {
            if (CharacterAnimator == null)
            {
                CharacterAnimator = GetComponent<Animator>();
            }
        }

        /// <summary>
        /// This function is executed on start.
        /// Check if character animator has humanoid bone structure, then find all usable bone transforms
        /// </summary>
        void Start()
        {
            if (CharacterAnimator.hasTransformHierarchy && CharacterAnimator.isHuman)
            {
                SkeletalBones = FetchBones();
				DefaultRotations = FetchDefaultBoneRotations(SkeletalBones);
				ResetRotations = FetchDefaultResetRotations(SkeletalBones, CharacterAnimator);
            }
            else
            {
                Debug.LogWarning("GENERAL RETARGETING: Retargeting is impossible for non humanoid character rigs");
            }
        }

        /// <summary>
        /// This function is called by MonoBehaviour start.
        /// Find all bones in the character rig, then put them inside a dictionary that we can manipulate later on.
        /// </summary>
        /// 
        /// <returns>Dictionary containing bone transforms.</returns>
        private Dictionary<HumanBodyBones, Transform> FetchBones()
        {
            Dictionary<HumanBodyBones, Transform> bones = new Dictionary<HumanBodyBones, Transform>();

            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (CharacterAnimator.GetBoneTransform(bone) != null)
                {
                    bones.Add(bone, CharacterAnimator.GetBoneTransform(bone));
                }
            }

            return bones;
        }
		
		private Quaternion[] FetchDefaultBoneRotations(Dictionary<HumanBodyBones, Transform> bones) {
			Quaternion[] rotations = new Quaternion[bones.Count];

			foreach(KeyValuePair<HumanBodyBones, Transform> bone in bones) {
				rotations[(int)bone.Key] = bone.Value.localRotation;
			}

			return rotations;
		}

		private Quaternion[] FetchDefaultResetRotations(Dictionary<HumanBodyBones, Transform> bones, Animator animator) {
			Quaternion[] rotations = new Quaternion[bones.Count];

			foreach(KeyValuePair<HumanBodyBones, Transform> bone in bones) {
				rotations[(int)bone.Key] = Quaternion.Inverse(bone.Value.parent.rotation) * animator.transform.rotation;
			}

			return rotations;
		}

        /// <summary>
        /// This function can be called in a MonoBehaviour update.
        /// Set the position of a provided bone, the transform is referenced form skeletal bones dictionary.
        /// </summary>
        /// 
        /// <param name="bone">HumanBodyBones enumeration value</param>
        /// <param name="position">Vector3 transform positional value</param>
        public void SetBonePosition(HumanBodyBones bone, Vector3 position)
        {
            if (SkeletalBones.ContainsKey(bone))
            {
                SkeletalBones[bone].position = position;
            }
            else
            {
                Debug.LogError("GENERAL RETARGETING: The provided bone is not provided by the animator's character rig");
            }
        }

        /// <summary>
        /// This function can be called in a MonoBehaviour update.
        /// Set the rotation of a provided bone, the transform is referenced form skeletal bones dictionary.
        /// </summary>
        /// 
        /// <param name="bone">HumanBodyBones enumeration value</param>
        /// <param name="rotation">Quaternion transform rotational value</param>
        public void SetBoneRotation(HumanBodyBones bone, Quaternion rotation)
        {
            if (SkeletalBones.ContainsKey(bone))
            {
                SkeletalBones[bone].localRotation = rotation;
            }
            else
            {
                Debug.LogError("GENERAL RETARGETING: The provided bone is not provided by the animator's character rig");
            }
        }
    }

}