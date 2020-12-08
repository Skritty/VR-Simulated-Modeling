using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialStructureGrid : MonoBehaviour
{
    [Header("Nodes")]
    public Node[][] grid;
    public List<Node> surface = new List<Node>();

    [Header("Material Data")]
    public Vector2 nodesXY;
    public Vector2 bondRange;
    public float breakRange;

    [Header("Physics")]
    public bool useComputeShader = false;
    [Tooltip("Can be upwards of 8 times this amount")]
    public int updatesPerTick = 1;
    public Vector2 gravity;
    public ComputeShader physics;

    private float avgDistBetween;
    private PriorityQueue.Node physicsQueue; // Ordered from largest force to smallest force
    private Vector2 offset;
    private Node previousUpdated;
    private float gap;

    public class Node
    {
        public Vector2 position = Vector2.zero; // Position in space
        public Vector2 queuedForce = Vector2.zero; // Distance to be moved on next physics update
        public int arrayPosX = 0; // Reference to positions in the grid
        public int arrayPosY = 0;
        public Node previousNodeHere = null; // Reference to a node that this one moved on top of (that node is no longer in the grid) 
        public int actors = 0;
        public int distance = 0;
        public bool locked = false;

        public Node(Vector2 pos, int x, int y)
        {
            position = pos;
            arrayPosX = x;
            arrayPosY = y;
        }
    }

    private void Start()
    {
        // Initialize the physics queue
        physicsQueue = PriorityQueue.newNode(new Node(new Vector2(0, 0), -1, -1), Mathf.Infinity);

        // Calculate the middle of the bond range
        gap = (1 / Mathf.Sqrt(2)) * (bondRange.x + bondRange.y)/2 - bondRange.x;
        avgDistBetween = bondRange.x + gap/2;
        
        offset = new Vector2(avgDistBetween * (int)nodesXY.x, avgDistBetween * (int)nodesXY.x);

        Generate();
            
    }

    private void Generate()
    {
        // Initialize the physics grid with nodes at the centers of squares
        grid = new Node[(int)nodesXY.x * 2][];

        // Set up the grid
        for (int i = 0; i < (int)nodesXY.x * 2; i++)
            grid[i] = new Node[(int)nodesXY.y * 2];

        // Add the nodes starting 25% the way in and stopping at 75% the way in
        for (int i = (int) (grid.Length * (1f/4f)); i < grid.Length * (3f / 4f); i++)
            for (int j = (int)(grid[i].Length * (1f / 4f)); j < grid[i].Length * (3f/4f); j++)
                grid[i][j] = new Node(new Vector2(i * avgDistBetween + avgDistBetween / 2, j * avgDistBetween + avgDistBetween / 2), i, j);

        // Add walls
        for(int i = 0; i < grid.Length; i++)
        {
            grid[0][i] = new Node(new Vector2(i * avgDistBetween + avgDistBetween / 2, i * avgDistBetween + avgDistBetween / 2), 0, i);
            grid[0][i].locked = true;
            grid[grid.Length-1][i] = new Node(new Vector2(i * avgDistBetween + avgDistBetween / 2, i * avgDistBetween + avgDistBetween / 2), 0, i);
            grid[grid.Length-1][i].locked = true;
            if (i == 0 || i == grid.Length - 1) continue;
            grid[i][0] = new Node(new Vector2(i * avgDistBetween + avgDistBetween / 2, i * avgDistBetween + avgDistBetween / 2), 0, i);
            grid[i][0].locked = true;
            grid[i][grid.Length-1] = new Node(new Vector2(i * avgDistBetween + avgDistBetween / 2, i * avgDistBetween + avgDistBetween / 2), 0, i);
            grid[i][grid.Length-1].locked = true;
        }
    }

    private void FixedUpdate()
    {
        if (useComputeShader)
            ComputeShaderPhysicsUpdate();
        else
            PhysicsUpdate();
    }

    // GPU Physics : O(2n+8)
    private void ComputeShaderPhysicsUpdate()
    {
        // Create temporary 1d arrays to store the grid's information while doing physics
        Vector2[] positions = new Vector2[grid.Length * grid.Length];
        Vector2[] forces = new Vector2[grid.Length * grid.Length];
        for (int i = 0; i < grid.Length; i++)
            for (int j = 0; j < grid[i].Length; j++)
            {
                if (grid[i][j] == null) continue;
                positions[i + j * (grid.Length-1)] = grid[i][j].position;
                if (grid[i][j].locked) forces[i + j * (grid.Length - 1)] = Vector2.zero;
                else
                {
                    forces[i + j * (grid.Length - 1)] = grid[i][j].queuedForce + gravity;
                    Vector2 newPos = grid[i][j].position + forces[i + j * (grid.Length - 1)];
                    if (newPos.y > (grid.Length - 1) * avgDistBetween || newPos.y < 0 || newPos.x < 0 || newPos.x > (grid.Length - 1) * avgDistBetween)
                    {
                        forces[i + j * (grid.Length - 1)] = -forces[i + j * (grid.Length - 1)];
                    }
                }
            }

        // Send to the compute shader to apply forces and check which new ones need to happen
        int kernal = physics.FindKernel("MaterialPhysics");
        uint x, y, z;
        physics.GetKernelThreadGroupSizes(kernal, out x, out y, out z);

        ComputeBuffer positionsIn = new ComputeBuffer(positions.Length, sizeof(float) * 2);
        ComputeBuffer forcesIn = new ComputeBuffer(forces.Length, sizeof(float) * 2);
        physics.SetInt("gridDim", grid.Length);
        physics.SetVector("bondRange", bondRange);

        ComputeBuffer positionsOut = new ComputeBuffer(positions.Length, sizeof(float) * 2);
        positionsOut.SetData(positions);
        physics.SetBuffer(kernal, "positionsOut", positionsOut);

        ComputeBuffer forcesOut = new ComputeBuffer(forces.Length, sizeof(float) * 2);
        forcesOut.SetData(forces);
        physics.SetBuffer(kernal, "forcesOut", forcesOut);

        for (int update = 0; update < updatesPerTick; update++)
        {
            positionsIn.SetData(positions);
            physics.SetBuffer(kernal, "positionsIn", positionsIn);

            forcesIn.SetData(forces);
            physics.SetBuffer(kernal, "forcesIn", forcesIn);

            physics.Dispatch(kernal, (int)(grid.Length/x), (int)(grid.Length/y), 1);

            positionsOut.GetData(positions);
            forcesOut.GetData(forces);
        }

        positionsOut.Release();
        forcesOut.Release();
        positionsIn.Release();
        forcesIn.Release();

        surface.Clear();

        // Update all the node data
        for (int i = 0; i < grid.Length; i++)
            for (int j = 0; j < grid[i].Length; j++)
            {
                if (grid[i][j] == null) continue;
                grid[i][j].position = positions[i + j * (grid.Length - 1)];
                grid[i][j].queuedForce = forces[i + j * (grid.Length - 1)];

                // Determine if it is a surface node (at least one nearby node is null)
                if(!grid[i][j].locked)
                    for (int xx = -1; xx <= 1; xx++)
                        for (int yy = -1; yy <= 1; yy++)
                            if(grid[i+xx][j+yy] == null)
                                surface.Add(grid[i][j]);

            }
        
    }

    // CPU Physics : <= O(8n)
    private void PhysicsUpdate()
    {
        if(physicsQueue.next != null)
        Debug.Log(physicsQueue.next.data.arrayPosX);
        for(int update = 0; update < updatesPerTick; update++)
        {
            // Check if there are any updates needing to be done
            if (physicsQueue.data.arrayPosX == -1) break;

            // Get the current node we are working with
            Node current = physicsQueue.data;
            physicsQueue = PriorityQueue.pop(physicsQueue);
            
            //Check if the queued force is zero and skip this update if it is (this can happen when one node gets queued multiple times)
            if (current.queuedForce == Vector2.zero)
            {
                update--;
                continue;
            }

            //Debug.Log((current.queuedForce/current.actors).magnitude);

            // Move the current node to its new position and set its queued force to zero
            //Debug.DrawLine(current.position - offset, current.position + current.queuedForce - offset, Color.cyan, 5f);//UnityEditor.EditorApplication.isPaused = true;
            current.position += current.queuedForce/current.actors;
            current.queuedForce = Vector2.zero;
            current.actors = 0;
            previousUpdated = current;
            
            // Go through all adjacent nodes and add to their queued force (also queue them if they arent already queued)
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    // Make sure its on the grid and theres a node there
                    if (current.arrayPosX + x < 0 || current.arrayPosX + x > nodesXY.x*2 || current.arrayPosY + y < 0 || current.arrayPosY + y > nodesXY.y*2) continue;
                    Node connected = grid[current.arrayPosX + x][current.arrayPosY + y];
                    if (connected == null) continue;

                    // Use the previous node that was in this spot if one exists (because it hasnt been updated yet)
                    if (connected.previousNodeHere != null && connected.previousNodeHere.queuedForce != Vector2.zero)
                        connected = connected.previousNodeHere;

                    // Find the distance vector between this node and the new position of the current node ----------------------- UPDATE TO SHOW TRUE MOVEMENT OF THE CURRENT NODE RATHER THAN JUST THE NEW LOCATION
                    Vector2 newDistance = current.position - connected.position;
                    float dist = newDistance.magnitude;

                    // If it is still within acceptable range, do nothing
                    if ((dist <= bondRange.y && dist >= bondRange.x) || connected.distance > 80 || dist >= breakRange) continue;

                    // Find how much the node needs to be moved to get back into acceptable range
                    Vector2 force = newDistance - newDistance.normalized * (dist < bondRange.x ? bondRange.x : bondRange.y);
                    //if (force.magnitude < gap) continue;

                    if (connected.actors < 1)
                    {
                        connected.distance = current.distance + 1;
                        //Debug.Log(connected.distance);
                    }
                    // Queue the node to be updated if it isnt already queued
                    if (connected.queuedForce == Vector2.zero)
                        physicsQueue = PriorityQueue.push(physicsQueue, connected, connected.distance);
                    connected.queuedForce += force;
                    connected.actors++;
                    
                    
                    
                    //if (x == 1 && y == 1) Debug.Log("success");
                }
            
            // Move the current node to a new spot on the grid (maybe), and take note of the node current there, if there is one
            Vector2 newPos = LocateNewNodePosition(current);
            newPos = new Vector2(Mathf.Clamp(newPos.x, 0, grid.Length-1), Mathf.Clamp(newPos.y, 0, grid.Length-1));

            if (grid[(int)newPos.x][(int)newPos.y] == current) continue;

            if (grid[(int)newPos.x][(int)newPos.y] != null)
                current.previousNodeHere = grid[(int)newPos.x][(int)newPos.y];

            current.arrayPosX = (int)newPos.x;
            current.arrayPosY = (int)newPos.y;
            grid[(int)newPos.x][(int)newPos.y] = current;
        }
    }

    public void AddForceAt(Vector2 position, Vector2 force)
    {
        // Get the array position of the targeted location
        Vector2 pos = LocateNodeArrayPosition(position + offset);

        //Debug.Log(pos);
        // Check if a node exists there
        if (pos.x <= 0 | pos.x >= grid.Length || pos.y <= 0 || pos.y >= grid[0].Length || grid[(int)pos.x][(int)pos.y] == null) return;

        // Add the force and add the node to the physics queue
        Node node = grid[(int)pos.x][(int)pos.y];
        node.queuedForce += force;//new Vector2(Mathf.Clamp(force.x, -(breakRange - (bondRange.x + 2*gap)), breakRange - (bondRange.x + 2 * gap)), Mathf.Clamp(force.y, -(breakRange - (bondRange.x + 2 * gap)), breakRange - (bondRange.x + 2 * gap))); ;
        node.actors++;
        node.distance = 0;
        physicsQueue = PriorityQueue.push(physicsQueue, node, node.distance);
    }

    public void AddForceOverCircle(Vector2 position, float radius, Vector2 force, Vector2 falloff)
    {
        float dist;
        foreach(Node n in surface)
        {
            dist = Vector2.Distance(n.position-offset, position);
            if (dist <= radius)
            {
                n.queuedForce += force/2 * Mathf.Lerp(falloff.x, falloff.y, dist/radius);
            }
        }
    }

    private Vector2 LocateNodeArrayPosition(Vector3 worldPosition)
    {
        return ((Vector2)worldPosition - (Vector2)transform.position) / avgDistBetween;
    }

    private Vector2 LocateNewNodePosition(Node node)
    {
        Vector2 percents = new Vector2((node.position.x / avgDistBetween) % 1f, (node.position.y / avgDistBetween) % 1f);
        float acceptable = gap / (bondRange.x + 2 * gap);
        if (percents.x <= 1 - acceptable && percents.x >= acceptable && percents.y <= 1 - acceptable && percents.y >= acceptable)
        {
            return node.position / avgDistBetween;
        }
        else return new Vector2(node.arrayPosX, node.arrayPosY);
    }

    private void OnDrawGizmos()
    {
        foreach(Node n in surface)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(n.position - offset, bondRange.x);
        }
        /*if (grid == null) return;
        foreach (Node[] col in grid)
        {
            foreach (Node n in col)
            {
                if(n != null && n.queuedForce != Vector2.zero)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawSphere(n.position - offset, bondRange.x);
                }
            }
        }*/
    }
}
