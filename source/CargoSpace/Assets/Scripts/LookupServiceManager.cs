public class LookupServiceManager
{
    private static readonly ILookupService LookupService = new LookupService();
    public static ILookupService GetService()
    {
        return LookupService;
    }
}