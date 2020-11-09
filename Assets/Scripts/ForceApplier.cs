using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceApplier : MonoBehaviour
{
    [SerializeField] MixedSimulation simulation;
    [SerializeField] float minimumRange;
    [SerializeField] float maximumRange;
    [SerializeField] float falloff;
    [SerializeField] float forceMagnitude = 10f;

    Vector3 currentPos = Vector3.zero;
    Vector3 selectedPoint = Vector3.zero;
    float initialDistFromCamera = 0;
    

    private void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MixedSimulation.Node node = simulation.ClosestPointToRay(Camera.main.ScreenPointToRay(Input.mousePosition), simulation.distBetween);
            if (node != null)
            {
                selectedPoint = node.position;
                initialDistFromCamera = Vector3.Distance(node.position, Camera.main.transform.position);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedPoint = Vector3.zero;
        }

        // Find the force to apply
        if (Input.GetMouseButton(0) && simulation != null)
        {
            currentPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, initialDistFromCamera));
            if(selectedPoint != Vector3.zero)
                ApplyForcesOverVolume();
        }
    }

    private void ApplyForcesOverVolume()
    {
        foreach(MixedSimulation.Node n in simulation.surfaceNodes)
        {
            float dist = Vector3.Distance(n.position, selectedPoint);
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
