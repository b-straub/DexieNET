using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class EqualSpecialFields : DexieTest<TestDB>
    {
        public EqualSpecialFields(TestDB db) : base(db)
        {
        }

        public override string Name => "EqualSpecialFields";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.FieldTests;
            await table.Clear();

            var fieldsData = DataGenerator.GetFieldTestRandom().ToArray().OrderBy(f => f.Id);
            await table.BulkAdd(fieldsData);

            var fields = await table.OrderBy(f => f.Id).ToArray();

            var b1 = fieldsData.Last().Blob;
            var b2 = fields.Last().Blob;

            if (!b1.SequenceEqual(b2))
            {
                throw new InvalidOperationException("BlobDirect Item not identical.");
            }

            var blobLast = fieldsData.Last().Blob;
            var blobs = await table.Where(f => f.Blob).Equal(blobLast).ToArray();

            if (!blobLast.SequenceEqual(blobs.Last().Blob))
            {
                throw new InvalidOperationException("Blob Item not identical.");
            }

            var arrayLast = fieldsData.Last().Array;
            var arrays = await table.Where(f => f.Array).Equal(arrayLast).ToArray();

            if (!arrayLast.SequenceEqual(arrays.Last().Array))
            {
                throw new InvalidOperationException("Blob Item not identical.");
            }

            var fieldsDataInclude = fieldsData.Where(f => f.Include);
            var fieldsInclude = await table.Where(f => f.Include).Equal(true).ToArray();

            if (fieldsDataInclude.Count() != fieldsInclude.Count())
            {
                throw new InvalidOperationException("Include Items not identical.");
            }

            var fieldsDataBoolNoIndex = fieldsData.Where(f => f.BoolNoIndex);
            var fieldsBoolNoIndex = (await table.ToArray()).Where(f => f.BoolNoIndex);

            if (fieldsDataBoolNoIndex.Count() != fieldsBoolNoIndex.Count())
            {
                throw new InvalidOperationException("BoolNoIndex Items not identical.");
            }

            var fieldsDataIncludeME = fieldsData.Where(f => f.IncludeME.Contains(false));
            var fieldsIncludeME = await table.Where(f => f.IncludeME).Equal(false).ToArray();

            if (fieldsDataIncludeME.Count() != fieldsIncludeME.Count())
            {
                throw new InvalidOperationException("IncludeME Items not identical.");
            }

            var blobQuery = new byte[] { 0x10, 0x20 };

            var fieldsDataBlobME = fieldsData.Where(f => f.BlobME.Any(b => b.SequenceEqual(blobQuery)));
            var fieldsBlobME = await table.Where(f => f.BlobME).Equal(blobQuery).ToArray();

            if (fieldsDataBlobME.Count() != fieldsBlobME.Count())
            {
                throw new InvalidOperationException("IncludeME Items not identical.");
            }

            var dateLow = new DateOnly(1999, 1, 1);
            var keyLow = (dateLow, true);
            var dateHigh = new DateOnly(2002, 1, 1);
            var keyHigh = (dateHigh, true);

            var fieldsDataDate = fieldsData.Where(f => (f.Date >= dateLow && f.Date < dateHigh) && f.Include);
            var col = table.Where(f => f.Date, f => f.Include).Equal((dateHigh, true));
            var c = await col.Count();

            var fieldsDate = await table.Where(f => f.Date, f => f.Include).Between(keyLow, keyHigh).ToArray();

            if (fieldsDataDate.Count() != fieldsDate.Count())
            {
                throw new InvalidOperationException("Date Keys not identical.");
            }

            var fieldsDataDateInclude = fieldsData.Where(f => f.Date == dateHigh && f.Include);
            var fieldsDateInclude = await table.Where(f => f.Date, dateHigh, f => f.Include, true).ToArray();

            if (fieldsDataDateInclude.Count() != fieldsDateInclude.Count())
            {
                throw new InvalidOperationException("DateInclude Keys not identical.");
            }

            var fieldsDateIncludeGet = await table.Get(f => f.Date, dateHigh, f => f.Include, true);

            if (fieldsDataDateInclude.First().Date != fieldsDateInclude.First().Date ||
                fieldsDataDateInclude.First().Include != fieldsDateInclude.First().Include)
            {
                throw new InvalidOperationException("Get DateInclude Item not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(fieldsData);

                var whereB = table.Where(f => f.Blob);
                var collectionB = whereB.Equal(blobLast);
                blobs = await collectionB.ToArray();

                var whereA = table.Where(f => f.Array);
                var collectionA = whereA.Equal(arrayLast);
                arrays = await collectionA.ToArray();

                var where1 = table.Where(f => f.Include);
                var collection1 = where1.Equal(true);
                fieldsInclude = await collection1.ToArray();

                var where2 = table.Where(f => f.Date, f => f.Include);
                var collection2 = where2.Between(keyLow, keyHigh);
                fieldsDate = await collection2.ToArray();

                collection2 = table.Where(f => f.Date, dateHigh, f => f.Include, true);
                fieldsDateInclude = await collection2.ToArray();

                fieldsDateIncludeGet = await table.Get(f => f.Date, dateHigh, f => f.Include, true);
            });

            if (!blobLast.SequenceEqual(blobs.Last().Blob))
            {
                throw new InvalidOperationException("Blob Item not identical.");
            }

            if (!arrayLast.SequenceEqual(arrays.Last().Array))
            {
                throw new InvalidOperationException("Blob Item not identical.");
            }

            if (fieldsDataInclude.Count() != fieldsInclude.Count())
            {
                throw new InvalidOperationException("Include Items not identical.");
            }

            if (fieldsDataDate.Count() != fieldsDate.Count())
            {
                throw new InvalidOperationException("Date Keys not identical.");
            }

            if (fieldsDataDateInclude.Count() != fieldsDateInclude.Count())
            {
                throw new InvalidOperationException("DateInclude Keys not identical.");
            }

            if (fieldsDataDateInclude.First().Date != fieldsDateInclude.First().Date ||
               fieldsDataDateInclude.First().Include != fieldsDateInclude.First().Include)
            {
                throw new InvalidOperationException("Get DateInclude Item not identical.");
            }

            return "OK";
        }
    }
}
