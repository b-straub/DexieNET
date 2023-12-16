using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class PutPExtern(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "PutPExtern";

        public override async ValueTask<string?> RunTest()
        {
            var byteTable = DB.FriendNPs<byte[]>();
            var guidTable = DB.FriendNPs<Guid>();
            await guidTable.Clear();
            var cTable = DB.FriendNPs<(string, int)>();
            await cTable.Clear();

            var friends = new[] {
                new FriendNP("TestName1"),
                new FriendNP("TestName2")
            };

            var friendsU = new[] {
                new FriendNP("Updated"),
                new FriendNP("Updated")
            };

            var guids = new[] {
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            var bytes = new[] {
                new byte[] {1, 0},
                [1, 1]
            };

            var compounds = new[] {
                ("Name1" , 30),
                ("Name2" , 50),
            };

            var keyG = await guidTable.Put(friends.First(), guids.First());

            if (keyG != guids.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await guidTable.Put(friendsU.First(), guids.First());
            var friendAdded = (await guidTable.ToArray()).FirstOrDefault();

            if (friendAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            await guidTable.Clear();
            var keyGs = await guidTable.BulkPut(friends, guids);

            if (keyGs.First() != guids.Last())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await guidTable.Clear();
            keyGs = await guidTable.BulkPut(friends, guids, true);

            if (keyGs.First() != guids.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await guidTable.BulkPut(friendsU, guids);
            friendAdded = (await guidTable.ToArray()).FirstOrDefault();

            if (friendAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            var keyB = await byteTable.Put(friends.First(), bytes.First());

            if (!keyB.SequenceEqual(bytes.First()))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await byteTable.Put(friendsU.First(), bytes.First());
            friendAdded = (await byteTable.ToArray()).FirstOrDefault();

            if (friendAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            await byteTable.Clear();
            var keyBs = await byteTable.BulkPut(friends, bytes);

            if (!keyBs.First().SequenceEqual(bytes.Last()))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await byteTable.Clear();
            keyBs = await byteTable.BulkPut(friends, bytes, true);

            if (!keyBs.SequenceEqual(bytes))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await byteTable.BulkPut(friendsU, bytes);
            friendAdded = (await byteTable.ToArray()).FirstOrDefault();

            if (friendAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            var keyC = await cTable.Put(friends.First(), compounds.First());

            if (keyC != compounds.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await cTable.Put(friendsU.First(), compounds.First());
            var compoundAdded = (await guidTable.ToArray()).FirstOrDefault();

            if (compoundAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            await cTable.Clear();
            var keyCs = await cTable.BulkPut(friends, compounds);

            if (keyCs.First() != compounds.Last())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await cTable.Clear();
            keyCs = await cTable.BulkPut(friends, compounds, true);

            if (keyCs.First() != compounds.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await cTable.BulkPut(friendsU, compounds);
            friendAdded = (await cTable.ToArray()).FirstOrDefault();

            if (friendAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            await DB.Transaction(async _ =>
            {
                await guidTable.Clear();
                keyG = await guidTable.Put(friends.First(), guids.First());

                await byteTable.Clear();
                keyB = await byteTable.Put(friends.First(), bytes.First());

                await cTable.Clear();
                keyC = await cTable.Put(friends.First(), compounds.First());
            });

            if (keyG != guids.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (!keyB.SequenceEqual(bytes.First()))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (keyC != compounds.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await guidTable.Clear();
                keyGs = await guidTable.BulkPut(friends, guids);

                await byteTable.Clear();
                keyBs = await byteTable.BulkPut(friends, bytes);

                await cTable.Clear();
                keyCs = await cTable.BulkPut(friends, compounds);
            });

            if (keyGs.First() != guids.Last())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (!keyBs.First().SequenceEqual(bytes.Last()))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (keyCs.First() != compounds.Last())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await guidTable.Clear();
                keyGs = await guidTable.BulkPut(friends, guids, true);

                await byteTable.Clear();
                keyBs = await byteTable.BulkPut(friends, bytes, true);

                await cTable.Clear();
                keyCs = await cTable.BulkPut(friends, compounds, true);
            });

            if (keyGs.First() != guids.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (!keyBs.SequenceEqual(bytes))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (keyCs.First() != compounds.First())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            return "OK";
        }
    }
}
