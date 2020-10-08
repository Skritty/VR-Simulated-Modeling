using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompressionConstraint : Constraint
{
    private int index1;
    private int index2;
    private float initialDist;
    private Vector3 prevPos;

    public CompressionConstraint(MixedSimulation material, int i1, int i2) : base(material)
    {
        index1 = i1;
        index2 = i2;
        initialDist = Vector3.Distance(material.nodes[i1].position, material.nodes[i2].position);
        prevPos = material.nodes[i1].position;
    }

    public override void ConstrainPositions(float di)
    {
        MixedSimulation.Node n1 = material.nodes[index1];
        MixedSimulation.Node n2 = material.nodes[index2];
        float dist = Vector3.Distance(n1.predictedPosition, n2.predictedPosition);
        Vector3 dir = (n1.predictedPosition - n2.predictedPosition).normalized;

        if (dist >= material.minDist) return;
        
        switch (material.compressionType)
        {
            case MixedSimulation.CompressionType.Rigid:
                //n1.correctedDisplacement -= Vector3.Project(n1.correctedDisplacement, dir) * (material.stiffness);
                n1.correctedDisplacement -= dir * (dist - initialDist) / n1.nearby.Count;
                n1.predictedPosition += n1.correctedDisplacement * material.stiffness;
                break;

            case MixedSimulation.CompressionType.AdvancedRigid:
                prevPos = n1.prevPredicted;

                Vector3 closestPoint = prevPos + Vector3.Project((n2.predictedPosition - prevPos), (prevPos - n1.predictedPosition));
                float closestDist = Vector3.Distance(n2.predictedPosition, closestPoint);

                if (Vector3.Distance(closestPoint, n1.predictedPosition) > Vector3.Distance(n1.predictedPosition, prevPos) || closestDist >= material.minDist) return;

                float predictedToClosest = Vector3.Distance(n1.predictedPosition, closestPoint);
                float closestToMin = Mathf.Sqrt(Mathf.Abs(material.minDist * material.minDist - closestDist * closestDist));
                Vector3 trueStopPt = (prevPos - n1.predictedPosition).normalized * (predictedToClosest + closestToMin);
                n1.correctedDisplacement += trueStopPt * di;
                break;
        }
    }
    public override void UpdateInitial()
    {
        initialDist = Vector3.Distance(material.nodes[index1].position, material.nodes[index2].position);
    }
}
