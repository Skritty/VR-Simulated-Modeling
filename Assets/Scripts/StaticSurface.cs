using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StaticSurface : MonoBehaviour
{
    [Range(0, 1)]
    [Tooltip("0 friction slides, 1 stops all movement")]
    [SerializeField]
    private float friction = 0;
    [SerializeField]
    [Range(0,1)]
    private float bounciness = 0;
    [SerializeField]
    private bool _static = false;
    public Vector3 deltaDist;
    private Vector3 previousPos;

    public void SetStats(float _friction, float _bounciness, bool isStatic)
    {
        friction = _friction;
        bounciness = _bounciness;
        _static = isStatic;
    }

    private void Awake()
    {
        previousPos = transform.position;
    }

    private void Update()
    {
        deltaDist = transform.position - previousPos;
        previousPos = transform.position;
    }

    public StaticConstraint GenerateConstraint(MixedSimulation material, Vector3 position, int index)
    {
        return new StaticConstraint(material, index, friction, _static, this, bounciness);
    }
}
