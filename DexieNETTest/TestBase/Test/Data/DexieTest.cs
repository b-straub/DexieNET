using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal abstract class DexieTest<T> where T : IDBBase
    {
        public abstract string Name { get; }

        protected T DB { get; }

        public DexieTest(T db)
        {
            DB = db;
        }

        public abstract ValueTask<string?> RunTest();
    }
}
