using DexieCloudNET;
using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using R3;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListMemberService
    {
        public partial class MemberScope(ToDoListMemberService service)
            : RxBLStateScope<ToDoListMemberService>(service)
        {
            public IEnumerable<Member> Members { get; private set; } = [];
            public IEnumerable<string> Users { get; private set; } = [];
            public ToDoDBList? List { get; private set; }
            
            private IDisposable? _membersDisposable;

            public MemberRowScope CreateRowScope()
            {
                return new MemberRowScope(Service, this);
            }

            public void SetList(ToDoDBList list)
            {
                List = list;
            }

            public override ValueTask OnContextReadyAsync()
            {
                ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                ArgumentNullException.ThrowIfNull(List);
                
                var memberQuery = Service._dbService.DB.LiveQuery(async () =>
                {
                    var m = await Service._dbService.DB.Members.ToArray();
                    return m.Where(mb => mb.RealmId == List.RealmId).ToArray();
                });

                _membersDisposable = memberQuery.AsObservable.SelectAwait(async (members, _) =>
                {
                    Members = members;

                    var usersInvited = Members
                        .Where(mb => mb.Invite.True())
                        .Select(mb => mb.UserId);

                    Users = Service._configuration
                        .GetUsers()
                        .Where(u => u != Service._dbService.UserLogin.Value?.UserId && !usersInvited.Contains(u))
                        .Append("by eMail");
                   
                    var otherPeople = Members
                        .Any(m => m.UserId != Service._dbService.DB.CurrentUserId());

                    if (!otherPeople && Members.Any())
                    {
                        // Only our own member left.
                        await Service.UnshareWithEveryone(List);
                    }
                    
                    return Unit.Default;
                }).Subscribe(Service);

                return ValueTask.CompletedTask;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _membersDisposable?.Dispose();
                    _membersDisposable = null;
                }

                base.Dispose(disposing);
            }

            private async Task ChangeAccess(Member Member, MemberRole Role, MemberRole NewRole)
            {
                ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                ArgumentNullException.ThrowIfNull(List?.ID);
                ArgumentNullException.ThrowIfNull(List?.Owner);
                ArgumentNullException.ThrowIfNull(Member.Id);

                var realmId = Service._dbService.DB.GetTiedRealmID(List.ID);

                await Service._dbService.DB.Transaction(async _ =>
                {
                    if (Role is not MemberRole.OWNER && NewRole is MemberRole.OWNER)
                    {
                        // Cannot give ownership to user before invite is accepted.
                        ArgumentNullException.ThrowIfNull(Member.UserId);

                        // Before changing owner, give full permissions to the old owner:
                        await Service._dbService.DB.Members
                            .Where(m => m.RealmId, realmId, m => m.UserId, List.Owner)
                            .Modify(m => m.Roles, new[] { MemberRole.ADMIN.ToString().ToLowerInvariant() });

                        // Change owner of all members in the realm:
                        await Service._dbService.DB.Members
                            .Where(m => m.RealmId, realmId)
                            .Modify(m => m.Owner, Member.UserId);

                        // Change owner of the todo list:
                        await Service._dbService.DB.ToDoDBLists
                            .Update(List.ID, l => l.Owner, Member.UserId);

                        // Change owner of the todo list items
                        await Service._dbService.DB.ToDoDBItems
                            .Where(i => i.ListID, List.ID)
                            .Modify(i => i.Owner, Member.UserId);

                        // Change owner of realm:
                        await Service._dbService.DB.Realms
                            .Update(realmId, r => r.Owner, Member.UserId);
                    }

                    if (NewRole is not MemberRole.OWNER)
                    {
                        await Service._dbService.DB.Members
                            .Update(Member.Id, m => m.Permissions, new Permission(), m => m.Roles,
                                new[] { NewRole.ToString().ToLowerInvariant() });
                    }

                    if (Role is MemberRole.OWNER && NewRole is not MemberRole.OWNER)
                    {
                        // Remove ownership by letting current user take ownership instead:
                        await Service._dbService.DB.ToDoDBLists
                            .Update(List.ID, l => l.Owner, Service._dbService.DB.CurrentUserId());

                        // Change ownership of the todo list items
                        await Service._dbService.DB.ToDoDBItems
                            .Where(i => i.ListID).Equal(List.ID)
                            .Modify(i => i.Owner, Service._dbService.DB.CurrentUserId());

                        await Service._dbService.DB.Realms
                            .Update(realmId, r => r.Owner, Service._dbService.DB.CurrentUserId());
                    }
                });
            }

            public async Task AddMember(string user)
            {
                ArgumentNullException.ThrowIfNull(List);
                await Service.ShareWith(List, user, user, true, MemberRole.USER);
            }

            public bool CanAddMember()
            {
                return Users.Any() && Service._membersTable is not null &&
                       (Service._permissionsMember?.CanAdd(List, Service._membersTable)).True();
            }

            public Func<IStateCommandAsync, Task> ChangeMemberState(Member member) => async _ =>
            {
                ArgumentNullException.ThrowIfNull(List);
                
                var memberAction = GetMemberAction(member);

                if (memberAction is MemberAction.DELETE)
                {
                    await Service.UnShareWith(List, member);
                }
                else if (memberAction is MemberAction.LEAVE)
                {
                    await Service.Leave(List);
                }
                else if (memberAction is MemberAction.ACCEPT)
                {
                    ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                    await Service._dbService.DB.AcceptInvite(member);
                }
            };

            public Func<bool> CanChangeMemberState(Member member) => () =>
            {
                var memberAction = GetMemberAction(member);

                return memberAction switch
                {
                    MemberAction.DELETE => (Service._permissionsMember?.CanDelete(member)).True(),
                    MemberAction.LEAVE => true,
                    MemberAction.ACCEPT => (Service._permissionsMember?.CanUpdate(member, m => m.Accepted)).True(),
                    _ => false
                };
            };

            public MemberAction GetMemberAction(Member member)
            {
                ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                ArgumentNullException.ThrowIfNull(List);

                var isOwner = member.UserId == List.Owner;
                var isCurrentUser = member.UserId == Service._dbService.DB.CurrentUserId();
                var aTime = member.Accepted ?? DateTime.UtcNow;
                var rTime = member.Rejected ?? member.Accepted;

                if (isOwner)
                {
                    return MemberAction.OWNER;
                }

                if (!isCurrentUser && (Service._permissionsMember?.CanDelete(member)).True())
                {
                    return MemberAction.DELETE;
                }

                if (isCurrentUser)
                {
                    return aTime >= rTime ? MemberAction.LEAVE : MemberAction.ACCEPT;
                }

                return MemberAction.NONE;
            }

            private bool CanUpdateRole(Member member)
            {
                var isOwner = GetMemberRole(member) is MemberRole.OWNER;

                return !isOwner && (Service._permissionsMember?.CanUpdate(member, m => m.Roles)).True();
            }

            private bool CanChangeToRole(Member member, MemberRole newRole)
            {
                ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                ArgumentNullException.ThrowIfNull(List);
                
                var canUpdateRoles = (Service._permissionsMember?.CanUpdate(member, m => m.Roles)).True();
                var canUpdateOwner = (Service._permissionsRealm?.CanUpdate(List, m => m.Owner)).True();

                return newRole switch
                {
                    MemberRole.OWNER => member?.UserId is not null && canUpdateRoles && canUpdateOwner,
                    _ => canUpdateRoles
                };
            }

            private MemberRole GetMemberRole(Member member)
            {
                ArgumentNullException.ThrowIfNull(Service._dbService?.Roles);
                ArgumentNullException.ThrowIfNull(List);
                
                var roleName = member.Roles?.FirstOrDefault();
                var memberRole = MemberRole.GUEST;

                if (roleName is not null && Service._dbService.Roles.HasValue() &&
                    Service._dbService.Roles.Value.TryGetValue(roleName, out var role))
                {
                    if (role.DisplayName is not null)
                    {
                        memberRole = (MemberRole)Enum.Parse(typeof(MemberRole), role.DisplayName.ToUpperInvariant());
                    }
                }

                memberRole = member.UserId == List.Owner ? MemberRole.OWNER : memberRole;

                return memberRole;
            }
        }
    }
}