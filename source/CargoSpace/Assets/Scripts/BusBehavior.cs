using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusBehavior : MonoBehaviour
{
    public void Publish(string topic, Dictionary<string, string> body)
    {

    }

    public IDisposable Subscribe(string topic, Func<Dictionary<string,string>, Task> callback)
    {
        return null;
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
}
