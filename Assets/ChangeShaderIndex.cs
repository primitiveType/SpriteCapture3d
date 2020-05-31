using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChangeShaderIndex : MonoBehaviour
{
    public int Max = 8;

    [SerializeField] private GameObject materialGameObject;

    private Material m_MyMat;
    private Material MyMat => m_MyMat != null ? m_MyMat : m_MyMat = materialGameObject.GetComponent<Renderer>().sharedMaterial;

    private Camera mainCam;
    private static readonly int Perspective = Shader.PropertyToID("Perspective");

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
    }

    public void TestFunction(string testParam)
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float index = MyMat.GetFloat(Perspective);
        MyMat.SetFloat(Perspective, GetFacingIndex());
        // transform.LookAt(mainCam.transform);
    }

    private float GetFacingIndex()
    {
        var camPosition = new Vector3(mainCam.transform.position.x, 0, mainCam.transform.position.z);
        var myPosition = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 camDirection = Vector3.Normalize(camPosition - myPosition);
        Debug.DrawLine(transform.position, transform.position + camDirection);
        Debug.Log("CamDirection" + camDirection);

        // Debug.Log(camDirection);
        float angle = Vector3.SignedAngle(transform.forward, camDirection, Vector3.up);
//        Debug.Log(angle);

        //half of one perspective rotation
        var halfStep = 360f / 16f;
        //offset to account for baked-in spritesheet rotation
        var offset = 180;
        //the angle after accounting for offsets
        var resultingAngle = (angle + offset);
        Debug.Log(resultingAngle);
        var index = (resultingAngle / 360f) * Max;
        var answer = Max - index;
        return answer;//invert because rotation direction is wrong
    }
}