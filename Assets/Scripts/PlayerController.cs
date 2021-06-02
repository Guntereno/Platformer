using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
	[SerializeField] private float _speed = 1.0f;
	[SerializeField] private float _inertiaDecay = 0.1f;
	[SerializeField] private float _deadzone = 0.2f;
	
	[SerializeField] private float _jumpImpulse = 4.0f;
	[SerializeField] private float _fallFactor = 0.0f;
	[SerializeField] private float _lowJumpFactor = 0.0f;
	[SerializeField] private float _coyoteTime = 0.0f;

	[SerializeField] private Transform _groundChecker = null;
	[SerializeField] private float _groundCheckerRadius = 1.0f;
	[SerializeField] private LayerMask _groundLayer = 0;

	private Vector2 _spawnPos = default;
	private Rigidbody2D _rigidBody = null;
	
	private Vector2 _moveVector = default;
	private bool _jumpHeld = default;
	private bool _onGround = false;
	private float _lastTimeOnGround = float.MinValue;


	public bool OnGround => _onGround;
	public Vector2 Position => transform.position;

	private void Start()
	{
		_spawnPos = transform.position;
		_rigidBody = GetComponent<Rigidbody2D>();
	}

	private void Update()
	{
		UpdateGroundCheck();
		UpdateMovement();
		UpdateJump();
		UpdateFallDeath();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(
			_groundChecker.position,
			_groundCheckerRadius);
	}

	private void UpdateGroundCheck()
	{
		Collider2D collider =
			Physics2D.OverlapCircle(
				_groundChecker.position,
				_groundCheckerRadius,
				_groundLayer);

		if (collider != null)
		{
			_onGround = true;

			_lastTimeOnGround = Time.time;
		}
		else
		{
			_onGround = false;
		}
	}

	private void UpdateMovement()
	{
		float xVelocity;
		if (Mathf.Abs(_moveVector.x) > _deadzone)
		{
			xVelocity = ((_moveVector.x > 0.0f) ? 1.0f : -1.0f) * _speed;
		}
		else
		{
			// Maintain velocity if in air
			if(!_onGround)
				return;

			// Lerp towards stationary
			xVelocity = Mathf.Lerp(
				_rigidBody.velocity.x,
				0.0f,
				_inertiaDecay);
			const float epsilon = 0.1f;

			if(Mathf.Abs(xVelocity) <= epsilon)
			{
				xVelocity = 0.0f;
			}
		}
		 
		_rigidBody.velocity =
			new Vector2(xVelocity, _rigidBody.velocity.y);
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

	private void OnMove(InputValue input)
	{
		_moveVector = input.Get<Vector2>();
	}

	private void OnJump(InputValue input)
	{
		bool jumpHeld = input.Get<float>() > 0.0f;

		bool canJump = _onGround || InCoyoteTime();
		if (canJump && (!_jumpHeld && jumpHeld))
		{
			ApplyJumpImpuse();
		}

		_jumpHeld = jumpHeld;
	}

	private bool InCoyoteTime()
	{
		return (Time.time - _lastTimeOnGround) < _coyoteTime;
	}

	private void UpdateFallDeath()
	{
		if(transform.position.y < -1.0f)
		{
			transform.position = _spawnPos;
		}
	}
}
