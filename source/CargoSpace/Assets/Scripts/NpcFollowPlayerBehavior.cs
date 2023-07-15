using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcFollowPlayerBehavior : MonoBehaviour, IDamageable
{
    public GameObject DirectorObject;
    public GameObject MovableFollower;
    private MovableBehavior _movable;
    public float MinFollowDistance = 2;

    private PlayerTrackerBehavior _playerTrackerBehavior;

    // Start is called before the first frame update
    void Start()
    {
        SetDirectorObject(DirectorObject);
        SetMovableFollower(MovableFollower);
    }

    void FixedUpdate()
    {
        var curDistance = Vector3.Distance(MovableFollower.transform.position, _playerTrackerBehavior.PlayerPosition);
        if (curDistance <= MinFollowDistance)
        {
            return;
        }
        _movable.ThrustTowards(_playerTrackerBehavior.PlayerPosition);
    }

    public float LastPercentHp { get; private set; } = 100;
    public void OnDamageChanged(float percentHp)
    {
        const float epsilon = 0.0001f;
        if (!LastPercentHp.HasChanged(percentHp, epsilon))
        {
            return;
        }

        enabled = percentHp > 0;
    }

    public void SetDirectorObject(GameObject gameObject)
    {
        if (!gameObject)
        {
            return;
        }
        _playerTrackerBehavior = gameObject.GetComponent<PlayerTrackerBehavior>();
    }

    public void SetMovableFollower(GameObject npcFollowerGameObject)
    {
        if (!npcFollowerGameObject)
        {
            return;
        }
        _movable = MovableFollower.GetComponent<MovableBehavior>();
    }
}