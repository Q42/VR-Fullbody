using UnityEngine;
using System.Collections;

/********************************************************************************
* This is a very basic example showing which KeyCodes are used by the
* buttons of the Shutter.
*********************************************************************************/

public class ShutterKeyBindingExample : MonoBehaviour {
	void Update() {
		// SHUTTER
		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			Debug.Log("SHUTTER - Forward Trigger pressed (L Button).");		
		}		
		if (Input.GetKeyDown (KeyCode.UpArrow)) {
			Debug.Log("SHUTTER - Top right button pressed (R Button).");		
		}
		
		if (Input.GetKeyDown (KeyCode.B)) {
			Debug.Log("SHUTTER - Red stick pressed downwards.");		
		}		
		if (Input.GetKeyDown (KeyCode.Tab)) {
			Debug.Log("SHUTTER - Red stick pressed to the right.");		
		}		
		if (Input.GetKeyDown (KeyCode.Return)) {
			Debug.Log("SHUTTER - Red stick pressed to the left.");		
		}
		if (Input.GetKeyDown (KeyCode.Escape)) {
			Debug.Log("SHUTTER - Red stick pressed upwards (Mode ESC).");
		}
	}
}
