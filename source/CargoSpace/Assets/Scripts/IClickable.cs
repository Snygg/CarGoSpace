/// <summary>
/// what we can know when an object is clicked
/// </summary>
public interface IClickable
{
    /// <summary>
    ///  <see cref="ITargetable.TargetId"/> or null if this is not targetable by player weapons
    /// </summary>
    string TargetId { get; }
}