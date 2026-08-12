using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshEdgeOutline : MonoBehaviour
{
    [Header("Outline")]
    public Material outlineMaterial;
    [Min(0.0001f)]
    public float thickness = 0.02f;

    [Header("Edge")]
    public bool includeInternalEdges = true;

    private MeshFilter meshFilter;
    private GameObject outlineObject;

    private void Awake()
    {
        BuildOutline();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            BuildOutline();
    }

    public void BuildOutline()
    {
        ClearOutline();

        meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Mesh sourceMesh = meshFilter.sharedMesh;

        Vector3[] vertices = sourceMesh.vertices;
        int[] triangles = sourceMesh.triangles;

        // Tìm toàn bộ edge duy nhất
        HashSet<Edge> edges = new HashSet<Edge>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            edges.Add(new Edge(a, b));
            edges.Add(new Edge(b, c));
            edges.Add(new Edge(c, a));
        }

        // Tạo mesh line
        List<Vector3> outlineVertices = new List<Vector3>();
        List<int> outlineTriangles = new List<int>();

        foreach (Edge edge in edges)
        {
            Vector3 a = vertices[edge.a];
            Vector3 b = vertices[edge.b];

            AddEdgeQuad(
                a,
                b,
                thickness,
                outlineVertices,
                outlineTriangles
            );
        }

        Mesh outlineMesh = new Mesh();
        outlineMesh.name = sourceMesh.name + "_EdgeOutline";

        outlineMesh.SetVertices(outlineVertices);
        outlineMesh.SetTriangles(outlineTriangles, 0);

        outlineMesh.RecalculateNormals();
        outlineMesh.RecalculateBounds();

        // GameObject outline
        outlineObject = new GameObject("Edge Outline");
        outlineObject.transform.SetParent(transform, false);

        MeshFilter mf = outlineObject.AddComponent<MeshFilter>();
        MeshRenderer mr = outlineObject.AddComponent<MeshRenderer>();

        mf.sharedMesh = outlineMesh;
        mr.sharedMaterial = outlineMaterial;
    }

    private void AddEdgeQuad(
        Vector3 a,
        Vector3 b,
        float width,
        List<Vector3> vertices,
        List<int> triangles)
    {
        Vector3 direction = (b - a).normalized;

        // Tạo một hướng vuông góc ổn định
        Vector3 side;

        if (Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f)
            side = Vector3.Cross(direction, Vector3.up).normalized;
        else
            side = Vector3.Cross(direction, Vector3.right).normalized;

        side *= width * 0.5f;

        int index = vertices.Count;

        vertices.Add(a - side);
        vertices.Add(a + side);
        vertices.Add(b + side);
        vertices.Add(b - side);

        triangles.Add(index + 0);
        triangles.Add(index + 1);
        triangles.Add(index + 2);

        triangles.Add(index + 0);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    private void ClearOutline()
    {
        if (outlineObject != null)
        {
            if (Application.isPlaying)
                Destroy(outlineObject);
            else
                DestroyImmediate(outlineObject);
        }
    }

    private struct Edge
    {
        public int a;
        public int b;

        public Edge(int a, int b)
        {
            if (a < b)
            {
                this.a = a;
                this.b = b;
            }
            else
            {
                this.a = b;
                this.b = a;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge))
                return false;

            Edge other = (Edge)obj;

            return a == other.a && b == other.b;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (a * 397) ^ b;
            }
        }
    }
}