using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableBehavior : MonoBehaviour
{
    public float MaxSpeedFactor = 1;
    public float CurrentSpeedFactor { get; private set; }

    private Rigidbody2D _rb2d = null;
    private Rigidbody2D _rigidbody2D
    {
        get
        {
            if (!_rb2d)
            {
                _rb2d =gameObject.GetComponent<Rigidbody2D>();;
            }

            return _rb2d;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentSpeedFactor = MaxSpeedFactor;
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
        var current = transform.position;
        var path = new Vector3(target.x, target.y, 0) - current;
        var normalized = path.normalized;
        var destination = current + (normalized * CurrentSpeedFactor);

        MoveTo(destination);
    }

    public void MoveTo(Vector3 destination)
    {
        transform.position = destination;
    }

    public void ThrustTowards(Vector2 target)
    {
        var current = (Vector2)transform.position;
        var path = new Vector2(target.x, target.y) - current;
        var normalized = path.normalized;
        var factored = normalized * CurrentSpeedFactor;
        ApplyThrust(factored);
    }

    private void ApplyThrust(Vector2 force)
    {
        _rigidbody2D.AddForce(force);
    }

    public void SetCurrentSpeedFactor(float factor)
    {
        CurrentSpeedFactor = factor;
    }
}
