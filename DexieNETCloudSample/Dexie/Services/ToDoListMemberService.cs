using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using Microsoft.Extensions.Options;
using RxBlazorLightCore;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DexieNETCloudSample.Dexie.Services
{
    public class MemberRoleSelection
    {
        public MemberRole Role { get; }
        public string? RoleName { get; set; }

        public MemberRoleSelection(MemberRole role)
        {
            Role = role;
        }

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

    public sealed partial class ToDoListMemberService : RxBLServiceBase, IDisposable
    {
        public bool IsDBOpen => _dbService.DB is not null;
        public ToDoDBList? List { get; private set; }

        public IEnumerable<Member> Members { get; private set; } = Enumerable.Empty<Member>();
        public IEnumerable<string> Users { get; private set; } = Enumerable.Empty<string>();

        // Commands
        public ICommand<ToDoDBList> SetList;
        public ICommandAsync<string> InviteUser => new InviteUserCmd(this);
        public ICommandAsync<Member> ChangeMemberState => new ChangeMemberStateCmd(this);
        public IInputGroupAsync<MemberRoleSelection, Member> MemberRoleSelection => new MemberRoleSelectionIPG(this);

        private readonly DexieCloudService _dbService;
        private ITable? _membersTable;
        private readonly CompositeDisposable _dbDisposeBag = new();
        private readonly IOptions<DemoSettings>? _settings;
        private readonly CompositeDisposable _permissionsDisposeBag = new();
        private IUsePermissions<Member>? _permissionsMember;
        private IUsePermissions<Realm>? _permissionsRealm;

        public ToDoListMemberService(DexieCloudService databaseService, IOptions<DemoSettings>? settings)
        {
            _dbService = databaseService;
            _settings = settings;

            SetList = new SetListCmd(this);
        }

        public override async Task OnInitializedAsync()
        {
            if (IsDBOpen)
            {
                await InitDB();
            }

            _dbService.OnDelete += () => Dispose(false);

            _dbDisposeBag.Add(
                _dbService
               .Select(async c =>
               {
                   switch (c)
                   {
                       case DBChangedMessage.Cloud:
                           await InitDB();
                           break;
                       case DBChangedMessage.UserLogin:
                       case DBChangedMessage.Invites:
                       case DBChangedMessage.Roles:
                           StateHasChanged();
                           break;
                   };

                   return Unit.Default;
               }).Subscribe());

            ArgumentNullException.ThrowIfNull(_dbService.DB);

            var memberQuery = await _dbService.DB.LiveQuery(async () =>
                await _dbService.DB.Members().Where(m => m.RealmId).Equal(List?.RealmId).ToArray());

            ArgumentNullException.ThrowIfNull(_settings?.Value?.Users);

            var useMemberQuery = memberQuery.UseLiveQuery(SetList.Executed);

            _dbDisposeBag.Add(useMemberQuery.Subscribe(m =>
            {
                Members = m;

                var usersInvited = Members
                    .Where(m => m.Invite.True())
                    .Select(m => m.UserId);

                Users = _settings.Value.Users.Where(u =>
                {
                    return u != _dbService.UserLogin?.UserId && !usersInvited.Contains(u);
                })
                .Append("by eMail");

                StateHasChanged();
            }));
        }

        public bool CanAddMember()
        {
            return Users.Any() && _membersTable is not null &&
                (_permissionsMember?.CanAdd(List, _membersTable)).True();
        }

        public bool CanUpdateRole(Member Member)
        {
            var isOwner = GetMemberRole(Member) is MemberRole.OWNER;

            return !isOwner && (_permissionsMember?.CanUpdate(Member, m => m.Roles)).True();
        }

        public bool CanChangeToRole(Member? Member, MemberRole newRole)
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            ArgumentNullException.ThrowIfNull(Member);
            ArgumentNullException.ThrowIfNull(List);

            var canUpdateRoles = (_permissionsMember?.CanUpdate(Member, m => m.Roles)).True();
            var canUpdateOwner = (_permissionsRealm?.CanUpdate(List, m => m.Owner)).True();

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
            ArgumentNullException.ThrowIfNull(List);

            var isOwner = member.UserId == List.Owner;
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

        public MemberState GetMemberState(Member? member)
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

        public MemberRole GetMemberRole(Member member)
        {
            ArgumentNullException.ThrowIfNull(_dbService?.Roles);
            ArgumentNullException.ThrowIfNull(List);

            var roleName = member.Roles?.FirstOrDefault();
            var memberRole = MemberRole.GUEST;

            if (roleName is not null && _dbService.Roles.TryGetValue(roleName, out var role))
            {
                if (role.DisplayName is not null)
                {
                    memberRole = (MemberRole)Enum.Parse(typeof(MemberRole), role.DisplayName.ToUpperInvariant());
                }
            }

            memberRole = member.UserId == List.Owner ? MemberRole.OWNER : memberRole;

            return memberRole;
        }

        public string? GetRoleDisplayName(MemberRole memberRole)
        {
            if (memberRole is MemberRole.OWNER)
            {
                return "Owner";
            }

            ArgumentNullException.ThrowIfNull(_dbService?.Roles);

            if (_dbService.Roles.TryGetValue(memberRole.ToString().ToLowerInvariant(), out var role))
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

            _dbService.OnDelete -= () => Dispose(false);
        }

        private async Task InitDB()
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            _membersTable = await _dbService.DB.Members();

            _permissionsMember = await _dbService.DB.Members().CreateUsePermissions();

            _permissionsDisposeBag.Add(_permissionsMember.Subscribe(_ =>
            {
                StateHasChanged();
            }));

            _permissionsRealm = await _dbService.DB.Realms().CreateUsePermissions();

            _permissionsDisposeBag.Add(_permissionsRealm.Subscribe(_ =>
            {
                StateHasChanged();
            }));
        }

        private async Task DoInviteUser(string user)
        {
            ArgumentNullException.ThrowIfNull(List);

            await ShareWith(List, user, user, true, MemberRole.USER);
        }
    }
}
