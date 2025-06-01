using DexieCloudNET;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListMemberService
    {
        public partial class MemberScope
        {
            public class MemberRowScope(ToDoListMemberService service, MemberScope memberScope)
                : RxBLStateScope<ToDoListMemberService>(service)
            {
                public MemberScope MemberScope => memberScope;

                private static readonly MemberRoleSelection[] _roleSelections =
                [
                    new(MemberRole.OWNER),
                    new(MemberRole.ADMIN),
                    new(MemberRole.USER),
                    new(MemberRole.GUEST),
                ];

                public IStateGroupAsync<MemberRoleSelection> MemberRoleSelection =
                    service.CreateStateGroupAsync(_roleSelections);

                public Func<MemberRoleSelection> GetInitialMemberRole(Member member) => () =>
                {
                    var role = memberScope.GetMemberRole(member);

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
                };

                public Func<MemberRoleSelection, MemberRoleSelection, Task> MemberRoleChangingAsync(Member member) =>
                    async (or, nr) => { await memberScope.ChangeAccess(member, or.Role, nr.Role); };

                public Func<bool> CanChangeMemberRole(Member member) => () => memberScope.CanUpdateRole(member);

                public Func<int, bool> MemberRoleDisabledCallback(Member member) => index =>
                {
                    var role = _roleSelections[index].Role;
                    return !memberScope.CanChangeToRole(member, role);
                };
            }
        }
    }
}