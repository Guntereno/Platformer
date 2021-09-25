using UnityEngine;
using Core.Audio;
using System.Collections;
using Momo.Core;

namespace Game.Guns
{
	[RequireComponent(typeof(AudioSource))]
	class Gun : Weapon
	{
		[SerializeField] private Transform _gunTransform = null;
		[SerializeField] private GameObject _muzzleFlash = null;
		[SerializeField] private float _cooldownTime = 0.0f;
		[SerializeField] private float _spreadRadians = 0.0f;
		[SerializeField] private Projectile _projectilePrefab = null;
		[SerializeField] private int _maxLiveProjectiles = 0;
		[SerializeField] private Transform _muzzle = null;
		[SerializeField] private float _projectileLifeSpan = 1.0f;
		[SerializeField] private float _projectileSpeed = 1.0f;
		[SerializeField] private float _recoilFactor = 0.1f;
		[SerializeField] private float _spriteRecoilDistance = 0.1f;
		[SerializeField] private float _spriteRecoilTime = 0.1f;
		[SerializeField] private RandomAudioClipPool _audioPool = null;

		private Projectile[] _projectilePool = null;
		private float _lastFired = float.MinValue;

		private AudioSource _audioSource = null;
		private Coroutine _recoilCoroutine;

		private bool OnCooldown => (Time.time - _lastFired) < _cooldownTime;

		private void Awake()
		{
			if(_audioPool != null)
			{
				_audioPool.Init();
			}

			_audioSource = GetComponent<AudioSource>();

			_projectilePool = new Projectile[_maxLiveProjectiles];
			for(int i=0; i<_maxLiveProjectiles; ++i)
			{
				_projectilePool[i] = GameObject.Instantiate(_projectilePrefab);
				_projectilePool[i].gameObject.SetActive(false);
			}

			_muzzleFlash.SetActive(false);
		}

		private IEnumerator RecoilCoroutine()
		{
			_muzzleFlash.SetActive(true);
			int muzzleFrames = 4;

			float recoilTime;
			do
			{
				recoilTime = Mathf.Clamp(Time.time - _lastFired, 0.0f, _spriteRecoilTime);

				float distance =
					(1.0f - (recoilTime / _spriteRecoilTime))
					* _spriteRecoilDistance;
				_gunTransform.transform.localPosition = Vector2.left * distance;
				yield return 0;

				_muzzleFlash.SetActive(--muzzleFrames > 0);
			}
			while(recoilTime < _spriteRecoilTime);

			_recoilCoroutine = null;
		}


		public override bool OnFire(bool fireHeld, out Vector2 recoil)
		{
			recoil = Vector2.zero;
			if (!fireHeld || OnCooldown)
			{
				return false;
			}

			Projectile projectile = GetNextAvailableProjectile();
			if (projectile == null)
			{
				return false;
			}

			projectile.transform.position = _muzzle.position;
			projectile.gameObject.SetActive(true);

			Vector2 direction = _muzzle.TransformDirection(_muzzle.right);

			float spread = Random.Range(-_spreadRadians, _spreadRadians);
			direction = direction.Rotate(spread);
			
			Vector2 velocity = direction * _projectileSpeed;
			projectile.Spawn(velocity, _projectileLifeSpan);

			recoil = -velocity * _recoilFactor;

			Singleton.Instance.CameraController.ShotFired(velocity);

			_lastFired = Time.time;
			_gunTransform.localPosition = recoil;

			if ((_audioSource != null) && (_audioPool != null))
			{
				_audioSource.clip = _audioPool.Next();
				_audioSource.Play();
			}

			if(_recoilCoroutine != null)
			{
				StopCoroutine(_recoilCoroutine);
			}
			_recoilCoroutine = StartCoroutine(RecoilCoroutine());

			return true;
		}

		private Projectile GetNextAvailableProjectile()
		{
			for(int i=0; i<_maxLiveProjectiles; ++i)
			{
				if(!_projectilePool[i].gameObject.activeInHierarchy)
				{
					return _projectilePool[i];
				}
			}

			return null;
		}
	}
}
