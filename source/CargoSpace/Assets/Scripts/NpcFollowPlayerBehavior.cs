using System.Collections.Generic;
using Module;
using UnityEngine;

public class NpcFollowPlayerBehavior : ModuleBusParticipant
{
    public GameObject DirectorObject;

    private MovableBehavior _mv=null;
    private MovableBehavior _movable
    {
        get
        {
            if (!_mv)
            {
                _mv = GetComponent<MovableBehavior>();
            }
            return _mv;
        }
    }

    public float MinFollowDistance = 2;

    private PlayerTrackerBehavior _playerTrackerBehavior;

    protected override void Awoke()
    {
        AddLifeTimeSubscription(Subscribe(ModuleEvents.HpPercentChanged, OnHpPercentChanged));
    }

    // Start is called before the first frame update
    void Start()
    {
        if (DirectorObject)
        {
            _playerTrackerBehavior = DirectorObject.GetComponent<PlayerTrackerBehavior>();    
        } 
    }

    void FixedUpdate()
    {
        var curDistance = Vector3.Distance(transform.position, _playerTrackerBehavior.PlayerPosition);
        if (curDistance <= MinFollowDistance)
        {
            return;
        }
        _movable.ThrustTowards(_playerTrackerBehavior.PlayerPosition);
    }

    private void OnHpPercentChanged(IReadOnlyDictionary<string,string> body)
    {
        if (!body.TryGetFloat("PercentHp", out var percentHp))
        {
            return;
        }
        var speedFactor = percentHp > 30
            ? _movable.MaxSpeedFactor
            : percentHp > 0
            ? _movable.MaxSpeedFactor * 0.5f
            : 0;
        _movable.SetCurrentSpeedFactor(speedFactor);
    }

    public void SetDirectorObject(GameObject gameObject)
    {
        if (!gameObject)
        {
            return;
        }

        DirectorObject = gameObject;
        _playerTrackerBehavior = gameObject.GetComponent<PlayerTrackerBehavior>();
    }
}