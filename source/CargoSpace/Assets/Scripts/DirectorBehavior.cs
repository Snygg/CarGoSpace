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
    private List<GameObject> _npcs = new List<GameObject>();
    private LogBehavior _logger;

    public GameObject DummyNpc;
    public GameObject DirectorObject;
    public GameObject PlayerTargeted { get; private set; }
    public GameObject PlayerShipObject;
    public GameObject LaserPrefab;

    // Start is called before the first frame update
    void Start()
    {
        _logger = LogManager.Initialize(LogObject);
        AddLifeTimeSubscription(Subscribe("npcCreate", OnCreateNpc));
        AddLifeTimeSubscription(Subscribe("npcCommand", OnNpcCommand));
        AddLifeTimeSubscription(Subscribe("playerClicked", OnPlayerClicked));
        AddLifeTimeSubscription(Subscribe("turretFired", OnTurretFired));
    }

    private async Task OnPlayerClicked(Dictionary<string, string> body)
    {
        const string key = "location";
        if (!body.ContainsKey(key))
        {
            _logger.Bus.LogError(new ArgumentException($"arg does not contain {key}", nameof(body)));
            return;
        }

        if (!VectorUtils.TryParseVector2(body[key], out var location))
        {
            _logger.Bus.LogError(new ArgumentException($"{key} not a valid vector", nameof(body)));
            return;
        }

        var clicked = GetClickedObject(GetRayCastHitList(location));
        if (_npcs.Contains(clicked)) // next: needs to only set target if turret is selected
        {
            PlayerTargeted = clicked;
            //publish 
            Publish("playerTargetSelected", new Dictionary<string, string>
            {
                { "hasTarget", "true" }
            });
        }
        //else if (clicked == <one of the turrets>)
        //{
        //embellish to add turrets, check if turret is selected, then go into targeting mode
        //}
        else
        {
            PlayerTargeted = null;
            Publish("playerTargetSelected", new Dictionary<string, string>
            {
                { "hasTarget", "false" }
            });
        }
    }

    private List<RaycastHit2D> GetRayCastHitList(Vector2 clickLocation)
    {
        List<RaycastHit2D> hitResults = new List<RaycastHit2D>();
        Physics2D.Raycast(clickLocation, Vector2.up * .00001f, new ContactFilter2D(), hitResults);
        return hitResults;
    }

    private GameObject GetClickedObject (List<RaycastHit2D> hitResults)
    {
        var result = hitResults.FirstOrDefault(hr =>
            hr.collider &&
            hr.collider.gameObject &&
            hr.collider.gameObject.GetComponent<TargetableBehavior>());

        if (!result)
        {
            return null;
        }

        return result.collider.gameObject;
    }

    private async Task OnTurretFired(Dictionary<string, string> body)
    {
        const string key = "source";
        if (!body.ContainsKey(key))
        {
            _logger.Bus.LogError(new ArgumentException($"arg does not contain {key}", nameof(body)));
            return;
        }

        if (!VectorUtils.TryParseVector2(body[key], out var location))
        {
            _logger.Bus.LogError(new ArgumentException($"{key} not a valid vector", nameof(body)));
            return;
        }


        FireLaser(location, (Vector2) PlayerTargeted.transform.position - location);
    }



    private void FireLaser(Vector2 source, Vector2 targetDirection)
    {
        List<RaycastHit2D> hitResults = new List<RaycastHit2D>();
        Physics2D.Raycast(source, targetDirection, new ContactFilter2D(), hitResults);
        var raycastHit2D = hitResults.FirstOrDefault(hr =>
            hr.collider &&
            hr.collider.gameObject &&
            hr.collider.gameObject.GetComponent<TargetableBehavior>());
        if (raycastHit2D)
        {
            var gameObject = raycastHit2D.collider.gameObject;
            RenderLazer(source, gameObject.transform.position);
            _logger.Combat.LogDebug("Player hit target");
        }
        else
        {
            RenderLazer(source, targetDirection * 100);
            _logger.Combat.LogVerbose("Player fired and missed");
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
            var startPosition = npc.transform.position;
            var endPosition = PlayerShipObject.transform.position;
            
            RenderLazer(startPosition, endPosition);
        }
        
    }

    private void RenderLazer(Vector3 startPosition, Vector3 endPosition)
    {
        var go = Instantiate<GameObject>(LaserPrefab, Vector3.zero, new Quaternion());
        var lineRenderer = go.GetComponentInChildren<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(new[] {startPosition, endPosition});
    }
}
