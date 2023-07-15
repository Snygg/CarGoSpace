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
    public float InitialHealth = 1;
    public float CurrentHealth { get; private set; }
    public GameObject Module;
    private FixedJoint2D joint;
    /// <summary>
    /// This should contain a list of sibling behaviors which implement the <see cref="IDamageable"/> interface
    /// </summary>
    public List<MonoBehaviour> DamageableChildren;

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = InitialHealth;
        _previousPercentHealth = GetPercent(CurrentHealth, InitialHealth);
        _logger = Logging.LogManager.Initialize();
        
        SetModule(Module);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private float _previousPercentHealth =0;
    public void ApplyDamage(float strength)
    {
        if (Indestructable)
        {
            return;
        }

        var previousHealth = CurrentHealth;
        CurrentHealth = Math.Max(0, CurrentHealth - strength);

        if (CurrentHealth == 0)
        {
            Destroy(joint);
            Module.transform.parent = null;
        }

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
        foreach (var damageableChild in DamageableChildren.ToArray())
        {
            if (!damageableChild)
            {
                continue;
            }

            if (!(damageableChild is IDamageable damageable))
            {
                _logger.Combat.LogWarning($"this object is in the damageable list, but does not implement {nameof(IDamageable)}:{damageableChild.name}");
                DamageableChildren.Remove(damageableChild);
                continue;
            }
            damageable.OnDamageChanged(percentHealth);
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

    /// <summary>
    /// Should only be used to initialize an object that has just been instantiated from prefab
    /// </summary>
    /// <param name="targetableBehaviorGameObject"></param>
    public void SetModule(GameObject targetableBehaviorGameObject)
    {
        if (!targetableBehaviorGameObject)
        {
            return;
        }

        joint = targetableBehaviorGameObject.GetComponent<FixedJoint2D>();
    }
}

public interface IDamageable
{
    void OnDamageChanged(float percentHp);
}
