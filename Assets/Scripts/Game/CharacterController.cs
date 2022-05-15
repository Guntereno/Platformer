using System;
using UnityEngine;

namespace Game
{
	[RequireComponent(typeof(CapsuleCollider2D)), RequireComponent(typeof(Rigidbody2D))]
	public class CharacterController : MonoBehaviour
	{
		protected struct BoundingCircle
		{
			public Vector2 Origin;
			public float Radius;
		}

		protected struct BoundingBox
		{
			public Vector2 BoxSize;
			public Vector2 Origin;
		}

		protected Transform _transform = null;
		protected CapsuleCollider2D _bodyCollider = null;
		protected Rigidbody2D _rigidBody = null;

		// Body box is the oblong part of the capsule (without the caps)
		private float _bodyBoxWidth;
		private float _bodyBoxHeight;

		public Vector2 BodyBoxSize => new Vector2(_bodyBoxWidth, _bodyBoxHeight);
		public Vector2 Origin => (Vector2)_transform.position;

		#region Unity Callbacks

		protected virtual void Start()
		{
			_transform = transform;
			_bodyCollider = GetComponent<CapsuleCollider2D>();
			_rigidBody = GetComponent<Rigidbody2D>();

			if (_bodyCollider.size.x >= _bodyCollider.size.y)
			{
				throw new Exception("Only capsules which are taller than they are wide are currently supported!");
			}

			_bodyBoxWidth = _bodyCollider.size.x;
			_bodyBoxHeight = (_bodyCollider.size.y - _bodyCollider.size.x);
		}

		#endregion


		#region Helpers

		protected BoundingBox BuildBodyBox()
		{
			BoundingBox result;
			result.BoxSize = BodyBoxSize;
			result.Origin = Origin;

			return result;
		}

		protected BoundingCircle BuildBoundingCircle()
		{
			BoundingCircle result;

			result.Origin = _bodyCollider.offset;
			result.Origin.y -= (_bodyCollider.size.y - _bodyCollider.size.x) * 0.5f;
			result.Radius = _bodyCollider.size.x * 0.5f;

			return result;
		}

		protected Vector2 GetEdgePoint(Vector2 dir)
		{
			return
				Origin +
				new Vector2(
					dir.x * (_bodyCollider.size.x * 0.5f),
					dir.y * (_bodyCollider.size.y * 0.5f));
		}

		#endregion
	}
}