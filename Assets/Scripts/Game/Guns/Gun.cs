using UnityEngine;
using Core.Audio;

namespace Game.Guns
{
	[RequireComponent(typeof(AudioSource))]
	[RequireComponent(typeof(RandomAudioClipPool))]
	class Gun : Weapon
	{
		[SerializeField] private float _cooldownTime = 0.0f;
		[SerializeField] private Projectile _projectilePrefab = null;
		[SerializeField] private int _maxLiveProjectiles = 0;
		[SerializeField] private Transform _muzzle = null;
		[SerializeField] private float _projectileLifeSpan = 1.0f;
		[SerializeField] private float _projectileSpeed = 1.0f;
		[SerializeField] private float _recoilFactor = 0.1f;

		private Projectile[] _projectilePool = null;
		private float _lastFired = float.MinValue;

		private AudioSource _audioSource = null;
		private RandomAudioClipPool _audioPool = null;


		private bool OnCooldown => (Time.time - _lastFired) < _cooldownTime;

		private void Awake()
		{
			_audioSource = GetComponent<AudioSource>();
			_audioPool = GetComponent<RandomAudioClipPool>();

			_projectilePool = new Projectile[_maxLiveProjectiles];
			for(int i=0; i<_maxLiveProjectiles; ++i)
			{
				_projectilePool[i] = GameObject.Instantiate(_projectilePrefab);
				_projectilePool[i].gameObject.SetActive(false);
			}
		}



		public override void OnFire(bool firePressed, out Vector2 recoil)
		{
			if(firePressed && !OnCooldown)
			{
				Fire(out recoil);
				return;
			}

			recoil = Vector2.zero;
		}

		private void Fire(out Vector2 recoil)
		{
			Projectile projectile = GetNextAvailableProjectile();
			if(projectile == null)
			{
				recoil = Vector2.zero;
				return;
			}

			projectile.transform.position = _muzzle.position;
			projectile.gameObject.SetActive(true);
			Vector2 velocity = 
				_muzzle.TransformDirection(_muzzle.right) * _projectileSpeed;
			projectile.Spawn(velocity, _projectileLifeSpan);

			recoil = -velocity * _recoilFactor;

			Singleton.Instance.CameraController.ShotFired(velocity);

			_lastFired = Time.time;

			if(_audioSource != null)
			{
				_audioSource.clip = _audioPool.Next();
				_audioSource.Play();
			}
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
