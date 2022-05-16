using UnityEngine;

namespace Momo.Core.Geometry
{
	public struct Box
	{
		public Vector2 Size;
		public Vector2 Origin;

		public float Top => Origin.y + (Size.y * 0.5f);
		public float Right => Origin.x + (Size.x * 0.5f);
		public float Bottom => Origin.y - (Size.y * 0.5f);
		public float Left => Origin.x - (Size.x * 0.5f);
	}
}