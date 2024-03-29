using Game.Guns;
using Momo.Core;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
	public class PlayerController : CharacterController
	{
		[Flags]
		private enum InputFlags
		{
			None = 0,

			Jump = 1 << 0,
			Crouch = 1 << 1,
			Fire = 1 << 2
		}

		[SerializeField] private Animator _animator = null;
		[SerializeField] private SpriteRenderer _renderer = null;

		[SerializeField] private Weapon[] _weapons = null;

		[Header("Input Settings")]
		[SerializeField] private float _deadzone = 0.2f;
		[SerializeField] private float _acceleration = 1.0f;

		[Header("Ground Settings")]
		[SerializeField] private float _groundFriction = 0.5f;
		[SerializeField] private float _groundMinVelocity = 0.1f;

		[Header("Crouch Settings")]
		[SerializeField] private float _crouchRecoilFactor = 0.5f;

		[Header("Air Settings")]
		[SerializeField] private float _jumpImpulse = 4.0f;
		[SerializeField] private int _numAirJumps = 1;
		[SerializeField] private float _fallFactor = 0.0f;
		[SerializeField] private float _lowJumpFactor = 0.0f;
		[SerializeField] private float _airborneAccelerationFactor = 1.0f;
		[SerializeField] private float _coyoteTime = 0.0f;
		[SerializeField] private float _fallDeathHeight = 0.0f;

		[Header("Wall Settings")]
		[SerializeField] private float _wallGripGravityScale = 0.2f;
		[SerializeField] private float _wallJumpAngleRadians = 0.0f;
		[SerializeField] private float _wallJumpImpulse = 4.0f;
		[SerializeField] private float _wallJumpLaunchDuration = 0.25f;

		[Header("Pain Settings")]
		[SerializeField] private float _bounceForce = 10.0f;

		[Header("Invincibility Settings")]
		[SerializeField] private float _invincibilityDurationSeconds = 1.0f;
		[SerializeField] private float _invincibilityFlashIntervalSeconds = 0.01f;
		[SerializeField] private Color _invincibilityFlashColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

		private Vector2 _spawnPos = default;
		private Color _defaultSpriteColor = Color.white;

		private int _animIsOnGroundId;
		private int _animSpeedX;
		private int _animVelocityYId;
		private int _animIsCrouchingId;
		private int _animIsGrippingWallId;

		private Vector2 _moveVector = default;

		private Flags32<InputFlags> _inputFlags = InputFlags.None;

		private int _airJumpCounter = 0;
		private float _lastTimeWallJumped = float.MinValue;

		private ContactFlags _grippingWall = ContactFlags.None;

		private float _lastTimeOnGround = float.MinValue;

		private Weapon _currentWeapon = null;

		private const int _maxHealth = 5;
		private int _currentHealth = _maxHealth;

		public Vector2 Position => _transform.position;


		public bool IsGrippingWall
		{
			get => _grippingWall != ContactFlags.None;
		}

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

		private bool HasJustWallJumped => (Time.time - _lastTimeWallJumped) < _wallJumpLaunchDuration;

		#region Unity Callbacks

		protected override void Start()
		{
			base.Start();

			_spawnPos = _transform.position;
			_defaultSpriteColor = _renderer.color;

			_animIsOnGroundId = Animator.StringToHash("IsOnGround");
			_animSpeedX = Animator.StringToHash("SpeedX");
			_animVelocityYId = Animator.StringToHash("VelocityY");
			_animIsCrouchingId = Animator.StringToHash("IsCrouching");
			_animIsGrippingWallId = Animator.StringToHash("IsGrippingWall");

			_painLayerMask = LayerMask.GetMask("Enemies");

			SetWeaponIndex(0);
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();

			FixedUpdateVelocity();

			if (IsOnGround)
			{
				_lastTimeOnGround = Time.time;
			}

			UpdateGravityScale();
		}

		protected override void Update()
		{
			base.Update();

			UpdateFiring();
			UpdateWallGrip();
			UpdateAnimation();
			UpdateFallDeath();

			if (IsOnGround || IsInCoyoteTime || IsGrippingWall)
			{
				_airJumpCounter = 0;
			}
		}

		protected override void OnGUI()
		{
			DebugGui();
		}

		#endregion


		#region Input Callbacks

		private void OnMove(InputValue input)
		{
			_moveVector = input.Get<Vector2>();

			if(IsCrouchHeld)
			{
				_moveVector.x = 0.0f;
			}
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

		protected override void DebugGui()
		{
#if UNITY_EDITOR
			GUILayout.Label($"Contact Flags: {CurrentContactFlags}");
			GUILayout.Label($"Velocity: {_rigidBody.velocity}");
			GUILayout.Label($"Air Jump Counter: {_airJumpCounter}");
			GUILayout.Label($"Move Vector: {_moveVector}");
			GUILayout.Label($"Gripping Wall: {IsGrippingWall}");

			base.DebugGui();
#endif
		}

		protected override void OnHurt(Collision2D collision)
		{
			base.OnHurt(collision);

			Vector2 direction = transform.position - collision.transform.position;
			direction = direction.normalized;
			_rigidBody.AddForce(direction * _bounceForce, ForceMode2D.Impulse);

			HurtPlayer();
		}

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
			if (_currentWeapon != null)
			{
				_currentWeapon.gameObject.SetActive(false);
			}
			_currentWeapon = _weapons[index];
			_currentWeapon.gameObject.SetActive(true);
		}

		private Vector2 GetWallJumpDirection()
		{
			Vector2 jumpDir = new Vector2(Mathf.Cos(_wallJumpAngleRadians), Mathf.Sin(_wallJumpAngleRadians));

			if (CurrentContactFlags.TestAll(ContactFlags.OnRightWall))
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

			velocity = ApplyFriction(velocity, _groundFriction, _groundMinVelocity);

			Vector2 inputAcceleration = CalculateInputAcceleration();
			velocity += inputAcceleration * Time.fixedDeltaTime;

			if (IsGrippingWall && !HasJustWallJumped)
			{
				velocity = Vector2.zero;
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

			velocity = ClampToMaxSpeed(velocity);

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

			acceleration = ClampIntoWall(CurrentContactFlags, acceleration);

			if (!IsOnGround)
			{
				acceleration *= _airborneAccelerationFactor;
			}

			return acceleration;
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
					runSpeed = (absXVel / MaxSpeed);
				}
			}
			_animator.SetFloat(_animSpeedX, runSpeed);

			_animator.SetBool(_animIsOnGroundId, IsOnGround || IsInCoyoteTime);
			_animator.SetFloat(_animVelocityYId, _rigidBody.velocity.y);
			_animator.SetBool(_animIsCrouchingId, IsCrouching);
			_animator.SetBool(_animIsGrippingWallId, IsGrippingWall);

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
				if (CurrentContactFlags.TestAll(ContactFlags.OnLeftWall))
				{
					return ContactFlags.OnLeftWall;
				}
				else if (CurrentContactFlags.TestAll(ContactFlags.OnRightWall))
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

		private void UpdateWallGrip()
		{
			if (IsGrippingWall)
			{
				CheckLetGoOfWall();
			}
			else
			{
				CheckGripWall();
			}
		}

		private void CheckLetGoOfWall()
		{
			if (IsOnGround || IsCrouchHeld)
			{
				_grippingWall = ContactFlags.None;
				return;
			}

			switch (_grippingWall)
			{
				case ContactFlags.OnLeftWall:
					{
						if (!CurrentContactFlags.TestAll(ContactFlags.OnLeftWall) || _moveVector.x > 0)
						{
							_grippingWall = ContactFlags.None;
						}
					}
					break;

				case ContactFlags.OnRightWall:
					{
						if (!CurrentContactFlags.TestAll(ContactFlags.OnRightWall) || (_moveVector.x < 0))
						{
							_grippingWall = ContactFlags.None;
						}
					}
					break;

				default:
					{
						Debug.LogError($"Invalid ContactFlags for _grippingWall!: {_grippingWall}");
						break;
					}
			}
		}

		private void CheckGripWall()
		{
			if (!CurrentContactFlags.TestAny(ContactFlags.OnWall))
			{
				return;
			}

			if (CurrentContactFlags.TestAny(ContactFlags.OnLeftWall) && (_moveVector.x < 0))
			{
				_grippingWall = ContactFlags.OnLeftWall;
				return;
			}

			if (CurrentContactFlags.TestAny(ContactFlags.OnRightWall) && (_moveVector.x > 0))
			{
				_grippingWall = ContactFlags.OnRightWall;
				return;
			}
		}

		private void UpdateFallDeath()
		{
			if (_transform.position.y < _fallDeathHeight)
			{
				_transform.position = _spawnPos;
				_rigidBody.velocity = Vector2.zero;

				HurtPlayer();
			}
		}

		private void HurtPlayer()
		{
			UpdateHealth(_currentHealth - 1);

			TriggerInvincibility(InvincibilityCoroutine());
		}

		private void UpdateHealth(int newHealth)
		{
			_currentHealth = Mathf.Clamp(newHealth, 0, (_maxHealth - 1));
			Singleton.Instance.UiEvents.UpdateHealth(newHealth);
		}

		private IEnumerator InvincibilityCoroutine()
		{
			float elapsedTime = 0.0f;
			_renderer.color = _defaultSpriteColor;

			while (elapsedTime < _invincibilityDurationSeconds)
			{
				_renderer.color = _invincibilityFlashColor;
				yield return new WaitForSeconds(_invincibilityFlashIntervalSeconds);
				_renderer.color = _defaultSpriteColor;
				yield return new WaitForSeconds(_invincibilityFlashIntervalSeconds);
				elapsedTime += _invincibilityFlashIntervalSeconds * 2.0f;
			}

			_renderer.color = _defaultSpriteColor;

			EndInvincibility();
		}

		#region Debug Drawing

		protected override void DebugDraw()
		{
			base.DebugDraw();

#if UNITY_EDITOR
			DebugDrawWallJump();
#endif
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