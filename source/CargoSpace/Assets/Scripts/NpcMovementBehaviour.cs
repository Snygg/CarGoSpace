using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcMovementBehaviour : MonoBehaviour
{
    public GameObject NpcShip;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        NpcShip.transform.position = NpcShip.transform.position + (Vector3.right / 1000);
    }
}
