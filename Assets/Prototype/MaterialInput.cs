using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialInput : MonoBehaviour
{
    public MaterialStructureGrid material;
    public MaterialPhysicsSinglePass mp;
    private Vector2 start;
    public float brushRadius = 4f;
    public Vector2 brushStrengthFalloff = new Vector2(1,0);

    private void Update()
    {
        if(material != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                start = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(0))
            {
                //material.AddForceAt(start, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - start);
                material.AddForceOverCircle(start, brushRadius, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - start, brushStrengthFalloff);
            }
        }
        
        if(mp != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                start = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(0))
            {
                //material.AddForceAt(start, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - start);
                //mp.AddForceOverCircle(start, brushRadius, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - start, brushStrengthFalloff);
            }
        }
    }
}
