using R3;

namespace Weapons
{
    public interface IGroupableWeapon
    {
        ReadOnlyReactiveProperty<string> GroupName { get; }
    }
}