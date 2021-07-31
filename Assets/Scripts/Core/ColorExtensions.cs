using UnityEngine;

namespace Momo.Core
{
	public static class ColorExtensions
	{
		public static Color WithR(this Color color, float val)
		{
			color.r = val;
			return color;
		}

		public static Color WithG(this Color color, float val)
		{
			color.g = val;
			return color;
		}

		public static Color WithB(this Color color, float val)
		{
			color.b = val;
			return color;
		}
		public static Color WithA(this Color color, float val)
		{
			color.a = val;
			return color;
		}
	}
}