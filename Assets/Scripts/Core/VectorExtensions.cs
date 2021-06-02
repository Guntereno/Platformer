using UnityEngine;

public static class VectorExtensions
{
	public static Vector2 WithX(this Vector2 vec, float val)
	{
		vec.x = val;
		return vec;
	}

	public static Vector2 WithY(this Vector2 vec, float val)
	{
		vec.y = val;
		return vec;
	}

	public static Vector3 WithX(this Vector3 vec, float val)
	{
		vec.x = val;
		return vec;
	}

	public static Vector3 WithY(this Vector3 vec, float val)
	{
		vec.y = val;
		return vec;
	}

	public static Vector3 WithZ(this Vector3 vec, float val)
	{
		vec.z = val;
		return vec;
	}
}
