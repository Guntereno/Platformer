using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
	[CustomEditor(typeof(PlayerController))]
	public class PlayerControllerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
	}
}

