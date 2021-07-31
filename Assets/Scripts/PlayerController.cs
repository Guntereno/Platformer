using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

	[SerializeField] private Animator _animator = null;
	[SerializeField] private SpriteRenderer _spriteRenderer = null;

	[SerializeField] private float _speed = 1.0f;
	[SerializeField] private float _inertiaDecay = 0.1f;
	[SerializeField] private float _deadzone = 0.2f;

	[SerializeField] private float _jumpImpulse = 4.0f;
	[SerializeField] private float _fallFactor = 0.0f;
	[SerializeField] private float _lowJumpFactor = 0.0f;
	[SerializeField] private float _coyoteTime = 0.0f;

	private Vector2 _spawnPos = default;
	private Rigidbody2D _rigidBody = null;

	private int _animIsOnGroundId;
	private int _animIsRunning;
	private int _animVelocityYId;

	private Vector2 _moveVector = default;
	private bool _jumpHeld = default;
	private ContactFlags _contactFlags = ContactFlags.None;
	private float _lastTimeOnGround = float.MinValue;

	private readonly PlayerCollisionFlags CollisionFlags;

	public bool OnGround => (_contactFlags & ContactFlags.Ground) != ContactFlags.None;
	public bool OnWall => (_contactFlags & (ContactFlags.LeftWall | ContactFlags.RightWall)) != ContactFlags.None;

	public Vector2 Position => transform.position;

	#region Unity Callbacks

	private void Start()
	{
		_spawnPos = transform.position;
		_rigidBody = GetComponent<Rigidbody2D>();

		_animIsOnGroundId = Animator.StringToHash("IsOnGround");
		_animIsRunning = Animator.StringToHash("IsRunning");
		_animVelocityYId = Animator.StringToHash("VelocityY");
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
			_contactFlags |= GetContactFlags(contact);
		}
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		foreach (ContactPoint2D contact in collision.contacts)
		{
			Debug.DrawRay(contact.point, contact.normal, Color.white);
			_contactFlags |= GetContactFlags(contact);
		}
	}

	private void UpdateAnimation()
	{
		_animator.SetBool(_animIsOnGroundId, OnGround);
		_animator.SetBool(_animIsRunning, OnGround && ((Mathf.Abs(_moveVector.x) > _deadzone)));
		_animator.SetFloat(_animVelocityYId, _rigidBody.velocity.y);

		if (_rigidBody.velocity.x > 0.0f)
		{
			_spriteRenderer.flipX = false;
		}
		else if (_rigidBody.velocity.x < 0.0f)
		{
			_spriteRenderer.flipX = true;
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
		bool jumpHeld = input.Get<float>() > 0.0f;

		bool canJump = OnGround || IsInCoyoteTime;
		if (canJump && (!_jumpHeld && jumpHeld))
		{
			ApplyJumpImpuse();
		}

		_jumpHeld = jumpHeld;
	}

	#endregion


	private void UpdateMovement()
	{
		float xVelocity = 0.0f;

		if (Mathf.Abs(_moveVector.x) > _deadzone)
		{
			// Apply movent requested via input
			xVelocity = ((_moveVector.x > 0.0f) ? 1.0f : -1.0f) * _speed;
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

	private static float ClampVelocityIntoWall(ContactFlags contactFlags, float xVelocity)
	{
		if (((contactFlags & ContactFlags.LeftWall) != ContactFlags.None) && (xVelocity < 0.0f))
		{
			return 0.0f;
		}

		if (((contactFlags & ContactFlags.RightWall) != ContactFlags.None) && (xVelocity > 0.0f))
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
		else if ((_rigidBody.velocity.y > 0.0f) && !_jumpHeld)
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
		if (transform.position.y < -1.0f)
		{
			transform.position = _spawnPos;
		}
	}

	private ContactFlags GetContactFlags(ContactPoint2D contact)
	{
		float dot = Vector3.Dot(Vector2.up, contact.normal);
		if (dot >= (1.0f - float.Epsilon))
			return ContactFlags.Ground;
		else if (dot <= float.Epsilon)
			return (contact.normal.x > 0.0f) ? ContactFlags.LeftWall : ContactFlags.RightWall;
		else if (dot <= (-1.0f - float.Epsilon))
			return ContactFlags.Ceiling;
		else
			return ContactFlags.Unknown;
	}
}
