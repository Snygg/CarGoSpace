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
    public GameObject PlayerShipObject;
    public GameObject LaserPrefab;

    // Start is called before the first frame update
    private void Start()
    {
        _logger = LogManager.GetLogger();
        AddLifeTimeSubscription(Subscribe(SceneEvents.NpcCreate, OnCreateNpc));
        AddLifeTimeSubscription(Subscribe(SceneEvents.NpcCommand, OnNpcCommand));
        AddLifeTimeSubscription(Subscribe(SceneEvents.KeyPressed, OnKeyPressed));
    }

    private void OnKeyPressed(IReadOnlyDictionary<string, string> body)
    {
        if (!body.TryGetValue("key", out var key))
        {
            return;
        }

        ToggleWeaponGroup(key);
    }

    private void ToggleWeaponGroup(string group)
    {
        Publish(SceneEvents.ToggleWeaponGroup, new Dictionary<string, string>
        {
            {"group", group }
        });
    }

    private void OnCreateNpc(IReadOnlyDictionary<string, string> body)
    {
        const string locationKey = "location";
        if (!body.TryGetVector3(locationKey, out var location))
        {
            _logger.Bus.LogError(new Exception($"{locationKey} was not a valid vector"));
            return;
        }

        var npcGo = Instantiate(ModuleRootPrefab, location, new Quaternion());
        var timeStamp = DateTime.Now.Ticks % 900_000_000_000L;
        npcGo.name = $"npc {timeStamp}";

        //todo: remove them from the list when they get destroyed
        npcGo.transform.parent = NpcList.transform;
        
        var npcEngine = Instantiate(NpcEnginePrefab, npcGo.transform);
        npcEngine.name = $"mod {timeStamp}.1";
        npcEngine.transform.Translate(-1, 0,0);
        CreateConnection(npcGo, npcEngine);
        
        var empty = Instantiate(EmptyModulePrefab,npcGo.transform);
        empty.name = $"mod {timeStamp}.2";
        CreateConnection(npcGo, empty);
        
        var npcFollower = npcGo.AddComponent<NpcFollowBehavior>();
        npcFollower.target = PlayerShipObject.transform;

        //todo: publish npc created event
        //Publish(SceneEvents.NpcCreated, new Dictionary<string, string>{{"targetId",npcGo.name}}, context:this);
    }

    public static void CreateConnection(GameObject parent, GameObject child)
    {
        if (!parent.TryGetComponent<IModuleHost>(out var moduleHost))
        {
            return;
        }
        
        if (!child.TryGetComponent<IModuleRoot>(out var module))
        {
            return;
        }
        
        var connectionBehavior = child.AddComponent<JointConnectionBehavior>();
        connectionBehavior.Attach(moduleHost, module);

        moduleHost.Attach(connectionBehavior);
        module.Attach(connectionBehavior);
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
        targetModule.OnDamaged(5);
    }

    private void RenderLazer(Vector3 startPosition, Vector3 endPosition)
    {
        var go = Instantiate<GameObject>(LaserPrefab, startPosition, new Quaternion());
        var lineRenderer = go.GetComponentInChildren<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(new[] {startPosition, endPosition});
    }
}