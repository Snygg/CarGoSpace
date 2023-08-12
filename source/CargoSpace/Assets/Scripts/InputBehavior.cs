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

        var key3 = Input.GetKeyDown(KeyCode.Alpha3);
        var key4 = Input.GetKeyDown(KeyCode.Alpha4);
        
        var keyPressedTopic = "keyPressed";

        if (key3)
        {
            Publish(keyPressedTopic,
                new Dictionary<string, string>
                {
                    { "key", "3" }
                });
        }

        if (key4)
        {
            Publish(keyPressedTopic,
                new Dictionary<string, string>
                {
                    { "key", "333" }
                });
        }
    }
}
