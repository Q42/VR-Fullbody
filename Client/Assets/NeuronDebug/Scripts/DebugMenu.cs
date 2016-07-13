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
using System.Collections.Generic;
using UnityEngine.UI;
using Neuron;

public class DebugMenu : NeuronInstancesManager
{
	public GameObject UIElements;
	public GameObject VerticalGrid;

	public Camera MainCamera;
	public Camera[] SideCameras;

	public InputField IPField;
	public InputField PortField;
	public InputField NewActorOffsetZ;
	public Toggle UDPToggle;
	public Toggle VSyncToggle;
	public Text ButtonLabel;
	public Text infoText;
	public GameObject FPSText;
	
	public int currentInstanceIndex = 0;
	
	private int _currentLineMode = 0;
	private int _currentViewMode = 1;
	private int _currentVSyncCount = 0;

	void Awake()
	{
		numOfReserveInstances = 1;
	}
	
	new void OnApplicationQuit()
	{
		base.OnApplicationQuit();
	}

	void Start ()
	{
		ToggleViewMode ();
	}
	
	new void Update()
	{
		base.Update();
		
		if( infoText != null && Time.frameCount % 30 == 0 )
		{
			infoText.text = GenerateNeuronMasterInfo( currentInstanceIndex );
		}
		
		// handle keybindings
		if (Input.GetKeyDown (KeyCode.C))
		{
			ToggleViewMode();
		}

		if (Input.GetKeyDown (KeyCode.L))
		{
			// Switch between different modes and then toggle accordingly
			if (_currentLineMode == 0)
			{
				_currentLineMode = 1;
			}
			else if(_currentLineMode == 1)
			{
				_currentLineMode = 2;
			}
			else
			{
				_currentLineMode = 0;
			}
			
			ToggleTrailRenderers();
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			UIElements.SetActive(!UIElements.activeSelf);
		}
		
		if (Input.GetKeyDown(KeyCode.G))
		{
			VerticalGrid.SetActive(!VerticalGrid.activeSelf);
		}
		
		if( Input.GetKeyDown(KeyCode.T) )
		{
			++currentInstanceIndex;
			currentInstanceIndex = currentInstanceIndex % numOfInstances;
		}

		if( Input.GetKeyDown(KeyCode.F) )
		{
			FPSText.SetActive(!FPSText.activeSelf);
		}
		
		SetCameraTargets( currentInstanceIndex );
		
		// toggle VSync
		ToggleVSync();
	}
	
	void ToggleVSync()
	{
		if( QualitySettings.vSyncCount == 2 )
		{
			_currentVSyncCount = 2;
			VSyncToggle.isOn = true;
		}
		else if( ( VSyncToggle.isOn && _currentVSyncCount != 1 || !VSyncToggle.isOn && _currentVSyncCount != 0 ) && _currentVSyncCount != 2 )
		{
			if( VSyncToggle.isOn )
			{
				QualitySettings.vSyncCount = 1;
				_currentVSyncCount = 1;
			}
			else
			{
				QualitySettings.vSyncCount = 0;
				_currentVSyncCount = 0;
			}
		}
		else if( QualitySettings.vSyncCount != _currentVSyncCount )
		{
			_currentVSyncCount = QualitySettings.vSyncCount;
			if( _currentVSyncCount == 0 )
			{
				VSyncToggle.isOn = false;
			}
			else if( _currentVSyncCount == 1 )
			{
				VSyncToggle.isOn = true;
			}
		}
	}
	
	public void AddConnection()
	{
		string address = IPField.text.ToString();
		int port = int.Parse (PortField.text);
		
		NeuronSource source = FindSource( address, port );
		if( source != null )
		{
			if ( source.numOfActiveActors == 0 )
			{
				Disconnect( source.address, source.port );
			} 
			else 
			{
				Debug.Log( string.Format( "[NeuronDebugViewer] Connection to {0}:{1} already present.", address, port ) );
				return;
			}
		}
		
		NeuronConnection.SocketType socketType = UDPToggle.isOn ? NeuronConnection.SocketType.UDP : NeuronConnection.SocketType.TCP;
		Connect( address, port, -1, socketType );
	}
	
	public void RemoveConnection()
	{
		string address = IPField.text.ToString();
		int port = int.Parse (PortField.text);
		Debug.Log( string.Format( "[NeuronDebugViewer] Remove connection {0}:{1}", address, port ) );
		Disconnect( address, port );
	}
	
	new public void DiconnectAll()
	{
		Debug.Log( "[NeuronDebugViewer] DisconnectAll" );
		base.DiconnectAll();
	}

	private string GenerateNeuronMasterInfo( int instanceIndex )
	{
		if( instanceIndex >= numOfInstances )
		{
			return string.Empty;
		}
		
		NeuronAnimatorInstance instance = GetInstances()[instanceIndex];
		if( instance == null )
		{
			return string.Empty;
		}
		
		string line = string.Empty;
		NeuronActor actor = instance.GetActor();
		if( actor != null )
		{
			line += string.Format( "Data version: {0}.{1}.{2}.{3}\n", actor.version.Major, actor.version.Minor, actor.version.Revision, actor.version.BuildNumb );
			line += string.Format( "Actor name: {0}\n", actor.name );
			line += string.Format( "Actor index: {0}\n", actor.index );
			line += string.Format( "With displacement: {0}\n", actor.withDisplacement );
			line += string.Format( "With reference: {0}\n", actor.withReference );
			line += string.Format( "Number of connections: {0}\n", NeuronConnection.numOfSources );
			line += string.Format( "Number of active actors: {0}\n", actor.owner.numOfActiveActors );
			line += string.Format( "Number of suspended actors: {0}\n", actor.owner.numOfSuspendedActors );
		}
		return line;
	}

	private void SetCameraTargets( int instanceIndex )
	{
		if( instanceIndex < numOfInstances )
		{
			NeuronAnimatorInstance instance = GetInstances()[instanceIndex];
			MainCamera.GetComponent<CameraController> ().TargetToMoveTo = instance.boundAnimator.GetBoneTransform (HumanBodyBones.Hips);
			SideCameras[0].GetComponent<CameraController> ().TargetToMoveTo = instance.boundAnimator.GetBoneTransform (HumanBodyBones.RightIndexProximal);
			SideCameras[1].GetComponent<CameraController> ().TargetToMoveTo = instance.boundAnimator.GetBoneTransform (HumanBodyBones.LeftIndexProximal);
			VerticalGrid.GetComponent<GridFollowActor>().Target = instance.boundAnimator.GetBoneTransform (HumanBodyBones.Hips);
		}
	}
	
	private void ToggleTrailRenderers()
	{
		NeuronAnimatorInstance[] instances = GetInstances();
		for( int i = 0; i < instances.Length; ++i )
		{
			NeuronAnimatorInstance instance = instances[i];
			ToggleTrailRenderers( instance );
		}
	}
	
	private void ToggleTrailRenderers( NeuronAnimatorInstance instance )
	{
		TrailRenderer[] trailRenderes = instance.gameObject.GetComponentsInChildren<TrailRenderer>();
			
		foreach (TrailRenderer t in trailRenderes )
		{
			if (_currentLineMode == 0) { // all off
				t.enabled = false;
			} 

			if (_currentLineMode == 1) { // fingers only
				if (t.name == "Robot_RightHand" || 
				    t.name == "Robot_LeftHand" || 
				    t.name == "Robot_RightFoot" || 
				    t.name == "Robot_LeftFoot") {
					t.enabled = false; 
				} else {
					t.enabled = true;
				}
			}

			if (_currentLineMode == 2) { // hands and feet only 
				if (t.name == "Robot_RightHand" || 
				    t.name == "Robot_LeftHand" || 
				    t.name == "Robot_RightFoot" || 
				    t.name == "Robot_LeftFoot") {
					t.enabled = true; 
				} else {
					t.enabled = false;
				}
			}
		}
	}

	private void ToggleViewMode()
	{
		if (_currentViewMode == 1) {
			MainCamera.rect = new Rect(0,0,1.0f,1.0f);
			foreach (Camera c in SideCameras) {
				c.enabled = false;
			}
			_currentViewMode = 0;

		} else if (_currentViewMode == 0) {
			MainCamera.rect = new Rect(0,0,0.6f,1.0f);
			foreach (Camera c in SideCameras) {
				c.enabled = true;
			}
			_currentViewMode = 1;
		}
		
		SetCameraTargets( currentInstanceIndex );
	}
}
