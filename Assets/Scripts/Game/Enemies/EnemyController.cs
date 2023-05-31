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


		private int _animWalkSpeedId;
		private int _animIsOnGroundId;
		
		// Mask for items which cause enemy to turn around
		private int _turnAroundMask;

		private bool FacingRight => _transform.localScale.x > 0.0f;
		private float FacingSign => Mathf.Sign(_transform.localScale.x);
		private Vector2 Forward => new Vector2(FacingSign, 0.0f);

		#region Unity Callbacks

		protected override void Start()
		{
			base.Start();

			_animWalkSpeedId = Animator.StringToHash("WalkSpeed");
			_animIsOnGroundId = Animator.StringToHash("IsOnGround");

			_turnAroundMask = LayerMask.GetMask("Enemies", "Props", "Ground");
		}

		protected override void Update()
		{
			base.Update();
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();

			FixedUpdateVelocity();
		}

		#endregion


		#region Helpers

		private void FixedUpdateVelocity()
		{
			Vector2 velocity = _rigidBody.velocity;

			bool isWalking = false;
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

				velocity = ApplyFriction(velocity, _groundFriction, 0.1f);

				if (isWalking)
				{
					Vector2 accelleration = Vector2.zero.WithX(_acceleration * FacingSign);
					accelleration = ClampIntoWall(CurrentContactFlags, accelleration);
					velocity += accelleration * Time.fixedDeltaTime;
				}
			}

			velocity = ClampToMaxSpeed(velocity);

			_rigidBody.velocity = velocity;

			_animator.SetFloat(_animWalkSpeedId, Mathf.Abs(velocity.x));
			_animator.SetBool(_animIsOnGroundId, IsOnGround);
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

		#endregion
	}
}
