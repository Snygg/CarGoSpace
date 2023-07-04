using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Logging
{
    public static class LogManager
    {
        public static LogBehavior Initialize()
        {
            const string objectName = "LoggerObject";
            var existingLogger = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go =>
            {
                return go.name == objectName;
            });
            if (existingLogger == null)
            {
                //todo: consider creating a prefab and GameObject.Instantiate(prefab) here
                existingLogger = new GameObject();
                existingLogger.name = objectName;
                existingLogger.AddComponent<LogBehavior>();
            }

            var behavior = existingLogger.GetComponent<LogBehavior>();

            return behavior;
        }
    }
}