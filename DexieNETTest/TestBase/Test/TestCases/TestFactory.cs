namespace DexieNETTest.TestBase.Test
{
    internal class TestFactory
    {
        private readonly List<(string Category, DexieTest<TestDB> Test)> _tests = new();

        public TestFactory(TestDB db)
        {
            PopulateTests(db);
        }

        public IEnumerable<(string Category, DexieTest<TestDB> Test)> GetTests(string? testName = null)
        {
            var tests = Enumerable.Empty<(string Category, DexieTest<TestDB> Test)>();

            if (testName?.ToLowerInvariant() == "playwright")
            {
                return _tests.Where(t => t.Test.Name != "PersistanceTest");
            }

            tests = testName is null ? _tests : _tests.Where(t => t.Test.Name == testName);

            return tests.Any() ? tests : Enumerable.Empty<(string Category, DexieTest<TestDB> Test)>();
        }

        private void PopulateTests(TestDB db)
        {
            // General
            _tests.Add(("General", new PersistanceTest(db)));
            _tests.Add(("General", new VersionUpdate(db)));
            _tests.Add(("General", new KeyTest(db)));
            _tests.Add(("General", new OpenClose(db)));
            _tests.Add(("General", new FailedTransaction(db)));
            _tests.Add(("General", new TransactionAwaited(db)));
            _tests.Add(("General", new TransactionAwaited(db, true)));
            _tests.Add(("General", new TransactionsParallel(db)));
            _tests.Add(("General", new TransactionsParallel(db, true)));
            _tests.Add(("General", new TransactionsNested(db)));
            _tests.Add(("General", new TransactionsNested(db, true)));
            _tests.Add(("General", new EqualSpecialFields(db)));
            _tests.Add(("General", new Reverse(db)));
            _tests.Add(("General", new CompoundPrimary(db)));
            _tests.Add(("General", new LiveQueryTest(db)));
            // Table
            _tests.Add(("Table", new ClearCount(db)));
            _tests.Add(("Table", new Add(db)));
            _tests.Add(("Table", new AddPNotAuto(db)));
            _tests.Add(("Table", new AddPExtern(db)));
            _tests.Add(("Table", new AddClass(db)));
            _tests.Add(("Table", new AddComplex(db)));
            _tests.Add(("Table", new BulkAdd(db)));
            _tests.Add(("Table", new BulkAdd(db, true)));
            _tests.Add(("Table", new Put(db)));
            _tests.Add(("Table", new PutPExtern(db)));
            _tests.Add(("Table", new PutPNotAuto(db)));
            _tests.Add(("Table", new BulkPut(db, true)));
            _tests.Add(("Table", new Update(db)));
            _tests.Add(("Table", new Get(db)));
            _tests.Add(("Table", new BulkGet(db)));
            _tests.Add(("Table", new Delete(db)));
            _tests.Add(("Table", new BulkDelete(db)));
            _tests.Add(("Table", new ToCollection(db)));
            _tests.Add(("Table", new TableFilter(db)));
            _tests.Add(("Table", new OrderBy(db)));
            // Collection
            _tests.Add(("Collection", new CollectionFirstLast(db)));
            _tests.Add(("Collection", new CollectionFilter(db)));
            _tests.Add(("Collection", new CollectionEach(db)));
            _tests.Add(("Collection", new CollectionModify(db)));
            _tests.Add(("Collection", new CollectionPrimaryKeys(db)));
            _tests.Add(("Collection", new CollectionUntil(db)));
            _tests.Add(("Collection", new Distinct(db)));
            _tests.Add(("Collection", new Keys(db)));
            _tests.Add(("Collection", new LimitOffset(db)));
            _tests.Add(("Collection", new Or(db)));
            // WhereClause
            _tests.Add(("WhereClause", new Above(db)));
            _tests.Add(("WhereClause", new InAnyRange(db)));
            _tests.Add(("WhereClause", new AnyOf(db)));
            _tests.Add(("WhereClause", new Below(db)));
            _tests.Add(("WhereClause", new Between(db)));
            _tests.Add(("WhereClause", new Equal(db)));
            _tests.Add(("WhereClause", new NoneOf(db)));
            _tests.Add(("WhereClause", new NotEqual(db)));
            _tests.Add(("WhereClause", new StartsWith(db)));
        }
    }
}
