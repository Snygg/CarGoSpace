using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableBehavior : MonoBehaviour
{
    public GameObject Movable;

    public float SpeedFactor = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Move the movable towards the location at the current speed
    /// </summary>
    /// <param name="target">position to move to</param>
    public void MoveTowards(Vector2 target)
    {
        var current = Movable.transform.position;
        var path = new Vector3(target.x, target.y, 0) - current;
        var normalized = path.normalized;
        var destination = current + (normalized * SpeedFactor);

        MoveTo(destination);
    }

    public void MoveTo(Vector3 destination)
    {
        Movable.transform.position = destination;
    }
}
