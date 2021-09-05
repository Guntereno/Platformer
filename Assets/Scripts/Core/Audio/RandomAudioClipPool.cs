using UnityEngine;

using Random = Core.Random;

namespace Core.Audio
{
	public class RandomAudioClipPool: MonoBehaviour
	{
		[SerializeField] private AudioClip[] _audioClips = null;

		private System.Random _random = new System.Random();

		private int[] _order = null;
		private int[] _selectionPool = null;

		private int _currentIndex = 0;

		private void Start()
		{
			int numClips = _audioClips.Length;

			_order = new int[numClips];
			_selectionPool = new int[numClips];
			
			for (int i = 0; i < numClips; ++i)
			{
				_order[i] = i;
			}
		}

		public AudioClip Next()
		{
			if(_audioClips.Length == 0)
			{
				return null;
			}
			if(_audioClips.Length == 1)
			{
				return _audioClips[0];
			}

			if (_currentIndex >= _audioClips.Length)
			{
				Shuffle();
			}

			return _audioClips[_order[_currentIndex++]];
		}

		private void Shuffle()
		{
			_currentIndex = 0;

			if (_audioClips.Length == 2)
			{
				// There's no point in shuffling
				return;
			}

			Random.Shuffle<int>(_random, _order, _selectionPool);
		}
	}
}
