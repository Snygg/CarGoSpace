using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcFollowPlayerBehavior : MonoBehaviour
{
    public GameObject DirectorObject;
    public GameObject MovableFollower;
    public MovableBehavior _movable;
    public float MinFollowDistance = 2;

    private PlayerTrackerBehavior _playerTrackerBehavior;
    // Start is called before the first frame update
    void Start()
    {
        _playerTrackerBehavior = DirectorObject.GetComponent<PlayerTrackerBehavior>();
        _movable = MovableFollower.GetComponent<MovableBehavior>();
    }

    // Update is called once per frame
    void Update()
    {
        var curDistance = Vector3.Distance(MovableFollower.transform.position, _playerTrackerBehavior.PlayerPosition);
        if (curDistance <= MinFollowDistance)
        {
            return;
        }
        _movable.MoveTowards(_playerTrackerBehavior.PlayerPosition);
    }
}
