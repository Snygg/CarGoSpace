using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bus;
using Logging;
using UnityEngine;

public class TargetableBehavior : BusParticipant
{
    private LogBehavior _logger;
    string id { get; }
    public bool Indestructable;
    public bool IsPlayer;

    // Start is called before the first frame update
    void Start()
    {
        _logger = Logging.LogManager.Initialize();
        Subscribe($"AutoHit{id}", OnAutoHitAction);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async Task OnAutoHitAction(IReadOnlyDictionary<string, string> body)
    {
        if (body == null)
        {
            _logger.Combat.LogError(new ArgumentException("body cannot be null", nameof(body)), context:this);
            return;
        }

        if (body.TryGetValue("type", out string hitType))
        {
            switch (hitType)
            {
                case "laser":
                    OnLaser(body);
                    break;
                case "tractor":
                    OnTractorBeam(body);
                    break;
            }
        }
    }

    private void OnLaser(IReadOnlyDictionary<string, string> body)
    {
        if (Indestructable)
        {
            return;
        }
        if (body.TryGetValue("strength", out string strength))
        {
            //calculate and apply damage
        }

        if (body.TryGetValue("source", out string source))
        {
            //apply aggro if needed
        }
    }

    private void OnTractorBeam(IReadOnlyDictionary<string, string> body)
    {
        if (body.TryGetValue("strength", out string strength))
        {
            //calculate and apply damage
        }

        if (body.TryGetValue("source", out string source))
        {
            //apply aggro if needed
        }
    }
}
