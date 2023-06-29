using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShipBehavior : MonoBehaviour
{
    public GameObject Bus;
    private BusBehavior BusBehavior;
    // Start is called before the first frame update
    void Start()
    {
        BusBehavior = Bus.GetComponent<BusBehavior>();
        BusBehavior.Publish("InitObj", BusBehavior.EmptyDictionary);
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
        BusBehavior.Publish("CommsMessage", msg);
    }

    //to do: move to shared location
    enum MessageType
    {
        plead,
        threaten,
        trade
    }
}
