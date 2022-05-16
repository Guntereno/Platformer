using Momo.Core;
using UnityEngine;

namespace Game.Enemies
{
	class EnemyController : CharacterController
	{
		[SerializeField] private Animator _animator = null;

		[SerializeField] private float _speed = 0.2f;
		[SerializeField] private float _permittedDropHeight = 0.0f;

		private int _animIsWalkingId;

		private float FacingFactor => _transform.localScale.x;


		#region Unity Callbacks

		protected override void Start()
		{
			base.Start();

			_animIsWalkingId = Animator.StringToHash("IsWalking");
		}

		private void Update()
		{
			_animator.SetBool(_animIsWalkingId, true);

			bool shouldTurn = CheckForTurn();
			if (shouldTurn)
			{
				_transform.localScale = _transform.localScale.WithX(-1.0f * _transform.localScale.x);
			}
			_rigidBody.velocity = new Vector2(_speed * FacingFactor, 0.0f);
		}

		private bool CheckForTurn()
		{
			Vector2 castOrigin = GetEdgePoint(new Vector2(FacingFactor, 0.0f));
			float halfHeight = _bodyCollider.bounds.size.y * 0.5f;

			RaycastHit2D hit = Physics2D.Raycast(
				castOrigin,
				Vector2.down,
				_permittedDropHeight);

			bool hitOccured = hit;

#if UNITY_EDITOR
			Debug.DrawLine(castOrigin, castOrigin + (Vector2.down * _permittedDropHeight), hitOccured ? Color.green : Color.red);
#endif

			return !hitOccured;
		}

		#endregion
	}
}
