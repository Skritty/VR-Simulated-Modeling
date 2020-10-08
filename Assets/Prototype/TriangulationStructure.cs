using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TriangulationStructure
{
    float distanceInterval;
    float maxDist;
    Anchor[] anchors = new Anchor[3];

    public class Anchor
    {
        public Node[] nodes; // Sorted by distance from anchor
        public Vector3 position;
        public int index;
        public float distanceInterval;

        public Anchor(int maxDist, float _distanceInterval, Vector3 _position, int _index, Anchor[] _anchors)
        {
            distanceInterval = _distanceInterval;
            position = _position;
            index = _index;
            nodes = new Node[maxDist];
            for(int x = nodes.Length - 1; x >= 0; x--)
            {
                nodes[x] = new Node(new Vector3(x, 0, 0) + position);
                nodes[x].next[index] = x + 1 >= nodes.Length ? null : nodes[x + 1];
            }
        }

        public void AddPoint(Node toAdd)
        {
            float dist = Vector3.Distance(position, toAdd.position) / distanceInterval; 
            Node current = nodes[(int)dist];
            if(Vector3.Distance(position, current.next[index].position) > dist)
                while (Vector3.Distance(position, current.next[index].position) < dist) {  current = current.next[index]; }
            toAdd.next = current.next; 
            current.next[index] = toAdd; 
        }
        
        public List<Node> GetPointsAround(Vector3 point, float innerRadius, float outerRadius)
        {
            List<Node> found = new List<Node>();
            float dist = Vector3.Distance(position, point) / distanceInterval;
            if (dist >= nodes.Length) return found;
            float start = dist - outerRadius / distanceInterval;
            float end = dist - innerRadius / distanceInterval;
            Node current = nodes[(int)start];
            while (Vector3.Distance(position, current.next[index].position) <= start) current = current.next[index];
            while (Vector3.Distance(position, current.position) <= end)
            {
                found.Add(current);
                if (Vector3.Distance(position, current.position) <= end) break;
                current = current.next[index];
            }
            start = dist + innerRadius / distanceInterval;
            end = dist + outerRadius / distanceInterval;
            current = nodes[(int)start];
            while (Vector3.Distance(position, current.next[index].position) <= start) current = current.next[index];
            while (Vector3.Distance(position, current.position) <= end)
            {
                found.Add(current);
                current = current.next[index];
            }
            return found;
        }
    }

    public class Node
    {
        public Vector3 position;
        public Node[] next = new Node[3];

        public Node(Vector3 _position)
        {
            position = _position;
        }
    }

    public TriangulationStructure(float interval, float maxRadius)
    {
        distanceInterval = interval;
        maxDist = maxRadius * 2;
        int arraySize = (int)(maxRadius / interval*2) + 1;
        anchors[0] = new Anchor(arraySize, interval, new Vector3(0.0f, 1.0f) * maxRadius, 0, anchors);
        anchors[1] = new Anchor(arraySize, interval, new Vector3(0.866f, -0.5f) * maxRadius, 1, anchors);
        anchors[2] = new Anchor(arraySize, interval, new Vector3(-0.866f, -0.5f) * maxRadius, 2, anchors);
    }

    public bool AddNode(Vector3 position)
    {
        Node node = new Node(position); 
        foreach (Anchor anchor in anchors)
        {
            float dist = Vector3.Distance(anchor.position, position);//Debug.Log(dist);
            if (dist > maxDist) return false;
            anchor.AddPoint(node);
        }
        return true;
    }

    public Node[] GetNodes(Vector3 position, float innerRadius, float outerRadius)
    {
        List<Node> nodesInRange = new List<Node>();
        foreach (Anchor anchor in anchors)
        {
            nodesInRange.AddRange(anchor.GetPointsAround(position, innerRadius, outerRadius));
        }
        return nodesInRange.GroupBy(x => x).Where(g => g.Count() > 2).Select(y => y.Key).ToArray();
    }
}
