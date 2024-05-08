namespace Module
{
    public interface IThrusterProvider
    {
        /// <summary>
        /// returns the thrusters contained by this object
        /// </summary>
        /// <returns></returns>
        IThruster[] GetThrusters();
    }
}