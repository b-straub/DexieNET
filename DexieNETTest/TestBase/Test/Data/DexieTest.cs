using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal abstract class DexieTest<T>(T db) where T : IDBBase
    {
        public abstract string Name { get; }

        protected T DB { get; } = db;

        public abstract ValueTask<string?> RunTest();
    }
}
