using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This external constraint prevents nodes from continuing in the direction of the surface normal, and bounces back forces
/// </summary>
public class StaticConstraint : Constraint
{
    private Vector3 normal;
    private int index;
    private float friction;
    private bool trueStatic = false;
    private Collider bounds;
    private float bounciness;

    public StaticConstraint(MixedSimulation material, int _index, Vector3 _normal, float _friction, bool _static, Collider _bounds, float bounce) : base(material)
    {
        index = _index;
        normal = _normal;
        friction = _friction;
        trueStatic = _static;
        bounds = _bounds;
        bounciness = bounce;
    }

    public override void ConstrainPositions(float di)
    {
        MixedSimulation.Node n = material.nodes[index];

        if (!bounds.bounds.Contains(n.predictedPosition)) return;

        Vector3 toMove = n.position - n.predictedPosition;

        if (trueStatic)
        {
            n.velocity = Vector3.zero;
            Vector3 surfaceMove = bounds.transform.position - bounds.GetComponent<StaticSurface>().previousPos;
            Vector3 surfaceVertical = Vector3.Project(surfaceMove, normal); ;
            Vector3 surfaceHorizontal = Vector3.ProjectOnPlane(surfaceMove, normal);
            if (Vector3.Dot(surfaceMove, normal) < 0) surfaceVertical = Vector3.zero;
            material.AddForce(n, (-n.externalForces * (material.bounciness + bounciness)/2 + surfaceVertical) * di);
            material.nodes[index].correctedDisplacement = toMove * (material.bounciness + bounciness) / 2 + surfaceVertical;
            surfaceHorizontal = surfaceHorizontal.normalized * Mathf.Clamp(Vector3.Project(n.externalForces, normal).magnitude * friction - surfaceHorizontal.magnitude, 0, surfaceHorizontal.magnitude);
            n.predictedPosition = n.position + surfaceVertical + surfaceHorizontal;
            return;
        }

        if (Vector3.Dot(normal, toMove) >= 0)
        {
            n.velocity = Vector3.zero;
            material.AddForce(n, -n.externalForces * (material.bounciness + bounciness) / 2);
            Vector3 normalForce = Vector3.Project(n.position - n.predictedPosition, normal);
            Vector3 frictionForce = Vector3.ProjectOnPlane(n.position - n.predictedPosition, normal) * friction;
            material.nodes[index].correctedDisplacement = normalForce + frictionForce;
            return;
        }
    }

    public override void UpdateInitial()
    {

    }
}
