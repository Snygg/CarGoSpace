using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bus;
using Logging;
using UnityEngine;

public class TargetableBehavior : BusParticipant, ITargetable
{
    private LogBehavior _logger;
    string id { get; }
    public bool Indestructable;
    public bool IsPlayer;
    public float InitialHealth = 1;
    public float CurrentHealth { get; private set; }
    private float _previousPercentHealth =0;
    private List<IDamageable> _damageables;
    

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = InitialHealth;
        _previousPercentHealth = GetPercent(CurrentHealth, InitialHealth);
        _logger = Logging.LogManager.Initialize();
        
        SetModule(gameObject);
    }

    public void SetModule(GameObject module)
    {
        if (!module)
        {
            return;
        }
        _damageables = new List<IDamageable>(module.GetComponentsInChildren<IDamageable>());
        
        //todo: create an IsDirty flag and re-find damageables/children when it is true
    }

    // Update is called once per frame
    void Update()
    {
        
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

        _previousPercentHealth = percentHealth;
        foreach (var damageable in _damageables)
        {
            damageable.OnHpPercentChanged(percentHealth);
        }
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