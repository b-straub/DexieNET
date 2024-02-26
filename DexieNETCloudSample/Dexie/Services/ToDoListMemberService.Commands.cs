using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Reflection.Metadata;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListMemberService
    {
        private class SetListST(ToDoListMemberService service) : StateTransformer<ToDoListMemberService, ToDoDBList>(service)
        {
            public override bool CanTransform(ToDoDBList? value)
            {
                return value is not null && value != Service.List;
            }

            protected override void TransformState(ToDoDBList value)
            {
                Service.List = value;
            }
        }

        private class InviteUserCmd(ToDoListMemberService service) : CommandServiceAsync<ToDoListMemberService, string>(service)
        {
            protected override async Task DoExecute(string parameter, CancellationToken cancellationToken)
            {
                await Service.DoInviteUser(parameter);
            }

            public override bool CanExecute(string? parameter)
            {
                return Service.CanAddMember();
            }
        }

        private class ChangeMemberStateCmd(ToDoListMemberService service) : CommandServiceAsync<ToDoListMemberService, Member>(service)
        {
            protected override async Task DoExecute(Member parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.List);

                var memberAction = Service.GetMemberAction(parameter);

                if (memberAction is MemberAction.DELETE)
                {
                    await Service.UnShareWith(Service.List, parameter);
                }
                else if (memberAction is MemberAction.LEAVE)
                {
                    await Service.Leave(Service.List);
                }
                else if (memberAction is MemberAction.ACCEPT)
                {
                    ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                    await Service._dbService.DB.AcceptInvite(parameter);
                }
            }

            public override bool CanExecute(Member? parameter)
            {
                if (parameter is null)
                {
                    return false;
                }

                var memberAction = Service.GetMemberAction(parameter);

                return memberAction switch
                {
                    MemberAction.DELETE => (Service._permissionsMember?.CanDelete(parameter)).True(),
                    MemberAction.LEAVE => true,
                    MemberAction.ACCEPT => (Service._permissionsMember?.CanUpdate(parameter, m => m.Accepted)).True(),
                    _ => false
                };
            }
        }

        public class MemberRoleSelectionIPG(ToDoListMemberService service) : InputGroupPAsync<ToDoListMemberService, MemberRoleSelection, Member>(service, _roleSelections[3])
        {
            private static readonly MemberRoleSelection[] _roleSelections =
            [
                new(MemberRole.OWNER),
                new(MemberRole.ADMIN),
                new(MemberRole.USER),
                new(MemberRole.GUEST),
            ];

            public override MemberRoleSelection[] GetItems()
            {

                return _roleSelections;
            }

            public override void InitializeContext()
            {
                ArgumentNullException.ThrowIfNull(Parameter);
                var role = Service.GetMemberRole(Parameter);

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
                SetInitialValue(initialSelection);
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
