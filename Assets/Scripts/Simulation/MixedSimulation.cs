using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MixedSimulation : MonoBehaviour
{
    
    
    public Node[] nodes { get; private set; }
    private List<Constraint> internalConstraints = new List<Constraint>();
    private List<Constraint> externalConstraints = new List<Constraint>();
    private Vector3 offset;

    [Header("Material Settings")]
    [Tooltip("Will always start as a cube")]
    [Range(1,5)]
    public int dim = 5;
    public float distBetween = .1f;
    public float minDist = .05f;
    public float mass = 1;
    [Space]
    [Range(0, 1)]
    [Tooltip("Stiffness")]
    public float stiffness = .8f;
    [Range(0, 1)]
    public float pliability = .2f;
    [Range(0, 1)]
    public float pliability2 = 10;
    [Range(0,1)]
    public float bounciness = 0f;

    [Header("Simulation Settings")]
    public Vector3 gravity = new Vector3(0, -1, 0);
    public int iterations = 2;
    [Range(.00001f, .01f)]
    public float forceNormalThreshold = .005f;
    [Range(1, 26)]
    public int maxRecieved = 26;
    [Range(1, 90)]
    public float angleOfTransfer = 70f;

    [Header("Gizmo Settings")]
    [SerializeField] bool DrawAllNodes = false;
    [SerializeField] bool DrawSurfaceNodes = false;
    [SerializeField] bool DrawEdgeMesh = false;
    [SerializeField] bool ShowNodeUps = false;

    Queue<Node> normalQueue = new Queue<Node>(); // Add forces to this
    public List<Node> surfaceNodes = new List<Node>();
    MeshGenerator meshGenerator;
    Mesh mesh;
    
    float volume = 0f;
    int connections = 0;
    [HideInInspector]
    public float volumeCorrection = 0;
    float inverseNodeCount = 0;
    float inverseVolume = 0;
    public Quaternion objectRotate = Quaternion.identity;

    private void Start()
    {
        Generate();
        GenerateInternalConstraints();
        GenerateExternalConstraints();

        mesh = new Mesh();
        meshGenerator = new MeshGenerator(mesh, surfaceNodes);
        GetComponent<MeshFilter>().mesh = mesh;
        meshGenerator.GenerateMesh();
        meshGenerator.UpdateMesh(transform);

        volume = VolumeOfMesh(mesh);
        inverseNodeCount = 1f / nodes.Length;
        inverseVolume = 1f / volume;
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
        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                for (int z = 0; z < dim; z++)
                {
                    int index = x + dim * (y + dim * z);
                    Node n = nodes[index];

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
                                connections++;
                                if ((i != 0 && (j == 0 && k == 0)) || (j != 0 && (i == 0 && k == 0)) || (k != 0 && (j == 0 && i == 0)))
                                {
                                    n.nearbyForMesh.Add(nearby);
                                }
                            }
                        }
                    }
                    if (n.nearby.Count < 26)
                    {
                        surfaceNodes.Add(n);
                        n.surfaceIndex = surfaceNodes.Count - 1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates the internal constraints
    /// </summary>
    private void GenerateInternalConstraints()
    {
        foreach(Node n in nodes)
        {
            internalConstraints.Add(new BendingConstraint(this, n.index));
            foreach (Node near in n.nearby)
            {
                // TODO: 1 constraint per node pair please
                internalConstraints.Add(new CompressionConstraint(this, n.index, near.index));
                internalConstraints.Add(new StretchConstraint(this, n.index, near.index));
                //internalConstraints.Add(new VolumeConstraint(this, n.index, near.index));
            }
        }
    }

    private void FixedUpdate()
    {
        meshGenerator.UpdateMesh(transform);
        ResetNodes();
        PropagateQueuedForces();
        Simulate();
        
    }

    private void ResetNodes()
    {
        foreach (Node n in nodes)
        {
            n.externalForces = Vector3.zero;
            n.correctedDisplacement = Vector3.zero;
            n.correctedRotation = Quaternion.identity;
            n.rotations = 0;
        }
    }

    private static float CubeRoot(float x)
    {
        if (x < 0)
            return -Mathf.Pow(-x, 1f/3f);
        else
            return Mathf.Pow(x, 1f / 3f);
    }

    /// <summary>
    /// Moves the particles
    /// </summary>
    private void Simulate()
    {
        //float currentVolume = VolumeOfMesh(mesh);
        //volumeCorrection = -CubeRoot(1 - volume / currentVolume) * inverseNodeCount; //Too inefficent :(

        // Update the velocity and predicted positions of particles
        foreach (Node n in nodes)
        {
            n.externalForces += gravity;
            if (n.normalCount > 0)
                n.normal /= n.normalCount;

            n.correctedDisplacement += Time.fixedDeltaTime * n.normal / iterations;
            n.velocity += Time.fixedDeltaTime * n.inverseMass * (n.externalForces);
            n.initialPredictedPos = n.predictedPosition = n.position + Time.fixedDeltaTime * n.velocity;
            n.velocity = Vector3.zero;
            n.prevPredicted = n.position;
            n.normalCount = 0;
            n.normal = Vector3.zero;

            n.offsetFromGoal = n.initialPredictedPos - n.position;
            n.offsetFromGoal *= pliability;
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

        // Update the real velocity, positions, and rotations
        foreach (Node n in nodes)
        {
            n.velocity += (n.predictedPosition - n.position) / Time.fixedDeltaTime;
            n.position = n.predictedPosition;
            int i = 0;
            foreach (Node near in n.nearby)
            {
                Vector3 currentDir = near.predictedPosition - n.predictedPosition;
                Vector3 initialDir = n.rotation * -n.nearInitialDirs[i];
                Quaternion rotate = Quaternion.FromToRotation(initialDir.normalized, currentDir.normalized);
                n.rotation *= rotate;
                i++;
            }
        }
    }

    public void GenerateExternalConstraints()
    {
        StaticSurface[] surfaces = GameObject.FindObjectsOfType<StaticSurface>();
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
        while(i < mesh.triangles.Length)
            sum += SignedVolumeOfTriangle(mesh.vertices[mesh.triangles[i++]], mesh.vertices[mesh.triangles[i++]], mesh.vertices[mesh.triangles[i++]]);
        return Mathf.Abs(sum);
    }

    public void ResetAll()
    {
        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                for (int z = 0; z < dim; z++)
                {
                    int index = x + dim * (y + dim * z);
                    nodes[index].Reset();
                    nodes[index].position = new Vector3(distBetween * x, distBetween * y, distBetween * z) - offset + transform.position;
                }
            }
        }
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
            if (ShowNodeUps)
            {
                Gizmos.DrawRay(n.position, n.rotation * Vector3.up);
            }
        }
        foreach(Node n in surfaceNodes)
        {
            if(DrawSurfaceNodes && !DrawAllNodes)
            {
                Gizmos.DrawWireSphere(n.position, distBetween / 10);
            }
            if (DrawEdgeMesh)
            {
                foreach(MeshGenerator.Edge e in meshGenerator.GetEdges())
                {
                    Gizmos.DrawLine(surfaceNodes[e.i1].position, surfaceNodes[e.i2].position);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(distBetween * (dim - 1), distBetween * (dim - 1), distBetween * (dim - 1)));
    }
}