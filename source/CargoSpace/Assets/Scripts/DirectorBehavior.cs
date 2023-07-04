using Bus;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DirectorBehavior : BusParticipant
{
    public GameObject DummyNpc;
    public GameObject DirectorObject;

    // Start is called before the first frame update
    void Start()
    {
        AddLifeTimeSubscription(Subscribe("npcCreate", OnCreateNpc));
        AddLifeTimeSubscription(Subscribe("npcCommand", OnNpcCommand));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async Task OnCreateNpc(Dictionary<string, string> body)
    {
        if (!body.TryGetValue("location", out string loc) || !loc.TryParseVector3(out var location))
        {
            return;
        }

        var go = Instantiate<GameObject>(DummyNpc, location, new Quaternion());
        var follow = go.GetComponent<NpcFollowPlayerBehavior>();
        follow.DirectorObject = DirectorObject;
    }

    private async Task OnNpcCommand(Dictionary<string, string> body)
    {
        
    }
}
