using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BendingConstraint : Constraint
{
    Node n1;
    
    Quaternion initialRotation;
    Quaternion targetRotation;

    public BendingConstraint(MixedSimulation material, int i1) : base(material)
    {
        n1 = material.nodes[i1];
        targetRotation = initialRotation = n1.rotation;
        n1.nearInitialDirs = new Vector3[n1.nearby.Count];
        int i = 0;
        foreach(Node near in n1.nearby)
        {
            n1.nearInitialDirs[i] = n1.position - near.position;
            i++;
        }
    }

    public override void ConstrainPositions(float di)
    {
        Vector3 expectedMove = Vector3.zero;
        int i = 0;
        Quaternion tempRot = Quaternion.identity;

        // Find new rotation for n
        
        //n1.correctedRotation *= Quaternion.Lerp(Quaternion.identity, tempRot, 1f / i);

        i = 0;
        // Use rotation to correct nearby nodes
        foreach (Node near in n1.nearby)
        {
            Vector3 currentDir = near.predictedPosition - n1.predictedPosition;
            Quaternion toRotate = Quaternion.FromToRotation(currentDir.normalized, n1.rotation * -n1.nearInitialDirs[i].normalized);
            //expectedMove = (toRotate * currentDir).normalized * currentDir.magnitude + n1.predictedPosition - near.predictedPosition;
            near.correctedDisplacement += expectedMove / near.nearby.Count;
            i++;
        }
    }

    public override void UpdateInitial()
    {
        throw new System.NotImplementedException();
    }
}
