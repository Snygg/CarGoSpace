using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bus
{
    public static class BusManager
    {
        public static BusBehavior Initialize(GameObject linkedGameObject = null)
        {
            if (linkedGameObject)
            {
                var linkedBehavior = linkedGameObject.GetComponent<BusBehavior>();
                return linkedBehavior;
            }
            const string objectName = "BusObject";
            var existingLogger = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go =>
            {
                return go.name == objectName;
            });
            if (!existingLogger)
            {
                existingLogger = GameObject.Instantiate(Resources.Load<GameObject>("BusObject"), Vector3.zero,
                    Quaternion.identity);
                existingLogger.name = objectName;
            }

            var behavior = existingLogger.GetComponent<BusBehavior>();
            return behavior;
        }
    }
}