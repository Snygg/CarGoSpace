using System.Collections;
using System.Collections.Generic;
using Bus;
using Scene;
using UnityEngine;

public class InputBehavior : SceneBusParticipant
{
    private bool isFiring;
    // Update is called once per frame
    protected void Update()
    {
        var vert = Input.GetAxis("Vertical");
        var horz = Input.GetAxis("Horizontal");
        if (vert != 0.0 || horz != 0)
        {
            //todo: pass vector2 here
            var body = new Dictionary<string, string>();
            body["vert"] = vert.ToString();
            body["horz"] = horz.ToString();
            Publish(SceneEvents.Input, body);
        }

        var fire1Axis = Input.GetAxis("Fire1");
        var wasFiring = isFiring;
        isFiring = fire1Axis > 0;

        if (wasFiring && !isFiring)
        {
            var worldLocation = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Publish(SceneEvents.PlayerClicked, new Dictionary<string, string>
            {
                {"location",  ((Vector2) worldLocation).ToString()}
            });
        }

        var key3 = Input.GetKeyDown(KeyCode.Alpha3);
        var key4 = Input.GetKeyDown(KeyCode.Alpha4);
        
        if (key3)
        {
            Publish(SceneEvents.KeyPressed,
                new Dictionary<string, string>
                {
                    { "key", "3" }
                });
        }

        if (key4)
        {
            Publish(SceneEvents.KeyPressed,
                new Dictionary<string, string>
                {
                    { "key", "333" }
                });
        }
    }
}
