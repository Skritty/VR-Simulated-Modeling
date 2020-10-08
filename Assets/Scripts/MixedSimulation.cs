using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public Vector3 predictedPosition; // Where the particle will move to
        public Vector3 prevPredicted;
        public Vector3 correctedDisplacement; // The distance that will be moved
        public Vector3 relUp = Vector3.up; // The relative up vector, make sure to change this as a node moves
        public List<Node> nearby = new List<Node>();
        public Vector3 normal = Vector3.zero;
        public int normalCount = 0;
        //public Vector3 normalSum = Vector3.zero;
        public bool queued = false;

        public Node(Vector3 p, float m, int i)
        {
            position = p;
            mass = m;
            inverseMass = 1 / mass;
            index = i;
        }
    }

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

    Queue<Node> normalQueue = new Queue<Node>(); // Add forces to this

    private void Start()
    {
        surfaces = GameObject.FindObjectsOfType<StaticSurface>();
        Generate();
        GenerateInternalConstraints();
        GenerateExternalConstraints();
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

                    //
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

                                internalConstraints.Add(new StretchConstraint(this, index, nearIndex));
                                internalConstraints.Add(new CompressionConstraint(this, index, nearIndex));
                                internalConstraints.Add(new BendingConstraint(this, index, nearIndex));
                                //internalConstraints.Add(new TestConstraint(this, index));

                            }
                        }
                    }
                    #endregion
                }
            }
        }
    }

    private void FixedUpdate()
    {
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
            n.predictedPosition = n.position + Time.fixedDeltaTime * n.velocity;
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

        // Update the real velocity and positions
        foreach (Node n in nodes)
        {
            n.velocity = (n.predictedPosition - n.position) / Time.fixedDeltaTime;
            n.position = n.predictedPosition;
        }
        foreach (StaticSurface s in surfaces)
        {
            s.previousPos = s.transform.position;
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
        //int why = 0;
        foreach (Constraint c in internalConstraints)
        {
            c.ConstrainPositions(di);//why++;
        }
        foreach (Constraint c in externalConstraints)
        {
            c.ConstrainPositions(di);
        }
        //Debug.Log(why);
        foreach (Node n in nodes)
        {
            n.prevPredicted = n.predictedPosition;
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

    private void OnDrawGizmos()
    {
        if (nodes == null) return;
        Vector3 prev = nodes[0].position;
        foreach (Node n in nodes)
        {
            Gizmos.DrawWireSphere(n.position, distBetween / 2);
            prev = n.position;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(distBetween * (dim - 1), distBetween * (dim - 1), distBetween * (dim - 1)));
    }
}
