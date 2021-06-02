using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
	[SerializeField] PlayerController _target;

	[SerializeField] float _focusZoneOffsetX;
	[SerializeField] float _focusZoneWidth;
	[SerializeField] float _focusZoneOffsetY;
	[SerializeField] float _focusZoneHeight;
	[SerializeField] float _cameraLerpParam;

	[SerializeField] bool _scrollY;

	private float _initialY = 0.0f;

	bool _headingRight = true;

	private Camera _camera;
	private Camera Camera
	{
		get
		{
			if(_camera == null)
			{
				_camera = GetComponent<Camera>();
			}
			return _camera;
		}
	}

	private void Start()
	{
		_initialY = transform.position.y;
	}

	void Update()
	{
		GetHorizontalBounds(out float minX, out float maxX);

		Vector3 cameraTarget = transform.position;
		float targetPosX = _target.Position.x;
		if(targetPosX < minX)
		{
			if(_headingRight)
			{
				// Change heading, and move so character at front of zone
				_headingRight = false;
			}

			cameraTarget.x = targetPosX + _focusZoneOffsetX;
		}
		else if(targetPosX > maxX)
		{
			if(!_headingRight)
			{
				_headingRight = true;
			}

			cameraTarget.x = targetPosX - _focusZoneOffsetX;
		}


		if(_scrollY)
		{
			GetVerticalBounds(out float minY, out float maxY);

			float targetPosY = _target.Position.y;
			if((targetPosY > maxY) && (_target.OnGround))
			{
				cameraTarget.y = targetPosY;
			}
			else if (targetPosY < minY)
			{
				cameraTarget.y = targetPosY;

				if(cameraTarget.y < _initialY)
				{
					cameraTarget.y = _initialY;
				}
			}
		}

		transform.position = Vector3.Lerp(
			transform.position,
			cameraTarget,
			_cameraLerpParam);
	}

	private void GetHorizontalBounds(out float min, out float max)
	{
		max = _focusZoneOffsetX;
		min = max - _focusZoneWidth;

		if (!_headingRight)
		{
			float temp = -max;
			max = -min;
			min = temp;
		}

		max += transform.position.x;
		min += transform.position.x;
	}

	private void GetVerticalBounds(out float min, out float max)
	{
		max = _focusZoneOffsetY;
		min = max - _focusZoneHeight;

		max += transform.position.y;
		min += transform.position.y;
	}

	private void OnDrawGizmos()
	{
		float screenMaxY = Camera.orthographicSize;
		float screenMinY = -screenMaxY;
		float screenMaxX = screenMaxY * Camera.aspect;
		float screenMinX = -screenMaxX;

		GetHorizontalBounds(out float minX, out float maxX);

		DrawBoundsGizmo(
			Color.cyan.WithA(0.2f),
			minX, maxX,
			screenMinY, screenMaxY);

		GetVerticalBounds(out float minY, out float maxY);

		DrawBoundsGizmo(
			Color.magenta.WithA(0.2f),
			screenMinX, screenMaxX,
			minY, maxY);
	}

	private void DrawBoundsGizmo(
		Color color,
		float minX, float maxX,
		float minY, float maxY)
	{
		Vector3 center = new Vector3(
			Mathf.Lerp(minX, maxX, 0.5f),
			Mathf.Lerp(minY, maxY, 0.5f));

		Vector3 size = new Vector3(
			maxX - minX,
			maxY - minY);

		Gizmos.color = color;
		Gizmos.DrawCube(center, size);
	}
}
