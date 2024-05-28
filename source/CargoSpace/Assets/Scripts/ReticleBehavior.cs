using System.Collections.Generic;
using Scene;
using UnityEngine;

public class ReticleBehavior : SceneBusParticipant
{
    public ITransformProvider TargetTransformProvider;
    private ILookupService _lookupService;

    protected override void Awoke()
    {
        _lookupService = LookupServiceManager.GetService();
        AddLifeTimeSubscription(SceneEvents.PlayerTargetChanged, OnPlayerTargetChanged,context:this);      
    }

    private void OnPlayerTargetChanged(IReadOnlyDictionary<string, string> body)
    {
        
        if (!body.TryGetValue("targetId", out var targetId))
        {
            return;
        }
        
        if (string.IsNullOrWhiteSpace(targetId))
        {
            TargetTransformProvider = null;
            return;
        }
        var targetable = _lookupService.GetTargetableById(targetId);
        
        if (targetable == null && TargetTransformProvider != null)
        {
            TargetTransformProvider = null;
            return;
        }
        TargetTransformProvider = targetable.TransformProvider;
    }

    void Update()
    {
        if (TargetTransformProvider == null)
        {
            return;
        }

        transform.position = TargetTransformProvider.Transform.position;
    }
}