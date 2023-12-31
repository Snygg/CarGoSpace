using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bus;
using Logging;
using UnityEngine;

public class PlayerTrackerBehavior : BusParticipant
{
    public GameObject PlayerShipObject;
    private LogBehavior _logger;
    public Vector3 PlayerPosition { get; private set; } = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        _logger = LogManager.Initialize(LogObject);
        AddLifeTimeSubscription(Subscribe("PlayerTransform", OnPlayerMoved));
    }

    // Update is called once per frame
    void Update()
    {

    }

    private async Task OnPlayerMoved(IReadOnlyDictionary<string, string> body)
    {
        if (body == null)
        {
            _logger.System.LogError(new ArgumentException("body cannot be null", nameof(body)), context: this);
            return;
        }

        var positionKey = "position";
        if (!body.TryGetVector3(positionKey,out var vec))
        {
            _logger.System.LogError(new ArgumentException($"{positionKey} is not a valid {nameof(Vector3)}", nameof(body)),
                context: this);
            return;
        }

        PlayerPosition = vec;
    }
}
