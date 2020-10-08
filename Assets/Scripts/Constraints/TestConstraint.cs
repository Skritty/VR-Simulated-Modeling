using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConstraint : Constraint
{
    int index;
    public TestConstraint(MixedSimulation material, int i1) : base(material)
    {
        index = i1;
    }
    public override void ConstrainPositions(float di)
    {
        MixedSimulation.Node n1 = material.nodes[index];
        n1.correctedDisplacement += (n1.position - n1.predictedPosition)/n1.nearby.Count;
    }
    public override void UpdateInitial()
    {
        throw new System.NotImplementedException();
    }
}

