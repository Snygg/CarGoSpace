namespace CargoSpace.Core
{
    public static class Constants
    {
        public const int ServerPort = 7777;
        public const string ServerAddress = "127.0.0.1";
        public const int TileSize = 64;
        
        // Minimum log level: 0=Debug, 1=Info, 2=Warning, 3=Error
        // Set to 0 for development, 1+ for production
        public const int MinimumLogLevel = 0;
    }
}
