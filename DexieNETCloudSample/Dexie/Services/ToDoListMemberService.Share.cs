using DexieNET;
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

        private async Task ChangeAccess((Member Member, MemberRole Role, MemberRole NewRole)? MemberAccess)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(List?.ID);
            ArgumentNullException.ThrowIfNull(List?.Owner);
            ArgumentNullException.ThrowIfNull(MemberAccess?.Member.Id);
            ArgumentNullException.ThrowIfNull(MemberAccess?.Role);
            ArgumentNullException.ThrowIfNull(MemberAccess?.NewRole);

            var realmId = _dbService.DB.GetTiedRealmID(List.ID);

            await _dbService.DB.Transaction(async _ =>
            {
                if (MemberAccess.Value.Role is not MemberRole.OWNER && MemberAccess.Value.NewRole is MemberRole.OWNER)
                {
                    // Cannot give ownership to user before invite is accepted.
                    ArgumentNullException.ThrowIfNull(MemberAccess?.Member.UserId);

                    // Before changing owner, give full permissions to the old owner:
                    await _dbService.DB.Members
                        .Where(m => m.RealmId, realmId, m => m.UserId, List.Owner)
                        .Modify(m => m.Roles, new[] { MemberRole.ADMIN.ToString().ToLowerInvariant() });

                    // Change owner of all members in the realm:
                    await _dbService.DB.Members
                        .Where(m => m.RealmId, realmId)
                        .Modify(m => m.Owner, MemberAccess.Value.Member.UserId);

                    // Change owner of the todo list:
                    await _dbService.DB.ToDoDBLists
                        .Update(List.ID, l => l.Owner, MemberAccess.Value.Member.UserId);

                    // Change owner of the todo list items
                    await _dbService.DB.ToDoDBItems
                       .Where(i => i.ListID, List.ID)
                       .Modify(i => i.Owner, MemberAccess.Value.Member.UserId);

                    // Change owner of realm:
                    await _dbService.DB.Realms
                        .Update(realmId, r => r.Owner, MemberAccess.Value.Member.UserId);
                }

                if (MemberAccess.Value.NewRole is not MemberRole.OWNER)
                {
                    await _dbService.DB.Members
                        .Update(MemberAccess.Value.Member.Id, m => m.Permissions, new Permission(), m => m.Roles, new[] { MemberAccess.Value.NewRole.ToString().ToLowerInvariant() });
                }

                if (MemberAccess.Value.Role is MemberRole.OWNER && MemberAccess.Value.NewRole is not MemberRole.OWNER)
                {
                    // Remove ownership by letting current user take ownership instead:
                    await _dbService.DB.ToDoDBLists
                        .Update(List.ID, l => l.Owner, _dbService.DB.CurrentUserId());

                    // Change ownership of the todo list items
                    await _dbService.DB.ToDoDBItems
                       .Where(i => i.ListID).Equal(List.ID)
                       .Modify(i => i.Owner, _dbService.DB.CurrentUserId());

                    await _dbService.DB.Realms
                        .Update(realmId, r => r.Owner, _dbService.DB.CurrentUserId());
                }
            });
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
