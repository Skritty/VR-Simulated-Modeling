using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceApplier : MonoBehaviour
{
    [SerializeField] MixedSimulation simulation;
    [SerializeField] float minimumRange = 0;
    [SerializeField] float maximumRange = 1;
    [SerializeField] float falloff;
    [SerializeField] float forceMagnitude = 10f;

    Vector3 currentPos = Vector3.zero;
    Vector3 selectedPoint = Vector3.zero;
    Node selectedNode = null;
    Transform selectedObject = null;
    float initialDistFromCamera = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    private void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if(hit.transform.tag == "Moveable")
                {
                    selectedObject = hit.transform;
                    initialDistFromCamera = Vector3.Project(selectedObject.position - Camera.main.transform.position, Camera.main.transform.forward).magnitude;
                }
                
            }

            Node node = simulation.ClosestPointToRay(ray, simulation.distBetween);
            if (node != null)
            {
                selectedPoint = node.position;
                selectedNode = node;
                initialDistFromCamera = Vector3.Project(node.position - Camera.main.transform.position, Camera.main.transform.forward).magnitude;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedPoint = Vector3.zero;
            selectedNode = null;
            selectedObject = null;
        }

        // Find the force to apply
        if (Input.GetMouseButton(0) && simulation != null)
        {
            currentPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, initialDistFromCamera));
            if (selectedObject != null)
            {
                selectedObject.position = currentPos;
            }
            else if(selectedPoint != Vector3.zero)
                ApplyForcesOverVolume();
        }
    }

    private void ApplyForcesOverVolume()
    {
        foreach(Node n in simulation.surfaceNodes)
        {
            float dist = Vector3.Distance(n.position, selectedNode.position);
            if(dist > minimumRange && dist < maximumRange)
            {
                simulation.AddForce(n, (currentPos - selectedPoint) * forceMagnitude);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(selectedPoint != Vector3.zero)
        {
            Gizmos.DrawLine(selectedPoint, currentPos);
        }
    }
}
