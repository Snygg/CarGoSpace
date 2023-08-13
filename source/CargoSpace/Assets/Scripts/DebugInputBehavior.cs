using System.Collections;
using System.Collections.Generic;
using Bus;
using Scene;
using UnityEngine;

public class DebugInputBehavior : SceneBusParticipant
{
    // Update is called once per frame
    protected void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width
                || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
            {
                return;
            }

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPosition.z = 0;
            Dictionary<string, string> body = new Dictionary<string, string>();
            body.Add("location", worldPosition.ToString());
            //...
            Publish(SceneEvents.NpcCreate, body);
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            Dictionary<string, string> body = new Dictionary<string, string>();
            body.Add("command", "fire");
            //...
            Publish(SceneEvents.NpcCommand, body);
        }
    }
}
