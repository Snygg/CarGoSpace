using System;
using System.Collections.Generic;
using Bus;

namespace Module
{
    public static class ModuleEvents
    {
        public static readonly BusTopic Attached = new BusTopic("Attached");
        public static readonly BusTopic Detached = new BusTopic("Detached");

        public static readonly BusTopic HpPercentChanged = new BusTopic("HpPercentChanged",
            new Dictionary<string, Type>
            {
                {"Previous", typeof(float)},
                {"PercentHp", typeof(float)}
            });
    }
}