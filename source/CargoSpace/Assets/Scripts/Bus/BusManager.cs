using System.Linq;
using System.Runtime.CompilerServices;
using Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bus
{
    public static class BusManager
    {
        public static BusBehavior Initialize(
            GameObject linkedGameObject = null,
            LogBehavior logger = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (!logger)
            {
                logger = LogManager.Initialize();
            }
            if (linkedGameObject)
            {
                var linkedBehavior = linkedGameObject.GetComponent<BusBehavior>();
                return linkedBehavior;
            }
            logger.System.LogWarning(
                "This object does not link the bus object", 
                callerMemberName:callerMemberName, 
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);
            const string objectName = "BusObject";
            var existingObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go => go.name == objectName);
            if (!existingObject)
            {
                logger.System.LogInformation(
                    "Creating Bus on Scene", 
                    callerMemberName:callerMemberName, 
                    callerLineNumber: callerLineNumber);
                const string prefabName = objectName;
                existingObject = GameObject.Instantiate(Resources.Load<GameObject>(prefabName), Vector3.zero,
                    Quaternion.identity);
                existingObject.name = objectName;
            }

            var behavior = existingObject.GetComponent<BusBehavior>();
            return behavior;
        }
    }
}