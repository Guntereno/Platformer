using Momo.Core.Audio;
using System;
using UnityEngine;

namespace Game
{
	public class Singleton : MonoBehaviour
	{
		[SerializeField]
		private CameraController _cameraController = null;
		[SerializeField]
		private LevelController _levelController = null;
		[SerializeField]
		private AudioPools _audioPools = null;

		private static Singleton _instance;

		public static Singleton Instance => _instance;
		public CameraController CameraController => _cameraController;
		public LevelController LevelController => _levelController;
		public AudioPools AudioPools => _audioPools;


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
