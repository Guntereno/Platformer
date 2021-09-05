using UnityEngine;

namespace Game.Guns
{
	abstract class Weapon: MonoBehaviour
	{
		public abstract void OnFire(bool firePressed, out Vector2 recoil);
	}
}
