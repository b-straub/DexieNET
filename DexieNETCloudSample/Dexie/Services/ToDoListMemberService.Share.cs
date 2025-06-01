using DexieNET;
using DexieCloudNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;

namespace DexieNETCloudSample.Dexie.Services
{
    public sealed partial class ToDoListMemberService
    {
        public bool IsSharable(ToDoDBList list)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(list.ID);

            return list.RealmId == _dbService.DB.GetTiedRealmID(list.ID);
        }

        private async Task ShareWith(ToDoDBList list, string name, string email, bool sendEmail, MemberRole role)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(list.ID);
            ArgumentNullException.ThrowIfNull(list.RealmId);

            await _dbService.DB.Transaction(async t =>
            {
                var realmId = list.RealmId;

                if (t.Collecting || !IsSharable(list))
                {
                    realmId = await MakeSharable(list);
                }

                // Add given name and email as a member with full permissions
                var member = new Member(realmId, name, email, sendEmail, new[] { role.ToString().ToLowerInvariant() });
                await _dbService.DB.Members.Put(member);
            });
        }

        private async Task UnShareWith(ToDoDBList list, Member member)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(member.Id);
            var currentUserId = _dbService.DB.CurrentUserId();

            await _dbService.DB.Transaction(async t =>
            {
                await _dbService.DB.Members.Delete(member.Id);

                var numOtherPeople = await _dbService.DB.Members
                    .Where(m => m.RealmId, member.RealmId)
                    .Filter(m => m.UserId != currentUserId)
                    .Count();

                if (t.Collecting || numOtherPeople == 0)
                {
                    // Only our own member left.
                    await UnshareWithEveryone(list);
                }
            });
        }

        private async Task Leave(ToDoDBList list)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(list.RealmId);
            var currentUserId = _dbService.DB.CurrentUserId();

            await _dbService.DB.Members.Where(m => m.RealmId, list.RealmId, m => m.UserId, currentUserId).Delete();
        }

        private async Task<string> MakeSharable(ToDoDBList list)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(list.ID);

            if (IsPrivate(list))
            {
                throw new InvalidOperationException("Private lists cannot be made sharable!");
            }

            var currentRealmId = list.RealmId;
            var newRealmId = _dbService.DB.GetTiedRealmID(list.ID);

            await _dbService.DB.Transaction(async t =>
            {
                // Create tied realm
                // We use put() here in case same user does this on
                // two offline devices to add different members - we don't
                // want one of the actions to fail - we want both to succeed
                // and add both members
                var realm = new Realm(list.Title, "a to-do list", null, newRealmId);
                await _dbService.DB.Realms.Put(realm);

                // "Realmify entity" (setting realmId equals own id will make it become a Realm)
                await _dbService.DB.ToDoDBLists
                        .Update(list.ID, l => l.RealmId, newRealmId);

                // Move all todo items into the new realm consistently (modify() is consistent across sync peers)
                await _dbService.DB.ToDoDBItems
                        .Where(i => i.ListID, list.ID, i => i.RealmId, currentRealmId)
                        .Modify(i => i.RealmId, newRealmId);
            });

            return newRealmId;
        }

        private async Task UnshareWithEveryone(ToDoDBList list)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(list.ID);

            var tiedRealmId = _dbService.DB.GetTiedRealmID(list.ID);
            var currentUserId = _dbService.DB.CurrentUserId();

            await _dbService.DB.Transaction(async _ =>
            {
                // Move todoItems out of the realm in a sync-consistent operation:
                await _dbService.DB.ToDoDBItems
                        .Where(i => i.ListID, list.ID, i => i.RealmId, tiedRealmId)
                        .Modify(i => i.RealmId, currentUserId);

                // Move the todoList back into your private realm:
                await _dbService.DB.ToDoDBLists
                        .Update(list.ID, l => l.RealmId, currentUserId);

                // Remove all access (Collection.delete() is a sync-consistent operation)
                await _dbService.DB.Members.Where(m => m.RealmId).Equal(tiedRealmId).Delete();

                // Delete tied realm
                await _dbService.DB.Realms.Delete(tiedRealmId);
            });
        }

        private static bool IsPrivate(ToDoDBList list)
        {
            return (list.ID?.StartsWith("#")).True();
        }
    }
}
