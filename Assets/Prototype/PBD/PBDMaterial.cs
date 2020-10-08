using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PBDMaterial : MonoBehaviour
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
        public Vector3 correctedDisplacement; // The distance that will be moved

        public Node(Vector3 p, float m, int i)
        {
            position = p;
            mass = m;
            inverseMass = 1 / mass;
            index = i;
        }
    }

    public Node[] nodes { get; private set; }
    private List<Constraint> internalConstraints = new List<Constraint>();
    private List<Constraint> externalConstraints = new List<Constraint>();
    private StaticSurface[] surfaces;
    private Vector3 offset;

    [Header("Material Settings")]
    [Tooltip("Will always start as a cube")]
    public int dim = 10;
    [Range(0, 1)]
    [Tooltip("Stiffness")]
    public float stiffness = .5f;
    [Tooltip("Mass per node")]
    public float mass = 1;
    public Vector3 gravity = new Vector3(0, -1, 0);
    public float distBetween = .1f;
    [Tooltip("Simulation accuracy")]
    public int iterations = 2;

    private void Awake()
    {
        surfaces = GameObject.FindObjectsOfType<StaticSurface>();
        Generate();
        GenerateInternalConstraints();
    }

    /// <summary>
    /// Spawns the object as a cube
    /// </summary>
    private void Generate()
    {
        nodes = new Node[dim*dim*dim];
        offset = new Vector3(.1f * dim, .1f * dim, .1f * dim) / 2;
        for(int x = 0; x < dim; x++)
        {
            for(int y = 0; y < dim; y++)
            {
                for (int z = 0; z < dim; z++)
                {
                    int index = x + dim * (y + dim * z);
                    nodes[index] = new Node(new Vector3(distBetween * x, distBetween * y, distBetween * z) - offset, mass, index);
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

                    for (int i = -1; i <= 1; i++)
                    {
                        if (x + i < 0 || x + i > dim - 1) continue;
                        for (int j = -1; j <= 1; j++)
                        {
                            if (y + j < 0 || y + j > dim - 1) continue;
                            for (int k = -1; k <= 1; k++)
                            {
                                if (z + k < 0 || z + k > dim - 1 || (i == 0 && j == 0 && k == 0)) continue;
                                int nearIndex = (x+i) + dim * ((y+j) + dim * (z+k));
                                Node nearby = nodes[nearIndex];
                                //internalConstraints.Add(new StretchConstraint(this, index, nearIndex));
                            }
                        }
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        GenerateExternalConstraints();
        Simulate();
    }

    /// <summary>
    /// Moves the particles
    /// </summary>
    private void Simulate()
    {
        // Update the velocity and predicted positions of particles
        foreach(Node n in nodes)
        {
            n.externalForces += gravity * Time.fixedDeltaTime;
            n.velocity += Time.fixedDeltaTime * n.inverseMass * n.externalForces;
            n.predictedPosition = n.position + Time.fixedDeltaTime * n.velocity;
        }

        // Adjust the predicted positions based on constraints
        for(int i = 0; i < iterations; i++)
        {
            ProjectConstraints(1/iterations);
        }

        // Update the real velocity and positions
        foreach (Node n in nodes)
        {
            n.velocity = (n.predictedPosition - n.position) / Time.fixedDeltaTime;
            n.position = n.predictedPosition;
        }
    }

    private void GenerateExternalConstraints()
    {
        externalConstraints.Clear();
        foreach(StaticSurface s in surfaces)
        {
            foreach (Node n in nodes)
            {
                //StaticConstraint sc = s.GenerateConstraint(this, n.position, n.index);
                //if (sc != null) externalConstraints.Add(sc);
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
            //Debug.Log(c);
        }
        foreach (Node n in nodes)
        {
            n.predictedPosition += n.correctedDisplacement;
        }
        foreach (Constraint c in externalConstraints)
        {
            c.ConstrainPositions(di);
        }
    }

    private void OnDrawGizmos()
    {
        if (nodes == null) return;
        foreach(Node n in nodes)
        {
            Gizmos.DrawWireSphere(n.position, distBetween/2);
        }
    }
}
