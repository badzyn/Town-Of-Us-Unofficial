namespace TownOfUs.Roles
{
    public class Spy : Role
    {
        KillButton _adminButton;
        public Spy(PlayerControl player) : base(player)
        {
            Name = "Spy";
            ImpostorText = () => "Snoop Around And Find Stuff Out";
            TaskText = () => "Gain extra information on the Admin Table";
            Color = Patches.Colors.Spy;
            RoleType = RoleEnum.Spy;
            AddToRoleHistory(RoleType);
            Alignment = Alignment.CrewmateInvestigative;
        }
        public KillButton AdminButton
        {
            get => _adminButton;
            set
            {
                _adminButton = value;
                ExtraButtons.Clear();
                ExtraButtons.Add(value);
            }
        }
    }
}