using System.Collections;
using System.Collections.Generic;
using Bus;
using UnityEngine;

public class AutoHitBehavior : BusParticipant
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Fire(string target, string source, string type, float strength)
    {
        var body = new Dictionary<string, string>();
        body.Add("target", target);
        body.Add("source", source);
        body.Add("strength", strength.ToString());
        body.Add("type", type);
        Publish($"AutoHit{target}", body);
    }
}
