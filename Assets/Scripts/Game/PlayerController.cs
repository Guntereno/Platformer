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

	[RequireComponent(typeof(Rigidbody2D))]
	public class PlayerController : MonoBehaviour
	{
		[Flags]
		private enum ContactFlags
		{
			None = 0,

			Unknown = 1 << 0,
			Ceiling = 1 << 1,
			RightWall = 1 << 2,
			Ground = 1 << 3,
			LeftWall = 1 << 4
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

		[SerializeField] private float _deadzone = 0.2f;
		[SerializeField] private float _speed = 1.0f;
		[SerializeField] private float _runFactor = 1.5f;
		[SerializeField] private float _inertiaDecay = 0.1f;

		[SerializeField] private float _jumpImpulse = 4.0f;
		[SerializeField] private float _fallFactor = 0.0f;
		[SerializeField] private float _lowJumpFactor = 0.0f;
		[SerializeField] private float _coyoteTime = 0.0f;

		[SerializeField] private float _groundSlope = 0.02f;

		[SerializeField] private Weapon _currentGun = null;

		private Vector2 _spawnPos = default;
		private Rigidbody2D _rigidBody = null;
		private Transform _transform = null;

		private int _animIsOnGroundId;
		private int _animSpeedX;
		private int _animVelocityYId;
		private int _animIsCrouchingId;

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

		public bool OnGround => _contactFlags.TestAny(ContactFlags.Ground);
		public bool OnWall => _contactFlags.TestAny(ContactFlags.LeftWall | ContactFlags.RightWall);
		public bool IsCrouching => OnGround && CrouchHeld;

		public Vector2 Position => _transform.position;

		#region Unity Callbacks

		private void Start()
		{
			_transform = transform;
			_rigidBody = GetComponent<Rigidbody2D>();

			_spawnPos = _transform.position;

			_animIsOnGroundId = Animator.StringToHash("IsOnGround");
			_animSpeedX = Animator.StringToHash("SpeedX");
			_animVelocityYId = Animator.StringToHash("VelocityY");
			_animIsCrouchingId = Animator.StringToHash("IsCrouching");

			int count;

			Flags32<ContactFlags> flags;
			flags = ContactFlags.Ceiling;
			count = flags.CountBits();
			flags = ContactFlags.Ceiling | ContactFlags.LeftWall;
			count = flags.CountBits();
			flags = ContactFlags.Ceiling | ContactFlags.LeftWall | ContactFlags.Unknown;
			count = flags.CountBits();
		}

		private void FixedUpdate()
		{
			// Reset contact flags prior to doing the physics step
			_contactFlags = ContactFlags.None;

			UpdateFallDeath();
		}

		private void Update()
		{
			if (OnGround)
			{
				_lastTimeOnGround = Time.time;
			}

			UpdateMovement();
			UpdateJump();
			UpdateAnimation();
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			foreach (ContactPoint2D contact in collision.contacts)
			{
				Debug.DrawRay(contact.point, contact.normal, Color.green);
				_contactFlags.Set(GetContactFlags(contact));
			}
		}

		private void OnCollisionStay2D(Collision2D collision)
		{
			foreach (ContactPoint2D contact in collision.contacts)
			{
				Debug.DrawRay(contact.point, contact.normal, Color.white);
				_contactFlags.Set(GetContactFlags(contact));
			}
		}

		private void UpdateAnimation()
		{
			_animator.SetBool(_animIsOnGroundId, OnGround);
			_animator.SetFloat(_animSpeedX, Mathf.Abs(_rigidBody.velocity.x) / (_speed * _runFactor));
			_animator.SetFloat(_animVelocityYId, _rigidBody.velocity.y);
			_animator.SetBool(_animIsCrouchingId, IsCrouching);

			if (_rigidBody.velocity.x > 0.0f)
			{
				_transform.localScale = _transform.localScale.WithX(1.0f);
			}
			else if (_rigidBody.velocity.x < 0.0f)
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

			bool canJump = OnGround || IsInCoyoteTime;
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
			if(_currentGun != null)
			{
				_currentGun.OnFire(input.Get<float>() > 0.0f);
			}
		}

		#endregion


		private void UpdateMovement()
		{
			float xVelocity = 0.0f;

			if ((Mathf.Abs(_moveVector.x) > _deadzone) && !IsCrouching)
			{
				float speed = _speed;
				if (RunHeld)
				{
					speed *= _runFactor;
				}

				// Apply movent requested via input
				xVelocity = ((_moveVector.x > 0.0f) ? 1.0f : -1.0f) * speed;
			}
			else if (OnGround)
			{
				// Lerp towards stationary, simulating inertia
				xVelocity = Mathf.Lerp(
					_rigidBody.velocity.x,
					0.0f,
					_inertiaDecay);
				const float epsilon = 0.1f;

				if (Mathf.Abs(xVelocity) <= epsilon)
				{
					xVelocity = 0.0f;
				}
			}

			// Remove airborne velocity into a wall
			if (OnWall && !OnGround)
			{
				xVelocity = ClampVelocityIntoWall(_contactFlags, xVelocity);
			}

			_rigidBody.velocity = _rigidBody.velocity.WithX(xVelocity);
		}

		private static float ClampVelocityIntoWall(Flags32<ContactFlags> contactFlags, float xVelocity)
		{
			if (contactFlags.TestAny(ContactFlags.LeftWall) && (xVelocity < 0.0f))
			{
				return 0.0f;
			}

			if (contactFlags.TestAny(ContactFlags.RightWall) && (xVelocity > 0.0f))
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
			if (_transform.position.y < -1.0f)
			{
				_transform.position = _spawnPos;
			}
		}

		private Flags32<ContactFlags> GetContactFlags(ContactPoint2D contact)
		{
			float dot = Vector3.Dot(Vector2.up, contact.normal);
			if ((1.0f - dot) <= _groundSlope)
				return ContactFlags.Ground;
			else if (dot <= float.Epsilon)
				return (contact.normal.x > 0.0f) ? ContactFlags.LeftWall : ContactFlags.RightWall;
			else if (dot <= (-1.0f - float.Epsilon))
				return ContactFlags.Ceiling;
			else
				return ContactFlags.Unknown;
		}
	}

}