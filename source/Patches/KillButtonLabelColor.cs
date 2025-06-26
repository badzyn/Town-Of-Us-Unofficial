using HarmonyLib;
using TownOfUs.Roles;

namespace KillButtonLabelColor
{
    public class ColorPatch
    {
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public class KillButtonLabelColor
        {
            public static void Postfix(HudManager __instance)
            {
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
                var role = Role.GetRole(PlayerControl.LocalPlayer);
                if (role == null) return;
                if (!__instance.KillButton.buttonLabelText.isActiveAndEnabled) return;
                __instance.KillButton.buttonLabelText.SetOutlineColor(role.Color);
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public class VentButtonLabelColor
        {
            public static void Postfix(HudManager __instance)
            {
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
                var role = Role.GetRole(PlayerControl.LocalPlayer);
                if (role == null) return;
                if (!__instance.ImpostorVentButton.buttonLabelText.isActiveAndEnabled) return;
                __instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(role.Color);
            }
        }
    }
}