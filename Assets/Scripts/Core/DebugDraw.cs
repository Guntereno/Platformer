using UnityEngine;
using Momo.Core.Geometry;

namespace Momo.Core
{
	public static class DebugDraw
	{
		public static void Circle(Vector2 pos, float radius, Color color, int segments = 24)
		{
			float thetaOffset = (Mathf.PI * 2.0f) / segments;

			float currentTheta = 0.0f;
			Vector2 currentPoint = PointOnCircle(pos, radius, currentTheta);
			
			for(int segment=0; segment<segments; ++segment)
			{
				currentTheta += thetaOffset;

				Vector2 nextPoint = PointOnCircle(pos, radius, currentTheta);
				Debug.DrawLine(currentPoint, nextPoint, color);

				currentPoint = nextPoint;
			}
		}

		public static void Arc(Vector2 pos, float thetaStart, float turnTheta, float radius, Color color, int segments = 12)
		{
			float thetaOffset = turnTheta / segments;

			float currentTheta = thetaStart;
			Vector2 currentPoint = PointOnCircle(pos, radius, currentTheta);

			for (int segment=1; segment<=segments; ++segment)
			{
				currentTheta += thetaOffset;
				
				Vector2 nextPoint = PointOnCircle(pos, radius, currentTheta);

				Debug.DrawLine(currentPoint, nextPoint, color);

				currentPoint = nextPoint;
			}
		}

		private static Vector2 PointOnUnitCircle(float theta)
		{
			return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
		}

		private static Vector2 PointOnCircle(Vector2 pos, float radius, float theta)
		{
			return pos + (PointOnUnitCircle(theta) * radius);
		}

		public static void Box(Box box, Color color)
		{
			Box(box.Origin, box.Size, color);
		}

		public static void Box(Vector2 origin, Vector2 size, Color color)
		{
			float halfX = size.x * 0.5f;
			float halfY = size.y * 0.5f;

			float top = origin.y + halfY;
			float right = origin.x + halfX;
			float bottom = origin.y - halfY;
			float left = origin.x - halfX;

			Vector2 topLeft = new Vector2(left, top);
			Vector2 topRight = new Vector2(right, top);
			Vector2 bottomRight = new Vector2(right, bottom);
			Vector2 bottomLeft = new Vector2(left, bottom);

			Debug.DrawLine(topLeft, topRight, color);
			Debug.DrawLine(topRight, bottomRight, color);
			Debug.DrawLine(bottomRight, bottomLeft, color);
			Debug.DrawLine(bottomLeft, topLeft, color);
		}

		public static void BoxCast(Vector2 origin, Vector2 size, Vector2 dir, float distance, Color color)
		{
			origin += dir * distance;
			Box(origin, size, color);
		}
	}
}
