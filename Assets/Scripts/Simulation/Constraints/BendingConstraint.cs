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
        n1.nearTargetDirs = new Vector3[n1.nearby.Count];
        int i = 0;
        foreach(Node near in n1.nearby)
        {
            n1.nearTargetDirs[i] = n1.nearInitialDirs[i] = n1.position - near.position;
            i++;
        }
    }

    public override void ConstrainPositions(float di)
    {
        Vector3 expectedMove = Vector3.zero;
        int i = 0;
        foreach (Node near in n1.nearby)
        {
            Vector3 currentDir = n1.predictedPosition - near.predictedPosition;
            Vector3 initialDir = n1.rotation * n1.nearInitialDirs[i];

            n1.nearTargetDirs[i] = Vector3.Lerp(n1.nearTargetDirs[i], currentDir, material.rotationalPliability);
            if (material.experimentalRotation && n1.nearby.Count > 10) n1.nearTargetDirs[i] = Vector3.Lerp(n1.nearTargetDirs[i], n1.rotation * n1.nearInitialDirs[i], material.rotationalStiffness);
            else n1.nearTargetDirs[i] = Vector3.Lerp(n1.nearTargetDirs[i], n1.nearInitialDirs[i], material.rotationalStiffness);
            expectedMove += n1.nearTargetDirs[i] - currentDir;
            
            i++;
        }
        
        n1.correctedDisplacement += expectedMove / n1.nearby.Count;
        //n1.correctedRotation += Quaternion.FromToRotation()
        n1.relUp = n1.correctedDisplacement;
    }

    public override void UpdateInitial()
    {
        throw new System.NotImplementedException();
    }

    public override void Reset()
    {
        int i = 0;
        foreach (Node near in n1.nearby)
        {
            n1.nearTargetDirs[i] = n1.nearInitialDirs[i];
            i++;
        }
        n1.relUp = Vector3.up;
        n1.rotation = Quaternion.identity;
    }
}
