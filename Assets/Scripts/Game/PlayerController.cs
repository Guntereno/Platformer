using Core;
using Game.Guns;
using Momo.Core;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(CapsuleCollider2D))]
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
			Crouch = 1 << 1,
			Fire = 1 << 2
		}

		[SerializeField] private Animator _animator = null;
		[SerializeField] private CapsuleCollider2D _bodyCollider = null;
		[SerializeField] private Rigidbody2D _rigidBody = null;

		[SerializeField] private float _deadzone = 0.2f;
		[SerializeField] private float _maxSpeed = 1.0f;
		[SerializeField] private float _acceleration = 1.0f;

		[SerializeField] private float _jumpImpulse = 4.0f;
		[SerializeField] private int _numAirJumps = 1;
		[SerializeField] private float _fallFactor = 0.0f;
		[SerializeField] private float _lowJumpFactor = 0.0f;
		[SerializeField] private float _airborneAccelerationFactor = 1.0f;

		[SerializeField] private float _coyoteTime = 0.0f;
		[SerializeField] private float _contectCheckDistance = 0.1f;

		[SerializeField] private Weapon[] _weapons = null;

		[SerializeField] private float _crouchRecoilFactor = 0.5f;

		[SerializeField] private float _wallGripGravityScale = 0.2f;

		[SerializeField] private float _groundFriction = 0.5f;
		[SerializeField] private float _groundMinVelocity = 0.1f;

		[SerializeField] private float _wallJumpAngleRadians = 0.0f;
		[SerializeField] private float _wallJumpImpulse = 4.0f;
		[SerializeField] private float _wallJumpLaunchDuration = 0.25f;


		private Vector2 _spawnPos = default;
		private Transform _transform = null;

		private float _bodyBoxWidth;
		private float _bodyBoxHeight;

		private int _animIsOnGroundId;
		private int _animSpeedX;
		private int _animVelocityYId;
		private int _animIsCrouchingId;
		private int _animIsGrippingWallId;

		private int _groundAndPropsMask;
		private int _groundMask;

		private Vector2 _groundNormalLeft;
		private Vector2 _groundNormalRight;

		private Vector2 _moveVector = default;

		private Flags32<InputFlags> _inputFlags = InputFlags.None;

		private int _airJumpCounter = 0;
		private float _lastTimeWallJumped = float.MinValue;

		private Flags32<ContactFlags> _contactFlags = ContactFlags.None;
		private float _lastTimeOnGround = float.MinValue;

		GroundCheckParams _groundCheckParams;

		private Weapon _currentWeapon = null;


		public Vector2 Position => _transform.position;
		public bool IsOnGround => _contactFlags.TestAny(ContactFlags.OnGround);
		private bool IsOnWall => _contactFlags.TestAny(ContactFlags.OnLeftWall | ContactFlags.OnRightWall);
		public bool IsGrippingWall => !IsOnGround && !IsCrouchHeld && IsOnWall;
		private bool IsCrouching => IsOnGround && IsCrouchHeld;
		private bool IsInCoyoteTime => !IsOnGround && (Time.time - _lastTimeOnGround) < _coyoteTime;

		private bool IsJumpHeld
		{
			get => _inputFlags.TestAll(InputFlags.Jump);
			set => _inputFlags.Assign(InputFlags.Jump, value);
		}

		private bool IsCrouchHeld
		{
			get => _inputFlags.TestAll(InputFlags.Crouch);
			set => _inputFlags.Assign(InputFlags.Crouch, value);
		}

		private bool IsFireHeld
		{
			get => _inputFlags.TestAll(InputFlags.Fire);
			set => _inputFlags.Assign(InputFlags.Fire, value);
		}

		private Vector2 GroundDirectionLeft => Vector2.Perpendicular(_groundNormalLeft);
		private Vector2 GroundDirectionRight => -Vector2.Perpendicular(_groundNormalRight);

		private bool HasJustWallJumped => (Time.time - _lastTimeWallJumped) < _wallJumpLaunchDuration;


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
			_animIsGrippingWallId = Animator.StringToHash("IsGrippingWall");

			_groundAndPropsMask = LayerMask.GetMask("Ground", "Props");
			_groundMask = LayerMask.GetMask("Ground");

			_groundCheckParams = BuildGroundCheckParams();

			SetWeaponIndex(0);
		}

		private void FixedUpdate()
		{
			_contactFlags = CheckForContact();

			FixedUpdateVelocity();

			ResetGroundNormals();

			if (IsOnGround)
			{
				_lastTimeOnGround = Time.time;
			}

			UpdateGravityScale();
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			HandleGroundContacts(collision);
		}

		private void OnCollisionStay2D(Collision2D collision)
		{
			HandleGroundContacts(collision);
		}

		private void Update()
		{
			UpdateFiring();
			UpdateAnimation();
			UpdateFallDeath();

			if (IsOnGround || IsInCoyoteTime || (IsOnWall && !IsCrouchHeld))
			{
				_airJumpCounter = 0;
			}

			DebugDraw();
		}

		void OnGUI()
		{
#if UNITY_EDITOR
			GUI.Label(new Rect(25, 25, 180, 20), $"IsOnGround: {IsOnGround}");
			GUI.Label(new Rect(25, 45, 180, 20), $"Velocity: {_rigidBody.velocity}");
			GUI.Label(new Rect(25, 65, 180, 20), $"Air Jump Counter: {_airJumpCounter}");
#endif
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
			bool wasHeld = IsJumpHeld;

			if (IsGrippingWall)
			{
				if (!wasHeld && isHeld)
				{
					Vector2 jumpDir = GetWallJumpDirection();
					AddJumpForce(jumpDir * _wallJumpImpulse);

					_lastTimeWallJumped = Time.time;
				}
			}
			else
			{
				bool canJump = IsOnGround || IsInCoyoteTime || (_airJumpCounter < _numAirJumps);
				if (canJump && (!wasHeld && isHeld))
				{
					AddJumpForce(new Vector2(0.0f, _jumpImpulse));
				}
			}

			IsJumpHeld = isHeld;
		}

		private void OnCrouch(InputValue input)
		{
			IsCrouchHeld = (input.Get<float>() > 0.0f);
		}

		private void OnFire(InputValue input)
		{
			IsFireHeld = (input.Get<float>() > 0.0f);
		}

		private void OnNextWeapon(InputValue input)
		{
			if (input.Get<float>() > 0.0f)
			{
				NextWeapon();
			}
		}

		#endregion

		private void NextWeapon()
		{
			for (int i = 0; i < _weapons.Length; ++i)
			{
				if (_weapons[i] == _currentWeapon)
				{
					int nextWeaponIndex = (i + 1) % _weapons.Length;
					SetWeaponIndex(nextWeaponIndex);
					break;
				}
			}
		}

		private void SetWeaponIndex(int index)
		{
			if(_currentWeapon != null)
			{
				_currentWeapon.gameObject.SetActive(false);
			}
			_currentWeapon = _weapons[index];
			_currentWeapon.gameObject.SetActive(true);
		}

		private Vector2 GetWallJumpDirection()
		{
			Vector2 jumpDir = new Vector2(Mathf.Cos(_wallJumpAngleRadians), Mathf.Sin(_wallJumpAngleRadians));

			if (_contactFlags == ContactFlags.OnRightWall)
			{
				jumpDir *= new Vector2(-1.0f, 1.0f);
			}

			return jumpDir;
		}

		private void AddJumpForce(Vector2 force)
		{
			_rigidBody.velocity = force;

			if (!IsOnGround)
			{
				++_airJumpCounter;
			}
		}

		private void FixedUpdateVelocity()
		{
			Vector2 velocity = _rigidBody.velocity;

			// Apply friction with ground. (Player's physics material has zero
			// friction, so we must do this ourselves.)
			if (IsOnGround)
			{
				velocity += (-velocity * _groundFriction) * Time.fixedDeltaTime;
				if (velocity.sqrMagnitude < (_groundMinVelocity * _groundMinVelocity))
				{
					velocity = Vector2.zero;
				}
			}

			Vector2 inputAcceleration = CalculateInputAcceleration();
			velocity += inputAcceleration * Time.fixedDeltaTime;

			if (IsGrippingWall && !HasJustWallJumped)
			{
				velocity.y = 0;
			}
			else
			{
				// Ensure character falls faster than when rising
				if (velocity.y < 0.0f)
				{
					velocity += Physics2D.gravity * _fallFactor;
				}
				// Ensure character falls faster if they're not holding the
				// jump button
				else if ((velocity.y > 0.0f) && !IsJumpHeld)
				{
					velocity += Physics2D.gravity * _lowJumpFactor;
				}
			}

			velocity = new Vector2(
				Mathf.Clamp(velocity.x, -_maxSpeed, _maxSpeed),
				Mathf.Clamp(velocity.y, -_maxSpeed, _maxSpeed));

			_rigidBody.velocity = velocity;
		}

		private void UpdateGravityScale()
		{
			if (IsGrippingWall)
			{
				_rigidBody.gravityScale = _wallGripGravityScale;
			}
			else
			{
				_rigidBody.gravityScale = (IsOnGround || IsInCoyoteTime) ? 0.0f : 1.0f;
			}
		}

		private Vector2 CalculateInputAcceleration()
		{
			Vector2 acceleration = Vector2.zero;

			if ((Mathf.Abs(_moveVector.x) > _deadzone) && !IsCrouching)
			{
				acceleration = _moveVector.WithY(0.0f) * _acceleration;
			}

			acceleration = ClampIntoWall(_contactFlags, acceleration);

			if (!IsOnGround)
			{
				acceleration *= _airborneAccelerationFactor;
			}

			return acceleration;
		}

		private static Vector2 ClampIntoWall(Flags32<ContactFlags> contactFlags, Vector2 force)
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

		private void UpdateAnimation()
		{
			float runSpeed = 0.0f;

			Vector2 inputAcceleration = CalculateInputAcceleration();

			if (Mathf.Abs(inputAcceleration.x) > 0.0f)
			{
				float absXVel = Mathf.Abs(_rigidBody.velocity.x);
				if (absXVel > 0.05f)
				{
					runSpeed = (absXVel / _maxSpeed);
				}
			}
			_animator.SetFloat(_animSpeedX, runSpeed);

			_animator.SetBool(_animIsOnGroundId, IsOnGround || IsInCoyoteTime);
			_animator.SetFloat(_animVelocityYId, _rigidBody.velocity.y);
			_animator.SetBool(_animIsCrouchingId, IsCrouching);
			_animator.SetBool(_animIsGrippingWallId, IsOnWall);

			if (IsOnWall)
			{
				switch (IsOnWhichWall())
				{
					case ContactFlags.OnLeftWall:
						{
							LookRight();
							break;
						}

					case ContactFlags.OnRightWall:
						{
							LookLeft();
							break;
						}
				}
			}
			else
			{
				if (!IsFireHeld)
				{
					if (inputAcceleration.x > 0)
					{
						LookRight();
					}
					else if (inputAcceleration.x < 0)
					{
						LookLeft();
					}
				}
			}
		}

		private ContactFlags IsOnWhichWall()
		{
			if (IsOnWall)
			{
				if (_contactFlags.TestAll(ContactFlags.OnLeftWall))
				{
					return ContactFlags.OnLeftWall;
				}
				else if (_contactFlags.TestAll(ContactFlags.OnRightWall))
				{
					return ContactFlags.OnRightWall;
				}
			}
			return ContactFlags.None;
		}

		private void LookLeft()
		{
			_transform.localScale = _transform.localScale.WithX(-1.0f);
		}

		private void LookRight()
		{
			_transform.localScale = _transform.localScale.WithX(1.0f);
		}

		private void UpdateFiring()
		{
			if (_currentWeapon != null)
			{
				bool weaponDischarged = _currentWeapon.OnFire(IsFireHeld, out Vector2 recoil);

				if (weaponDischarged)
				{
					if (IsCrouching)
					{
						recoil *= _crouchRecoilFactor;
					}

					_rigidBody.AddForce(recoil, ForceMode2D.Impulse);
				}
			}
		}

		private void UpdateFallDeath()
		{
			if (_transform.position.y < 0.0f)
			{
				_transform.position = _spawnPos;
				_rigidBody.velocity = Vector2.zero;
			}
		}

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

		private GroundCheckParams BuildGroundCheckParams()
		{
			GroundCheckParams result;

			result.Origin = _bodyCollider.offset;
			result.Origin.y -= (_bodyCollider.size.y - _bodyCollider.size.x) * 0.5f;
			result.Radius = _bodyCollider.size.x * 0.5f;

			return result;
		}

		private RaycastHit2D GroundCheck()
		{
			RaycastHit2D hit = Physics2D.CircleCast(
				(Vector2)(_transform.position) + _groundCheckParams.Origin,
				_groundCheckParams.Radius,
				Vector2.down,
				_contectCheckDistance, _groundAndPropsMask);

			return hit;
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

		private void ResetGroundNormals()
		{
			_groundNormalLeft = _groundNormalRight = Vector2.up;
		}

		private void HandleGroundContacts(Collision2D collision)
		{
			foreach (ContactPoint2D contact in collision.contacts)
			{
				Debug.DrawRay(contact.point, contact.normal, Color.green);

				float upDotNorm = Vector2.Dot(Vector2.up, contact.normal);
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


		#region Debug Drawing

		private void DebugDraw()
		{
#if UNITY_EDITOR
			DebugDrawCollisionChecks();
			DebugDrawGroundNormals();
			DebugDrawWallJump();
#endif
		}

		private void DebugDrawCollisionChecks()
		{
			DebugDrawGroundCheck(_contactFlags.TestAll(ContactFlags.OnGround));
			DebugDrawWallCheck(Vector2.left, _contactFlags.TestAll(ContactFlags.OnLeftWall));
			DebugDrawWallCheck(Vector2.right, _contactFlags.TestAll(ContactFlags.OnRightWall));
		}

		private void DebugDrawGroundNormals()
		{
			Vector2 groundNormalStart = (Vector2)_transform.position
				+ _bodyCollider.offset
				- (_bodyCollider.size * 0.5f).WithX(0.0f);

			Debug.DrawLine(groundNormalStart, groundNormalStart + GroundDirectionLeft, Color.cyan);
			Debug.DrawLine(groundNormalStart, groundNormalStart + GroundDirectionRight, Color.cyan);
		}

		private void DebugDrawGroundCheck(bool hitOccured)
		{
			float startTheta = Mathf.PI;
			Vector2 arcCenter = (Vector2)_transform.position
				+ _groundCheckParams.Origin
				+ (Vector2.down * _contectCheckDistance);
			Core.DebugDraw.Arc(
				arcCenter,
				startTheta,
				Mathf.PI,
				_groundCheckParams.Radius,
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

		private void DebugDrawWallJump()
		{
			if (IsOnWall)
			{
				Vector2 jumpDir = GetWallJumpDirection();
				Vector2 start = _transform.position;
				Debug.DrawLine(start, start + jumpDir, Color.yellow);
			}
		}

		#endregion
	}

}