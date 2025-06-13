using HarmonyLib;
using TownOfUs.Roles;

namespace TownOfUs.NeutralRoles.ForetellerMod
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))] // BBFDNCCEJHI
    public static class VotingComplete
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Foreteller))
            {
                var fore = Role.GetRole<Foreteller>(PlayerControl.LocalPlayer);
                ShowHideButtonsFore.HideButtonsFore(fore);
                ShowHideButtonsFore.HideTextFore(fore);
            }
        }
    }
}