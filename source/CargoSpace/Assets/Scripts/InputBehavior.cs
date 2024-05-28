using System.Collections.Generic;
using Scene;
using UnityEngine;

public class InputBehavior : SceneBusParticipant
{
    private bool isFiring;
    // Update is called once per frame
    protected void Update()
    {
        var vert = Input.GetAxis("Vertical");
        var horz = Input.GetAxis("Horizontal");
        if (vert != 0.0 || horz != 0)
        {
            //todo: pass vector2 here
            var body = new Dictionary<string, string>();
            body["vect"] = new Vector2(horz, vert).ToString();
            Publish(SceneEvents.Input, body);
        }

        var fire1Axis = Input.GetAxis("Fire1");
        var wasFiring = isFiring;
        isFiring = fire1Axis > 0;

        if (wasFiring && !isFiring)
        {
            var worldLocation = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            OnPlayerClicked(worldLocation);
        }

        var key3 = Input.GetKeyDown(KeyCode.Alpha3);
        var key4 = Input.GetKeyDown(KeyCode.Alpha4);
        
        if (key3)
        {
            Publish(SceneEvents.KeyPressed,
                new Dictionary<string, string>
                {
                    { "key", "3" }
                });
        }

        if (key4)
        {
            Publish(SceneEvents.KeyPressed,
                new Dictionary<string, string>
                {
                    { "key", "333" }
                });
        }
    }
    
    private void OnPlayerClicked(Vector2 location)
    {
        var rayCastHitList = GetRayCastHitList(location);
        if (rayCastHitList == null)
        {
            return;
        }
        var clicked = GetClickedTargetable(rayCastHitList);
        
        Publish(SceneEvents.PlayerTargetChanged, 
            new Dictionary<string, string>{{"targetId",clicked?.TargetId}},
            context:this);
    }
    
    private List<RaycastHit2D> GetRayCastHitList(Vector2 clickLocation)
    {
        List<RaycastHit2D> hitResults = new List<RaycastHit2D>();
        Physics2D.Raycast(clickLocation, Vector2.up * .00001f, new ContactFilter2D(), hitResults);
        return hitResults;
    }

    private ITargetable GetClickedTargetable (List<RaycastHit2D> hitResults)
    {
        ITargetable targetable = null;
        foreach (var hitResult in hitResults)
        {
            if (!hitResult.collider ||
                !hitResult.collider.gameObject ||
                !hitResult.collider.gameObject.TryGetComponent(out targetable))
            {
                continue;
            }
            break;
        }

        return targetable;
    }
}
