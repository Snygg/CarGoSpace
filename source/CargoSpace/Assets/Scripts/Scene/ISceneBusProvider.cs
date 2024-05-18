using Bus;

namespace Scene
{
    public interface ISceneBusProvider
    {
        CgsBus Bus { get; }
    }
}