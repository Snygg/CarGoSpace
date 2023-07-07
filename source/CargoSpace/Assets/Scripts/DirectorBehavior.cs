using System;
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
    private GameObject _playerTargeted;

    // Start is called before the first frame update
    void Start()
    {
        _logger = LogManager.Initialize(LogObject);
        AddLifeTimeSubscription(Subscribe("npcCreate", OnCreateNpc));
        AddLifeTimeSubscription(Subscribe("npcCommand", OnNpcCommand));
        AddLifeTimeSubscription(Subscribe("PlayerFired",OnPlayerFired));
    }

    private async Task OnPlayerFired(Dictionary<string, string> arg)
    {
        if (arg == null)
        {
            _logger.Bus.LogError(new ArgumentException("arg was null", nameof(arg)));
            return;
        }

        const string positionKey = "Position";
        if (!arg.ContainsKey(positionKey))
        {
            _logger.Bus.LogError(new ArgumentException($"arg does not contain {positionKey}", nameof(arg)));
            return;
        }
        
        if(!VectorUtils.TryParseVector2(arg[positionKey], out var targetPosition))
        {
            _logger.Bus.LogError(new ArgumentException($"{positionKey} not a valid vector", nameof(arg)));
            return;
        }
        
        //todo: pass in list, so we know all the things we hit
        var playerPosition = PlayerShipObject.transform.position;
        var targetDirection = targetPosition - (Vector2)playerPosition;
        var raycastHit2D = Physics2D.Raycast(playerPosition, targetDirection);
        if (raycastHit2D.collider && raycastHit2D.collider.gameObject)
        {
            var gameObject = raycastHit2D.collider.gameObject;
            //Debug.DrawLine(playerPosition,gameObject.transform.position, Color.red,1);
            _playerTargeted = gameObject;
        }
        else
        {
            _playerTargeted = null;
        }
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
