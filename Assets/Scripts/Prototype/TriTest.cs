using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriTest : MonoBehaviour
{
    public TriangulationStructure tri;

    private void Awake()
    {
        tri = new TriangulationStructure(1, 10);
        tri.AddNode(new Vector3(5, 1));
        tri.AddNode(new Vector3(0, 5));
        tri.AddNode(new Vector3(5, 1));
        tri.AddNode(new Vector3(5, 2));
        foreach(TriangulationStructure.Node n in tri.GetNodes(new Vector3(5, 0), 0, 1))
        {
            Debug.Log(n.position);
        }
    }
}
