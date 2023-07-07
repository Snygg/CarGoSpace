using System.Collections;
using System.Collections.Generic;
using Bus;
using UnityEngine;

public class InputBehavior : BusParticipant
{
    // Start is called before the first frame update
    void Start()
    {
        
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
            Publish("Input", body);
        }

        var fire1Axis = Input.GetAxis("Fire1");
        if (fire1Axis > 0)
        {
            var x = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var y = 0;
            var r = new RaycastHit2D();
            var rc = Physics2D.Raycast(x, Vector2.up);
            var gameObject = rc.collider.gameObject;
        }
    }
}
