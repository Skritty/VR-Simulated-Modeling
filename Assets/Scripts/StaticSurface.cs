using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StaticSurface : MonoBehaviour
{
    private Collider c;
    [Range(0, 1)]
    [Tooltip("0 friction slides, 1 stops all movement")]
    [SerializeField]
    private float friction = 0;
    [SerializeField]
    [Range(0,1)]
    private float bounciness;
    [SerializeField]
    private bool _static = false;
    public Vector3 previousPos;

    private void Awake()
    {
        c = GetComponent<Collider>();
        previousPos = transform.position;
    }

    public StaticConstraint GenerateConstraint(MixedSimulation material, Vector3 position, int index)
    {
        return new StaticConstraint(material, index, transform.up, friction, _static, c, bounciness);
    }
}
