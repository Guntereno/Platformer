using System;
using UnityEngine;
using Momo.Core.Geometry;

namespace Game
{
	[RequireComponent(typeof(CapsuleCollider2D)), RequireComponent(typeof(Rigidbody2D))]
	public class CharacterController : MonoBehaviour
	{
		protected Transform _transform = null;
		protected CapsuleCollider2D _bodyCollider = null;
		protected Rigidbody2D _rigidBody = null;

		// Body box is the oblong part of the capsule (without the caps)
		public Vector2 BodyBoxSize => _bodyBoxSize;
		public float CapHeight => _capHeight;
		public Vector2 Origin => _bodyCollider.offset + (Vector2)_transform.position;
		
		private Vector2 _bodyBoxSize;
		private float _capHeight;

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

			_bodyBoxSize.x = _bodyCollider.size.x;
			_bodyBoxSize.y = (_bodyCollider.size.y - _bodyCollider.size.x);
			_capHeight = (_bodyCollider.size.y - _bodyBoxSize.y) * 0.5f;
		}

		#endregion


		#region Helpers

		protected Box BuildBodyBox()
		{
			return new Box
			{
				Size = _bodyBoxSize,
				Origin = Origin
			};
		}

		protected Circle BuildBoundingCircle()
		{
			Circle result;

			result.Origin = _bodyCollider.offset;
			result.Origin.y -= (_bodyCollider.size.y - _bodyCollider.size.x) * 0.5f;
			result.Radius = _bodyCollider.size.x * 0.5f;

			return result;
		}

		#endregion
	}
}