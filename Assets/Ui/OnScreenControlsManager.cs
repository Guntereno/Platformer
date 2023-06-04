using UnityEngine;

namespace Ui
{
	public class OnScreenControlsManager : MonoBehaviour
	{
		[SerializeField] private Transform _touchControlsRoot = null;

		private bool _hasTouchScreen = false;

		void Start()
		{
			if (SystemInfo.deviceType == DeviceType.Handheld)
			{
				if (Input.touchSupported)
				{
					_hasTouchScreen = true;
				}
			}

			_touchControlsRoot.gameObject.SetActive(_hasTouchScreen);
		}
	}
}
