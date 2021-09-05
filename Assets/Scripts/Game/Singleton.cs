using System;
using UnityEngine;

namespace Game
{
	public class Singleton : MonoBehaviour
	{
		[SerializeField]
		private CameraController _cameraController;

		private static Singleton _instance;


		public static Singleton Instance => _instance;
		public CameraController CameraController => _cameraController;

		void Start()
		{
			if (_instance != null)
			{
				throw new Exception("There can only be one instance of Singleton!");
			}
			_instance = this;
		}
	}
}
