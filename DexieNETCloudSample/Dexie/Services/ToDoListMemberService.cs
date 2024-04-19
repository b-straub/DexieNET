using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DexieNETCloudSample.Dexie.Services
{
    public class MemberRoleSelection(MemberRole role)
    {
        public MemberRole Role { get; } = role;
        public string? RoleName { get; set; }

        public override bool Equals(object? o)
        {
            var other = o as MemberRoleSelection;
            return other?.Role == Role;
        }

        public override int GetHashCode() => Role.GetHashCode();

        public override string ToString() => RoleName ?? Role.ToString();
    }

    public enum MemberRole
    {
        OWNER,
        ADMIN,
        USER,
        GUEST
    }

    public enum MemberAction
    {
        NONE,
        OWNER,
        ACCEPT,
        DELETE,
        LEAVE,
    }

    public enum MemberState
    {
        NONE,
        PENDING,
        ACCEPTED,
        REJECTED,
    }


    public sealed partial class ToDoListMemberService : RxBLService
    {
        public bool IsDBOpen => _dbService.DB is not null;
        public IState<ToDoDBList?> List { get; }
        public IEnumerable<Member> Members { get; private set; } = [];
        public IEnumerable<string> Users { get; private set; } = [];

        private readonly DexieCloudService _dbService;
        private ITable? _membersTable;
        private readonly CompositeDisposable _dbDisposeBag = [];
        private readonly IConfiguration? _configuration;
        private readonly CompositeDisposable _permissionsDisposeBag = [];
        private IUsePermissions<Member>? _permissionsMember;
        private IUsePermissions<Realm>? _permissionsRealm;

        public ToDoListMemberService(IServiceProvider serviceProvider)
        {
            _dbService = serviceProvider.GetRequiredService<DexieCloudService>();
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();

            List = this.CreateState((ToDoDBList?)null);
        }

        protected override ValueTask ContextReadyAsync()
        {
            if (IsDBOpen)
            {
                InitDB();
            }

            ArgumentNullException.ThrowIfNull(_dbService.DB);

            var memberQuery = _dbService.DB.LiveQuery(async () =>
                await _dbService.DB.Members.Where(m => m.RealmId).Equal(List.Value?.RealmId).ToArray());

            var useMemberQuery = memberQuery.UseLiveQuery(this.Where(cr => cr.ID == List.ID).Select(_ => Unit.Default));

            _dbDisposeBag.Add(useMemberQuery.Select(m =>
            {
                Members = m;

                var usersInvited = Members
                    .Where(m => m.Invite.True())
                    .Select(m => m.UserId);

                Users = _configuration.GetUsers().Where(u =>
                {
                    return u != _dbService.UserLogin.Value?.UserId && !usersInvited.Contains(u);
                })
                .Append("by eMail");
                return Unit.Default;
            }).Subscribe(this));

            return ValueTask.CompletedTask;
        }

        public MemberRowScope CreateRowScope()
        {
            return new MemberRowScope(this); 
        }

        public bool CanAddMember()
        {
            return Users.Any() && _membersTable is not null &&
                (_permissionsMember?.CanAdd(List.Value, _membersTable)).True();
        }

        private bool CanUpdateRole(Member Member)
        {
            var isOwner = GetMemberRole(Member) is MemberRole.OWNER;

            return !isOwner && (_permissionsMember?.CanUpdate(Member, m => m.Roles)).True();
        }

        private bool CanChangeToRole(Member? Member, MemberRole newRole)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(Member);
            ArgumentNullException.ThrowIfNull(List.Value);

            var canUpdateRoles = (_permissionsMember?.CanUpdate(Member, m => m.Roles)).True();
            var canUpdateOwner = (_permissionsRealm?.CanUpdate(List.Value, m => m.Owner)).True();

            return newRole switch
            {
                MemberRole.OWNER => Member?.UserId is not null && canUpdateRoles && canUpdateOwner,
                _ => canUpdateRoles
            };
        }

        public MemberAction GetMemberAction(Member? member)
        {
            if (member is null)
            {
                return MemberAction.NONE;
            }

            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(List.Value);

            var isOwner = member.UserId == List.Value.Owner;
            var isCurrentUser = member.UserId == _dbService.DB.CurrentUserId();
            var aTime = member.Accepted ?? DateTime.UtcNow;
            var rTime = member.Rejected ?? member.Accepted;

            if (isOwner)
            {
                return MemberAction.OWNER;
            }
            else if (!isCurrentUser && (_permissionsMember?.CanDelete(member)).True())
            {
                return MemberAction.DELETE;
            }

            if (isCurrentUser)
            {
                if (aTime >= rTime)
                {
                    return MemberAction.LEAVE;
                }
                else
                    return MemberAction.ACCEPT;
            }

            return MemberAction.NONE;
        }

        public static MemberState GetMemberState(Member? member)
        {
            if (member is null)
            {
                return MemberState.NONE;
            }

            if (member.Accepted is null && member.Rejected is null)
            {
                return MemberState.PENDING;
            }
            else if (member.Accepted is not null && member.Rejected is null)
            {
                return MemberState.ACCEPTED;
            }
            else if (member.Rejected is not null && member.Accepted is null)
            {
                return MemberState.REJECTED;
            }

            return MemberState.NONE;
        }

        private MemberRole GetMemberRole(Member member)
        {
            ArgumentNullException.ThrowIfNull(_dbService?.Roles);
            ArgumentNullException.ThrowIfNull(List.Value);

            var roleName = member.Roles?.FirstOrDefault();
            var memberRole = MemberRole.GUEST;

            if (roleName is not null && _dbService.Roles.HasValue() && _dbService.Roles.Value.TryGetValue(roleName, out var role))
            {
                if (role.DisplayName is not null)
                {
                    memberRole = (MemberRole)Enum.Parse(typeof(MemberRole), role.DisplayName.ToUpperInvariant());
                }
            }

            memberRole = member.UserId == List.Value.Owner ? MemberRole.OWNER : memberRole;

            return memberRole;
        }

        public string? GetRoleDisplayName(MemberRole memberRole)
        {
            if (memberRole is MemberRole.OWNER)
            {
                return "Owner";
            }

            ArgumentNullException.ThrowIfNull(_dbService?.Roles);

            if (_dbService.Roles.HasValue() && _dbService.Roles.Value.TryGetValue(memberRole.ToString().ToLowerInvariant(), out var role))
            {
                return role.DisplayName;
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool dispose)
        {
            _permissionsDisposeBag.Clear();
            _permissionsMember?.Dispose();
            _permissionsRealm?.Dispose();
            _permissionsMember = null;

            if (dispose)
            {
                _dbDisposeBag.Clear();
            }
        }

        private void InitDB()
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            _membersTable = _dbService.DB.Members;

            _permissionsMember = _dbService.DB.Members.CreateUsePermissions();
            _permissionsDisposeBag.Add(_permissionsMember.Subscribe(this));

            _permissionsRealm = _dbService.DB.Realms.CreateUsePermissions();
            _permissionsDisposeBag.Add(_permissionsRealm.Subscribe(this));
        }

        public async Task AddMember(string user)
        {
            ArgumentNullException.ThrowIfNull(List.Value);

            await ShareWith(List.Value, user, user, true, MemberRole.USER);
        }
    }
}
