using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MixedSimulation : MonoBehaviour
{
    public class Node
    {
        public int index;
        public Vector3 position; // The real current position
        public Vector3 velocity; // The real to-move
        public Vector3 externalForces; // Gathered from outside sources, such as the player and gravity
        public float mass; // Should be irrelivant if all particles have the same mass
        public float inverseMass; // 1/mass for efficiency
        public Vector3 initialPredictedPos;
        public Vector3 predictedPosition; // Where the particle will move to
        public Vector3 prevPredicted;
        public Vector3 correctedDisplacement; // The distance that will be moved
        public Vector3 relUp = Vector3.up; // The relative up vector, make sure to change this as a node moves
        public List<Node> nearby = new List<Node>();
        public List<Node> nearbyForMesh = new List<Node>();
        public List<Node> nearbySurface = new List<Node>();
        public List<Node> nearbySurfaceForMesh = new List<Node>();
        public int surfaceIndex;
        public Vector3 normal = Vector3.zero;
        public int normalCount = 0;
        //public Vector3 normalSum = Vector3.zero;
        public bool queued = false;
        public int triangles = 0;
        public bool edgesRendered = false;

        public Node(Vector3 p, float m, int i)
        {
            position = p;
            mass = m;
            inverseMass = 1 / mass;
            index = i;
        }
    }

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

    Vector3 np1;
    Vector3 np2;

    public enum StretchType { Rigid, Exponential, Hyperbolic }
    public enum CompressionType { Rigid, AdvancedRigid }

    public Node[] nodes { get; private set; }
    private List<Constraint> internalConstraints = new List<Constraint>();
    private List<Constraint> externalConstraints = new List<Constraint>();
    private StaticSurface[] surfaces;
    private Vector3 offset;

    [Header("Material Settings")]
    [Tooltip("Will always start as a cube")]
    public int dim = 10;
    public StretchType stretchType = StretchType.Rigid;
    public CompressionType compressionType = CompressionType.Rigid;
    [Range(0, 1)]
    [Tooltip("Stiffness")]
    public float stiffness = .5f;
    public int iterationsToRestore = 8;
    [Range(0,1)]
    public float bounciness = .5f;
    [Range(0, 1)]
    public float pliability;
    public float minDist = .05f;
    [Tooltip("Mass per node")]
    public float mass = 1;
    public Vector3 gravity = new Vector3(0, -1, 0);
    public float distBetween = .1f;
    [Tooltip("Simulation accuracy")]
    public int iterations = 2;
    [Range(.00001f, .01f)]
    public float forceNormalThreshold = .005f;
    [Range(1, 26)]
    public int maxRecieved = 26;
    [Range(1, 90)]
    public float angleOfTransfer = 50f;

    [Header("Gizmo Settings")]
    [SerializeField] bool DrawAllNodes;
    [SerializeField] bool DrawSurfaceNodes;
    [SerializeField] bool DrawEdgeMesh;

    Queue<Node> normalQueue = new Queue<Node>(); // Add forces to this
    public List<Node> surfaceNodes = new List<Node>();
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    List<Tri> tempTri = new List<Tri>();
    List<Edge> edgeCount = new List<Edge>();

    private void Start()
    {
        surfaces = GameObject.FindObjectsOfType<StaticSurface>();
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Generate();
        GenerateInternalConstraints();
        GenerateExternalConstraints();
        GenerateMesh();
    }

    /// <summary>
    /// Spawns the object as a cube
    /// </summary>
    private void Generate()
    {
        nodes = new Node[dim * dim * dim];
        offset = new Vector3(distBetween * (dim - 1), distBetween * (dim-1), distBetween * (dim - 1))/2;
        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                for (int z = 0; z < dim; z++)
                {
                    int index = x + dim * (y + dim * z);
                    nodes[index] = new Node(new Vector3(distBetween * x, distBetween * y, distBetween * z) - offset + transform.position, mass, index);
                }
            }
        }
    }

    /// <summary>
    /// Generates the internal constraints
    /// </summary>
    private void GenerateInternalConstraints()
    {
        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                for (int z = 0; z < dim; z++)
                {
                    int index = x + dim * (y + dim * z);
                    Node n = nodes[index];

                    #region constraints using nearby nodes
                    for (int i = -1; i <= 1; i++)
                    {
                        if (x + i < 0 || x + i > dim - 1) continue;
                        for (int j = -1; j <= 1; j++)
                        {
                            if (y + j < 0 || y + j > dim - 1) continue;
                            for (int k = -1; k <= 1; k++)
                            {
                                if (z + k < 0 || z + k > dim - 1 || (i == 0 && j == 0 && k == 0)) continue;
                                int nearIndex = (x + i) + dim * ((y + j) + dim * (z + k));
                                Node nearby = nodes[nearIndex];
                                n.nearby.Add(nearby);
                                if((i != 0 && (j == 0 && k == 0)) || (j != 0 && (i == 0 && k == 0)) || (k != 0 && (j == 0 && i == 0)))
                                {
                                    n.nearbyForMesh.Add(nearby);
                                }

                                internalConstraints.Add(new StretchConstraint(this, index, nearIndex));
                                internalConstraints.Add(new CompressionConstraint(this, index, nearIndex));
                                internalConstraints.Add(new BendingConstraint(this, index, nearIndex));

                            }
                        }
                    }
                    if (n.nearby.Count < 26)
                    {
                        surfaceNodes.Add(n);
                        n.surfaceIndex = surfaceNodes.Count - 1;
                    }
                    #endregion
                }
            }
        }
    }

    private void GenerateMesh()
    {
        int i = 0;
        vertices = new Vector3[surfaceNodes.Count];
        tempTri = new List<Tri>();
        edgeCount = new List<Edge>();

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
        triangles = new int[tempTri.Count*3];
        for(int x = 0; x < tempTri.Count; x++)
        {
            flatVerts.Add(vertices[tempTri[x].i1]);
            triangles[x*3] = flatVerts.Count - 1;
            flatVerts.Add(vertices[tempTri[x].i2]);
            triangles[x*3+1] = flatVerts.Count - 1;
            flatVerts.Add(vertices[tempTri[x].i3]);
            triangles[x*3+2] = flatVerts.Count - 1;
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

    private void FixedUpdate()
    {
        UpdateMesh();
        ResetNodes();
        PropagateQueuedForces();
        Simulate();
        Debug.Log(VolumeOfMesh(mesh));
    }

    void UpdateMesh()
    {
        int i = 0;
        foreach(Node n in surfaceNodes)
        {
            vertices[i] = n.position - transform.position;
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

    private void ResetNodes()
    {
        foreach (Node n in nodes)
        {
            n.externalForces = Vector3.zero;
            n.correctedDisplacement = Vector3.zero;
        }
    }

    /// <summary>
    /// Moves the particles
    /// </summary>
    private void Simulate()
    {
        // Update the velocity and predicted positions of particles
        foreach (Node n in nodes)
        {
            n.externalForces += gravity;
            if (n.normalCount > 0)
                n.normal /= n.normalCount;
            n.correctedDisplacement += Time.fixedDeltaTime * n.normal / iterations;
            n.velocity += Time.fixedDeltaTime * n.inverseMass * n.externalForces;
            n.initialPredictedPos = n.predictedPosition = n.position + Time.fixedDeltaTime * n.velocity;
            n.velocity = Vector3.zero;
            n.prevPredicted = n.position;
            n.normalCount = 0;
            n.normal = Vector3.zero;
            //n.normalSum = Vector3.zero;
        }

        // Adjust the predicted positions based on constraints
        for (int i = 0; i < iterations; i++)
        {
            ProjectConstraints(1f / iterations);
        }
        foreach (Constraint c in externalConstraints)
        {
            c.ConstrainPositions(0f);
        }
        // Update the real velocity and positions
        foreach (Node n in nodes)
        {
            n.velocity += (n.predictedPosition - n.position) / Time.fixedDeltaTime;
            n.position = n.predictedPosition;
        }
        /*foreach (Constraint c in internalConstraints)
        {
            c.UpdateInitial();
        }*/
    }

    private void GenerateExternalConstraints()
    {
        if (surfaces == null) return;
        externalConstraints.Clear();
        foreach (StaticSurface s in surfaces)
        {
            foreach (Node n in nodes)
            {
                StaticConstraint sc = s.GenerateConstraint(this, n.position, n.index);
                if (sc != null) externalConstraints.Add(sc);
            }
        }
    }

    /// <summary>
    /// Adjusts the predicted positions to be as close as possible to meeting constraint criteria
    /// </summary>
    private void ProjectConstraints(float di)
    {
        foreach (Constraint c in internalConstraints)
        {
            c.ConstrainPositions(di);
        }
        
        foreach (Node n in nodes)
        {
            n.predictedPosition += n.correctedDisplacement * di;
            n.correctedDisplacement = Time.fixedDeltaTime * n.normal * di;
        }
    }

    public void AddForce(Node n, Vector3 force)
    {
        n.normal += force;
        n.normalCount++;
        if (n.queued || force.magnitude > forceNormalThreshold) return; // Needed because of iterations
        n.queued = true;
        normalQueue.Enqueue(n);
    }

    private void PropagateQueuedForces()
    {
        int trials = 0;
        while (normalQueue.Count > 0 && trials < 10000)
        {
            Node current = normalQueue.Dequeue();
            trials++;
            current.queued = false;
            
            foreach (Node near in current.nearby)
            {
                Vector3 dir = (near.position - current.position).normalized;
                float angle = Vector3.Angle(dir, current.normal);
                if (angle > angleOfTransfer) continue;

                Vector3 projectedForceNormal = Vector3.Project(current.normal / current.normalCount, dir);

                if (projectedForceNormal.magnitude > forceNormalThreshold && near.normalCount < maxRecieved)
                {
                    near.normal += projectedForceNormal;
                    near.normalCount++;

                    if (!near.queued)
                    {
                        near.queued = true;
                        normalQueue.Enqueue(near);
                    }
                }
            }
        }
    }

    public Node ClosestPointToRay(Ray ray, float threshold)
    {
        Node closest = null;
        float closestDist = threshold;
        float closestOrigin = Mathf.Infinity;
        foreach(Node n in surfaceNodes)
        {
            float distToRay = Vector3.Distance(Vector3.Project(n.position - ray.origin, ray.direction) + ray.origin, n.position);
            float distToOrigin = Vector3.Distance(n.position, ray.origin);
            if (distToRay < threshold && distToOrigin < closestOrigin)
            {
                closestOrigin = distToOrigin;
                closest = n;
            }
        }
        return closest;
    }

    public static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(p1, Vector3.Cross(p2,p3)) / 6.0f;
    }

    public float VolumeOfMesh(Mesh mesh)
    {
        float sum = 0;
        int i = 0;
        sum += SignedVolumeOfTriangle(mesh.vertices[mesh.triangles[i++]], mesh.vertices[mesh.triangles[i++]], mesh.vertices[mesh.triangles[i++]]);
        return Mathf.Abs(sum);
    }

    private void OnDrawGizmos()
    {
        if (nodes == null) return;
        Gizmos.color = Color.white;
        foreach (Node n in nodes)
        {
            if (DrawAllNodes)
            {
                Gizmos.DrawWireSphere(n.position, distBetween / 10);
            }
        }
        foreach(Node n in surfaceNodes)
        {
            if(DrawSurfaceNodes && !DrawAllNodes)
            {
                Gizmos.DrawWireSphere(n.position, distBetween / 10);
            }
            if (DrawEdgeMesh && vertices?.Length > 0)
            {
                foreach(Edge e in edgeCount)
                {
                    Gizmos.DrawLine(surfaceNodes[e.i1].position, surfaceNodes[e.i2].position);
                }
                /*foreach(Node ns in n.nearbySurfaceForMesh)
                {
                    Gizmos.DrawLine(n.position, ns.position);
                }*/
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(distBetween * (dim - 1), distBetween * (dim - 1), distBetween * (dim - 1)));
    }
}
