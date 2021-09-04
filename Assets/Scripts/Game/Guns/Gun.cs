using UnityEngine;

namespace Game.Guns
{
	class Gun : Weapon
	{
		[SerializeField] private float _cooldownTime = 0.0f;
		[SerializeField] private Projectile _projectilePrefab = null;
		[SerializeField] private int _maxLiveProjectiles = 0;
		[SerializeField] private Transform _muzzle = null;
		[SerializeField] private float _projectileLifeSpan = 1.0f;
		[SerializeField] private float _projectileSpeed = 1.0f;

		Projectile[] _projectilePool = null;
		private float _lastFired = float.MinValue;

		private bool OnCooldown => (Time.time - _lastFired) < _cooldownTime;


		private void Awake()
		{
			_projectilePool = new Projectile[_maxLiveProjectiles];
			for(int i=0; i<_maxLiveProjectiles; ++i)
			{
				_projectilePool[i] = GameObject.Instantiate(_projectilePrefab);
				_projectilePool[i].gameObject.SetActive(false);
			}
		}

		public override void OnFire(bool firePressed)
		{
			if(firePressed && !OnCooldown)
			{
				Fire();
			}
		}

		private void Fire()
		{
			Projectile projectile = GetNextAvailableProjectile();
			if(projectile == null)
			{
				return;
			}

			projectile.transform.position = _muzzle.position;
			projectile.gameObject.SetActive(true);
			Vector2 velocity = 
				_muzzle.TransformDirection(_muzzle.right) * _projectileSpeed;
			projectile.Spawn(velocity, _projectileLifeSpan);

			_lastFired = Time.time;
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
