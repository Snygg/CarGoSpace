using Bus;

namespace Player
{
    public interface IShipBusProvider
    {
        /// <summary>
        /// The bus over which ship-wide messages travel
        /// </summary>
        CgsBus Bus { get; }
    }
}