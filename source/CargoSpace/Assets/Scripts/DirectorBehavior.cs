using Bus;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using UnityEngine;
using UnityEngine.Serialization;

public class DirectorBehavior : BusParticipant
{
    public GameObject DummyNpc;
    public GameObject DirectorObject;
    private List<GameObject> _npcs = new List<GameObject>();
    public GameObject PlayerShipObject;
    [FormerlySerializedAs("LazerPrefab")] public GameObject LaserPrefab;
    private LogBehavior _logger;

    // Start is called before the first frame update
    void Start()
    {
        _logger = LogManager.Initialize(LogObject);
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
        
        //todo: remove them from the list when they get destroyed
        _npcs.Add(go);
    }

    private async Task OnNpcCommand(Dictionary<string, string> body)
    {
        if (body == null)
        {
            _logger.Bus.LogError("body was null", context:this);
            return;
        }

        if (!body.ContainsKey("command"))
        {
            return;
        }
        _logger.Combat.LogDebug("Got command {0}", values: body);
        foreach (var npc in _npcs)
        {
            var go = Instantiate<GameObject>(LaserPrefab, Vector3.zero, new Quaternion());
            var lineRenderer = go.GetComponentInChildren<LineRenderer>();
            lineRenderer.positionCount=2;
            lineRenderer.SetPositions(new []{npc.transform.position, PlayerShipObject.transform.position});
        }
        
    }
}
