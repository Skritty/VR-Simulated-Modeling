using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] MeshFilter mf;
    [SerializeField] TMPro.TMP_InputField filename;
    public void MeshToFileFromUI()
    {
        ObjExporter.MeshToFile(mf, filename.text);
    }
}
