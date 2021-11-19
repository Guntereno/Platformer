using UnityEngine;
using Momo.Core;
using UnityEngine.Tilemaps;

namespace Game
{

	[RequireComponent(typeof(Camera))]
	public class CameraController : MonoBehaviour
	{
		[SerializeField] Camera _camera = null;
		[SerializeField] PlayerController _target = null;
		[SerializeField] Tilemap _tilemap = null;

		[SerializeField] float _focusZoneOffsetX = 0.0f;
		[SerializeField] float _focusZoneWidth = 0.0f;
		[SerializeField] float _focusZoneOffsetY = 0.0f;
		[SerializeField] float _focusZoneHeight = 0.0f;
		[SerializeField] float _cameraLerpParam = 0.0f;

		[SerializeField] bool _scrollY = false;

		[SerializeField] float _recoilLerpParam = 0.0f;
		[SerializeField] float _recoilFactor = 0.0f;

		private float _depthPlane = 0.0f;
		private Vector2 _dollyPosition = Vector2.zero;
		private Vector2 _recoilOffset = Vector2.zero;
		private bool _headingRight = true;

		public Vector2 CameraPosition => _camera.transform.position;
		public Bounds ViewportBounds
		{
			get
			{
				float camHeight = _camera.orthographicSize;
				float camWidth = camHeight * _camera.aspect;
				Vector2 camPos = CameraPosition;
				return new Bounds
				{
					min = new Vector3(camPos.x - camWidth, camPos.y - camHeight, 0.0f),
					max = new Vector3(camPos.x + camWidth, camPos.y + camHeight, 0.0f)
				};
			}
		}

		public void ShotFired(Vector2 velocity)
		{
			_recoilOffset += -(velocity) * _recoilFactor;
		}

		public Bounds CalculateDollyBounds()
		{
			Bounds localBounds = _tilemap.localBounds;

			float camHeight = _camera.orthographicSize;
			float camWidth = camHeight * _camera.aspect;

			Vector3 camOffset = new Vector3(camWidth, camHeight, 0.0f);

			Bounds levelBounds = Singleton.Instance.LevelController.LevelBounds;
			return new Bounds()
			{
				min = levelBounds.min + camOffset,
				max = levelBounds.max - camOffset
			};
		}

		private void Start()
		{
			_dollyPosition = transform.position;
			_depthPlane = transform.position.z;
		}

		void Update()
		{
			GetHorizontalBounds(out float minX, out float maxX);

			Vector3 cameraTarget = transform.position;
			float targetPosX = _target.Position.x;
			if (targetPosX < minX)
			{
				if (_headingRight)
				{
					// Change heading, and move so character at front of zone
					_headingRight = false;
				}

				cameraTarget.x = targetPosX + _focusZoneOffsetX;
			}
			else if (targetPosX > maxX)
			{
				if (!_headingRight)
				{
					_headingRight = true;
				}

				cameraTarget.x = targetPosX - _focusZoneOffsetX;
			}

			if (_scrollY)
			{
				GetVerticalBounds(out float minY, out float maxY);

				float targetPosY = _target.Position.y;
				if ((targetPosY > maxY) && (_target.IsOnGround || _target.IsGrippingWall))
				{
					cameraTarget.y = targetPosY;
				}
				else if (targetPosY < minY)
				{
					cameraTarget.y = targetPosY;
				}
			}

			Bounds cameraBounds = CalculateDollyBounds();

			cameraTarget.x = Mathf.Clamp(
					cameraTarget.x,
					cameraBounds.min.x,
					cameraBounds.max.x);
			cameraTarget.y = Mathf.Clamp(
					cameraTarget.y,
					cameraBounds.min.y,
					cameraBounds.max.y);

			_dollyPosition = Vector3.Lerp(
				_dollyPosition,
				cameraTarget,
				_cameraLerpParam);

			_recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.zero, _recoilLerpParam);

			transform.position = ((Vector3)(_dollyPosition + _recoilOffset)).WithZ(_depthPlane);
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
			float screenMaxY = _camera.orthographicSize;
			float screenMinY = -screenMaxY;
			float screenMaxX = screenMaxY * _camera.aspect;
			float screenMinX = -screenMaxX;

			GetHorizontalBounds(out float minX, out float maxX);

			DrawBoundsGizmo(
				Color.cyan.WithA(0.2f),
				minX, maxX,
				transform.position.y + screenMinY,
				transform.position.y + screenMaxY);

			GetVerticalBounds(out float minY, out float maxY);

			DrawBoundsGizmo(
				Color.magenta.WithA(0.2f),
				transform.position.x + screenMinX,
				transform.position.x + screenMaxX,
				minY, maxY);
		}

		private void DrawBoundsGizmo(
			Color color,
			float minX, float maxX,
			float minY, float maxY)
		{
			Bounds bounds = new Bounds
			{
				min = new Vector3(minX, minY, 0.0f),
				max = new Vector3(maxX, maxY, 0.0f)
			};
			DrawBoundsGizmo(color, bounds);
		}

		private void DrawBoundsGizmo(
			Color color,
			Bounds bounds)
		{
			Gizmos.color = color;
			Gizmos.DrawCube(bounds.center, bounds.size);
		}
	}

}
