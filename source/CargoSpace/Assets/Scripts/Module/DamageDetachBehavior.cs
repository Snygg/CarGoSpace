using System.Collections.Generic;

namespace Module
{
    public class DamageDetachBehavior : ModuleBusParticipant
    {
        public float DetachHpPercent = 0;
        public bool Attached { get; private set; }

        protected override void Awoke()
        {
            AddLifeTimeSubscription(ModuleEvents.HpPercentChanged, OnHpPercentChanged);
            AddLifeTimeSubscription(ModuleEvents.Attached, OnAttached);
            AddLifeTimeSubscription(ModuleEvents.Detached, OnDetached);
        }

        private void OnDetached(IReadOnlyDictionary<string, string> _)
        {
            Attached = false;
        }

        private void OnAttached(IReadOnlyDictionary<string, string> _)
        {
            Attached = true;
        }

        private void OnHpPercentChanged(IReadOnlyDictionary<string, string> body)
        {
            if (!Attached)
            {
                return;
            }
            if (DetachHpPercent < 0)
            {
                return;
            }

            if (!body.TryGetFloat("PercentHp", out var percentHp))
            {
                return;
            }

            if (percentHp > DetachHpPercent)
            {
                return;
            }

            Publish(ModuleEvents.Detached, context: this);
        }
    }
}
