using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshGenerator
{
    public class Tri
    {
        public List<Tri> nextTo = new List<Tri>();
        public int i1;
        public int i2;
        public int i3;
        public Tri(int _i1, int _i2, int _i3)
        {
            i1 = _i1;
            i2 = _i2;
            i3 = _i3;
        }
        public bool Contains(int _i1, int _i2, int _i3)
        {
            if (!(i1 == _i1 || i1 == _i2 || i1 == _i3)) return false;
            if (!(i2 == _i1 || i2 == _i2 || i2 == _i3)) return false;
            if (!(i3 == _i1 || i3 == _i2 || i3 == _i3)) return false;
            return true;
        }
        public override string ToString()
        {
            return "i1:" + i1 + ", i2:" + i2 + ", i3:" + i3;
        }
    }

    public class Edge
    {
        public int i1;
        public int i2;
        public int amt;
        public Edge(int _i1, int _i2)
        {
            i1 = _i1;
            i2 = _i2;
            amt = 1;
        }
        public int Contains(int _i1, int _i2)
        {
            if (!(i1 == _i1 || i1 == _i2)) return 0;
            if (!(i2 == _i1 || i2 == _i2)) return 0;
            return amt;
        }
    }

    Mesh mesh;
    List<Node> surfaceNodes;
    Vector3[] vertices;
    int[] triangles;
    List<Tri> tempTri;
    List<Edge> edgeCount;

    public MeshGenerator(Mesh _mesh, List<Node> _surfaceNodes)
    {
        mesh = _mesh;
        surfaceNodes = _surfaceNodes;
        vertices = new Vector3[surfaceNodes.Count];
        tempTri = new List<Tri>();
        edgeCount = new List<Edge>();
    }

    public void GenerateMesh()
    {
        int i = 0;

        // Find all surface neighbors and rebuild vertices array
        foreach (Node n in surfaceNodes)
        {
            vertices[i] = n.position;
            n.nearbySurface.Clear();
            n.nearbySurface.AddRange(n.nearby.Intersect(surfaceNodes));
            n.nearbySurfaceForMesh.AddRange(n.nearbyForMesh.Intersect(surfaceNodes));
            i++;
        }

        // Choose the tri
        Node n1 = surfaceNodes[0];
        Node n2 = n1.nearbySurface[0];
        Node n3 = n1.nearbySurfaceForMesh[1];
        edgeCount.Add(new Edge(n1.surfaceIndex, n2.surfaceIndex));
        edgeCount.Add(new Edge(n2.surfaceIndex, n3.surfaceIndex));
        edgeCount.Add(new Edge(n3.surfaceIndex, n1.surfaceIndex));
        Tri tri = new Tri(n1.surfaceIndex, n2.surfaceIndex, n3.surfaceIndex);
        tempTri.Add(tri);
        GenerateMesh(n3, n2);

        //Debug.Log(why);
        List<Vector3> flatVerts = new List<Vector3>();
        triangles = new int[tempTri.Count * 3];
        for (int x = 0; x < tempTri.Count; x++)
        {
            flatVerts.Add(vertices[tempTri[x].i1]);
            triangles[x * 3] = flatVerts.Count - 1;
            flatVerts.Add(vertices[tempTri[x].i2]);
            triangles[x * 3 + 1] = flatVerts.Count - 1;
            flatVerts.Add(vertices[tempTri[x].i3]);
            triangles[x * 3 + 2] = flatVerts.Count - 1;
        }
        //vertices = flatVerts.ToArray();
    }

    private void GenerateMesh(Node n1, Node n2)
    {
        // n1 and n2 make up an edge that is used to determine a tirangle with orientation similar to the other tri the edge connects to
        // If this edge is already part of 2 triangles, do not make another tri
        Edge edge1 = edgeCount.Find(e => e.Contains(n1.surfaceIndex, n2.surfaceIndex) > 0);
        if (edge1 == null) edgeCount.Add(edge1 = new Edge(n1.surfaceIndex, n2.surfaceIndex));
        if (edge1.amt == 2) return;
        else edge1.amt++;

        // Make a triangle for each valid spot
        foreach (Node n3 in n2.nearbySurfaceForMesh.Intersect(n1.nearbySurface))
        {
            // to avoid inside tris, if 1 && 3 share a nearbyMeshSurface node that isnt 2
            // to avoid more than 2 tris in a square, if 1 && 3 share one that isnt 1 cancel
            // h1 and h2 make up the largest edge and n3 will always be h2 if possible
            Node h1 = n1;
            Node h2 = n2;
            Node h3 = n3;
            float d12 = Vector3.Distance(n1.position, n2.position);
            float d23 = Vector3.Distance(n2.position, n3.position);
            float d31 = Vector3.Distance(n3.position, n1.position);
            if (d23 >= d12 && d23 >= d31)
            {
                h1 = n2;
                h2 = n3;
                h3 = n1;
            }
            if (d31 >= d23 && d31 >= d12)
            {
                h1 = n1;
                h2 = n3;
                h3 = n2;
            }

            if (h1.nearbySurfaceForMesh.Intersect(h2.nearbySurfaceForMesh).Count() < 2) continue;
            List<Node> intersects = h1.nearbySurfaceForMesh.Intersect(h2.nearbySurfaceForMesh).ToList();
            if (tempTri.Exists(t => t.Contains(h1.surfaceIndex, intersects[0].surfaceIndex, intersects[1].surfaceIndex) || t.Contains(h2.surfaceIndex, intersects[0].surfaceIndex, intersects[1].surfaceIndex))) continue;
            if (!tempTri.Exists(t => t.Contains(n1.surfaceIndex, n2.surfaceIndex, n3.surfaceIndex)))
            {
                // Update edge count
                Edge edge2 = edgeCount.Find(e => e.Contains(n2.surfaceIndex, n3.surfaceIndex) > 0);
                Edge edge3 = edgeCount.Find(e => e.Contains(n3.surfaceIndex, n1.surfaceIndex) > 0);
                if (edge2 == null) edgeCount.Add(edge2 = new Edge(n2.surfaceIndex, n3.surfaceIndex));
                else edge2.amt++;
                if (edge3 == null) edgeCount.Add(edge3 = new Edge(n3.surfaceIndex, n1.surfaceIndex));
                else edge3.amt++;

                Tri tri = new Tri(n1.surfaceIndex, n2.surfaceIndex, n3.surfaceIndex);
                tempTri.Add(tri);

                //GenerateMesh(n1, n2);
                GenerateMesh(n3, n2);
                GenerateMesh(n1, n3);
            }
        }
    }

    public void UpdateMesh(Transform objectTrans)
    {
        int i = 0;
        foreach (Node n in surfaceNodes)
        {
            vertices[i] = n.position - objectTrans.position;
            i++;
        }
        List<Vector3> flatVerts = new List<Vector3>();
        for (int x = 0; x < tempTri.Count; x++)
        {
            flatVerts.Add(vertices[tempTri[x].i1]);
            flatVerts.Add(vertices[tempTri[x].i2]);
            flatVerts.Add(vertices[tempTri[x].i3]);
        }
        //vertices = flatVerts.ToArray();
        mesh.Clear();
        mesh.vertices = flatVerts.ToArray();
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    public List<Edge> GetEdges()
    {
        return edgeCount;
    }
}
