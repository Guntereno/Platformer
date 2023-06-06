using Momo.Core;
using Momo.Core.Geometry;
using UnityEngine;

namespace Game.Enemies
{
	class EnemyController : CharacterController
	{
		[SerializeField] private Animator _animator = null;

		[Min(0.0f)]
		[SerializeField] private float _acceleration = 1.0f;

		[Min(0.0f)]
		[SerializeField] private float _permittedDropHeight = 0.0f;

		[Min(0.0f)]
		[SerializeField] private float _groundFriction = 0.1f;

		[Min(0.0f)]
		[SerializeField] private float _initialHealth = 1.0f;

		[Min(0.0f)]
		[SerializeField] private float _deathBounceForce = 1.0f;

		[SerializeField] private Collider2D _corpseCollider = null;


		private int _animWalkSpeedId;
		private int _animIsOnGroundId;
		private int _animIsDead;

		private float _currentHealth = 1.0f;

		// Mask for items which cause enemy to turn around
		private int _turnAroundMask;

		private bool FacingRight => _transform.localScale.x > 0.0f;
		private float FacingSign => Mathf.Sign(_transform.localScale.x);
		private Vector2 Forward => new Vector2(FacingSign, 0.0f);

		private bool IsDead
		{
			get => _currentHealth <= 0.0f;
		}


		#region Unity Callbacks

		protected override void Start()
		{
			base.Start();

			_animWalkSpeedId = Animator.StringToHash("WalkSpeed");
			_animIsOnGroundId = Animator.StringToHash("IsOnGround");
			_animIsDead = Animator.StringToHash("IsDead");

			_painLayerMask = LayerMask.GetMask("PlayerProjectiles");
			_turnAroundMask = LayerMask.GetMask("Enemies", "Props", "Ground");

			Init();
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();

			FixedUpdateVelocity();
		}

		protected override void Update()
		{
			base.Update();

			UpdateAnimation();
		}

		#endregion


		#region Helpers

		private void Init()
		{
			_currentHealth = _initialHealth;


			_bodyCollider.enabled = true;
			if (_corpseCollider != null)
			{
				_corpseCollider.enabled = false;
			}
		}

		private void FixedUpdateVelocity()
		{
			Vector2 velocity = _rigidBody.velocity;

			if (!IsDead)
			{
				bool isWalking;
				Box bodyBox = BuildBodyBox();
				if (IsOnGround)
				{
					isWalking = CanWalkInDirection(bodyBox, FacingRight);
					if (!isWalking)
					{
						isWalking = CanWalkInDirection(bodyBox, !FacingRight);
						if (isWalking)
						{
							TurnAround();
						}
					}

					if (isWalking)
					{
						Vector2 accelleration = Vector2.zero.WithX(_acceleration * FacingSign);
						accelleration = ClampIntoWall(CurrentContactFlags, accelleration);
						velocity += accelleration * Time.fixedDeltaTime;
					}
				}
			}

			velocity = ApplyFriction(velocity, _groundFriction, 0.1f);

			velocity = ClampToMaxSpeed(velocity);
			_rigidBody.velocity = velocity;
		}

		private void UpdateAnimation()
		{
			_animator.SetFloat(_animWalkSpeedId, Mathf.Abs(_rigidBody.velocity.x));
			_animator.SetBool(_animIsOnGroundId, IsOnGround);
			_animator.SetBool(_animIsDead, IsDead);
		}

		private void TurnAround()
		{
			_transform.localScale = _transform.localScale.WithX(-1.0f * _transform.localScale.x);
		}

		private bool CanWalkInDirection(Box bodyBox, bool toRight)
		{
			bool hasGround = CheckGround(bodyBox, toRight);
			if (hasGround)
			{
				RaycastHit2D hit = ContactWallCheck(toRight ? Vector2.right : Vector2.left, _turnAroundMask);
				if (!hit)
				{
					return true;
				}
			}

			return false;
		}

		private bool CheckGround(Box bodyBox, bool toRight)
		{
			Vector2 castOrigin = new Vector2
			{
				x = toRight ? bodyBox.Right : bodyBox.Left,
				y = bodyBox.Bottom
			};

			float castLength = CapHeight + _permittedDropHeight;
			RaycastHit2D hit = Physics2D.Raycast(
				castOrigin,
				Vector2.down,
				castLength,
				GroundMask);

			bool hitOccured = hit;

#if UNITY_EDITOR
			Debug.DrawLine(castOrigin, castOrigin + (Vector2.down * castLength), hitOccured ? Color.green : Color.red);
#endif

			return hitOccured;
		}

		protected override void OnHurt(Collision2D collision)
		{
			base.OnHurt(collision);

			if (IsDead)
				return;

			Hurt(1.0f);

			if (IsDead)
			{
				Vector2 direction = transform.position - collision.transform.position;
				direction = direction.normalized * _deathBounceForce;
				direction += Vector2.up * _deathBounceForce;
				_rigidBody.AddForce(direction * _deathBounceForce, ForceMode2D.Impulse);
			}

		}

		private void Hurt(float damage)
		{
			_currentHealth -= damage;
			if (_currentHealth <= 0.0f)
			{
				_currentHealth = 0.0f;

				gameObject.layer = LayerMask.NameToLayer("Corpses");
				
				if(_corpseCollider != null)
				{
					_corpseCollider.enabled = true;
					_bodyCollider.enabled = false;
				}
			}
		}

		#endregion
	}
}
