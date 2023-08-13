using System;
using System.Collections.Generic;
using System.Linq;

namespace Bus
{
    public class BusTopic
    {
        public readonly string Name;
        public readonly IReadOnlyDictionary<string, Type> RequiredFields;

        public static readonly IReadOnlyDictionary<string, Type> EmptyFields = new Dictionary<string, Type>();

        public BusTopic(string name, IReadOnlyDictionary<string, Type> requiredFields = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be blank", nameof(name));
            }
            Name = name;
            RequiredFields = requiredFields ?? EmptyFields;
        }
        
        public BusTopic(string name, params Field[] requiredFields)
            : this(name, new Dictionary<string, Type>(requiredFields.ToDictionary(kvp=>kvp.Name, kvp=>kvp.Type)))
        {
            
        }
        
        public class Field
        {
            public Field(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }
            public Type Type { get; }
        }
    }
}