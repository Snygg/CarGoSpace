namespace CargoSpace.Core
{
    public static class Constants
    {
        public const int ServerPort = 7777;
        public const string ServerAddress = "127.0.0.1";
        public const int TileSize = 64;

        // Absolute maximum extent of the buildable ship, in tiles from the origin.
        // This caps the AStarGrid2D region and prevents 50,000-tile bridges.
        public const int MaxShipRadius = 128;

        // Zone updates are split into chunks to avoid ENet "message too large" disconnects.
        // A 500-tile Vector2I array stays comfortably under Godot's ~32KB RPC limit.
        public const int ZoneChunkSize = 500;

        // Minimum log level: 0=Debug, 1=Info, 2=Warning, 3=Error
        // Set to 0 for development, 1+ for production
        public const int MinimumLogLevel = 0;
    }
}
