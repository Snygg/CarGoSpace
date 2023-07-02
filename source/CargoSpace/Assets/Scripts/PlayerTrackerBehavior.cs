using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerTrackerBehavior : BusParticipant
{
    public Vector3 PlayerPosition { get; private set; } = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        AddLifeTimeSubscription(Subscribe("PlayerTransform", OnPlayerMoved));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private async Task OnPlayerMoved(Dictionary<string, string> arg)
    {
        if (arg == null)
        {
            //todo: log this
            return;
        }

        if (!VectorUtils.TryParseVector3(arg["position"], out var vec))
        {
            //todo: log this
            return;
        }

        PlayerPosition = vec;
    }
}
