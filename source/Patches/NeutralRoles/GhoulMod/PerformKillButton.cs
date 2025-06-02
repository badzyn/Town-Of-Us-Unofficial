using HarmonyLib;
using Reactor.Utilities;
using TownOfUs.Roles;
using UnityEngine;
using AmongUs.GameOptions;
using System;

namespace TownOfUs.NeutralRoles.GhoulMod
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    public class PerformKillButton
    {
        public static bool Prefix(KillButton __instance)
        {
            var flag = PlayerControl.LocalPlayer.Is(RoleEnum.Ghoul);
            if (!flag) return true;
            if (!PlayerControl.LocalPlayer.CanMove) return false;
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            var role = Role.GetRole<Ghoul>(PlayerControl.LocalPlayer);

            if (__instance == role.eatButton)
            {
                var flag2 = __instance.isCoolingDown;
                if (flag2) return false;
                if (!__instance.enabled) return false;
                if (role.Player.inVent) return false;
                var maxDistance = LegacyGameOptions.KillDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
                if (Vector2.Distance(role.CurrentTarget.TruePosition,
                    PlayerControl.LocalPlayer.GetTruePosition()) > maxDistance) return false;
                var playerId = role.CurrentTarget.ParentId;
                var player = Utils.PlayerById(playerId);
                var abilityUsed = Utils.AbilityUsed(PlayerControl.LocalPlayer);
                if (!abilityUsed) return false;
                if (player.IsInfected() || role.Player.IsInfected())
                {
                    foreach (var pb in Role.GetRoles(RoleEnum.Plaguebearer)) ((Plaguebearer)pb).RpcSpreadInfection(player, role.Player);
                }

                Utils.Rpc(CustomRPC.GhoulEat, PlayerControl.LocalPlayer.PlayerId, playerId);
                role.LastEaten = DateTime.UtcNow; // Aktualizuje czas ostatniego zjedzenia
                role.Player.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown); // Resetuje cooldown
                Coroutines.Start(Coroutine.EatCoroutine(role.CurrentTarget, role));
                return false;
            }
            return true;
        }
    }
}