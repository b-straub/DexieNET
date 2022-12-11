namespace DexieNETTest.TestBase.Test
{
    internal class PersistanceTest : DexieTest<TestDB>
    {
        public PersistanceTest(TestDB db) : base(db)
        {
        }

        public override string Name => "PersistanceTest";

        public override async ValueTask<string?> RunTest()
        {
            var persistance = DB.Persistance();

            var persistanceType1 = await persistance.GetPersistanceType();
            var storageEstimate = await persistance.GetStorageEstimate();
            var persist = await persistance.RequestPersistance();
            var persistanceType2 = await persistance.GetPersistanceType();

            return $"PersistanceTest: OK, PersistanceType before: {persistanceType1}, Persistance: {persist}, PersistanceType after: {persistanceType2}, " +
                $"Quota: {Math.Round(storageEstimate.Quota / 1000000.0, 2)} MB, Used: {Math.Round(storageEstimate.Usage / 1000000.0, 2)} MB";
        }
    }
}
