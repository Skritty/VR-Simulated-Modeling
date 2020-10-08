using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Constraint
{
    protected MixedSimulation material;
    public Constraint(MixedSimulation m)
    {
        material = m;
    }
    public abstract void ConstrainPositions(float di);
    public abstract void UpdateInitial();
}
