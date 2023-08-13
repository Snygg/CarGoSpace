using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bus;
using Logging;
using Scene;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerShipMovementBehavior : SceneBusParticipant
{
    public GameObject Module;
    private Rigidbody2D _moduleRigidBody;
    /// <summary>
    /// The amount of input that becomes thrust
    /// </summary>
    [SerializeField]
    private float ThrustFactor = 1;

    // Start is called before the first frame update
    private void Start()
    {
        _logger = LogManager.GetLogger();
        if (!Module)
        {
            _logger.System.LogError("forgot to link player ship game object", context: this);
            return;
        }
        _moduleRigidBody = Module.GetComponent<Rigidbody2D>();
        if (!_moduleRigidBody)
        {
            _logger.System.LogError("could not find playership physics", context: this);
            return;
        }
        AddLifeTimeSubscription(Subscribe(SceneEvents.Input, OnInput));
    }

    private Vector2 _lastLocation;

    private LogBehavior _logger;

    // Update is called once per frame
    protected void Update()
    {
        var currentLocation = (Vector2)_moduleRigidBody.transform.position;
        var distance = Vector2.Distance(currentLocation, _lastLocation);
        if (distance > float.Epsilon)
        {
            PublishLocation(currentLocation);
        }
    }
    
    private void OnInput(IReadOnlyDictionary<string, string> body)
    {
        if (body == null)
        {
            _logger.System.LogError(new ArgumentException("got a null body", nameof(body)), context: this);
            return;
        }

        const string vectKey = "vect";
        if (!body.TryGetVector2(vectKey, out var vect))
        {
            _logger.System.LogError(new ArgumentException("could not parse body values", nameof(body)), context: this);
            return;
        }

        Vector2 thrustVector = ThrustFactor * vect;
        if (thrustVector.magnitude <= float.Epsilon)
        {
            return;
        }

        ThrustTowards(thrustVector);
    }

    //thrusts in the direction of the vector with the given magnitude
    private void ThrustTowards(Vector2 vector)
    {
        _moduleRigidBody.AddForce(vector);
    }

    //changes the location of the ship by the given amounts in x and y
    private void MoveByMeasure(Vector2 vector)
    {
        var current = Module.transform.position;

        Module.transform.position = new Vector3(current.x + vector.x, current.y + vector.y, current.z);
        PublishLocation(current);
    }

    private void PublishLocation(Vector3 position)
    {
        var body = new Dictionary<string, string>();
        body["position"] = position.ToString();

        Publish(SceneEvents.PlayerTransform, body);
    }
    
    
}