namespace Module
{
    public interface IThrusterSource
    {
        /// <summary>
        /// returns the thrusters contained by this object
        /// </summary>
        /// <returns></returns>
        IThruster[] GetThrusters();
    }
}