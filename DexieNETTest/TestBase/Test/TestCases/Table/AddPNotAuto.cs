﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class AddPNotAuto(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "AddPNotAuto";

        public override async ValueTask<string?> RunTest()
        {
            var tableS = DB.FriendIBPs;
            await tableS.Clear();

            var friendsS = new[] {
                new FriendIBP("TestName1", "AA"),
                new FriendIBP("TestName2", "BB")
            };

            var tableB = DB.FriendIBBPs;
            await tableB.Clear();

            var friendsB = new[] {
                new FriendIBBP("TestName1", [1, 0]),
                new FriendIBBP("TestName2", [1, 1])
            };

            var keyS = await tableS.Add(friendsS.First());
            var friendAddedS = (await tableS.ToArray()).FirstOrDefault();

            if (friendAddedS?.Id != keyS)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableS.Clear();
            var keySs = await tableS.BulkAdd(friendsS);

            if (keySs.FirstOrDefault() != friendsS.Last().Id)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableS.Clear();
            keySs = await tableS.BulkAdd(friendsS, true);

            if (keySs.FirstOrDefault() != friendsS.First().Id)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            var stringLast = keySs.Last();
            var foundFriendsString = await tableS.Where(tableS.PrimaryKey).Equal(stringLast).ToArray();

            if (foundFriendsString.Count() != 1)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            var keyB = await tableB.Add(friendsB.First());
            var friendAddedB = (await tableB.ToArray()).FirstOrDefault();

            if (!(friendAddedB?.Id.SequenceEqual(keyB)).True())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableB.Clear();
            var keyBs = await tableB.BulkAdd(friendsB);

            if (!keyBs.First().SequenceEqual(friendsB.Last().Id))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableB.Clear();
            keyBs = await tableB.BulkAdd(friendsB, true);

            if (!keyBs.SequenceEqual(friendsB.Select(x => x.Id)))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            var byteLast = keyBs.Last();
            var foundFriendsByte = await tableB.Where(tableB.PrimaryKey).Equal(byteLast).ToArray();

            if (foundFriendsByte.Count() != 1)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await tableS.Clear();
                await tableS.Add(friendsS.First());

                await tableB.Clear();
                await tableB.Add(friendsB.First());
            });

            friendAddedS = (await tableS.ToArray()).FirstOrDefault();

            if (friendAddedS?.Id != keyS)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            friendAddedB = (await tableB.ToArray()).FirstOrDefault();

            if (!(friendAddedB?.Id.SequenceEqual(keyB)).True())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await tableS.Clear();
                keySs = await tableS.BulkAdd(friendsS);

                await tableB.Clear();
                keyBs = await tableB.BulkAdd(friendsB);
            });

            if (keySs.FirstOrDefault() != friendsS.Last().Id)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (!keyBs.First().SequenceEqual(friendsB.Last().Id))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await tableS.Clear();
                keySs = await tableS.BulkAdd(friendsS, true);

                foundFriendsString = await tableS.Where(tableS.PrimaryKey).Equal(stringLast).ToArray();

                await tableB.Clear();
                keyBs = await tableB.BulkAdd(friendsB, true);

                foundFriendsByte = await tableB.Where(tableB.PrimaryKey).Equal(byteLast).ToArray();
            });

            if (keySs.FirstOrDefault() != friendsS.First().Id)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (!keyBs.SequenceEqual(friendsB.Select(x => x.Id)))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (foundFriendsString.Count() != 1)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (foundFriendsByte.Count() != 1)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            return "OK";
        }
    }
}
