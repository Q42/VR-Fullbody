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

/* Handles camera rotation and position
 * 
 */

public class CameraController : MonoBehaviour {
	public Transform Target;
	public bool FollowTarget;
	public bool CameraMovementEnabled;
	public Transform TargetToMoveTo;
	public float FollowSpeed = 1.0f;
	public float distance = 5.0f;
	public float ScreenSizeToSpeedFactor = 32.0f;
	public float yMinLimit = -20f;
	public float yMaxLimit = 80f;
	
	public float distanceMin = .5f;
	public float distanceMax = 15f;

	private Vector3 camTargetSpeed;
	private float x = 0.0f;
	private float y = 0.0f;	
	private float xSpeed = 120.0f;
	private float ySpeed = 120.0f;
	private bool MouseButton0Active;
	private bool MouseButton1Active;
	private float originalDistance;

	void Start () {
		Target.localPosition = Vector3.zero;
		Vector3 angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
		originalDistance = distance;
	}

	void Update() {
		if (Input.GetMouseButtonDown (0) && GetComponent<Camera>().pixelRect.Contains (Input.mousePosition)) {
			MouseButton0Active = true;
		}
		if (Input.GetMouseButtonDown (1) && GetComponent<Camera>().pixelRect.Contains (Input.mousePosition)) {
			MouseButton1Active = true;
		}
		
		if (Input.GetMouseButtonUp (0)) {
			MouseButton0Active = false;
		}
		
		if (Input.GetMouseButtonUp (1)) {
			MouseButton1Active = false;
		}

		// Camera position
		if (MouseButton0Active && CameraMovementEnabled) {
			camTargetSpeed.x -= Input.GetAxis ("Mouse X") * 0.025f;
			camTargetSpeed.y -= Input.GetAxis ("Mouse Y") * 0.025f;			
		}

		// Camera rotation
		if (MouseButton1Active) {
			x += Input.GetAxis ("Mouse X") * xSpeed * distance * 0.02f;
			y -= Input.GetAxis ("Mouse Y") * ySpeed * 0.02f;
			
			y = ClampAngle (y, yMinLimit, yMaxLimit);
		}

		// Camera local space movement
		Target.Translate (Vector3.left * camTargetSpeed.x, Space.Self);
		Target.Translate (Vector3.up * camTargetSpeed.y, Space.Self);
		Target.LookAt (transform.position);


	}

	void LateUpdate () {
		if (Target) {
			Quaternion rotation = Quaternion.Euler(y, x, 0);

			if (GetComponent<Camera>().pixelRect.Contains (Input.mousePosition)){
				distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel")*5, distanceMin, distanceMax);
			}
			Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
			Vector3 position = rotation * negDistance + Target.position;
			
			transform.rotation = rotation;
			transform.position = position;

			// reset
			if (Input.GetKeyDown (KeyCode.Backspace)) {
				Target.localPosition = Vector3.zero;
				distance = originalDistance;
			}

			if (Input.GetKeyDown(KeyCode.H) ) {
				if (CameraMovementEnabled) { // only allow turn on/off for cameras with movement
					if (!FollowTarget) Target.localPosition = Vector3.zero;

					FollowTarget = !FollowTarget;
				}
			}
		}

		if ( FollowTarget && TargetToMoveTo != null ) {
			transform.parent.position = TargetToMoveTo.position;
		}


		camTargetSpeed = Vector3.zero;
	}
	
	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		return Mathf.Clamp(angle, min, max);
	}
	
	
}