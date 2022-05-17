
using UnityEngine;

namespace Momo.Core.Geometry
{
	public static class Cast
	{
		public static RaycastHit2D Box(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, bool debug = false)
		{
			var result = Physics2D.BoxCast(origin, size, angle, direction, distance, layerMask);


#if UNITY_EDITOR
			if (debug)
			{
				DrawDebug.BoxCast(origin, size, angle, direction, distance, result ? Color.green : Color.red);
			}
#endif

			return result;
		}
	}
}
