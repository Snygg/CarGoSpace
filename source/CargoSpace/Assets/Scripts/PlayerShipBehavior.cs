using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class PlayerShipBehavior : BusParticipant
{
    public GameObject PlayerShip;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SendComms(MessageType message)
    {
        //to do: don't use some undefined literal
        var msg = new Dictionary<string, string>();
        msg.Add("CommsMessage", message.ToString());
        Publish("CommsMessage", msg);
    }

    //to do: move to shared location
    enum MessageType
    {
        plead,
        threaten,
        trade
    }

    public void Move(Vector2 vector)
    {
        var current = PlayerShip.transform.position;

        PlayerShip.transform.position = new Vector3(current.x + vector.x, current.y + vector.y, current.z);
        PublishLocation();
    }

    private void PublishLocation()
    {
        var body = new Dictionary<string, string>();
        body["position"] = PlayerShip.transform.position.ToString();

        Publish("PlayerTransform", body);
    }
}
