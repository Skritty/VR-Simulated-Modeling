using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialStructure : MonoBehaviour
{
    public List<Node> surfaceNodes = new List<Node>(); // list of all nodes that make up the surface of the 3d model
    public List<Node> allNodes = new List<Node>();

    public Vector2 nodesXY;
    public int radius;
    public int connectionsPer;
    public Vector2 bondRange;
    public float falloff;

    private int angleBetween;
    private float avgDistBetween;

    public void GenerateShape(Vector2 center) // generates a rectangular simulating volume
    {
        //existing links are the previous node, and the new node's left and right neighbours in List connections
        //or the previous node and the nodes at the new node +/- the angle between nodes
        angleBetween = (int)(360f / connectionsPer);
        avgDistBetween = (bondRange.x + bondRange.y) / 2;
        Node root = new Node(center);

    }

    private void GenerateHelper(Node previous, Node current, int currentRadius)
    {
        // Connect all existing nodes to this one
        int a = GetAngleBetween(current, previous);
        Node left = previous.connections[a - angleBetween];
        Node right = previous.connections[(a + angleBetween) % 360];
        current.connections.Add(GetAngleBetween(current, left), left);
        current.connections.Add(GetAngleBetween(current, right), right);

        // Add new nodes to the graph if they arent already there
        for (int i = 0; i < connectionsPer; i++)
        {
            if (current.connections.ContainsKey(i*angleBetween)) continue;

            Vector2 dir = new Vector2(Mathf.Cos(i * angleBetween * Mathf.Deg2Rad), Mathf.Sin(i * angleBetween * Mathf.Deg2Rad));
            Node n = new Node(current.position + avgDistBetween * dir);
            Edge e = new Edge(current, n);
            current.connections.Add(i * angleBetween, n);
        }

        // Continue adding
        foreach(KeyValuePair<int, Node> connection in current.connections)
        {
            if(currentRadius < radius)
            {
                //GenerateHelper(connection.Value, currentRadius++);
            }
        }
    }

    private int GetAngleBetween(Node current, Node previous)
    {
        return (int)Vector2.Angle(Vector2.up, current.position - previous.position);
    }

    public class Node
    {
        public Vector2 position; // position of the node in local space
        public Dictionary<int, Node> connections; // list of connected nodes

        public Node(Vector2 pos)
        {
            position = pos;
        }
    }

    public class Edge // contains two one-way edges
    {
        public Path[] paths = new Path[2];
        public float distance; // if this distance is lower than the min bond range or higher than the max bond range, pull it back to be closer
        
        public class Path
        {
            public int angle; // angle from relative up
            public Node next; // connected node (check if the current node is not this)

            public Path(Node node, int degrees)
            {
                angle = degrees;
                next = node;
            }
        }

        public Edge(Node current, Node next)
        {
            int angle = (current.position.y > next.position.y) ? (int)Vector2.Angle(Vector2.up, next.position - current.position) : (int)Vector2.Angle(Vector2.up, current.position - next.position);
            paths[0] = new Path(current, angle);
            paths[1] = new Path(next, (angle - 180));
        }
    }
}
