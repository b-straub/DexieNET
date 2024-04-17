using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Reflection.Metadata;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListMemberService
    {
        public Action SetList(ToDoDBList list) => () =>
        {
            List = list;
        };

        public Func<bool> CanSetList(ToDoDBList? list) => () =>
        {
            return list is not null && list != List;
        };

        public Func<IStateCommandAsync, Task> AddMember(string user) => async _ =>
        {
            await DoAddMember(user);
        };

        public Func<bool> CanAddMember() => () =>
        {
            return DoCanAddMember();

        };

        public Func<IStateCommandAsync, Task> ChangeMemberState(Member member) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(List);

            var memberAction = GetMemberAction(member);

            if (memberAction is MemberAction.DELETE)
            {
                await UnShareWith(List, member);
            }
            else if (memberAction is MemberAction.LEAVE)
            {
                await Leave(List);
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

        public MemberRoleSelection GetInitialMemberRole(Member? member)
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
        }

        public async Task MemberRoleChangingAsync(Member member, MemberRoleSelection oldValue, MemberRoleSelection newValue)
        {
            await ChangeAccess((member, oldValue.Role, newValue.Role));
        }

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
