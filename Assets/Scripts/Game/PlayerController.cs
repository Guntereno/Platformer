using Core;
using Game.Guns;
using Momo.Core;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{

	[Flags]
	enum PlayerCollisionFlags
	{
		None = 0,
		Left = 1 << 0,
		Right = 1 << 1
	}

	[RequireComponent(typeof(Animator), typeof(Collider), typeof(Rigidbody2D))]
	public class PlayerController : MonoBehaviour
	{
		[Flags]
		private enum ContactFlags
		{
			None = 0,

			Unknown = 1 << 0,
			RightWall = 1 << 1,
			Ground = 1 << 2,
			LeftWall = 1 << 3
		}

		[Flags]
		private enum InputFlags
		{
			None = 0,

			Jump = 1 << 0,
			Run = 1 << 1,
			Crouch = 1 << 2
		}

		[SerializeField] private Animator _animator = null;
		[SerializeField] private CapsuleCollider2D _collider = null;
		[SerializeField] private Rigidbody2D _rigidBody = null;

		[SerializeField] private float _deadzone = 0.2f;
		[SerializeField] private float _speed = 1.0f;
		[SerializeField] private float _runFactor = 1.5f;
		[SerializeField] private float _inertiaDecay = 0.1f;

		[SerializeField] private float _jumpImpulse = 4.0f;
		[SerializeField] private float _fallFactor = 0.0f;
		[SerializeField] private float _lowJumpFactor = 0.0f;
		[SerializeField] private float _coyoteTime = 0.0f;
		[SerializeField] private float _contectCheckDistance = 0.1f;

		[SerializeField] private Weapon _currentGun = null;

		private Vector2 _spawnPos = default;
		private Transform _transform = null;

		private float _capsuleBoxWidth;
		private float _capsuleBoxHeight;
		private float _capsuleCircleOffset;
		private float _capsuleCircleRadius;

		private int _animIsOnGroundId;
		private int _animSpeedX;
		private int _animVelocityYId;
		private int _animIsCrouchingId;

		private int _raycastMask;

		private Vector2 _moveVector = default;
		private Flags32<InputFlags> _inputFlags = InputFlags.None;

		private bool JumpHeld
		{
			get => _inputFlags.TestAll(InputFlags.Jump);
			set => _inputFlags.Assign(InputFlags.Jump, value);
		}

		private bool RunHeld
		{
			get => _inputFlags.TestAll(InputFlags.Run);
			set => _inputFlags.Assign(InputFlags.Run, value);
		}

		private bool CrouchHeld
		{
			get => _inputFlags.TestAll(InputFlags.Crouch);
			set => _inputFlags.Assign(InputFlags.Crouch, value);
		}

		private Flags32<ContactFlags> _contactFlags = ContactFlags.None;
		private float _lastTimeOnGround = float.MinValue;

		private readonly PlayerCollisionFlags CollisionFlags;

		public bool IsOnGround => _contactFlags.TestAny(ContactFlags.Ground);
		public bool OnWall => _contactFlags.TestAny(ContactFlags.LeftWall | ContactFlags.RightWall);
		public bool IsCrouching => IsOnGround && CrouchHeld;

		public Vector2 Position => _transform.position;

		#region Unity Callbacks

		private void Start()
		{
			_transform = transform;
			_rigidBody = GetComponent<Rigidbody2D>();

			if(_collider.size.x > _collider.size.y)
			{
				throw new Exception("Collider should be taller than it is wide!");
			}

			_capsuleBoxWidth = _collider.size.x;
			_capsuleBoxHeight = (_collider.size.y - _collider.size.x);
			_capsuleCircleOffset = _capsuleBoxHeight * 0.5f;
			_capsuleCircleRadius = _capsuleBoxWidth * 0.5f;

			_spawnPos = _transform.position;

			_animIsOnGroundId = Animator.StringToHash("IsOnGround");
			_animSpeedX = Animator.StringToHash("SpeedX");
			_animVelocityYId = Animator.StringToHash("VelocityY");
			_animIsCrouchingId = Animator.StringToHash("IsCrouching");

			_raycastMask = LayerMask.GetMask("Ground", "Props");
		}

		private void FixedUpdate()
		{
			_contactFlags = CheckForContact();

			UpdateFallDeath();
		}

		private void Update()
		{
			if (IsOnGround)
			{
				_lastTimeOnGround = Time.time;
			}

			UpdateMovement();
			UpdateJump();
			UpdateAnimation();
		}

		void OnGUI()
		{
#if UNITY_EDITOR
			GUI.Label(new Rect(25, 25, 180, 20), $"IsOnGround: {IsOnGround}");
			GUI.Label(new Rect(25, 45, 180, 20), $"Velocity: {_rigidBody.velocity}");
#endif
		}

		private Flags32<ContactFlags> CheckForContact()
		{
			Flags32<ContactFlags> result = ContactFlags.None;

			if(ContactCapCheck(Vector2.down))
			{
				result.Set(ContactFlags.Ground);
			}

			RaycastHit2D rightHit = ContactSideCheck(Vector2.right);
			if(rightHit)
			{
				result.Set(ContactFlags.RightWall);
			}

			RaycastHit2D leftHit = ContactSideCheck(Vector2.left);
			if (leftHit)
			{
				result.Set(ContactFlags.LeftWall);
			}

			return result;
		}

		private RaycastHit2D ContactCapCheck(Vector2 dir)
		{
			if (Mathf.Abs(dir.x) > 0.0f)
			{
				throw new Exception("Cap checks must be vertical!");
			}

			Vector2 origin = (Vector2)_transform.position + (dir * _capsuleCircleOffset);

			float radius = _capsuleCircleRadius;
			RaycastHit2D hit = Physics2D.CircleCast(origin, radius, Vector2.down, _contectCheckDistance, _raycastMask);

#if UNITY_EDITOR
			bool hitOccured = (hit.collider != null);
			Vector2 startDir = dir.PerpendicularR();
			float startTheta = Mathf.Atan2(startDir.y, startDir.x);
			Vector2 arcCenter = origin + (dir * _contectCheckDistance);
			DebugDraw.Arc(arcCenter, startTheta, Mathf.PI, radius, (hitOccured ? Color.green : Color.grey));
#endif

			return hit;
		}

		private RaycastHit2D ContactSideCheck(Vector2 dir)
		{
			if(Mathf.Abs(dir.y) > 0.0f)
			{
				throw new Exception("Side checks must be horizontal!");
			}

			Vector2 boxSize = new Vector2(_capsuleBoxWidth * 0.5f, _capsuleBoxHeight);
			Vector2 origin = (Vector2)_transform.position + (dir * (boxSize.x * 0.5f));

			RaycastHit2D hit = Physics2D.BoxCast(
				origin, boxSize, 
				angle:0.0f,
				dir,
				distance:_contectCheckDistance,
				layerMask:_raycastMask);

#if UNITY_EDITOR
			bool hitOccured = (hit.collider != null);
			Vector2 start = origin + (dir * ((boxSize.x * 0.5f) + _contectCheckDistance));
			start.y += boxSize.y * 0.5f;
			Vector2 end = start + (Vector2.down * boxSize.y);
			Debug.DrawLine(start, end, (hitOccured ? Color.green : Color.grey));
#endif

			return hit;
		}

		private void UpdateAnimation()
		{
			_animator.SetBool(_animIsOnGroundId, IsOnGround || IsInCoyoteTime);
			_animator.SetFloat(_animSpeedX, Mathf.Abs(_rigidBody.velocity.x) / (_speed * _runFactor));
			_animator.SetFloat(_animVelocityYId, _rigidBody.velocity.y);
			_animator.SetBool(_animIsCrouchingId, IsCrouching);

			// Miniscule movements, such as being pushed out of a contact, shouldn't make the
			// character change direction
			const float walkEpsilon = 0.5f;
			if (_rigidBody.velocity.x > walkEpsilon)
			{
				_transform.localScale = _transform.localScale.WithX(1.0f);
			}
			else if (_rigidBody.velocity.x < -walkEpsilon)
			{
				_transform.localScale = _transform.localScale.WithX(-1.0f);
			}
		}

		#endregion


		#region Input Callbacks

		private void OnMove(InputValue input)
		{
			_moveVector = input.Get<Vector2>();
		}

		private void OnJump(InputValue input)
		{
			bool isHeld = input.Get<float>() > 0.0f;

			bool canJump = IsOnGround || IsInCoyoteTime;
			bool wasHeld = JumpHeld;
			if (canJump && (!wasHeld && isHeld))
			{
				ApplyJumpImpuse();
			}

			JumpHeld = isHeld;
		}

		private void OnRun(InputValue input)
		{
			RunHeld = (input.Get<float>() > 0.0f);
		}

		private void OnCrouch(InputValue input)
		{
			CrouchHeld = (input.Get<float>() > 0.0f);
		}

		private void OnFire(InputValue input)
		{
			if (_currentGun != null)
			{
				_currentGun.OnFire(input.Get<float>() > 0.0f);
			}
		}

		#endregion


		private void UpdateMovement()
		{
			Vector2 velocity = _rigidBody.velocity;

			if ((Mathf.Abs(_moveVector.x) > _deadzone) && !IsCrouching)
			{
				float speed = _speed;
				if (RunHeld)
				{
					speed *= _runFactor;
				}

				// Apply movent requested via input
				velocity.x = ((_moveVector.x > 0.0f) ? 1.0f : -1.0f) * speed;
			}
			else if (IsOnGround)
			{
				// Lerp towards stationary, simulating inertia
				velocity.x = Mathf.Lerp(
					_rigidBody.velocity.x,
					0.0f,
					_inertiaDecay);
				const float epsilon = 0.1f;

				if (Mathf.Abs(velocity.x) <= epsilon)
				{
					velocity.x = 0.0f;
				}
			}

			_rigidBody.velocity = velocity;
		}

		private void UpdateJump()
		{
			// Ensure character falls faster than when rising
			if (_rigidBody.velocity.y < 0.0f)
			{
				_rigidBody.velocity +=
					GetGravityRelativeForce(_fallFactor) * Time.deltaTime;
			}
			// Ensure character falls faster if they're not holding the
			// jump button
			else if ((_rigidBody.velocity.y > 0.0f) && !JumpHeld)
			{
				_rigidBody.velocity +=
					GetGravityRelativeForce(_lowJumpFactor) * Time.deltaTime;
			}
		}

		private Vector2 GetGravityRelativeForce(float factor)
		{
			return Physics2D.gravity * factor;
		}

		private void ApplyJumpImpuse()
		{
			_rigidBody.velocity =
				new Vector2(_rigidBody.velocity.x, _jumpImpulse);
		}

		private bool IsInCoyoteTime => (Time.time - _lastTimeOnGround) < _coyoteTime;

		private void UpdateFallDeath()
		{
			if (_transform.position.y < 0.0f)
			{
				_transform.position = _spawnPos;
				_rigidBody.velocity = Vector2.zero;
			}
		}
	}

}