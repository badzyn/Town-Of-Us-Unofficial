using System;
using HarmonyLib;
using TownOfUs.Roles;
using UnityEngine;

namespace TownOfUs.ImpostorRoles.WraithMod
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    public class PerformKill
    {
        public static Sprite Noclip => TownOfUs.NoclipSprite;

        public static bool Prefix(KillButton __instance)
        {
            var flag = PlayerControl.LocalPlayer.Is(RoleEnum.Wraith);
            if (!flag) return true;
            if (!PlayerControl.LocalPlayer.CanMove) return false;
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            var role = Role.GetRole<Wraith>(PlayerControl.LocalPlayer);
            if (__instance == role.NoclipButton)
            {
                if (role.Player.inVent) return false;
                if (!__instance.isActiveAndEnabled || __instance.isCoolingDown) return false;
                var abilityUsed = Utils.AbilityUsed(PlayerControl.LocalPlayer);
                if (!abilityUsed) return false;
                role.NoclipSafePoint = PlayerControl.LocalPlayer.transform.position;
                role.TimeRemaining = CustomGameOptions.WraithDuration;
                role.WallWalk();
                return false;
            }
            if (role.NoclipTimer() != 0) return false;
            if (!role.Noclipped) return false;

            return true;
        }
    }
}
