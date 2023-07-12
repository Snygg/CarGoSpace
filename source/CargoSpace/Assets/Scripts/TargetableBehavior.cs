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

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = InitialHealth;
        _logger = Logging.LogManager.Initialize();
        
        if (Module)
        {
            joint = Module.GetComponent<FixedJoint2D>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    public void ApplyDamage(float strength)
    {
        if (Indestructable)
        {
            return;
        }

        CurrentHealth = Math.Max(0, CurrentHealth - strength);

        if (CurrentHealth == 0)
        {
            Destroy(joint);
            Module.transform.parent = null;
        }
    }

    private void OnTractorBeam(string source, float strength)
    {
        //...
    }
}
