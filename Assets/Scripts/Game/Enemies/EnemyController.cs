using Momo.Core;
using Momo.Core.Geometry;
using UnityEngine;

namespace Game.Enemies
{
	class EnemyController : CharacterController
	{
		[SerializeField] private Animator _animator = null;

		[Min(0.0f)]
		[SerializeField] private float _speed = 0.2f;

		[Min(0.0f)]
		[SerializeField] private float _permittedDropHeight = 0.0f;

		private int _animWalkSpeedId;
		private int _animIsOnGroundId;
		
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

			_turnAroundMask = LayerMask.GetMask("Enemies", "Props");
		}

		protected override void Update()
		{
			Box bodyBox = BuildBodyBox();
			Momo.Core.DebugDraw.Box(bodyBox, Color.magenta);

			bool isWalking = false;

			// Get speed _before_ we override it, so we can know if we've stopped
			float walkSpeed = Mathf.Abs(_rigidBody.velocity.x);

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

				float speed = isWalking ? _speed * FacingSign : 0.0f;
				_rigidBody.velocity = new Vector2(speed, 0.0f);
			}

			_animator.SetFloat(_animWalkSpeedId, walkSpeed);
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
