﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class PutPNotAuto(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "PutPNotAuto";

        public override async ValueTask<string?> RunTest()
        {
            var tableS = DB.FriendIBPs;
            await tableS.Clear();

            var tableB = DB.FriendIBBPs;
            await tableB.Clear();

            var friendsS = new[] {
                new FriendIBP("TestName1", "AA"),
                new FriendIBP("TestName2", "BB")
            };

            var friendsSU = new[] {
                new FriendIBP("Updated", "AA"),
                new FriendIBP("Updated", "BB")
            };

            var friendsB = new[] {
                new FriendIBBP("TestName1", [1, 0]),
                new FriendIBBP("TestName2", [1, 1])
            };

            var friendsBU = new[] {
                new FriendIBBP("Updated", [1, 0]),
                new FriendIBBP("Updated", [1, 1])
            };

            var keyS = await tableS.Put(friendsS.First());
            var friendAddedS = (await tableS.ToArray()).FirstOrDefault();

            if (friendAddedS?.Id != keyS)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableS.Put(friendsSU.First());
            friendAddedS = (await tableS.ToArray()).FirstOrDefault();

            if (friendAddedS?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            await tableS.Clear();
            var keySs = await tableS.BulkPut(friendsS);

            if (keySs.FirstOrDefault() != friendsS.Last().Id)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableS.Clear();
            keySs = await tableS.BulkPut(friendsS, true);

            if (keySs.FirstOrDefault() != friendsS.First().Id)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableS.BulkPut(friendsSU);
            friendAddedS = (await tableS.ToArray()).FirstOrDefault();

            if (friendAddedS?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            var keyB = await tableB.Put(friendsB.First());
            var friendAddedB = (await tableB.ToArray()).FirstOrDefault();

            if (!(friendAddedB?.Id.SequenceEqual(keyB)).True())
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableB.Put(friendsBU.First());
            friendAddedB = (await tableB.ToArray()).FirstOrDefault();

            if (friendAddedB?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            await tableB.Clear();
            var keyBs = await tableB.BulkPut(friendsB);

            if (!keyBs.First().SequenceEqual(friendsB.Last().Id))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableB.Clear();
            keyBs = await tableB.BulkPut(friendsB, true);

            if (!keyBs.SequenceEqual(friendsB.Select(x => x.Id)))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            await tableB.BulkPut(friendsBU);
            friendAddedB = (await tableB.ToArray()).FirstOrDefault();

            if (friendAddedB?.Name != "Updated")
            {
                throw new InvalidOperationException("Put not successful.");
            }

            await DB.Transaction(async _ =>
            {
                await tableS.Clear();
                await tableS.Put(friendsS.First());

                await tableB.Clear();
                await tableB.Put(friendsB.First());
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
                keySs = await tableS.BulkPut(friendsS);

                await tableB.Clear();
                keyBs = await tableB.BulkPut(friendsB);
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
                keySs = await tableS.BulkPut(friendsS, true);

                await tableB.Clear();
                keyBs = await tableB.BulkPut(friendsB, true);
            });

            if (keySs.FirstOrDefault() != friendsS.First().Id)
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (!keyBs.SequenceEqual(friendsB.Select(x => x.Id)))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            return "OK";
        }
    }
}
