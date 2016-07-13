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
using System.Collections.Generic;

public class DebugLogRedirector : MonoBehaviour {
		
	Queue<string>				lines = new Queue<string>();
	bool						dirty = false;
	
	public UnityEngine.UI.Text 	redirectToTarget = null;		// Target VisualComponent which recevied Unity.Debug logs
	public int 					maxNumOfLines = 20;				// Max lines
	public Color 				messageColor = Color.white;		// Debug.Log() color
	public Color 				warningColor = Color.yellow;	// Debug.LogWarning() color
	public Color 				errorColor = Color.red;			// Debug.LogError() color
	public Color 				exceptionColor = Color.red;		// Debug.LogException() color

	void Awake()
	{
		// if not set target 
		if( redirectToTarget == null )
		{
			redirectToTarget = GetComponent<UnityEngine.UI.Text>();
		}

#if UNITY_4_6
		Application.RegisterLogCallback( OnUnityLogCallback );
#elif UNITY_5_0
		Application.logMessageReceived += OnUnityLogCallback;
#endif

	}
	
	string Color2Hex( Color color )
	{
		string hex_string = Mathf.FloorToInt( color.r * 255 ).ToString( "X2" );
		hex_string += Mathf.FloorToInt( color.g * 255 ).ToString( "X2" );
		hex_string += Mathf.FloorToInt( color.b * 255 ).ToString( "X2" );
		hex_string += Mathf.FloorToInt( color.a * 255 ).ToString( "X2" );
		return hex_string;
	}
	
	string LogToRichText( string text, LogType type )
	{
		// prefix
		string rich_text = "<color=#";
		switch( type )
		{
			case LogType.Log:
			{
				rich_text += Color2Hex( messageColor );
			}
			break;
			case LogType.Warning:
			{
				rich_text += Color2Hex( warningColor );
			}
			break;
			case LogType.Error:
			{
				rich_text += Color2Hex( errorColor );
			}
			break;
			case LogType.Exception:
			{
				rich_text += Color2Hex( exceptionColor );
			}
			break;
		}
		rich_text += '>';
		
		// add text
		rich_text += text;
		if( rich_text[rich_text.Length-1] == '\n' )
		{
			rich_text.Remove( rich_text.Length-1 );
		}
		
		// suffix
		rich_text += "</color>\n";
		
		return rich_text;
	}
	
	void OnUnityLogCallback( string text, string stackTrace, LogType type )
	{
		string line = LogToRichText( text, type );
		lines.Enqueue( line );
		if( lines.Count > maxNumOfLines )
		{
			lines.Dequeue();
		}
		dirty = true;
	}
	
	void ReBuildTargetContent()
	{
		if( redirectToTarget != null && dirty )
		{
			// rebuild content
			redirectToTarget.text = string.Empty;
			
			for( int i = 0; i < lines.Count; ++i )
			{
				string line = lines.Dequeue();
				redirectToTarget.text += line;
				lines.Enqueue( line );
			}
			
			dirty = false;
		}
	}
	
	void Update()
	{		
		ReBuildTargetContent();
	}
}
