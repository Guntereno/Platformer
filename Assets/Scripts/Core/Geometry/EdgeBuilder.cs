using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Momo.Core.Geometry
{
	public struct Edge
	{
		public Vector3 Start;
		public Vector3 End;
	}

	public class EdgeBuilder
	{
		private readonly Mesh _mesh = null;
		private HashSet<MeshEdge> _edges = new HashSet<MeshEdge>();

		private struct MeshEdge
		{
			public MeshEdge(int a, int b)
			{
				Start = a;
				End = b;
			}

			public int Start { get; private set; }
			public int End { get; private set; }

			public MeshEdge Reversed
			{
				get => new MeshEdge(End, Start);
			}
		}

		public EdgeBuilder(Mesh mesh)
		{
			_mesh = mesh;

			const int vertsInTriangle = 3;
			int numTriangles = _mesh.triangles.Length / vertsInTriangle;
			for (int triangleIndex = 0; triangleIndex < numTriangles; ++triangleIndex)
			{
				int firstIndex = triangleIndex * vertsInTriangle;

				for (int edgeIndex = 0; edgeIndex < vertsInTriangle; ++edgeIndex)
				{
					int indexA = _mesh.triangles[firstIndex + edgeIndex];
					int indexB = _mesh.triangles[firstIndex + ((edgeIndex + 1) % vertsInTriangle)];

					MeshEdge meshEdge = new MeshEdge(indexA, indexB);
					_edges.Add(meshEdge);
				}
			}
		}

		private static Edge MeshEdgeToEdge(Mesh mesh, MeshEdge meshEdge)
		{
			return new Edge
			{
				Start = mesh.vertices[meshEdge.Start],
				End = mesh.vertices[meshEdge.End]
			};
		}

		public List<Edge> BuildEdgeList()
		{
			var innerEdges = _edges
				.Where(meshEdge => _edges.Contains(meshEdge.Reversed));

			return _edges
				.Except(innerEdges)
				.Select(me => MeshEdgeToEdge(_mesh, me))
				.ToList();
		}
	}
}
