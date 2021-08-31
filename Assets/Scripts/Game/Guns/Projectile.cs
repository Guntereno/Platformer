using UnityEngine;

namespace Game.Guns
{
	[RequireComponent(typeof(Rigidbody2D))]
	public class Projectile : MonoBehaviour
	{
		private Transform _transform;
		private Rigidbody2D _rigidbody2D;

		private float _spawnTime = float.MinValue;
		private float _lifeSpan = float.MinValue;

		private float LifeTime => Time.time - _spawnTime;


		void Awake()
		{
			_transform = transform;
			_rigidbody2D = GetComponent<Rigidbody2D>();
		}

		void OnEnable()
		{
			_spawnTime = Time.time;
		}

		void Update()
		{
			if (LifeTime > _lifeSpan)
			{
				gameObject.SetActive(false);
			}
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			gameObject.SetActive(false);
		}


		public void Spawn(Vector2 velocity, float lifespan)
		{
			_rigidbody2D.velocity = velocity;
			_lifeSpan = lifespan;

			Vector2 direction = velocity.normalized;
			float theta = Mathf.Atan2(direction.y, direction.x);
			_transform.rotation = Quaternion.AngleAxis(theta * Mathf.Rad2Deg, Vector3.forward);
		}
	}
}

