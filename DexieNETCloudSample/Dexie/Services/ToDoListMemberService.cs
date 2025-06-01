using DexieNET;
using DexieCloudNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using R3;

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
        }

        protected override ValueTask ContextReadyAsync()
        {
            if (!IsDBOpen)
            {
                throw new InvalidOperationException("MemberService DB is not open");
            }

            InitDB();
            
            return ValueTask.CompletedTask;
        }

        public MemberScope CreateMemberScope()
        {
            return new MemberScope(this);
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

            if (member.Accepted is not null && member.Rejected is null)
            {
                return MemberState.ACCEPTED;
            }

            if (member.Rejected is not null && member.Accepted is null)
            {
                return MemberState.REJECTED;
            }

            return MemberState.NONE;
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

        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                _permissionsDisposeBag.Dispose();
                _dbDisposeBag.Dispose();
            }
        }

        private void InitDB()
        {
            ArgumentNullException.ThrowIfNull(_dbService.DB);
            _membersTable = _dbService.DB.Members;

            _permissionsMember = _dbService.DB.Members.CreateUsePermissions();
            _permissionsDisposeBag.Add(_permissionsMember.AsObservable.Subscribe(this));
            _permissionsRealm = _dbService.DB.Realms.CreateUsePermissions();
            _permissionsDisposeBag.Add(_permissionsRealm.AsObservable.Subscribe(this));
        }
    }
}
