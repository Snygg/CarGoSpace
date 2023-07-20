using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcFollowPlayerBehavior : MonoBehaviour, IDamageable
{
    public GameObject DirectorObject;

    private MovableBehavior _mv=null;
    private MovableBehavior _movable
    {
        get
        {
            if (!_mv)
            {
                _mv = GetComponent<MovableBehavior>();
            }
            return _mv;
        }
    }

    public float MinFollowDistance = 2;

    private PlayerTrackerBehavior _playerTrackerBehavior;

    // Start is called before the first frame update
    void Start()
    {
        if (DirectorObject)
        {
            _playerTrackerBehavior = DirectorObject.GetComponent<PlayerTrackerBehavior>();    
        } 
    }

    void FixedUpdate()
    {
        var curDistance = Vector3.Distance(transform.position, _playerTrackerBehavior.PlayerPosition);
        if (curDistance <= MinFollowDistance)
        {
            return;
        }
        _movable.ThrustTowards(_playerTrackerBehavior.PlayerPosition);
    }

    public void OnHpPercentChanged(float percentHp)
    {
        var speedFactor = percentHp > 10
            ? _movable.MaxSpeedFactor
            : _movable.MaxSpeedFactor * 0.5f;
        _movable.SetCurrentSpeedFactor(speedFactor);
    }

    public void SetDirectorObject(GameObject gameObject)
    {
        if (!gameObject)
        {
            return;
        }

        DirectorObject = gameObject;
        _playerTrackerBehavior = gameObject.GetComponent<PlayerTrackerBehavior>();
    }
}