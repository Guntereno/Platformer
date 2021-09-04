using System.Text;
using UnityEngine;

namespace Core
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
	}
}
