using Momo.Core;
using UnityEngine;

namespace Game.Enemies
{
	class EnemyController : CharacterController
	{
		[SerializeField] private float _speed = 0.2f;
		[SerializeField] private float _permittedDropHeight = 0.0f;

		private float FacingFactor => _transform.localScale.x;


		#region Unity Callbacks


		private void Update()
		{
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
			float castLength = halfHeight + _permittedDropHeight;

			RaycastHit2D hit = Physics2D.Raycast(
				castOrigin,
				Vector2.down,
				castLength);

			bool hitOccured = hit;

#if UNITY_EDITOR
			Debug.DrawLine(castOrigin, castOrigin + (Vector2.down * castLength), hitOccured ? Color.green : Color.red);
#endif

			return !hitOccured;
		}

		#endregion
	}
}
