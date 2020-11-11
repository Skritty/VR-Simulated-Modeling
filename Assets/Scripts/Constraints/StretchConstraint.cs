using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This constraint attempts to bring nodes back into line if they stretch too far
/// </summary>
public class StretchConstraint : Constraint
{
    private int index1;
    private int index2;
    private float initialDist;

    public StretchConstraint(MixedSimulation material, int i1, int i2) : base(material)
    {
        index1 = i1;
        index2 = i2;
        initialDist = Vector3.Distance(material.nodes[i1].position, material.nodes[i2].position);
    }

    public override void ConstrainPositions(float di)
    {
        MixedSimulation.Node n1 = material.nodes[index1];
        MixedSimulation.Node n2 = material.nodes[index2];
        float dist = Vector3.Distance(n1.predictedPosition, n2.predictedPosition);
        Vector3 dir = (n1.predictedPosition - n2.predictedPosition).normalized;

        if (dist <= initialDist) return;

        switch (material.stretchType)
        {
            case MixedSimulation.StretchType.Rigid:
                Vector3 expectedMove = dir * (dist - initialDist)/n1.nearby.Count + storedMovement;
                n1.correctedDisplacement -= expectedMove * material.stiffness;
                //storedMovement = expectedMove * (1 - material.stiffness);
                break;

            case MixedSimulation.StretchType.Exponential:
                n1.correctedDisplacement -= dir * Mathf.Pow((dist - initialDist), 2);
                break;

            case MixedSimulation.StretchType.Hyperbolic:
                n1.correctedDisplacement -= dir * Mathf.Clamp((Mathf.Pow(initialDist - 5f, 2) / Mathf.Pow(dist - 5f, 2) - 1), 0, dist) / 2;
                break;
        }
        //n2.correctedDisplacement -= (n2.predictedPosition - n2.predictedPosition).normalized * (dist - initialDist) * di;
        //n1.correctedDisplacement = -n1.inverseMass * (dist - initialDist) / (n1.inverseMass + n2.inverseMass) * (n1.predictedPosition - n2.predictedPosition).normalized;
        //n2.correctedDisplacement = -(n2.inverseMass * (dist - initialDist) / (n1.inverseMass + n2.inverseMass)) * (n1.predictedPosition - n2.predictedPosition) / dist;
    }
    public override void UpdateInitial()
    {
        initialDist = Vector3.Distance(material.nodes[index1].position, material.nodes[index2].position);
    }
}
