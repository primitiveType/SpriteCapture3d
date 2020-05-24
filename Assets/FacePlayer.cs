using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    private Camera ToFace;
    // Start is called before the first frame update
    void Start()
    {
        ToFace = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(ToFace.transform);
    }
}
