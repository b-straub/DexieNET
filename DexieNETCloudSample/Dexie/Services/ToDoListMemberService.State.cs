using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Data.Common;
using System.Reflection.Metadata;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListMemberService
    {

        public Func<bool> CanSetList(ToDoDBList? list) => () =>
        {
            return list is not null && list != List;
        };

        public Action SetList(ToDoDBList list) => () =>
        {
            List = list;
        };

        private class InviteUserST(ToDoListMemberService service) : StateTransformerAsync<ToDoListMemberService, string>(service)
        {
            protected override async Task TransformStateAsync(string value, CancellationToken cancellationToken)
            {
                await Service.DoInviteUser(value);
            }

            public override bool CanTransform(string? value)
            {
                return Service.CanAddMember();
            }

           
        }

        private class ChangeMemberStateST(ToDoListMemberService service) : StateTransformerAsync<ToDoListMemberService, Member>(service)
        {
            protected override async Task TransformStateAsync(Member value, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.List);

                var memberAction = Service.GetMemberAction(value);

                if (memberAction is MemberAction.DELETE)
                {
                    await Service.UnShareWith(Service.List, value);
                }
                else if (memberAction is MemberAction.LEAVE)
                {
                    await Service.Leave(Service.List);
                }
                else if (memberAction is MemberAction.ACCEPT)
                {
                    ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                    await Service._dbService.DB.AcceptInvite(value);
                }
            }

            public override bool CanTransform(Member? value)
            {
                if (value is null)
                {
                    return false;
                }

                var memberAction = Service.GetMemberAction(value);

                return memberAction switch
                {
                    MemberAction.DELETE => (Service._permissionsMember?.CanDelete(value)).True(),
                    MemberAction.LEAVE => true,
                    MemberAction.ACCEPT => (Service._permissionsMember?.CanUpdate(value, m => m.Accepted)).True(),
                    _ => false
                };
            }
        }

     
        private static readonly MemberRoleSelection[] _roleSelections =
        [
            new(MemberRole.OWNER),
            new(MemberRole.ADMIN),
            new(MemberRole.USER),
            new(MemberRole.GUEST),
        ];

        private MemberRoleSelection GetInitialMemberRole()
        {
            ArgumentNullException.ThrowIfNull(Parameter);
            var role = GetMemberRole(Parameter);

            MemberRoleSelection? initialSelection = null;

            foreach (var roleSelection in _roleSelections)
            {
                var displayName = Service.GetRoleDisplayName(roleSelection.Role);
                roleSelection.RoleName = displayName;
                if (roleSelection.Role == role)
                {
                    initialSelection = roleSelection;
                }
            }

            ArgumentNullException.ThrowIfNull(initialSelection);
            return initialSelection;
        }

            protected override async Task OnValueChangingAsync(MemberRoleSelection oldValue, MemberRoleSelection newValue)
            {
                ArgumentNullException.ThrowIfNull(Parameter);
                await Service.ChangeAccess((Parameter, oldValue.Role, newValue.Role));
            }

            public override bool CanChange()
            {
                return Parameter is not null && Service.CanUpdateRole(Parameter);
            }

            public override bool IsItemDisabled(int index)
            {
                var role = _roleSelections[index].Role;
                return !Service.CanChangeToRole(Parameter, role);
            }
        }
    }
}
