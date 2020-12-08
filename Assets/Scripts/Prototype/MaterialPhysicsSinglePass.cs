using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Apply force normals and forces to individual nodes
/// Step 1: Propagate the normals throughout the simulation
/// Step 2: Apply forces to the nodes within those constraints
/// and the contraints between nodes and their neighbors
/// </summary>
public class MaterialPhysicsSinglePass : MonoBehaviour
{
    [System.Serializable]
    private class Node
    {
        public Vector2 position;
        public int index;
        public Connection[] nearby = new Connection[8];
        public Vector2 force = Vector2.zero;
        public int forceCount = 0;
        public Vector2 tempForce = Vector2.zero;
        public int tempForceCount = 0;
        public Vector2 forceCountVector = Vector2.zero;
        public Vector2 normal = Vector2.zero;
        public Vector2 normal2 = Vector2.zero;
        public int normalCount = 0;
        public bool queued = false;
        [System.Serializable]
        public class Connection
        {
            public Vector2 initialDir = new Vector2();
            public int index = -1;
        }
    }

    List<Node> surfaceNodes = new List<Node>();
    [SerializeField]
    Node[] allNodes;
    Queue<Node> normalQueue = new Queue<Node>(); // Add forces to this

    public int dimensions;
    [Range(.1f,1)]
    public float setDist;
    //[Range(.002f, .01f)]
    public float largestStep;
    public float spacer;
    [Range(.1f, 1)]
    public float minRange;
    public float minRangeForce;
    [Range(.1f, 2)]
    public float maxRange;
    public float maxRangeForce;
    public float breakRange;
    [Range(-10,10)]
    public float floorPosiiton = -5;
    [Range(0, 5)]
    public float floorWidth = 1;
    public Vector2 gravity = Vector2.down;
    [Range(.00001f, .01f)]
    public float forceNormalThreshold = .005f;
    [Range(1, 16)]
    public int maxRecieved = 8;
    [Range(1, 90)]
    public float angleOfTransfer = 50f;
    public int particleSteps = 2;
    public bool showConnections = false;
    public bool showAllNodes = false;
    public bool showSurfaceNodes = false;
    public bool showForces = false;
    public bool showNormals = false;

    float colorLoop = 0;

    private void Start()
    {
        Generate();
    }

    private void FixedUpdate()
    {
        ResetNodePhysics();
        AddForces();
        CreateFloor();
        Simulate();
    }

    private void Generate()
    {
        float distBetween = setDist;//(minRange + maxRange) / 2;
        allNodes = new Node[dimensions * dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                Node n = allNodes[index] = new Node();
                n.position = new Vector2(distBetween * i - (distBetween * dimensions / 2), distBetween * j - (distBetween * dimensions / 2));
                n.index = index;

                for (int c = 0; c < n.nearby.Length; c++) n.nearby[c] = new Node.Connection(); 

                //Top row
                n.nearby[0].index = i == 0 || j == 0 ? -1 : index - 1 - dimensions;
                n.nearby[1].index = j == 0 ? -1 : index - dimensions;
                n.nearby[2].index = i == dimensions - 1 || j == 0 ? -1 : index + 1 - dimensions;
                //Middle row
                n.nearby[3].index = i == 0 ? -1 : index - 1;
                n.nearby[4].index = i == dimensions - 1 ? -1 : index + 1;
                //Bottom row
                n.nearby[5].index = i == 0 || j == dimensions - 1 ? -1 : index - 1 + dimensions;
                n.nearby[6].index = j == dimensions - 1 ? -1 : index + dimensions;
                n.nearby[7].index = i == dimensions - 1 || j == dimensions - 1 ? -1 : index + 1 + dimensions;

                foreach (Node.Connection near in n.nearby)
                {
                    if (near.index < 0)
                    {
                        surfaceNodes.Add(n);
                        break;
                    }
                }
            }
        }
        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                foreach (Node.Connection near in allNodes[index].nearby)
                {
                    if (near.index < 0) continue;
                    near.initialDir = (allNodes[near.index].position - allNodes[index].position).normalized;
                }
            }
        }
    }

    private void Simulate()
    {
        float physicsStep = 1;
        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                Node n = allNodes[index];
                float ps = n.force.magnitude / largestStep;
                if (ps > physicsStep) physicsStep = ps;
            }
        }

        for(int step = 0; step < physicsStep; step++)
        {
            for (int i = 0; i < dimensions; i++)
            {
                for (int j = 0; j < dimensions; j++)
                {
                    int index = i + j * (dimensions);
                    Node n = allNodes[index];
                    n.position += n.normal != Vector2.zero && FacingSameDir(n.force / physicsStep, -n.normal) ? n.force / physicsStep - (Vector2)Vector3.Project(n.force / physicsStep, n.normal) : n.force / physicsStep;
                    n.tempForce = Vector2.zero;
                }
            }
            DoParticlePhysics();
        }
    }

    private void PropagateForce(Vector2 force)
    {
        int trials = 0;
        while (normalQueue.Count > 0 && trials < 10000)
        {
            Node current = normalQueue.Dequeue();
            trials++;

            current.queued = false;

            foreach (Node.Connection near in current.nearby)
            {
                if (near.index < 0) continue;
                Node n = allNodes[near.index];
                Vector2 dir = (n.position - current.position).normalized;
                float angle = Vector2.Angle(dir, current.normal);
                if (angle <= angleOfTransfer)
                {
                    Vector2 projectedForceNormal = (Vector2)Vector3.Project(current.normal / current.normalCount, dir);
                    n.normal += projectedForceNormal;
                    n.normalCount++;
                    if (!n.queued && n.normalCount < maxRecieved && projectedForceNormal.magnitude > forceNormalThreshold)
                    {
                        n.queued = true;
                        normalQueue.Enqueue(n);
                    }
                }
            }
        }
    }

    private bool FacingSameDir(Vector2 dir1, Vector2 dir2)
    {
        return Vector2.Dot(dir1, dir2) > 0;
    }

    private void SortTopology()
    {

    }

    private void ResetNodePhysics()
    {
        foreach (Node n in allNodes)
        {
            n.force = Vector2.zero;
            n.forceCount = 0;
            n.forceCountVector = Vector2.zero;
            n.tempForce = Vector2.zero;
            n.tempForceCount = 0;
            n.normal = Vector2.zero;
            n.normalCount = 0;
            n.queued = false;
        }
    }

    private void DoParticlePhysics()
    {
        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                Node n = allNodes[index];

                foreach (Node.Connection near in n.nearby)
                {
                    if (near.index < 0) continue;

                    Vector2 dir = allNodes[near.index].position - n.position;
                    float dist = dir.magnitude;
                    dir = dir.normalized;
                    float avg = (maxRange + minRange) / 2;

                    if (dist >= breakRange)
                    {
                        near.index = -1;
                        continue;
                    }

                    if (dist > setDist)
                    {
                        Vector2 force;
                        //if (dist > maxRange) force = dir * (dist - setDist);
                        force = dir * Mathf.Clamp((Mathf.Pow(setDist + spacer - maxRange, 2) / Mathf.Pow(dist - maxRange, 2) - 1) * maxRangeForce, 0, dist) / 2;//maxRange-setDist);//Mathf.Clamp((1 / Mathf.Pow(dist - setDist + maxRange, 2)) + (1 / Mathf.Pow(dist - setDist - maxRange, 2)) - 2/Mathf.Pow(maxRange, 2), 0, 100000) / 2;
                        allNodes[near.index].tempForce -= force;
                        allNodes[near.index].forceCount++;
                    }
                    if (dist + 0.000001 < setDist)
                    {
                        if (allNodes[near.index].normal != Vector2.zero) n.normal += allNodes[near.index].normal;
                        Vector2 force;
                        //if (dist < minRange) force = -dir * (dist - setDist);
                        force = -dir * Mathf.Clamp((1 / Mathf.Pow(dist, 2)) + (1 / Mathf.Pow(dist - minRange - setDist, 2)) - 2/Mathf.Pow(minRange,2), 0, (setDist)) / 2;//(Mathf.Pow(setDist - spacer - minRange, 2) / Mathf.Pow(Mathf.Clamp(dist, minRange, setDist) - minRange, 2) - 1) * minRangeForce;//setDist);//
                        allNodes[near.index].tempForce += force;
                        allNodes[near.index].forceCount++;
                    }
                }
            }
        }

        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                Node n = allNodes[index];
                if (n.forceCount != 0)
                {
                    Vector2 force = n.tempForce / n.forceCount;
                    n.normal = n.normal.normalized;
                    n.position += n.normal != Vector2.zero && FacingSameDir(force, -n.normal) ? force - (Vector2)Vector3.Project(force, n.normal) : force;
                }
            }
        }
    }

    private void AddForces()
    {
        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                Node n = allNodes[index];
                n.force += gravity * Time.fixedDeltaTime;
            }
        }
    }

    private void AddForces2()
    {
        Vector2[] updatedPos = new Vector2[dimensions * dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                Node n = allNodes[index];
                updatedPos[index] = n.position;
            }
        }
        for (int c = 0; c < particleSteps; c++)
        {
            for (int i = 0; i < dimensions; i++)
            {
                for (int j = 0; j < dimensions; j++)
                {
                    int index = i + j * (dimensions);
                    Node n = allNodes[index];

                    foreach (Node.Connection near in n.nearby)
                    {
                        if (near.index < 0) continue;

                        Vector2 dir = updatedPos[near.index] - updatedPos[index];
                        float dist = dir.magnitude;
                        dir = dir.normalized;
                        float avg = (maxRange + minRange) / 2;

                        if (dist >= breakRange)
                        {
                            near.index = -1;
                            continue;
                        }

                        if(dist > setDist)
                        {
                            Vector2 force = dir * Mathf.Clamp(maxRange * (1 / Mathf.Pow((dist - setDist) / maxRange + 1, 2)) + (1 / Mathf.Pow((dist - setDist) / maxRange - 1, 2)) - 2, 0, breakRange) / 2;
                            //allNodes[near.index].force -= force;
                            allNodes[near.index].tempForce -= force;
                            //allNodes[near.index].forceCount++;
                            allNodes[near.index].forceCountVector += new Vector2(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
                            allNodes[near.index].tempForceCount++;
                        }
                        if(dist < setDist)
                        {
                            Vector2 force = dir * Mathf.Clamp(minRange * (1 / Mathf.Pow((setDist - dist) / minRange + 1, 2)) + (1 / Mathf.Pow((setDist - dist) / minRange - 1, 2)) - 2, 0, setDist - minRange) / 2;
                            //allNodes[near.index].force += force;
                            allNodes[near.index].tempForce += force;
                            //allNodes[near.index].forceCount++;
                            allNodes[near.index].tempForceCount++;
                            allNodes[near.index].forceCountVector += new Vector2(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
                        }
                    }
                }
            }
            for (int i = 0; i < dimensions; i++)
            {
                for (int j = 0; j < dimensions; j++)
                {
                    int index = i + j * (dimensions);
                    Node n = allNodes[index];
                    if (n.tempForceCount != 0)
                    {
                        n.force += n.tempForce / n.tempForceCount;
                        n.tempForce = n.tempForce / n.tempForceCount;
                        n.forceCount++;
                    }
                        
                    updatedPos[index] = updatedPos[index] + (n.normal != Vector2.zero && FacingSameDir(n.tempForce, -n.normal) ? n.tempForce - (Vector2)Vector3.Project(n.tempForce, n.normal) : n.tempForce);
                    n.tempForce = Vector2.zero;
                    n.tempForceCount = 0;
                }
            }
        }
        
        for (int i = 0; i < dimensions; i++)
        {
            for (int j = 0; j < dimensions; j++)
            {
                int index = i + j * (dimensions);
                Node n = allNodes[index];

                if (n.forceCount != 0)
                    n.force = n.force / n.forceCount;

                n.force += gravity * Time.fixedDeltaTime;
            }
        }
    }

    private void CreateFloor()
    {
        foreach(Node n in surfaceNodes)
        {
            if(n.position.y <= floorPosiiton && n.position.y >=floorPosiiton-floorWidth)
            {
                n.queued = true;
                n.normal = -gravity;//Vector2.up;
                n.normalCount++;
            }
        }
    }

    /*public void AddForceOverCircle(Vector2 position, float radius, Vector2 force, Vector2 falloff)
    {
        float dist;
        foreach (Node n in surface)
        {
            dist = Vector2.Distance(n.position - offset, position);
            if (dist <= radius)
            {
                n.queuedForce += force / 2 * Mathf.Lerp(falloff.x, falloff.y, dist / radius);
            }
        }
    }*/

    private void OnDrawGizmos()
    {
        if (allNodes == null) return;
        Gizmos.color = Color.gray;
        for (int i = 0; i < surfaceNodes.Count; i++)
        {
            if (showSurfaceNodes && !showAllNodes)
                Gizmos.DrawSphere(surfaceNodes[i].position + (Vector2)transform.position, 0.05f);
        }
        foreach (Node n in allNodes)
        {
            Gizmos.color = Color.gray;
            if (showAllNodes)
                Gizmos.DrawSphere(n.position + (Vector2)transform.position, 0.05f);
            Gizmos.color = Color.blue;
            if (showNormals)
                if (n.normal.sqrMagnitude > 0)
                {
                    Gizmos.DrawRay(n.position + (Vector2)transform.position, n.normal);
                }

            Gizmos.color = Color.red;
            if (showForces)
                Gizmos.DrawRay(n.position + (Vector2)transform.position, -n.force);

            if (showConnections)
                foreach (Node.Connection near in n.nearby)
                {
                    if (near.index < 0) continue;
                    Vector2 dist = allNodes[near.index].position - n.position;
                    Gizmos.color = new Color(Mathf.Clamp01((dist.magnitude-minRange)/maxRange), 0f, 1f - Mathf.Clamp01((dist.magnitude - minRange) / maxRange));
                    if (dist.magnitude < maxRange)
                        Gizmos.DrawLine(n.position, allNodes[near.index].position);
                    else Gizmos.DrawLine(n.position, n.position+dist.normalized*maxRange);
                }
                
        }
        
        Gizmos.DrawLine(new Vector2(-10, floorPosiiton), new Vector2(10, floorPosiiton));
        Gizmos.DrawLine(new Vector2(-10, floorPosiiton - floorWidth), new Vector2(10, floorPosiiton - floorWidth));
    }
}
