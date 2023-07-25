using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Logging
{
    public static class LogManager
    {
        const string objectName = "LoggerObject";
        public static LogBehavior Initialize(
            GameObject linkedGameObject = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (linkedGameObject)
            {
                var linkedBehavior = linkedGameObject.GetComponent<LogBehavior>();
                return linkedBehavior;
            }
            
            var existingLogger = GetOrAddLoggerToScene(out var hadToAddToScene);

            var behavior = existingLogger.GetComponent<LogBehavior>();
            behavior.System.LogWarning(
                "This object does not link the log object", 
                callerMemberName:callerMemberName, 
                callerLineNumber: callerLineNumber,
                callerFilePath: callerFilePath);
            if (hadToAddToScene)
            {
                behavior.System.LogInformation(
                    "Creating Log Object on scene", 
                    callerMemberName:callerMemberName, 
                    callerLineNumber: callerLineNumber,
                    callerFilePath: callerFilePath);
            }
            return behavior;
        }

        private static GameObject GetOrAddLoggerToScene(out bool hadToAddToScene)
        {
            var existingLogger = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go => go.name == objectName);
            hadToAddToScene = existingLogger == null;
            if (hadToAddToScene)
            {
                var newLogger = AddLoggerToScene();
                existingLogger = newLogger;
            }

            return existingLogger;
        }

        private static GameObject AddLoggerToScene()
        {
            const string prefabName = objectName;
            var newLogger = GameObject.Instantiate(Resources.Load<GameObject>(prefabName), Vector3.zero,
                Quaternion.identity);
            newLogger.name = objectName;
            newLogger.AddComponent<LogBehavior>();
            return newLogger;
        }

        public static LogBehavior GetLogger(
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            //todo: refactor this to eliminate the LoggerObject
            var existingLogger = GetOrAddLoggerToScene(out var hadToAddToScene);

            var behavior = existingLogger.GetComponent<LogBehavior>();
            if (hadToAddToScene)
            {
                behavior.System.LogInformation(
                    "Creating Log Object on scene", 
                    callerMemberName:callerMemberName, 
                    callerLineNumber: callerLineNumber,
                    callerFilePath: callerFilePath);
            }

            return behavior;
        }
    }
}