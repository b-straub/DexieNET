namespace DexieNETCloudSample.Extensions
{
    public static class CoreExtensions
    {
        public static bool True(this bool? value)
        {
            return value.GetValueOrDefault(false);
        }
    }
}
