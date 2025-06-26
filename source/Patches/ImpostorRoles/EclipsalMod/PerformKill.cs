using HarmonyLib;
using Hazel;
using System;
using TownOfUs.Roles;

namespace TownOfUs.ImpostorRoles.EclipsalMod
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    public class PerformKill
    {
        public static bool Prefix(KillButton __instance)
        {
            var flag = PlayerControl.LocalPlayer.Is(RoleEnum.Eclipsal);
            if (!flag) return true;
            if (!PlayerControl.LocalPlayer.CanMove) return false;
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            var role = Role.GetRole<Eclipsal>(PlayerControl.LocalPlayer);
            if (__instance == role.BlindButton)
            {
                if (__instance.isCoolingDown) return false;
                if (!__instance.isActiveAndEnabled) return false;
                if (role.Player.inVent) return false;
                if (role.BlindTimer() != 0) return false;
                var abilityUsed = Utils.AbilityUsed(PlayerControl.LocalPlayer);
                if (!abilityUsed) return false;

                role.TimeRemaining = CustomGameOptions.BlindDuration;
                role.GetBlinds();

                var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.Blind, SendOption.Reliable, -1);
                writer.Write((byte)role.Player.PlayerId);
                writer.Write((byte)role.BlindPlayers.Count);
                foreach (var player in role.BlindPlayers)
                {
                    writer.Write(player.PlayerId);
                }
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                return false;
            }

            return true;
        }
    }
}