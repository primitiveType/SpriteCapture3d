using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAroundObject : MonoBehaviour
{
    public GameObject target;
    public Vector3 axis = Vector3.up;

    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.RotateAround(target.transform.position, axis, speed);
        transform.LookAt(target.transform);
    }
}
