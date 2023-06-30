using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShipBehavior : MonoBehaviour
{
    public GameObject BusObj;
    private BusBehavior Bus;
    public GameObject PlayerShip;

    // Start is called before the first frame update
    void Start()
    {
        Bus = BusObj.GetComponent<BusBehavior>();
        Bus.Publish("InitObj", BusBehavior.EmptyDictionary);
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
        Bus.Publish("CommsMessage", msg);
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
        //todo: move the ship
        var current = PlayerShip.transform.position;

        PlayerShip.transform.position = new Vector3(current.x + vector.x, current.y + vector.y, current.z);
    }
}
