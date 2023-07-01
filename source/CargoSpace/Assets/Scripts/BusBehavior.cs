using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusBehavior : MonoBehaviour
{
    private readonly Dictionary<string, HashSet<SubscriptionBehavior>> Subscriptions = new Dictionary<string, HashSet<SubscriptionBehavior>>();

    public void Publish(string topic, Dictionary<string, string> body)
    {
        if (!Subscriptions.TryGetValue(topic, out var sublist))
        {
            return;
        }

        foreach(var sub in sublist)
        {
            sub.Callback(body).Wait();
        }
    }

    public IDisposable Subscribe(string topic, Func<Dictionary<string,string>, Task> callback)
    {
        SubscriptionBehavior sub = null;
        sub = new SubscriptionBehavior(callback, () => Unsubscribe(topic, sub));
        
        if (!Subscriptions.TryGetValue(topic, out var sublist))
        {
            sublist = new HashSet<SubscriptionBehavior>();
            Subscriptions.Add(topic, sublist);
        }

        sublist.Add(sub);

        return sub;
    }

    public static readonly Dictionary<string, string> EmptyDictionary = new Dictionary<string, string>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Unsubscribe(string topic, SubscriptionBehavior sb)
    {
        if (!Subscriptions.TryGetValue(topic, out var sublist))
        {
            return;
        }

        sublist.Remove(sb);
    }
}
