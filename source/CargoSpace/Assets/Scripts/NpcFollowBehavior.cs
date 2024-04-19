using Module;
using UnityEngine;

public class NpcFollowBehavior : MonoBehaviour
{
    public Transform target;
    public float minFollowDistance = 2;

    private IThrusterSource _thrusterSource;

    void Awake()
    {
        _thrusterSource = GetComponent<IThrusterSource>();
    }

    void FixedUpdate()
    {
        if (!target)
        {
            return;
        }
        var curDistance = Vector3.Distance(transform.position, target.position);
        if (curDistance <= minFollowDistance)
        {
            return;
        }

        foreach (var thruster in _thrusterSource.GetThrusters())
        {
            thruster.ThrustTowards(target.position);    
        }
    }
}