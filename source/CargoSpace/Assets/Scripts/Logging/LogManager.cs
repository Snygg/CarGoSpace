using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Logging
{
    public static class LogManager
    {
        public static LogBehavior Initialize(
            GameObject linkedGameObject = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            if (linkedGameObject)
            {
                var linkedBehavior = linkedGameObject.GetComponent<LogBehavior>();
                return linkedBehavior;
            }
            const string objectName = "LoggerObject";
            var existingLogger = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go => go.name == objectName);
            bool hadToAddToScene = existingLogger == null;
            if (hadToAddToScene)
            {
                const string prefabName = objectName;
                existingLogger = GameObject.Instantiate(Resources.Load<GameObject>(prefabName), Vector3.zero,
                    Quaternion.identity);
                existingLogger.name = objectName;
                existingLogger.AddComponent<LogBehavior>();
            }

            var behavior = existingLogger.GetComponent<LogBehavior>();
            behavior.System.LogWarning(
                "This object does not link the log object", 
                callerMemberName:callerMemberName, 
                callerLineNumber: callerLineNumber);
            if (hadToAddToScene)
            {
                behavior.System.LogInformation(
                    "Creating Log Object on scene", 
                    callerMemberName:callerMemberName, 
                    callerLineNumber: callerLineNumber);
            }
            return behavior;
        }
    }
}