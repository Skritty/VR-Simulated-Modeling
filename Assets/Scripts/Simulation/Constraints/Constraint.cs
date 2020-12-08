using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Constraint
{
    protected MixedSimulation material;
    protected Vector3 storedMovement = Vector3.zero;
    public Constraint(MixedSimulation m)
    {
        material = m;
    }
    public abstract void ConstrainPositions(float di);
    public abstract void UpdateInitial();
    public abstract void Reset();
}
