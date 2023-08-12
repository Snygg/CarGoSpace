using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReticleBehavior : MonoBehaviour
{
    public bool IsActive;
    public GameObject TargetedObject;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if (TargetedObject)
        {
            transform.position = TargetedObject.transform.position;
        }
        
    }
}
