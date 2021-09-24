using System.Collections;
using UnityEngine;

namespace Game.Guns
{
	[RequireComponent(typeof(Rigidbody2D))]
	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(Collider2D))]
	public class Projectile : MonoBehaviour
	{
		private Transform _transform;
		private Rigidbody2D _rigidbody2D;
		private Animator _animator;
		private Collider2D _collider;

		private float _spawnTime = float.MinValue;
		private float _lifeSpan = float.MinValue;
		private bool _hasCollided = false;

		private int _animCollided;

		private float LifeTime => Time.time - _spawnTime;

		enum State
		{
			Flying,
			Impact
		}

		void Awake()
		{
			_transform = transform;
			
			_rigidbody2D = GetComponent<Rigidbody2D>();
			_animator = GetComponent<Animator>();
			_collider = GetComponent<Collider2D>();

			_animCollided = Animator.StringToHash("Collided");
		}

		private void Update()
		{
			if (!_hasCollided && (LifeTime > _lifeSpan))
			{
				gameObject.SetActive(false);
			}

			_animator.SetBool(_animCollided, _hasCollided);
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			OnCollision();
		}

		private bool HasCollided
		{
			get => _hasCollided;
			set
			{
				_hasCollided = value;
				_rigidbody2D.isKinematic = value;
				_collider.enabled = !value;

				if(value)
				{
					_rigidbody2D.velocity = Vector2.zero;
				}
			}
		}

		public void Spawn(Vector2 velocity, float lifespan)
		{
			_rigidbody2D.isKinematic = false;
			_rigidbody2D.velocity = velocity;
			_lifeSpan = lifespan;

			Vector2 direction = velocity.normalized;
			float theta = Mathf.Atan2(direction.y, direction.x);
			_transform.rotation = Quaternion.AngleAxis(theta * Mathf.Rad2Deg, Vector3.forward);
			
			HasCollided = false;
			_spawnTime = Time.time;
		}

		private void OnCollision()
		{
			HasCollided = true;
		}

		void OnImpactFinished()
		{
			gameObject.SetActive(false);
		}
	}
}

