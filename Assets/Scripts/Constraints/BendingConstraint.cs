using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BendingConstraint : Constraint
{
    private int index1;
    private int index2;
    Vector3 iPos1;
    Vector3 iPos2;
    Vector3 initialDir;
    Quaternion initialRotation; // From node 2's up in the direction of node 1
    Quaternion reverseInitialRotation;

    public BendingConstraint(MixedSimulation material, int i1, int i2) : base(material)
    {
        index1 = i1;
        index2 = i2;
        MixedSimulation.Node n1 = material.nodes[index1];
        MixedSimulation.Node n2 = material.nodes[index2];
        iPos1 = n1.position;
        iPos2 = n2.position;
        initialDir = n1.position - n2.position;
        initialRotation = Quaternion.FromToRotation(n2.relUp, n1.position - n2.position);
        reverseInitialRotation = Quaternion.FromToRotation(n1.position - n2.position, n2.relUp);
    }
    public override void ConstrainPositions(float di)
    {
        MixedSimulation.Node n1 = material.nodes[index1];
        MixedSimulation.Node n2 = material.nodes[index2];

        Vector3 currentDir = n1.predictedPosition - n2.predictedPosition;
        n1.correctedDisplacement += (Vector3.RotateTowards(currentDir, initialDir, Vector3.Angle(currentDir, initialDir) * Mathf.Deg2Rad, 0) + n2.predictedPosition - n1.predictedPosition) / n1.nearby.Count;
    }
    public override void UpdateInitial()
    {
        MixedSimulation.Node n1 = material.nodes[index1];
        MixedSimulation.Node n2 = material.nodes[index2];
        if (n1.normalCount == 0 || n2.normalCount == 0) return;
        iPos1 = iPos1 - (n1.normal / n1.normalCount * material.pliability);
        iPos2  = iPos2 - (n2.normal / n2.normalCount * material.pliability);
        initialDir = iPos1 - iPos2;
        initialRotation = Quaternion.FromToRotation(Vector3.up, iPos1 - iPos2);
        reverseInitialRotation = Quaternion.FromToRotation(iPos1 - iPos2, Vector3.up);
    }
}
