using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusBehavior : MonoBehaviour
{
    private readonly Dictionary<string, HashSet<Subscription>> Subscriptions = new Dictionary<string, HashSet<Subscription>>();

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
        Subscription sub = null;
        sub = new Subscription(callback, () => Unsubscribe(topic, sub));
        
        if (!Subscriptions.TryGetValue(topic, out var sublist))
        {
            sublist = new HashSet<Subscription>();
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

    private void OnDestroy()
    {
        if (Subscriptions != null)
        {
            foreach (var kvp in Subscriptions)
            {
                if (kvp.Value == null)
                {
                    continue;
                }
                foreach (var subscription in kvp.Value)
                {
                    try
                    {
                        Unsubscribe(kvp.Key, subscription);
                    }
                    catch
                    {
                        //todo: log this, probably not critical
                    }
                }
            }
        }
    }

    void Unsubscribe(string topic, Subscription sb)
    {
        if (!Subscriptions.TryGetValue(topic, out var sublist))
        {
            return;
        }

        sublist.Remove(sb);
    }
}
