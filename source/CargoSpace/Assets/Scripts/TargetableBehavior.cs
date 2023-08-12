using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bus;
using Logging;
using Module;
using UnityEngine;

public class TargetableBehavior : ModuleBusParticipant, ITargetable
{
    string id { get; }
    public bool Indestructable;
    public bool IsPlayer;
    bool ITargetable.IsPlayer => IsPlayer;
    public float InitialHealth = 1;
    public float CurrentHealth { get; private set; }
    private float _previousPercentHealth =0;
    

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        CurrentHealth = InitialHealth;
        _logger = Logging.LogManager.Initialize(LogObject);
        
        _previousPercentHealth = GetPercent(CurrentHealth, InitialHealth);
    }

    public void OnDamaged(float strength)
    {
        if (Indestructable)
        {
            return;
        }

        var previousHealth = CurrentHealth;
        CurrentHealth = Math.Max(0, CurrentHealth - strength);

        const float epsilon = 0.001f;
        if (!previousHealth.HasChanged(CurrentHealth, epsilon))
        {
            return;
        }

        var percentHealth = GetPercent(CurrentHealth, InitialHealth);
        if (!_previousPercentHealth.HasChanged(percentHealth, epsilon))
        {
            return;
        }
        Publish(ModuleEvents.HpPercentChanged, new Dictionary<string, string>
        {
            {"Previous", _previousPercentHealth.ToString()},
            {"PercentHp", percentHealth.ToString()}
        });
        _previousPercentHealth = percentHealth;
        
    }

    private static float GetPercent(float current, float max)
    {
        var percentHealth = Mathf.Clamp((current / max) * 100, 0, 100);
        return percentHealth;
    }

    private void OnTractorBeam(string source, float strength)
    {
        //...
    }
}