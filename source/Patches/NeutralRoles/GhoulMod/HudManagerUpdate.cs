using HarmonyLib;
using UnityEngine;
using System.Linq;
using TownOfUs.Roles;

namespace TownOfUs.NeutralRoles.GhoulMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdate
    {
        public static Sprite EatSprite => TownOfUs.EatSprite;

        public static void Postfix(HudManager __instance)
        {
            if (PlayerControl.AllPlayerControls.Count <= 1) return;
            if (PlayerControl.LocalPlayer == null) return;
            if (PlayerControl.LocalPlayer.Data == null) return;
            if (!PlayerControl.LocalPlayer.Is(RoleEnum.Ghoul)) return;
            var role = Role.GetRole<Ghoul>(PlayerControl.LocalPlayer);

            if (role.eatButton == null)
            {
                role.eatButton = Object.Instantiate(__instance.KillButton, __instance.KillButton.transform.parent);
                role.eatButton.graphic.enabled = true;
                role.eatButton.gameObject.SetActive(false);
                PlayerControl.LocalPlayer.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown); // Startowy cooldown
            }

            role.eatButton.graphic.sprite = EatSprite;
            role.eatButton.transform.localPosition = new Vector3(-2f, 0f, 0f);

            role.eatButton.gameObject.SetActive((__instance.UseButton.isActiveAndEnabled || __instance.PetButton.isActiveAndEnabled)
                && !MeetingHud.Instance && !PlayerControl.LocalPlayer.Data.IsDead
                && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started);
            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                role.eatButton.SetCoolDown(PlayerControl.LocalPlayer.killTimer, GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown); // Używa KillCooldown
            }
        }
    }
}