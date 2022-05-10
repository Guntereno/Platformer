using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Momo.Core.Geometry;

public class LevelController : MonoBehaviour
{
	[SerializeField]
	Tilemap _tilemap = null;

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

	public List<Edge> Edges;

	public void Awake()
	{
		// BuildEdgeList();
	}

	public void Update()
	{
		// DebugRenderEdges();
	}

	private void BuildEdgeList()
	{
		var collider = _tilemap.gameObject.GetComponent<TilemapCollider2D>();
		Mesh mesh = collider.CreateMesh(true, true);
		EdgeBuilder edgeBuilder = new EdgeBuilder(mesh);
		Edges = edgeBuilder.BuildEdgeList();
	}

	private void DebugRenderEdges()
	{
		if (Edges == null)
		{
			return;
		}

		foreach(Edge edge in Edges)
		{
			Vector2 Start = edge.Start;
			Vector2 End = edge.End;
			Debug.DrawLine(Start, End, Color.yellow);

			Vector2 Dir = (End - Start).normalized;
			Vector2 Normal = Vector2.Perpendicular(Dir) * 0.2f;
			Debug.DrawLine(End, End + Normal);
		}
	}
}
