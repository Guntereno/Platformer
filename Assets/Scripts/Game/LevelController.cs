using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelController : MonoBehaviour
{
    [SerializeField]
    Tilemap _tilemap;

    public Bounds LevelBounds
	{
        get
		{
            Bounds localBounds = _tilemap.localBounds;
            return new Bounds()
            {
                min = _tilemap.transform.TransformPoint(localBounds.min),
                max = _tilemap.transform.TransformPoint(localBounds.max)
            };
        }
	}
}
