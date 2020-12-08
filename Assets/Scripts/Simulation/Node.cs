using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    // Core information
    public int index;
    public int surfaceIndex;
    public float mass;
    public float inverseMass;
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 externalForces;
    public Vector3 relUp = Vector3.up;
    public Quaternion rotation = Quaternion.identity;
    public Vector3[] nearInitialDirs;

    // Force transfer
    public Vector3 normal = Vector3.zero;
    public int normalCount = 0;
    public bool queued = false;

    // Neighbor Lists
    public List<Node> nearby = new List<Node>();
    public List<Node> nearbyForMesh = new List<Node>();
    public List<Node> nearbySurface = new List<Node>();
    public List<Node> nearbySurfaceForMesh = new List<Node>();

    // Constraint information
    public Vector3 initialPredictedPos = Vector3.zero;
    public Vector3 predictedPosition = Vector3.zero;
    public Vector3 prevPredicted = Vector3.zero;
    public Vector3 correctedDisplacement = Vector3.zero;
    public Quaternion correctedRotation = Quaternion.identity;
    public int rotations = 0;
    public Vector3 offsetFromGoal = Vector3.zero;

    public Node(Vector3 p, float m, int i)
    {
        position = p;
        mass = m;
        inverseMass = 1 / mass;
        index = i;
    }

    public void Reset()
    {
        velocity = Vector3.zero;
        externalForces = Vector3.zero;
        relUp = Vector3.up;
        initialPredictedPos = Vector3.zero;
        predictedPosition = Vector3.zero;
        prevPredicted = Vector3.zero;
        correctedDisplacement = Vector3.zero;
        correctedRotation = Quaternion.identity;
        offsetFromGoal = Vector3.zero;
    }
}
