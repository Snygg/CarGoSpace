using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TargetableBehavior : BusParticipant
{
    string id { get; }

    // Start is called before the first frame update
    void Start()
    {
        Subscribe($"AutoHit{id}", (attack) => OnAutoHitAction(attack));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async Task OnAutoHitAction(Dictionary<string, string> body)
    {
        if (body == null)
        {
            //todo: log this
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

    private void OnLaser(Dictionary<string, string> body)
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

    private void OnTractorBeam(Dictionary<string, string> body)
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
