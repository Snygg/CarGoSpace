using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bus;
using Logging;
using Scene;
using UnityEngine;

public class PlayerTrackerBehavior : SceneBusParticipant
{
    private LogBehavior _logger;
    public Vector3 PlayerPosition { get; private set; } = Vector3.zero;

    // Start is called before the first frame update
    private void Start()
    {
        _logger = LogManager.GetLogger();
        AddLifeTimeSubscription(Subscribe(SceneEvents.PlayerTransform, OnPlayerMoved));
    }

    private void OnPlayerMoved(IReadOnlyDictionary<string, string> body)
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
