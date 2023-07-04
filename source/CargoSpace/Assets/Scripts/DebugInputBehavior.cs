using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugInputBehavior : BusParticipant
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width
                || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
            {
                return;
            }
            Dictionary<string, string> body = new Dictionary<string, string>();
            body.Add("location", Input.mousePosition.ToString());
            //...
            Publish("npcCreate", body);
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            Dictionary<string, string> body = new Dictionary<string, string>();
            body.Add("command", "fire");
            //...
            Publish("npcCommand", body);
        }
1    }
}
