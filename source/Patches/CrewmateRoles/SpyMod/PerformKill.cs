using TownOfUs.Roles;
using HarmonyLib;

namespace TownOfUs.CrewmateRoles.SpyMod 
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    public class PerformKill 
    {
        public static bool Prefix(KillButton __instance) 
        {
            if (__instance != HudManager.Instance.KillButton) return true;
            var flag = PlayerControl.LocalPlayer.Is(RoleEnum.Spy);
            if (!flag) return true;
            var role = Role.GetRole<Spy>(PlayerControl.LocalPlayer);
            if (!__instance.enabled) return false;

            MapOptions mapOptions = GameManager.Instance.GetMapOptions();
            mapOptions.IncludeDeadBodies = true;
            mapOptions.AllowMovementWhileMapOpen = true;
            mapOptions.Mode = (MapOptions.Modes) 2;
            HudManager.Instance.ToggleMapVisible(mapOptions);

            return false;
        }
    }
}