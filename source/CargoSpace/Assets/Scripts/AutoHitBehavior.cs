using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bus;
using UnityEngine;

public class AutoHitBehavior : BusParticipant
{
    private DateTime? _nextShot;

    public double Interval;
    public GameObject Turret;
    // Start is called before the first frame update
    void Start()
    {
        AddLifeTimeSubscription(Subscribe("playerTargetSelected", OnPlayerTargetSelected));
    }

    // Update is called once per frame
    void Update()
    {
        if (_nextShot == null)
        {
            return;
        }
        if (_nextShot <= DateTime.Now)
        {
            Fire(Turret.transform.position, "fake", 9000.1f);
            SetNextShot();
        }
    }

    private void SetNextShot() => _nextShot = DateTime.Now.AddSeconds(Interval);

    private async Task OnPlayerTargetSelected(IReadOnlyDictionary<string, string> body)
    {
        //check if has target
        if (!body.TryGetValue("hasTarget", out var hasTarget))
        {
            //log
            return;
        }
        if (!bool.TryParse(hasTarget, out var has) || !has)
        {
            _nextShot = null;
            return;
        }

        SetNextShot();
    }

    private void Fire(Vector2 source, string type, float strength)
    {
        Publish("turretFired", new Dictionary<string, string>
        {
            { "source", source.ToString() },
            { "type", type },
            { "strength", strength.ToString() }
        });
    }
}
