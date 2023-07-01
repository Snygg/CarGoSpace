using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class BusParticipant : MonoBehaviour
{
    public GameObject BusObject;
    private BusBehavior _bus;
    private List<IDisposable> _subscriptions = new List<IDisposable>();

    // Awake is called before Start
    void Awake()
    {
        if (BusObject == null)
        {
            //todo: log warning: you forgot to attach BusObject
            int x = 0;
        }
        _bus = BusObject.GetComponent<BusBehavior>();
        InitializeSubscriptions();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    /// <summary>
    /// Override this when subscribing in Start() is too late in the lifetime of a monobehavior. This will be
    /// called immediately after Awake() on the base class
    /// </summary>
    protected virtual void InitializeSubscriptions() { }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        foreach (var subscription in _subscriptions ?? Enumerable.Empty<IDisposable>())
        {
            try
            {
                subscription.Dispose();
            }
            catch 
            {
                //todo: log this, but dont throw
            }
        }
    }

    protected void Publish(string topic, Dictionary<string, string> body)
    {
        _bus.Publish(topic, body);
    }

    protected IDisposable Subscribe(string topic, Func<Dictionary<string, string>, Task> handler)
    {
        if (_bus == null)
        {
            //todo: log this
            int x = 0;
        }
        return _bus.Subscribe(topic, handler);
    }

    protected void AddLifeTimeSubscription(IDisposable subscription)
    {
        if (subscription == null)
        {
            //todo: log this, but dont throw
            return;
        }
        _subscriptions.Add(subscription);
    }
}