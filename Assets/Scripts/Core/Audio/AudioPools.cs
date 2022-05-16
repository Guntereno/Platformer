using System.Collections.Generic;
using UnityEngine;

namespace Momo.Core.Audio
{
	public class AudioPools: MonoBehaviour
	{
		[SerializeField] private RandomAudioClipPool[] Pools = null;

		private Dictionary<string, RandomAudioClipPool> _poolLookup;

		void Awake()
		{
			_poolLookup = new Dictionary<string, RandomAudioClipPool>(Pools.Length);
			foreach (RandomAudioClipPool pool in Pools)
			{
				pool.Init();
				_poolLookup[pool.name] = pool;
			}
		}

		public AudioClip Next(string poolName)
		{
			return _poolLookup[poolName].Next();
		}
	}
}
