using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeConstraint : Constraint
{
    Node n1;
    Node n2;

    public VolumeConstraint(MixedSimulation material, int i1, int i2) : base(material)
    {
        n1 = material.nodes[i1];
        n2 = material.nodes[i2];
    }

    public override void ConstrainPositions(float di)
    {
        float dist = Vector3.Distance(n1.predictedPosition, n2.predictedPosition);
        Vector3 dir = (n1.predictedPosition - n2.predictedPosition).normalized;

        Vector3 expectedMove = dir * (material.volumeCorrection); 
        n1.correctedDisplacement -= expectedMove;
    }

    public override void UpdateInitial()
    {

    }

    public override void Reset()
    {

    }
}
