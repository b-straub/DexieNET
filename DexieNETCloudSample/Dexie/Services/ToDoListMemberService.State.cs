using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListMemberService
    {
        public Func<bool> CanSetList(ToDoDBList? list) => () =>
        {
            return list is not null && list != List.Value;
        };

        public Func<IStateCommandAsync, Task> ChangeMemberState(Member member) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(List.Value);

            var memberAction = GetMemberAction(member);

            if (memberAction is MemberAction.DELETE)
            {
                await UnShareWith(List.Value, member);
            }
            else if (memberAction is MemberAction.LEAVE)
            {
                await Leave(List.Value);
            }
            else if (memberAction is MemberAction.ACCEPT)
            {
                ArgumentNullException.ThrowIfNull(_dbService.DB);
                await _dbService.DB.AcceptInvite(member);
            }
        };

        public Func<bool> CanChangeMemberState(Member? member) => () =>
        {
            if (member is null)
            {
                return false;
            }

            var memberAction = GetMemberAction(member);

            return memberAction switch
            {
                MemberAction.DELETE => (_permissionsMember?.CanDelete(member)).True(),
                MemberAction.LEAVE => true,
                MemberAction.ACCEPT => (_permissionsMember?.CanUpdate(member, m => m.Accepted)).True(),
                _ => false
            };
        };

        private static readonly MemberRoleSelection[] _roleSelections =
        [
            new(MemberRole.OWNER),
            new(MemberRole.ADMIN),
            new(MemberRole.USER),
            new(MemberRole.GUEST),
        ];

        public Func<MemberRoleSelection> GetInitialMemberRole(Member? member) => () =>
        {
            ArgumentNullException.ThrowIfNull(member);
            var role = GetMemberRole(member);

            MemberRoleSelection? initialSelection = null;

            foreach (var roleSelection in _roleSelections)
            {
                var displayName = GetRoleDisplayName(roleSelection.Role);
                roleSelection.RoleName = displayName;
                if (roleSelection.Role == role)
                {
                    initialSelection = roleSelection;
                }
            }

            ArgumentNullException.ThrowIfNull(initialSelection);
            return initialSelection;
        };

        public Func<MemberRoleSelection, MemberRoleSelection, Task> MemberRoleChangingAsync(Member member) => async (or, nr) =>
        {
            await ChangeAccess(member, or.Role, nr.Role);
        };

        public Func<bool> CanChangeMemberRole(Member member) => () =>
        {
            return CanUpdateRole(member);
        };

        public bool IsMemberRoleDisabled(Member member, int index)
        {
            var role = _roleSelections[index].Role;
            return !CanChangeToRole(member, role);
        }
    }
}
