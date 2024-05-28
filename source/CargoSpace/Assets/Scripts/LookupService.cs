using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

internal class LookupService : ILookupService
{
    public ITargetable GetTargetableById(string id)
    {
        foreach (var rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (TryGetTargetableById(rootObject, id, 4, out var targetable))
            {
                return targetable;
            }
        }

        return null;
    }

    private bool TryGetTargetableById(GameObject obj, string targetId, int recurseRemaining, out ITargetable targetable)
    {
        //todo: this method is an awful hack. replace it with better searching or a cache or something
        
        if (recurseRemaining <= 0)
        {
            targetable = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            targetable = null;
            return false;
        }
        
        var topTargetable = obj.GetComponent<ITargetable>();
        if (topTargetable != null && topTargetable.TargetId == targetId)
        {
            targetable = topTargetable;
            return true;
        }
        
        foreach (var child in obj.GetComponentsInChildren<MonoBehaviour>().Select(mb=>mb.gameObject).Distinct())
        {
            if (TryGetTargetableById(child.gameObject, targetId, recurseRemaining-1, out targetable))
            {
                return true;
            }
        }
        targetable = null;
        return false;
    }
}