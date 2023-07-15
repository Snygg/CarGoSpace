using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableBehavior : MonoBehaviour
{
    public GameObject Movable;

    public float SpeedFactor = 1;

    private Rigidbody2D _rigidbody2D;
    // Start is called before the first frame update
    void Start()
    {
        SetMovable(Movable);
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

    public void ThrustTowards(Vector2 target)
    {
        var current = (Vector2)Movable.transform.position;
        var path = new Vector2(target.x, target.y) - current;
        var normalized = path.normalized;
        var factored = normalized * SpeedFactor;
        ApplyThrust(factored);
    }

    private void ApplyThrust(Vector2 force)
    {
        _rigidbody2D.AddForce(force);
    }

    public void SetMovable(GameObject movableGameObject)
    {
        if (!Movable)
        {
            return;
        }
        _rigidbody2D = Movable.GetComponent<Rigidbody2D>();
    }
}
