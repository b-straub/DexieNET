using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using DexieNET;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListMemberService
    {
        private class SetListCmd : CommandService<ToDoListMemberService, ToDoDBList>
        {
            public SetListCmd(ToDoListMemberService service) : base(service) { }

            protected override void DoExecute(ToDoDBList parameter)
            {
                Service.List = parameter;
            }

            public override bool CanExecute(ToDoDBList? parameter)
            {
                return parameter is not null && parameter != Service.List;
            }
        }

        private class InviteUserCmd : CommandServiceAsync<ToDoListMemberService, string>
        {
            public InviteUserCmd(ToDoListMemberService service) : base(service) { }

            protected override async Task DoExecute(string parameter, CancellationToken cancellationToken)
            {
                await Service.DoInviteUser(parameter);
            }

            public override bool CanExecute(string? parameter)
            {
                return Service.CanAddMember();
            }
        }

        private class ChangeMemberStateCmd : CommandServiceAsync<ToDoListMemberService, Member>
        {
            public ChangeMemberStateCmd(ToDoListMemberService service) : base(service) { }

            protected override async Task DoExecute(Member parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.List);

                var state = Service.GetMemberState(parameter);

                if (state is MemberState.DELETE)
                {
                    await Service.UnShareWith(Service.List, parameter);
                }
                else if (state is MemberState.LEAVE)
                {
                    await Service.Leave(Service.List);
                }
                else if (state is MemberState.ACCEPT)
                {
                    ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                    await Service._dbService.DB.AcceptInvite(parameter);
                }
                else if (state is MemberState.REINVITE)
                {
                    ArgumentNullException.ThrowIfNull(Service._dbService.DB);
                    ArgumentNullException.ThrowIfNull(parameter.Id);

                    await Service.UnShareWith(Service.List, parameter);
                    Service._reinvite = parameter.Name;
                }
            }

            public override bool CanExecute(Member? parameter)
            {
                if (parameter is null)
                {
                    return false;
                }

                var state = Service.GetMemberState(parameter);

                return state switch
                {
                    MemberState.DELETE => (Service._permissionsMember?.CanDelete(parameter)).True(),
                    MemberState.LEAVE => true,
                    MemberState.ACCEPT => (Service._permissionsMember?.CanUpdate(parameter, m => m.Accepted)).True(),
                    MemberState.REINVITE => (Service._permissionsMember?.CanDelete(parameter)).True() &&
                        (Service._permissionsMember?.CanUpdate(parameter, m => m.Accepted)).True(),
                    _ => false
                };
            }
        }

        public class MemberRoleSelectionIPG : InputGroupPAsync<ToDoListMemberService, MemberRoleSelection, Member>
        {
            private static readonly MemberRoleSelection[] _roleSelections =
            {
                new MemberRoleSelection(MemberRole.OWNER),
                new MemberRoleSelection(MemberRole.ADMIN), 
                new MemberRoleSelection(MemberRole.USER),
                new MemberRoleSelection(MemberRole.GUEST),
            };

            public MemberRoleSelectionIPG(ToDoListMemberService service) : base(service, _roleSelections[3])
            {

            }

            public override MemberRoleSelection[] GetItems()
            {

                return _roleSelections;
            }

            public override void Initialize()
            {
                ArgumentNullException.ThrowIfNull(Parameter);
                var role = Service.GetRole(Parameter);

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
                SetValue(initialSelection);
            }

            protected override async Task OnValueChangedAsync(MemberRoleSelection oldValue, MemberRoleSelection newValue)
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
