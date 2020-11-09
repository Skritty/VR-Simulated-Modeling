using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This external constraint prevents nodes from continuing in the direction of the surface normal, and bounces back forces
/// </summary>
public class StaticConstraint : Constraint
{
    private int index;
    private float friction;
    private bool trueStatic = false;
    private StaticSurface obj;
    private SphereCollider sCollider;
    private BoxCollider bCollider;
    private Rigidbody rb;
    private float bounciness;

    public StaticConstraint(MixedSimulation material, int _index, float _friction, bool _static, StaticSurface _obj, float bounce) : base(material)
    {
        index = _index;
        friction = _friction;
        trueStatic = _static;
        obj = _obj;
        if (obj.GetComponent<SphereCollider>()) sCollider = obj.GetComponent<SphereCollider>();
        if (obj.GetComponent<BoxCollider>()) bCollider = obj.GetComponent<BoxCollider>();
        if (obj.GetComponent<Rigidbody>()) rb = obj.GetComponent<Rigidbody>();
        bounciness = bounce;
    }

    public override void ConstrainPositions(float di)
    {
        MixedSimulation.Node n = material.nodes[index];
        Vector3 normal = Vector3.zero;

        if (sCollider != null)
        {
            if (Vector3.Distance(n.predictedPosition, obj.transform.position) > sCollider.radius * sCollider.transform.localScale.x) return;
            if (trueStatic)
            {
                normal = (n.position - n.predictedPosition).normalized;
            }
            else
            {
                normal = (n.predictedPosition - obj.transform.position).normalized;
            }
            
        }
        else if (bCollider != null)
        {
            if (!PointInBox(n.predictedPosition, bCollider)) return;
            if (trueStatic)
            {
                normal = (n.position - n.predictedPosition).normalized;
            }
            else
            {
                normal = GetBoxSurfaceDirFromCenter(n.predictedPosition, bCollider);
            }
        }
        else return;

        Vector3 toMove = n.position - n.predictedPosition;
        n.velocity = Vector3.zero;

        Vector3 surfaceVertical = Vector3.Project(obj.deltaDist, normal);
        Vector3 surfaceHorizontal = Vector3.ProjectOnPlane(obj.deltaDist, normal);

        if (Vector3.Dot(obj.deltaDist, normal) < 0) surfaceVertical = Vector3.zero;

        if (rb != null)
        {
            material.AddForce(n, (-n.externalForces * (material.bounciness + bounciness) / 2 + surfaceVertical) * rb.mass / n.mass);
            material.nodes[index].correctedDisplacement = (toMove * (material.bounciness + bounciness) / 2 + surfaceVertical) * rb.mass / n.mass;
            rb.velocity += (-n.externalForces * (material.bounciness + bounciness) / 2 - surfaceVertical) * n.mass / rb.mass;
        }
        else
        {
            material.AddForce(n, (-n.externalForces * (material.bounciness + bounciness) / 2 + surfaceVertical));
            material.nodes[index].correctedDisplacement = toMove * (material.bounciness + bounciness) / 2 + surfaceVertical;
        }
        surfaceHorizontal = surfaceHorizontal.normalized * Mathf.Clamp(Vector3.Project(n.externalForces, normal).magnitude * friction - surfaceHorizontal.magnitude, 0, surfaceHorizontal.magnitude);
        n.predictedPosition = n.position + surfaceVertical + surfaceHorizontal;
    }

    private bool PointInBox(Vector3 pos, BoxCollider b)
    {
        Vector3 localPos = b.transform.worldToLocalMatrix.MultiplyPoint3x4(pos);
        if (Mathf.Abs(localPos.x) > b.size.x / 2) return false;
        if (Mathf.Abs(localPos.y) > b.size.y / 2) return false;
        if (Mathf.Abs(localPos.z) > b.size.z / 2) return false;
        return true;
    }

    private Vector3 GetBoxSurfaceDirFromCenter(Vector3 pos, BoxCollider b)
    {
        float tempClosest = 0;
        float closestDist = Mathf.Infinity;
        Vector3 closestNorm = Vector3.zero;
        Vector3 localPos = b.transform.worldToLocalMatrix.MultiplyPoint3x4(pos);
        if ((tempClosest = b.size.x / 2 - Mathf.Abs(localPos.x)) < closestDist)
        {
            closestDist = tempClosest;
            closestNorm = b.transform.right * Mathf.Sign(localPos.x);
        }
        if ((tempClosest = b.size.y / 2 - Mathf.Abs(localPos.y)) < closestDist)
        {
            closestDist = tempClosest;
            closestNorm = b.transform.up * Mathf.Sign(localPos.y);
        }
        if ((tempClosest = b.size.z / 2 - Mathf.Abs(localPos.z)) < closestDist)
        {
            closestDist = tempClosest;
            closestNorm = b.transform.forward * Mathf.Sign(localPos.z);
        }
        return closestNorm;
    }

    public override void UpdateInitial()
    {

    }
}
