using UnityEngine;

namespace Game.Guns
{
	class Minigun: Gun
	{
		[SerializeField] private AudioSource _spinAudio = null;
		[SerializeField] private SpriteRenderer _gunSprite = null;
		[SerializeField] private float _spinAccelleration = 1.0f;
		[SerializeField] private AnimationCurve _volumeCurve = null;
		[SerializeField] private AnimationCurve _pitchCurve = null;
		[SerializeField] private Sprite[] _animSprites = null;
		[SerializeField] private float _animSpeed = 1.0f;


		private float _spinRatio = 0.0f;
		private bool _spinningUp = false;
		private float _animPos = 0.0f;

		private void Update()
		{
			float accelleration = _spinningUp ? _spinAccelleration : -_spinAccelleration;
			_spinRatio = Mathf.Clamp01(_spinRatio + (accelleration * Time.deltaTime));

			_spinAudio.volume = _volumeCurve.Evaluate(_spinRatio);
			_spinAudio.pitch = _pitchCurve.Evaluate(_spinRatio);

			_animPos += _spinRatio * _animSpeed;
			_animPos = _animPos % (float)_animSprites.Length;
			_gunSprite.sprite = _animSprites[(int)_animPos];
		}

		public void OnEnable()
		{
			_spinRatio = 0.0f;
			_spinningUp = false;
			_animPos = 0.0f;
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
