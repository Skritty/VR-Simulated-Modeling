using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompressionConstraint : Constraint
{
    Node n1;
    Node n2;
    private float initialDist;
    private float targetSetDist;

    public CompressionConstraint(MixedSimulation material, int i1, int i2) : base(material)
    {
        n1 = material.nodes[i1];
        n2 = material.nodes[i2];
        targetSetDist = initialDist = Vector3.Distance(n1.position, n2.position);
    }

    public override void ConstrainPositions(float di)
    {
        float dist = Vector3.Distance(n1.predictedPosition, n2.predictedPosition);
        Vector3 dir = (n1.predictedPosition - n2.predictedPosition).normalized;
        float offset = Vector3.Project(n1.offsetFromGoal, dir).magnitude;

        if (dist >= material.minDist) return;
        
        targetSetDist = Mathf.Lerp(targetSetDist, dist, material.pliability);
        targetSetDist = Mathf.Lerp(targetSetDist, initialDist, material.stiffness);

        Vector3 expectedMove = dir * (targetSetDist - dist);
        n1.correctedDisplacement += expectedMove / n1.nearby.Count * 2;
    }

    public override void UpdateInitial()
    {
        targetSetDist = initialDist = Vector3.Distance(n1.position, n2.position);
    }
}
