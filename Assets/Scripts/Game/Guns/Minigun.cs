using UnityEngine;

namespace Game.Guns
{
	class Minigun: Gun
	{
		[SerializeField] private float _spinAccelleration;
		[SerializeField] private AudioSource _spinAudio;
		[SerializeField] private AnimationCurve _volumeCurve;
		[SerializeField] private AnimationCurve _pitchCurve;

		private float _spinRatio = 0.0f;
		private bool _spinningUp = false;

		private void Update()
		{
			float accelleration = _spinningUp ? _spinAccelleration : -_spinAccelleration;
			_spinRatio = Mathf.Clamp01(_spinRatio + (accelleration * Time.deltaTime));

			_spinAudio.volume = _volumeCurve.Evaluate(_spinRatio);
			_spinAudio.pitch = _pitchCurve.Evaluate(_spinRatio);
		}

		public override bool OnFire(bool fireHeld, out Vector2 recoil)
		{
			_spinningUp = fireHeld;

			if(_spinRatio >= 1.0f)
			{
				return base.OnFire(fireHeld, out recoil);
			}

			recoil = default;
			return false;
		}
	}
}
