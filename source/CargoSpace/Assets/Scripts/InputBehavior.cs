using System.Collections;
using System.Collections.Generic;
using Bus;
using UnityEngine;

public class InputBehavior : BusParticipant
{
    private bool isFiring;
    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
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
        var wasFiring = isFiring;
        isFiring = fire1Axis > 0;

        if (wasFiring && !isFiring)
        {
            var worldLocation = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Publish("playerClicked", new Dictionary<string, string>
            {
                {"location",  ((Vector2) worldLocation).ToString()}
            });
        }
    }
}
