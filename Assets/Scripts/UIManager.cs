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

    [SerializeField] Slider stiffness1;
    [SerializeField] Slider stiffness2;
    [SerializeField] Slider pliability1;
    [SerializeField] Slider pliability2;
    [SerializeField] Slider bounce;
    [SerializeField] Toggle experimental;

    private void Start()
    {
        stiffness1.value = mat.compresiveStiffness;
        stiffness2.value = mat.rotationalStiffness;
        pliability1.value = mat.compresivePliability;
        pliability2.value = mat.rotationalPliability;
    }

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
        mat.compresiveStiffness = stiffness1.value;
        mat.rotationalStiffness = stiffness2.value;
        mat.compresivePliability = pliability1.value;
        mat.rotationalPliability = pliability2.value;
        mat.experimentalRotation = experimental.isOn;
        mat.bounciness = 0;
    }
}
