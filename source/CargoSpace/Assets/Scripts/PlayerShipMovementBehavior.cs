using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bus;
using Logging;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerShipMovementBehavior : BusParticipant
{
    public GameObject PlayerShip;
    private Rigidbody2D _playerShipRigidBody;
    /// <summary>
    /// The amount of input that becomes thrust
    /// </summary>
    [SerializeField]
    private float ThrustFactor = 1;

    // Start is called before the first frame update
    void Start()
    {
        _logger = LogManager.Initialize(LogObject);
        if (PlayerShip == null)
        {
            _logger.System.LogError("forgot to link player ship game object", context: this); 
        }
        _playerShipRigidBody = PlayerShip.GetComponent<Rigidbody2D>();
        if (_playerShipRigidBody == null)
        {
            _logger.System.LogError("could not find playership physics", context: this);
        }
        AddLifeTimeSubscription(Subscribe("Input", OnInput));
    }

    private Vector2 _lastLocation;

    private LogBehavior _logger;

    // Update is called once per frame
    void Update()
    {
        var currentLocation = (Vector2)_playerShipRigidBody.transform.position;
        var distance = Vector2.Distance(currentLocation, _lastLocation);
        if (distance > float.Epsilon)
        {
            PublishLocation(currentLocation);
        }
    }
    
    private async Task OnInput(Dictionary<string, string> body)
    {
        if (body == null)
        {
            _logger.System.LogError(new ArgumentException("got a null body", nameof(body)), context: this);
            return;
        }

        const string vertKey = "vert";
        const string horzKey = "horz";
        if (!body.ContainsKey(vertKey) || !body.ContainsKey(horzKey))
        {
            _logger.System.LogError(new ArgumentException("expected keys not found", nameof(body)), context: this);
            return;
        }

        if (!float.TryParse(body[vertKey], out var vert) ||
            !float.TryParse(body[horzKey], out var horz))
        {
            _logger.System.LogError(new ArgumentException("could not parse body values", nameof(body)), context: this);
            return;
        }

        Vector2 input = new Vector2(horz, vert);

        
        Vector2 thrustVector = ThrustFactor * input;
        if (thrustVector.magnitude <= float.Epsilon)
        {
            return;
        }

        ThrustTowards(thrustVector);
    }

    //thrusts in the direction of the vector with the given magnitude
    private void ThrustTowards(Vector2 vector)
    {
        _playerShipRigidBody.AddForce(vector);
    }

    //changes the location of the ship by the given amounts in x and y
    private void MoveByMeasure(Vector2 vector)
    {
        var current = PlayerShip.transform.position;

        PlayerShip.transform.position = new Vector3(current.x + vector.x, current.y + vector.y, current.z);
        PublishLocation(current);
    }

    private void PublishLocation(Vector3 position)
    {
        var body = new Dictionary<string, string>();
        body["position"] = position.ToString();

        Publish("PlayerTransform", body);
    }
    
    
}