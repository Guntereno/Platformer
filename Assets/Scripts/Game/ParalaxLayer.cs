using UnityEngine;

namespace Game
{
	public class ParalaxLayer : MonoBehaviour
	{
		[SerializeField] private float _speed = 0.0f;

		private Bounds _bounds;
		private Vector2 _startPos;

		void Awake()
		{
			_startPos = transform.position;
			_bounds = GetComponent<SpriteRenderer>().bounds;
		}

		void Update()
		{
			Bounds viewPortBounds = Singleton.Instance.CameraController.ViewportBounds;
			Vector2 camPos = viewPortBounds.center;

			float xOffset = (camPos.x * _speed);
			float yOffset = (camPos.y * _speed);

			//TODO: Adjust xOffset to allow for looping

			transform.position = new Vector3(
				_startPos.x + xOffset,
				_startPos.y + yOffset,
				transform.position.z);
		}
	}
}
