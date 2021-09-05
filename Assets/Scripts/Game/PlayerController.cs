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

	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(CapsuleCollider2D))]
	[RequireComponent(typeof(CircleCollider2D))]
	[RequireComponent(typeof(Rigidbody2D))]
	public class PlayerController : MonoBehaviour
	{
		[Flags]
		private enum ContactFlags
		{
			None = 0,

			Unknown = 1 << 0,
			OnGround = 1 << 1,
			OnRightWall = 1 << 2,
			OnLeftWall = 1 << 3
		}

		[Flags]
		private enum InputFlags
		{
			None = 0,

			Jump = 1 << 0,
			Run = 1 << 1,
			Crouch = 1 << 2,
			Fire = 1 << 3
		}

		[SerializeField] private Animator _animator = null;
		[SerializeField] private CapsuleCollider2D _bodyCollider = null;
		[SerializeField] private CircleCollider2D _feetCollider = null;
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
		[SerializeField] private float _crouchRecoilFactor = 0.5f;

		private Vector2 _spawnPos = default;
		private Transform _transform = null;

		private float _bodyBoxWidth;
		private float _bodyBoxHeight;

		private int _animIsOnGroundId;
		private int _animSpeedX;
		private int _animVelocityYId;
		private int _animIsCrouchingId;

		private int _groundAndPropsMask;
		private int _groundMask;

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

		private bool FireHeld
		{
			get => _inputFlags.TestAll(InputFlags.Fire);
			set => _inputFlags.Assign(InputFlags.Fire, value);
		}

		private Flags32<ContactFlags> _contactFlags = ContactFlags.None;
		private float _lastTimeOnGround = float.MinValue;

		private readonly PlayerCollisionFlags CollisionFlags;

		public bool IsOnGround => _contactFlags.TestAny(ContactFlags.OnGround);
		public bool OnWall => _contactFlags.TestAny(ContactFlags.OnLeftWall | ContactFlags.OnRightWall);
		public bool IsCrouching => IsOnGround && CrouchHeld;

		public Vector2 Position => _transform.position;

		#region Unity Callbacks

		private void Start()
		{
			_transform = transform;
			_rigidBody = GetComponent<Rigidbody2D>();

			if (_bodyCollider.size.x > _bodyCollider.size.y)
			{
				throw new Exception("Collider should be taller than it is wide!");
			}

			_bodyBoxWidth = _bodyCollider.size.x;
			_bodyBoxHeight = (_bodyCollider.size.y - _bodyCollider.size.x);

			_spawnPos = _transform.position;

			_animIsOnGroundId = Animator.StringToHash("IsOnGround");
			_animSpeedX = Animator.StringToHash("SpeedX");
			_animVelocityYId = Animator.StringToHash("VelocityY");
			_animIsCrouchingId = Animator.StringToHash("IsCrouching");

			_groundAndPropsMask = LayerMask.GetMask("Ground", "Props");
			_groundMask = LayerMask.GetMask("Ground");
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
			UpdateFiring();
			UpdateAnimation();

			DebugDrawCollisionChecks();
		}

		void OnGUI()
		{
#if UNITY_EDITOR
			GUI.Label(new Rect(25, 25, 180, 20), $"IsOnGround: {IsOnGround}");
			GUI.Label(new Rect(25, 45, 180, 20), $"Velocity: {_rigidBody.velocity}");
#endif
		}

		#endregion

		private Flags32<ContactFlags> CheckForContact()
		{
			Flags32<ContactFlags> result = ContactFlags.None;

			if (GroundCheck())
			{
				result.Set(ContactFlags.OnGround);
			}

			RaycastHit2D rightHit = ContactWallCheck(Vector2.right);
			if (rightHit)
			{
				result.Set(ContactFlags.OnRightWall);
			}

			RaycastHit2D leftHit = ContactWallCheck(Vector2.left);
			if (leftHit)
			{
				result.Set(ContactFlags.OnLeftWall);
			}

			return result;
		}

		private struct GroundCheckParams
		{
			public Vector2 Origin;
			public float Radius;
		}

		private GroundCheckParams BuildGroundCheckParams()
		{
			GroundCheckParams result;
			result.Origin = (Vector2)_transform.position + _feetCollider.offset;
			result.Radius = _feetCollider.radius;
			return result;
		}

		private RaycastHit2D GroundCheck()
		{
			GroundCheckParams checkParams = BuildGroundCheckParams();
			RaycastHit2D hit = Physics2D.CircleCast(
				checkParams.Origin, checkParams.Radius, Vector2.down,
				_contectCheckDistance, _groundAndPropsMask);

			return hit;
		}

		private struct WallCheckParams
		{
			public Vector2 BoxSize;
			public Vector2 Origin;
		}

		private WallCheckParams BuildWallCheckParams(Vector2 dir)
		{
			if (Mathf.Abs(dir.y) > 0.0f)
			{
				throw new Exception("Side checks must be horizontal!");
			}

			WallCheckParams result;
			result.BoxSize = new Vector2(_bodyBoxWidth * 0.5f, _bodyBoxHeight);
			result.Origin =
				(Vector2)_transform.position
				+ (dir * (result.BoxSize.x * 0.5f));
			return result;
		}

		private RaycastHit2D ContactWallCheck(Vector2 dir)
		{
			WallCheckParams checkParams = BuildWallCheckParams(dir);

			RaycastHit2D hit = Physics2D.BoxCast(
				checkParams.Origin, checkParams.BoxSize,
				angle: 0.0f,
				dir,
				distance: _contectCheckDistance,
				layerMask: _groundMask);

			return hit;
		}

		private void DebugDrawCollisionChecks()
		{
#if UNITY_EDITOR
			DebugDrawGroundCheck(_contactFlags.TestAll(ContactFlags.OnGround));
			DebugDrawWallCheck(Vector2.left, _contactFlags.TestAll(ContactFlags.OnLeftWall));
			DebugDrawWallCheck(Vector2.right, _contactFlags.TestAll(ContactFlags.OnRightWall));
#endif
		}

		private void DebugDrawGroundCheck(bool hitOccured)
		{
			GroundCheckParams checkParams = BuildGroundCheckParams();

			float startTheta = Mathf.PI;
			Vector2 arcCenter = checkParams.Origin + (Vector2.down * _contectCheckDistance);
			DebugDraw.Arc(
				arcCenter,
				startTheta,
				Mathf.PI,
				checkParams.Radius,
				(hitOccured ? Color.green : Color.grey));
		}

		private void DebugDrawWallCheck(Vector2 dir, bool hitOccured)
		{
			WallCheckParams checkParams = BuildWallCheckParams(dir);

			Vector2 start = checkParams.Origin + (dir * ((checkParams.BoxSize.x * 0.5f) + _contectCheckDistance));
			start.y += checkParams.BoxSize.y * 0.5f;
			Vector2 end = start + (Vector2.down * checkParams.BoxSize.y);
			Debug.DrawLine(start, end, (hitOccured ? Color.green : Color.grey));
		}

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
			FireHeld = (input.Get<float>() > 0.0f);
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

			velocity.x = ClampVelocityIntoWall(_contactFlags, velocity.x);

			_rigidBody.velocity = velocity;
		}

		private void UpdateAnimation()
		{
			_animator.SetBool(_animIsOnGroundId, IsOnGround || IsInCoyoteTime);
			_animator.SetFloat(_animSpeedX, Mathf.Abs(_rigidBody.velocity.x) / (_speed * _runFactor));
			_animator.SetFloat(_animVelocityYId, _rigidBody.velocity.y);
			_animator.SetBool(_animIsCrouchingId, IsCrouching);

			if(!FireHeld)
			{
				if (_moveVector.x > 0)
				{
					_transform.localScale = _transform.localScale.WithX(1.0f);
				}
				else if (_moveVector.x < 0)
				{
					_transform.localScale = _transform.localScale.WithX(-1.0f);
				}
			}
		}

		private void UpdateFiring()
		{
			if (_currentGun != null)
			{
				_currentGun.OnFire(FireHeld, out Vector2 recoil);

				if(IsCrouching)
				{
					recoil *= _crouchRecoilFactor;
				}

				_rigidBody.AddForce(recoil, ForceMode2D.Impulse);
			}
		}

		private static float ClampVelocityIntoWall(Flags32<ContactFlags> contactFlags, float xVelocity)
		{
			if (contactFlags.TestAny(ContactFlags.OnLeftWall) && (xVelocity < 0.0f))
			{
				return 0.0f;
			}
			else if (contactFlags.TestAny(ContactFlags.OnRightWall) && (xVelocity > 0.0f))
			{
				return 0.0f;
			}

			return xVelocity;
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