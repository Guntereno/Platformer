//#define TRACK_GROUND_NORMALS

using System;
using UnityEngine;
using Momo.Core.Geometry;
using Momo.Core;
using UnityEditor;

namespace Game
{
	[RequireComponent(typeof(CapsuleCollider2D)), RequireComponent(typeof(Rigidbody2D))]
	public class CharacterController : MonoBehaviour
	{
		[Flags]
		protected enum ContactFlags
		{
			None = 0,

			Unknown = 1 << 0,
			OnGround = 1 << 1,
			OnRightWall = 1 << 2,
			OnLeftWall = 1 << 3,

			OnWall = OnRightWall | OnLeftWall
		}

		[Flags]
		private enum DrawDebugFlags
		{
			None = 0,

			GroundCheck = 1 << 0,
			RightWallCheck = 1 << 1,
			LeftWallCheck = 1 << 2
		}

		[Min(0.0f)]
		[SerializeField] private float _contactCheckDistance = 0.1f;

		[Min(0.0f)]
		[SerializeField] private float _maxSpeed = 1.0f;

		[SerializeField]
		private DrawDebugFlags _drawDebugFlags;


		protected Transform _transform = null;
		protected CapsuleCollider2D _bodyCollider = null;
		protected Rigidbody2D _rigidBody = null;


		public bool IsOnGround => _contactFlags.TestAny(ContactFlags.OnGround);


		protected float MaxSpeed => _maxSpeed;
		protected Vector2 BodyBoxSize => _bodyBoxSize; // Body box is the oblong part of the capsule (without the caps)
		protected float CapHeight => _capHeight;
		protected Vector2 Origin => _bodyCollider.offset + (Vector2)_transform.position;
		protected bool IsOnWall => _contactFlags.TestAny(ContactFlags.OnLeftWall | ContactFlags.OnRightWall);
		protected Flags32<ContactFlags> CurrentContactFlags => _contactFlags;
		protected int GroundMask => _groundMask;

		private int _groundAndPropsMask;
		private int _groundMask;

		private Vector2 _bodyBoxSize;
		private float _capHeight;
		private Flags32<ContactFlags> _contactFlags = ContactFlags.None;

		private Circle _boundingCircle;


#if TRACK_GROUND_NORMALS
		private Vector2 GroundDirectionLeft => Vector2.Perpendicular(_groundNormalLeft);
		private Vector2 GroundDirectionRight => -Vector2.Perpendicular(_groundNormalRight);

		private Vector2 _groundNormalLeft;
		private Vector2 _groundNormalRight;
#endif // TRACK_GROUND_NORMALS


		#region Unity Callbacks

		protected virtual void Start()
		{
			_groundAndPropsMask = LayerMask.GetMask("Ground", "Props");
			_groundMask = LayerMask.GetMask("Ground");

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

			_boundingCircle = BuildBoundingCircle();
		}

		protected virtual void FixedUpdate()
		{
			_contactFlags = CheckForContact();

#if TRACK_GROUND_NORMALS
			ResetGroundNormals();
#endif
		}

		protected virtual void Update()
		{
			// Do nothing
		}

		protected virtual void LateUpdate()
		{
			DebugDraw();
		}

		protected virtual void OnCollisionEnter2D(Collision2D collision)
		{
#if TRACK_GROUND_NORMALS
			HandleGroundContacts(collision);
#endif
		}

		protected virtual void OnCollisionStay2D(Collision2D collision)
		{
#if TRACK_GROUND_NORMALS
			HandleGroundContacts(collision);
#endif
		}


		protected virtual void OnGUI()
		{
			// Do nothing
		}

#endregion


#region Helpers

		protected virtual void DebugGui()
		{
			// Do nothing
		}

		protected virtual void DebugDraw()
		{
#if UNITY_EDITOR
#if TRACK_GROUND_NORMALS
			DebugDrawGroundNormals();
#endif
#endif
		}

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

		protected RaycastHit2D ContactWallCheck(Vector2 dir, int layerMask, bool debugRender = false)
		{
			Box bodyBox = BuildBodyBox();

			// Bring the box in by an offset, to ensure the box doesn't start in collision with another
			// body, thus preventing the cast from registering.
			const float offset = 0.2f;
			Vector2 overrideSize = new Vector2(
				bodyBox.Size.x - offset,
				bodyBox.Size.y);
			float overrideDistance = _contactCheckDistance + offset;

			RaycastHit2D hit = Cast.Box(
				bodyBox.Origin, overrideSize,
				angle: 0.0f,
				dir,
				distance: overrideDistance,
				layerMask: layerMask,
				debug: debugRender);

			return hit;
		}

		protected Vector2 ApplyFriction(Vector2 velocity, float groundFriction, float velocityEpsilon)
		{
			// Apply friction with ground. (Characters's physics material has zero
			// friction, so we must do this ourselves.)
			if (IsOnGround)
			{
				velocity += (-velocity * groundFriction) * Time.fixedDeltaTime;
				if (velocity.sqrMagnitude < (velocityEpsilon * velocityEpsilon))
				{
					velocity = Vector2.zero;
				}
			}

			return velocity;
		}
		protected static Vector2 ClampIntoWall(Flags32<ContactFlags> contactFlags, Vector2 force)
		{
			if (contactFlags.TestAny(ContactFlags.OnLeftWall) && (force.x < 0.0f))
			{
				force.x = 0.0f;
			}
			else if (contactFlags.TestAny(ContactFlags.OnRightWall) && (force.x > 0.0f))
			{
				force.x = 0.0f;
			}

			return force;
		}

		protected Vector2 ClampToMaxSpeed(Vector2 velocity)
		{
			velocity = new Vector2(
				Mathf.Clamp(velocity.x, -MaxSpeed, MaxSpeed),
				Mathf.Clamp(velocity.y, -MaxSpeed, MaxSpeed));
			return velocity;
		}


		bool CheckDebugDrawFlag(DrawDebugFlags flags)
		{
			return ((_drawDebugFlags & flags) == flags);
		}

		private Flags32<ContactFlags> CheckForContact()
		{
			Flags32<ContactFlags> result = ContactFlags.None;

			if (GroundCheck())
			{
				result.Set(ContactFlags.OnGround);
			}

			bool drawRightDebug = CheckDebugDrawFlag(DrawDebugFlags.RightWallCheck);
			RaycastHit2D rightHit = ContactWallCheck(Vector2.right, _groundMask, drawRightDebug);
			if (rightHit)
			{
				result.Set(ContactFlags.OnRightWall);
			}

			bool drawLeftDebug = CheckDebugDrawFlag(DrawDebugFlags.LeftWallCheck);
			RaycastHit2D leftHit = ContactWallCheck(Vector2.left, _groundMask, drawLeftDebug);
			if (leftHit)
			{
				result.Set(ContactFlags.OnLeftWall);
			}

			return result;
		}

		private RaycastHit2D GroundCheck()
		{
			RaycastHit2D hit = Physics2D.CircleCast(
				(Vector2)(_transform.position) + _boundingCircle.Origin,
				_boundingCircle.Radius,
				Vector2.down,
				_contactCheckDistance, _groundAndPropsMask);

#if UNITY_EDITOR
			if(CheckDebugDrawFlag(DrawDebugFlags.GroundCheck))
			{
				DebugDrawGroundCheck(_contactFlags.TestAll(ContactFlags.OnGround));
			}
#endif
			return hit;
		}

		private void DebugDrawGroundCheck(bool hitOccured)
		{
			float startTheta = Mathf.PI;
			Vector2 arcCenter = (Vector2)_transform.position
				+ _boundingCircle.Origin
				+ (Vector2.down * _contactCheckDistance);
			DrawDebug.Arc(
				arcCenter,
				startTheta,
				Mathf.PI,
				_boundingCircle.Radius,
				(hitOccured ? Color.green : Color.grey));
		}

#if TRACK_GROUND_NORMALS
		private void ResetGroundNormals()
		{
			_groundNormalLeft = _groundNormalRight = Vector2.up;
		}

		private void HandleGroundContacts(Collision2D collision)
		{
			foreach (ContactPoint2D contact in collision.contacts)
			{
				Debug.DrawRay(contact.point, contact.normal, Color.green);

				bool isGround = (contact.normal.y > 0.7f);
				if (isGround)
				{
					if (contact.normal.x > _groundNormalLeft.x)
					{
						_groundNormalLeft = contact.normal;
					}

					if (contact.normal.x < _groundNormalRight.x)
					{
						_groundNormalRight = contact.normal;
					}
				}
			}
		}

		private void DebugDrawGroundNormals()
		{
			Vector2 groundNormalStart = (Vector2)_transform.position
				+ _bodyCollider.offset
				- (_bodyCollider.size * 0.5f).WithX(0.0f);

			Debug.DrawLine(groundNormalStart, groundNormalStart + GroundDirectionLeft, Color.cyan);
			Debug.DrawLine(groundNormalStart, groundNormalStart + GroundDirectionRight, Color.cyan);
		}
#endif

#endregion
	}
}