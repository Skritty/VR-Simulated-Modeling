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
    [SerializeField] float scrollSpeed = 0.2f;
    [SerializeField] float scrollScale = 0.1f;
    [SerializeField] Material tool;
    [SerializeField] Material toolSelected;

    Vector3 currentPos = Vector3.zero;
    Vector3 initialOffset = Vector3.zero;
    Vector3 selectedPoint = Vector3.zero;
    Node selectedNode = null;
    Transform selectedObject = null;
    float initialDistFromCamera = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out hit);
            if (hit.transform != null && hit.transform.tag == "Moveable" && selectedObject == null)
            {
                if (selectedObject != null) selectedObject.GetComponent<Renderer>().sharedMaterial = tool;
                selectedObject = hit.transform;
                initialOffset = selectedObject.transform.position - hit.point;
                initialDistFromCamera = Vector3.Project(selectedObject.position - initialOffset - Camera.main.transform.position, Camera.main.transform.forward).magnitude;
                selectedObject.GetComponent<Renderer>().sharedMaterial = toolSelected;
            }
            else
            {
                if (selectedObject != null) selectedObject.GetComponent<Renderer>().sharedMaterial = tool;
                selectedObject = null;
            }

            Node node = simulation.ClosestPointToRay(ray, simulation.distBetween);
            if (node != null)
            {
                selectedPoint = node.position;
                selectedNode = node;
                initialDistFromCamera = Vector3.Project(node.position - Camera.main.transform.position, Camera.main.transform.forward).magnitude;
            }
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.mouseScrollDelta.magnitude > 0)
            {
                if (selectedObject != null)
                {
                    selectedObject.localScale += selectedObject.localScale * Input.mouseScrollDelta.y * scrollScale;
                }
            }
        }
        else if (Input.mouseScrollDelta.magnitude > 0)
        {
            initialDistFromCamera += Input.mouseScrollDelta.y * scrollSpeed;
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedPoint = Vector3.zero;
            selectedNode = null;
        }
    }

    private void FixedUpdate()
    {
        // Find the force to apply
        if (simulation != null)
        {
            currentPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, initialDistFromCamera));
            if (selectedObject != null)
            {
                selectedObject.position = currentPos + initialOffset;
            }
            else if(selectedPoint != Vector3.zero && Input.GetMouseButton(0))
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
