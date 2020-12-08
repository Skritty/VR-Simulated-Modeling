using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] MixedSimulation mat;
    [SerializeField] Transform spawn;
    [SerializeField] MeshFilter mf;
    [SerializeField] TMPro.TMP_InputField filename;

    [SerializeField] Slider stiffness;
    [SerializeField] Slider pliability;
    [SerializeField] Slider bounce;

    public void MeshToFileFromUI()
    {
        ObjExporter.MeshToFile(mf, filename.text);
    }

    public void ResetMesh()
    {
        mat.transform.position = spawn.position;
        mat.ResetAll();
    }

    public void UpdateMatStats()
    {
        mat.stiffness = stiffness.value;
        mat.pliability = pliability.value;
        mat.bounciness = bounce.value;
    }
}
