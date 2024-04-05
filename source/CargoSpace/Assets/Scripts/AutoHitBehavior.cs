using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bus;
using Scene;
using UnityEngine;

public class AutoHitBehavior : SceneBusParticipant
{
    private DateTime? _nextShot;

    public double Interval;
    public GameObject Turret;
    public string WeaponGroup = "3";

    // Start is called before the first frame update
    private void Start()
    {
        AddLifeTimeSubscription(Subscribe(SceneEvents.ToggleWeaponGroup, OnToggleWeaponGroup));
    }

    // Update is called once per frame
    protected void Update()
    {
        if (_nextShot == null)
        {
            return;
        }
        if (_nextShot <= DateTime.Now)
        {
            Fire(Turret.transform.position, "laser", 9000.1f);
            SetNextShot();
        }
    }

    private void SetNextShot() => _nextShot = DateTime.Now.AddSeconds(Interval);

    private void OnToggleWeaponGroup(IReadOnlyDictionary<string, string> body)
    {
        //check if has target
        if (!body.TryGetValue("group", out var group))
        {
            //log
            return;
        }
        if (group != WeaponGroup)
        {
            return;
        }

        if (_nextShot != null)
        {
            _nextShot = null;
            return;
        }

        SetNextShot();
    }

    private void Fire(Vector2 source, string type, float strength)
    {
        Publish(SceneEvents.TurretFired, new Dictionary<string, string>
        {
            { "source", source.ToString() },
            { "type", type },
            { "strength", strength.ToString() }
        });
    }
}
