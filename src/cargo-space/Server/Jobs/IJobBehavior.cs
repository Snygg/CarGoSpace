namespace CargoSpace.Server
{
    public interface IJobBehavior
    {
        void Execute(Job job, IJobExecutionContext context);
    }
}
