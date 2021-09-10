using UnityEngine;

namespace Game.Guns
{
	abstract class Weapon: MonoBehaviour
	{
		public abstract bool OnFire(bool firePressed, out Vector2 recoil);
	}
}
