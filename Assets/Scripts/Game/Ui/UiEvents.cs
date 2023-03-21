using UnityEngine.Events;

namespace Game.Ui
{
	public class Events
	{
		private UnityEvent<int> _healthUpdated = new UnityEvent<int>();

		public void AddHealthUpdatedListener(UnityAction<int> Listener)
		{
			_healthUpdated.AddListener(Listener);
		}

		public void RemoveHealthUpdatedListener(UnityAction<int> Listener)
		{
			_healthUpdated.RemoveListener(Listener);
		}

		public void UpdateHealth(int health)
		{
			_healthUpdated.Invoke(health);
		}
	}
}
