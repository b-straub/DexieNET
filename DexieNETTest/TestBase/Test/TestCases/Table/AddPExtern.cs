using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class AddPExtern : DexieTest<TestDB>
    {
        public AddPExtern(TestDB db) : base(db)
        {
        }

        public override string Name => "AddPExtern";

        public override async ValueTask<string?> RunTest()
        {
            var byteTable = await DB.FriendNPs<byte[]>();
            var guidTable = await DB.FriendNPs<Guid>();
            await guidTable.Clear();
            var cTable = await DB.FriendNPs<(string, int)>();
            await cTable.Clear();

            var friends = new[] {
                new FriendNP("TestName1"),
                new FriendNP("TestName2")
            };

            var guids = new[] {
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            var bytes = new[] {
                new byte[] {1, 0},
                new byte[] {1, 1}
            };

            var compounds = new[] {
                ("Name1" , 30),
                ("Name2" , 50),
            };

            var keyG = await guidTable.Add(friends.First(), guids.First());

            if (keyG != guids.First())
            {
                throw new InvalidOperationException($"Error: {0}, Keys not identical E: {guids.First()}, A: {keyG}.");
            }

            await guidTable.Clear();
            var keyGs = await guidTable.BulkAdd(friends, guids);

            if (keyGs.First() != guids.Last())
            {
                throw new InvalidOperationException($"Error: {1}, Keys not identical.");
            }

            var guidLast = guids.Last();
            var foundFriendsGuid = await guidTable.Where(guidTable.PrimaryKey).Equal(guidLast).ToArray();

            if (foundFriendsGuid.Count() != 1)
            {
                throw new InvalidOperationException($"Error: {2}, Keys not identical.");
            }

            await guidTable.Clear();
            keyGs = await guidTable.BulkAdd(friends, guids, true);

            if (keyGs.First() != guids.First())
            {
                throw new InvalidOperationException($"Error: {3}, Keys not identical.");
            }

            var primaryKeysGuid1 = await guidTable.ToCollection().Keys();
            var primaryKeysGuid2 = await guidTable.OrderBy(guidTable.PrimaryKey).Keys();

            if (!primaryKeysGuid1.SequenceEqual(primaryKeysGuid2))
            {
                throw new InvalidOperationException($"Error: {4}, Keys not identical.");
            }

            if (!primaryKeysGuid2.OrderBy(k => k).SequenceEqual(guids.OrderBy(k => k)))
            {
                throw new InvalidOperationException($"Error: {5}, Keys not identical.");
            }

            var pgks = await guidTable.OrderBy(f => f.Name).PrimaryKeys();

            if (!pgks.SequenceEqual(guids))
            {
                throw new InvalidOperationException($"Error: {6}, Keys not identical.");
            }

            var keyB = await byteTable.Add(friends.First(), bytes.First());

            if (!keyB.SequenceEqual(bytes.First()))
            {
                throw new InvalidOperationException($"Error: {7}, Keys not identical.");
            }

            await byteTable.Clear();
            var keyBs = await byteTable.BulkAdd(friends, bytes);

            if (!keyBs.First().SequenceEqual(bytes.Last()))
            {
                throw new InvalidOperationException($"Error: {8}, Keys not identical.");
            }

            var indexLast = bytes.Last();
            var foundFriendsByte = await byteTable.Where(byteTable.PrimaryKey).Equal(indexLast).ToArray();

            if (foundFriendsByte.Count() != 1)
            {
                throw new InvalidOperationException($"Error: {9}, Keys not identical.");
            }

            await byteTable.Clear();
            keyBs = await byteTable.BulkAdd(friends, bytes, true);

            if (!keyBs.SequenceEqual(bytes))
            {
                throw new InvalidOperationException($"Error: {10}, Keys not identical.");
            }

            var pbks = await byteTable.OrderBy(f => f.Name).PrimaryKeys();

            if (!pbks.SequenceEqual(bytes))
            {
                throw new InvalidOperationException($"Error: {11}, Keys not identical.");
            }

            var primaryKeysByte1 = await byteTable.ToCollection().Keys();
            var primaryKeysByte2 = await byteTable.OrderBy(byteTable.PrimaryKey).Keys();

            if (!primaryKeysByte1.SequenceEqual(primaryKeysByte2))
            {
                throw new InvalidOperationException($"Error: {12}, Keys not identical.");
            }

            if (!primaryKeysByte2.OrderBy(k => k.ToString()).SequenceEqual(bytes.OrderBy(k => k.ToString())))
            {
                throw new InvalidOperationException($"Error: {13}, Keys not identical.");
            }

            var keyC = await cTable.Add(friends.First(), compounds.First());

            if (keyC != compounds.First())
            {
                throw new InvalidOperationException($"Error: {14}, Keys not identical.");
            }

            await cTable.Clear();
            var keyCs = await cTable.BulkAdd(friends, compounds);

            if (keyCs.First() != compounds.Last())
            {
                throw new InvalidOperationException($"Error: {15}, Keys not identical.");
            }

            await cTable.Clear();
            keyCs = await cTable.BulkAdd(friends, compounds, true);

            if (keyCs.First() != compounds.First())
            {
                throw new InvalidOperationException($"Error: {16}, Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await guidTable.Clear();
                keyG = await guidTable.Add(friends.First(), guids.First());

                await byteTable.Clear();
                keyB = await byteTable.Add(friends.First(), bytes.First());

                await cTable.Clear();
                keyC = await cTable.Add(friends.First(), compounds.First());
            });

            if (keyG != guids.First())
            {
                throw new InvalidOperationException($"Error: {17}, Keys not identical.");
            }

            if (!keyB.SequenceEqual(bytes.First()))
            {
                throw new InvalidOperationException($"Error: {18}, Keys not identical.");
            }

            if (keyC != compounds.First())
            {
                throw new InvalidOperationException($"Error: {19}, Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await guidTable.Clear();
                keyGs = await guidTable.BulkAdd(friends, guids);

                foundFriendsGuid = await guidTable.Where(guidTable.PrimaryKey).Equal(guidLast).ToArray();

                await byteTable.Clear();
                keyBs = await byteTable.BulkAdd(friends, bytes);

                foundFriendsByte = await byteTable.Where(byteTable.PrimaryKey).Equal(indexLast).ToArray();

                await cTable.Clear();
                keyCs = await cTable.BulkAdd(friends, compounds);
            });

            if (foundFriendsGuid.Count() != 1)
            {
                throw new InvalidOperationException($"Error: {20}, Keys not identical.");
            }

            if (foundFriendsByte.Count() != 1)
            {
                throw new InvalidOperationException($"Error: {21}, Keys not identical.");
            }

            if (keyGs.First() != guids.Last())
            {
                throw new InvalidOperationException($"Error: {22}, Keys not identical.");
            }

            if (!keyBs.First().SequenceEqual(bytes.Last()))
            {
                throw new InvalidOperationException($"Error: {23}, Keys not identical.");
            }

            if (keyCs.First() != compounds.Last())
            {
                throw new InvalidOperationException($"Error: {24}, Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await guidTable.Clear();
                keyGs = await guidTable.BulkAdd(friends, guids, true);
                pgks = await guidTable.OrderBy(f => f.Name).PrimaryKeys();
                primaryKeysGuid1 = await guidTable.ToCollection().Keys();
                primaryKeysGuid2 = await guidTable.OrderBy(guidTable.PrimaryKey).Keys();

                await byteTable.Clear(); // for yet unknown reasons when using Playwright with Webkit clear will not work here
                keyBs = await byteTable.BulkAdd(friends, bytes, true);
                pbks = await byteTable.OrderBy(f => f.Name).PrimaryKeys();
                primaryKeysByte1 = await byteTable.ToCollection().Keys();
                primaryKeysByte2 = await byteTable.OrderBy(byteTable.PrimaryKey).Keys();

                keyCs = await cTable.BulkAdd(friends, compounds, true);
            });

            if (keyGs.First() != guids.First())
            {
                throw new InvalidOperationException($"Error: {25}, Keys not identical.");
            }

            if (pgks.First() != guids.First())
            {
                throw new InvalidOperationException($"Error: {26}, Keys not identical.");
            }

            if (!keyBs.SequenceEqual(bytes))
            {
                throw new InvalidOperationException($"Error: {27}, Keys not identical.");
            }

            if (!pbks.SequenceEqual(bytes))
            {
                throw new InvalidOperationException($"Error: {28}, Keys not identical (E: {bytes.Length} - G: {pbks.Count()}).");
            }

            if (!primaryKeysGuid1.SequenceEqual(primaryKeysGuid2))
            {
                throw new InvalidOperationException($"Error: {29}, Keys not identical.");
            }

            if (!primaryKeysGuid2.OrderBy(k => k).SequenceEqual(guids.OrderBy(k => k)))
            {
                throw new InvalidOperationException($"Error: {30}, Keys not identical.");
            }

            if (!primaryKeysByte1.SequenceEqual(primaryKeysByte2))
            {
                throw new InvalidOperationException($"Error: {31}, Keys not identical.");
            }

            if (!primaryKeysByte2.OrderBy(k => k.ToString()).SequenceEqual(bytes.OrderBy(k => k.ToString())))
            {
                throw new InvalidOperationException($"Error: {32}, Keys not identical.");
            }

            if (keyCs.First() != compounds.First())
            {
                throw new InvalidOperationException($"Error: {33}, Keys not identical.");
            }

            return "OK";
        }
    }
}
