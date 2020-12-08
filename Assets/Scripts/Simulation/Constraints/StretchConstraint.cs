using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This constraint attempts to bring nodes back into line if they stretch too far
/// </summary>
public class StretchConstraint : Constraint
{
    Node n1;
    Node n2;
    private float initialDist;

    public StretchConstraint(MixedSimulation material, int i1, int i2) : base(material)
    {
        n1 = material.nodes[i1];
        n2 = material.nodes[i2];
        initialDist = Vector3.Distance(material.nodes[i1].position, material.nodes[i2].position);
    }

    public override void ConstrainPositions(float di)
    {
        float dist = Vector3.Distance(n1.predictedPosition, n2.predictedPosition);
        Vector3 dir = (n1.predictedPosition - n2.predictedPosition).normalized;

        if (dist <= initialDist) return;

        Vector3 expectedMove = dir * (dist - initialDist);
        n1.correctedDisplacement -= expectedMove / n1.nearby.Count;
    }
    public override void UpdateInitial()
    {
        initialDist = Vector3.Distance(n1.position, n2.position);
    }
}
