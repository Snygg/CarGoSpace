namespace Logging
{
    public enum CgsLogLevel
    {
        /// <summary>
        /// Turns off all logging. Be very careful with this, do not check in code with logging turned off
        /// </summary>
        None = -1,
        /// <summary>
        /// Critical errors that cannot be recovered from
        /// </summary>
        Error,
        /// <summary>
        /// Situations which should not occur, but we want to keep an eye on them
        /// </summary>
        Warning,
        /// <summary>
        /// Common boilerplate status messages. Do not log things that happen very frequently as Info
        /// </summary>
        Info,
        /// <summary>
        /// Messages useful for debugging issues
        /// </summary>
        Debug,
        /// <summary>
        /// Messages useful for debugging issues, but happen very frequently or contain a lot of detail
        /// </summary>
        Verbose
    }
}