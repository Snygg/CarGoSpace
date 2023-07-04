using System.Collections;
using System.Collections.Generic;
using Bus;
using UnityEngine;

public class InputBehavior : MonoBehaviour
{
    public GameObject BusObj;
    private BusBehavior Bus;
    // Start is called before the first frame update
    void Start()
    {
        Bus = BusManager.Initialize(BusObj);
    }

    // Update is called once per frame
    void Update()
    {
        var vert = Input.GetAxis("Vertical");
        var horz = Input.GetAxis("Horizontal");
        if (vert != 0.0 || horz != 0)
        {
            var body = new Dictionary<string, string>();
            body["vert"] = vert.ToString();
            body["horz"] = horz.ToString();
            Bus.Publish("Input", body);
        }
    }
}
