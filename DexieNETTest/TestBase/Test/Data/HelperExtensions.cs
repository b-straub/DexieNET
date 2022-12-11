
namespace DexieNETTest.TestBase.Test
{
    internal static class HelperExtensions
    {

        public static bool True(this bool? value)
        {
            return value.GetValueOrDefault(false);
        }

        public static bool SequenceEqual<T>(this IEnumerable<IEnumerable<T>> values, IEnumerable<IEnumerable<T>> tests)
        {
            foreach (var (item, index) in values.Select((value, i) => (value, i)))
            {
                if (!item.SequenceEqual(tests.ElementAt(index)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
