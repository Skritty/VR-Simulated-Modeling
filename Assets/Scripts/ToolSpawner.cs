using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolSpawner : MonoBehaviour
{
    [SerializeField] MixedSimulation sim;
    [Header("Objects")]
    [SerializeField] GameObject sphere;
    [SerializeField] GameObject cube;

    [Header("Settings")]
    [SerializeField] Transform spawn;
    [SerializeField] Slider frictionSlider;
    [SerializeField] TMP_Dropdown shape;

    public void Spawn()
    {
        StaticSurface surface = null;
        switch (shape.value)
        {
            case 0:
                surface = Instantiate(sphere, spawn.position, spawn.rotation).GetComponent<StaticSurface>();
                break;
            case 1:
                surface = Instantiate(cube, spawn.position, spawn.rotation).GetComponent<StaticSurface>();
                break;
        }
        surface.SetStats(frictionSlider.value, 0f, false);
        sim.GenerateExternalConstraints();
    }
}
