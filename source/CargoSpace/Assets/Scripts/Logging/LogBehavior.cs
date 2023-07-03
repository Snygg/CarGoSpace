using UnityEngine;

namespace Logging
{
    /// <summary>
    /// Add this to each scene
    /// </summary>
    public class LogBehavior : MonoBehaviour
    {
        public readonly CgsLogger System = new CgsLogger();
        public readonly CgsLogger Player = new CgsLogger();
        public readonly CgsLogger Combat = new CgsLogger();
        public readonly CgsLogger Bus = new CgsLogger();
        //todo: add any other categories of logs here

    }
}