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

		private int _animIsWalkingId;
		private LayerMask _groundMask;

		private bool FacingRight => _transform.localScale.x > 0.0f;
		private float FacingSign => Mathf.Sign(_transform.localScale.x);

		#region Unity Callbacks

		protected override void Start()
		{
			base.Start();

			_animIsWalkingId = Animator.StringToHash("IsWalking");

			_groundMask = LayerMask.GetMask("Ground");
		}

		private void Update()
		{
			_animator.SetBool(_animIsWalkingId, true);

			bool shouldTurn = CheckForTurn();
			if (shouldTurn)
			{
				_transform.localScale = _transform.localScale.WithX(-1.0f * _transform.localScale.x);
			}
			_rigidBody.velocity = new Vector2(_speed * FacingSign, 0.0f);
		}

		private bool CheckForTurn()
		{
			Box bodyBox = BuildBodyBox();
			DebugDraw.Box(bodyBox, Color.magenta);

			Vector2 castOrigin = new Vector2
			{
				x = FacingRight ? bodyBox.Right : bodyBox.Left,
				y = bodyBox.Bottom
			};

			float castLength = CapHeight + _permittedDropHeight;
			RaycastHit2D hit = Physics2D.Raycast(
				castOrigin,
				Vector2.down,
				castLength,
				_groundMask);

			bool hitOccured = hit;

#if UNITY_EDITOR
			Debug.DrawLine(castOrigin, castOrigin + (Vector2.down * castLength), hitOccured ? Color.green : Color.red);
#endif

			return !hitOccured;
		}

		#endregion
	}
}
