using HarmonyLib;
using TownOfUs.Roles;
using UnityEngine;
using AmongUs.GameOptions;

namespace TownOfUs.NeutralRoles.GhoulMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class PlayerControlUpdate
    {
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

            role.eatButton.gameObject.SetActive((__instance.UseButton.isActiveAndEnabled || __instance.PetButton.isActiveAndEnabled)
                    && !MeetingHud.Instance && !PlayerControl.LocalPlayer.Data.IsDead
                    && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started);
            role.eatButton.graphic.sprite = TownOfUs.EatSprite;

            var data = PlayerControl.LocalPlayer.Data;
            var isDead = data.IsDead;
            var truePosition = PlayerControl.LocalPlayer.GetTruePosition();
            var maxDistance = LegacyGameOptions.KillDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
            var flag = (GameOptionsManager.Instance.currentNormalGameOptions.GhostsDoTasks || !data.IsDead) &&
                       (!AmongUsClient.Instance || !AmongUsClient.Instance.IsGameOver) &&
                       PlayerControl.LocalPlayer.CanMove;
            var allocs = Physics2D.OverlapCircleAll(truePosition, maxDistance,
                LayerMask.GetMask(new[] { "Players", "Ghost" }));

            var killButton = role.eatButton;
            DeadBody closestBody = null;
            var closestDistance = float.MaxValue;

            foreach (var collider2D in allocs)
            {
                if (!flag || isDead || collider2D.tag != "DeadBody") continue; // Poprawiony warunek
                var component = collider2D.GetComponent<DeadBody>();
                if (!(Vector2.Distance(truePosition, component.TruePosition) <= maxDistance)) continue;

                var distance = Vector2.Distance(truePosition, component.TruePosition);
                if (!(distance < closestDistance)) continue;
                closestBody = component;
                closestDistance = distance;
            }

            KillButtonTarget.SetTarget(killButton, closestBody, role);
            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                role.eatButton.SetCoolDown(PlayerControl.LocalPlayer.killTimer, GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown); // Używa KillCooldown
            }
        }
    }
}