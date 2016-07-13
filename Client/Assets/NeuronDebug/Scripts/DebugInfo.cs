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
using Neuron;

public class DebugInfo : MonoBehaviour
{
	//bool							dirty = false;
	
	public UnityEngine.UI.Text 		infoText = null;
	public NeuronAnimatorInstance	currentInstance = null;
	
	void Awake()
	{
		if( infoText == null )
		{
			infoText = GetComponent<UnityEngine.UI.Text>();
		}
	}

	void Update()
	{
		// update every 60 frames
		if( infoText && Time.frameCount % 60 == 0 )
		{
			GenerateNeuronMasterInfo( currentInstance );
		}
	}
	
	string GenerateNeuronMasterInfo( NeuronAnimatorInstance instance )
	{
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
			line += string.Format( "Total connections: {0}\n", NeuronConnection.numOfSources );
			line += string.Format( "Total number of Actors: {0}\n", actor.owner.numOfActiveActors );
			line += string.Format( "Total number of Actors: {0}\n", actor.owner.numOfSuspendedActors );
		}
		return line;
	}
}
