using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompressionConstraint : Constraint
{
    private int index1;
    private int index2;
    private float initialDist;
    private float currentSetDist;
    private Vector3 prevPos;

    public CompressionConstraint(MixedSimulation material, int i1, int i2) : base(material)
    {
        index1 = i1;
        index2 = i2;
        initialDist = Vector3.Distance(material.nodes[i1].position, material.nodes[i2].position);
        currentSetDist = initialDist;
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
                //n1.correctedDisplacement -= Vector3.Project(n1.correctedDisplacement, dir) * (material.stiffness); -1 = 0 pli, 1 = 1 pli y = pli, x = dist
                float pAmt = material.pliability * 2 - 1;
                float pDist = initialDist - dist;
                //Vector3 expectedMove = dir * (dist - initialDist) * ((pAmt * pAmt) - pAmt + (2*pDist*(1-pAmt*pAmt)))/2 * di + storedMovement;
                //currentSetDist += (initialDist - currentSetDist)*(material.stiffness/material.iterationsToRestore * di * Time.fixedDeltaTime);
                Vector3 expectedMove = dir * (dist - currentSetDist) * di * (1 - material.pliability) + storedMovement;
                //storedMovement = expectedMove * material.pliability;
                //currentSetDist = currentSetDist - expectedMove.magnitude * material.pliability * di;
                n1.correctedDisplacement -= expectedMove;// * (1 - material.pliability);
                //n1.predictedPosition += n1.correctedDisplacement * material.stiffness;
                
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
