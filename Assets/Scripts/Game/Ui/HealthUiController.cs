using UnityEngine;

namespace Game.Ui
{
	public class HealthUiController : MonoBehaviour
	{
		[SerializeField] private SpriteRenderer _heartPrefab = null;
		[SerializeField] private Transform _rootNode = null;
		[SerializeField] private Sprite _fullHeartSprite = null;
		[SerializeField] private Sprite _emptyHeartSprite = null;

		private const int _maxHealth = 5;
		private SpriteRenderer[] _heartRenderers = null;

		protected void Start()
		{
			_heartRenderers = new SpriteRenderer[_maxHealth];
			for (int index=0; index < _maxHealth; ++index)
			{
				_heartRenderers[index] = GameObject.Instantiate<SpriteRenderer>(_heartPrefab, _rootNode);
				UpdateHeart(index, _maxHealth);
			}

			Singleton.Instance.UiEvents.AddHealthUpdatedListener(UpdateHealth);
		}

		protected void OnDestroy()
		{
			Singleton.Instance.UiEvents.RemoveHealthUpdatedListener(UpdateHealth);
		}

		public void UpdateHealth(int current)
		{
			if((current < 0) || (current >= _maxHealth))
			{
				Debug.LogWarning($"Specified health of '{current}' can not be displayed!");
				current = Mathf.Clamp(current, 0, (_maxHealth - 1));
			}

			for (int i = 0; i < _maxHealth; ++i)
			{
				UpdateHeart(i, current);
			}
		}

		private void UpdateHeart(int index, int health)
		{
			Sprite sprite = (index < health) ?
				_fullHeartSprite:
				_emptyHeartSprite;
			_heartRenderers[index].sprite = sprite;
		}
	}
}
