using System;
using System.Collections.Generic;
using System.Linq;
using Logging;
using Module;
using Npc;
using Scene;
using UnityEngine;
using Utils;

public class DirectorBehavior : SceneBusParticipant
{
    private LogBehavior _logger;

    public GameObject ModuleRootPrefab;
    public GameObject NpcEnginePrefab;
    public GameObject EmptyModulePrefab;
    public GameObject NpcList; 
    public GameObject PlayerTargeted { get; private set; }
    public GameObject PlayerShipObject;
    public GameObject LaserPrefab;
    public GameObject PlayerReticle;

    // Start is called before the first frame update
    private void Start()
    {
        _logger = LogManager.GetLogger();
        AddLifeTimeSubscription(Subscribe(SceneEvents.NpcCreate, OnCreateNpc));
        AddLifeTimeSubscription(Subscribe(SceneEvents.NpcCommand, OnNpcCommand));
        AddLifeTimeSubscription(Subscribe(SceneEvents.PlayerClicked, OnPlayerClicked));
        AddLifeTimeSubscription(Subscribe(SceneEvents.TurretFired, OnTurretFired));
        AddLifeTimeSubscription(Subscribe(SceneEvents.KeyPressed, OnKeyPressed));
    }

    private void OnKeyPressed(IReadOnlyDictionary<string, string> body)
    {
        if (!PlayerTargeted)
        {
            return;
        }

        if (!body.TryGetValue("key", out var key))
        {
            return;
        }

        FireWeaponGroup(key);
    }

    private void FireWeaponGroup(string group)
    {
        Publish(SceneEvents.ToggleWeaponGroup, new Dictionary<string, string>
        {
            {"group", group }
        });
    }

    private void OnPlayerClicked(IReadOnlyDictionary<string, string> body)
    {
        const string key = "location";
        if (!body.TryGetVector2(key, out var location))
        {
            _logger.Bus.LogError(new ArgumentException($"{key} not a valid vector", nameof(body)));
            return;
        }

        var clicked = GetClickedObject(GetRayCastHitList(location));
        if (!clicked)
        {
            PlayerTargeted = null;
            var ret = PlayerReticle.GetComponent<ReticleBehavior>();
            ret.TargetedObject = null;
            return;
        }
        var targets = clicked.GetComponentsInChildren<ITargetable>();
        var otherTarget = targets.FirstOrDefault(t => !t.IsPlayer);
        if (otherTarget != null) // next: needs to only set target if turret is selected
        {
            PlayerTargeted = clicked;
            //add reticle , get component elsewhere
            var ret = PlayerReticle.GetComponent<ReticleBehavior>();
            ret.TargetedObject = PlayerTargeted;
        }
        //else if (clicked == <one of the turrets>)
        //{
        //embellish to add turrets, check if turret is selected, then go into targeting mode
        //}
        else
        {
            PlayerTargeted = null;
            var ret = PlayerReticle.GetComponent<ReticleBehavior>();
            ret.TargetedObject = null;
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
            hr.collider.gameObject.TryGetComponent<ITargetable>(out _));

        if (!result)
        {
            return null;
        }

        return result.collider.gameObject;
    }

    private void OnTurretFired(IReadOnlyDictionary<string, string> body)
    {
        if (!PlayerTargeted)
        {
            return;
        }
        const string key = "source";
        if (!body.TryGetVector2(key, out var location))
        {
            _logger.Bus.LogError(new ArgumentException($"{key} not a valid vector", nameof(body)));
            return;
        }

        if (body == null)
        {
            _logger.Combat.LogError(new ArgumentException("body cannot be null", nameof(body)), context: this);
            return;
        }

        if (body.TryGetValue("type", out string hitType))
        {
            if (!body.TryGetFloat("strength", out float strength))
            {
                //log
                return;
            }

            if (!body.TryGetValue("source", out string source))
            {
                //log
                return;
            }

            switch (hitType)
            {
                case "laser":
                    FireLaser(location, (Vector2)PlayerTargeted.transform.position - location, strength);
                    break;
                case "tractor":
                    //FireTractorBeam(source, strength);
                    break;
            }
        }
    }

    private void FireLaser(Vector2 source, Vector2 targetDirection, float strength)
    {
        List<RaycastHit2D> hitResults = new List<RaycastHit2D>();
        Physics2D.Raycast(source, targetDirection, new ContactFilter2D(), hitResults);
        var raycastHit2D = hitResults.FirstOrDefault(hr =>
            hr.collider &&
            hr.collider.gameObject);
        if (raycastHit2D && raycastHit2D.collider.gameObject.TryGetComponent<ITargetable>(out var tgt))
        {
            RenderLazer(source, raycastHit2D.point);
            tgt.OnDamaged(strength);
            _logger.Combat.LogDebug("Player hit target");
        }
        else
        {
            RenderLazer(source, targetDirection * 100);
            _logger.Combat.LogVerbose("Player fired and missed");
        }
    }

    private void OnCreateNpc(IReadOnlyDictionary<string, string> body)
    {
        const string locationKey = "location";
        if (!body.TryGetVector3(locationKey, out var location))
        {
            _logger.Bus.LogError(new Exception($"{locationKey} was not a valid vector"));
            return;
        }

        var npcRoot = Instantiate(ModuleRootPrefab, location, new Quaternion());
        var timeStamp = DateTime.Now.Ticks % 900_000_000_000L;
        npcRoot.name = $"npc {timeStamp}";

        //todo: remove them from the list when they get destroyed
        npcRoot.transform.parent = NpcList.transform;
        
        var moduleRoot = npcRoot.GetComponent<ModuleHostBehaviour>();
        
        var npcEngine = Instantiate(NpcEnginePrefab, npcRoot.transform);
        var engineRoot = npcEngine.GetComponent<IModuleRoot>();
        var engineConnectionBehavior = CreateConnection(timeStamp, npcRoot, npcEngine);;
        
        moduleRoot.AddModule(engineRoot, engineConnectionBehavior);
        var npcEngineModule = npcEngine.AddComponent<NpcModuleBehavior>();
        npcEngineModule.Attach(engineConnectionBehavior);
        
        var empty = Instantiate(EmptyModulePrefab, npcRoot.transform);
        var emptyRoot = empty.GetComponent<IModuleRoot>();
        var emptyConnectionBehavior = CreateConnection(timeStamp, npcRoot, empty);

        moduleRoot.AddModule(emptyRoot, emptyConnectionBehavior);
        var npcEmptyModule = empty.AddComponent<NpcModuleBehavior>();
        npcEmptyModule.Attach(emptyConnectionBehavior);

        var npcFollower = npcRoot.AddComponent<NpcFollowBehavior>();
        npcFollower.target = PlayerShipObject.transform;
    }

    private static TransformConnectionBehavior CreateConnection(long timeStamp, GameObject parent, GameObject child)
    {
        var emptyConnection = new GameObject($"connection {timeStamp}");
        var emptyTransformBehavior = emptyConnection.AddComponent<TransformConnectionBehavior>();
        emptyTransformBehavior.Attach(parent, emptyConnection);
        emptyTransformBehavior.Attach(parent, child);
        return emptyTransformBehavior;
    }

    private void OnNpcCommand(IReadOnlyDictionary<string, string> body)
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
        
        //todo: get closest module?
        var targetModule = PlayerShipObject.GetComponentsInChildren<ITargetable>().FirstOrDefault();
        if (targetModule == null)
        {
            return;
        }
        var endPosition = ((MonoBehaviour)targetModule).transform.position;
        foreach (var npc in NpcList.GetComponentsInChildren<ModuleHostBehaviour>().Select(b=>b.gameObject))
        {
            //todo: search for a (not written yet) weapon component
            var sourceModule = npc.GetComponentsInChildren<Rigidbody2D>().FirstOrDefault();

            if (sourceModule == null)
            {
                continue;
            }
            var startPosition = sourceModule.gameObject.transform.position;
            RenderLazer(startPosition, endPosition);
        }
        
    }

    private void RenderLazer(Vector3 startPosition, Vector3 endPosition)
    {
        var go = Instantiate<GameObject>(LaserPrefab, startPosition, new Quaternion());
        var lineRenderer = go.GetComponentInChildren<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(new[] {startPosition, endPosition});
    }
}