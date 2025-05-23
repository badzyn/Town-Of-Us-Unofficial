using AmongUs.GameOptions;
using HarmonyLib;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using TownOfUs.Roles.Modifiers;
using UnityEngine;

namespace TownOfUs
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    public static class LowLights
    {
        public static bool Prefix(ShipStatus __instance, [HarmonyArgument(0)] NetworkedPlayerInfo player,
            ref float __result)
        {
            if (GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek)
            {
                if (GameOptionsManager.Instance.currentHideNSeekGameOptions.useFlashlight)
                {
                    if (player.IsImpostor()) __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentHideNSeekGameOptions.ImpostorFlashlightSize;
                    else __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentHideNSeekGameOptions.CrewmateFlashlightSize;
                }
                else
                {
                    if (player.IsImpostor()) __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentHideNSeekGameOptions.ImpostorLightMod;
                    else __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentHideNSeekGameOptions.CrewLightMod;
                }
                return false;
            }

            if (player == null || player.IsDead)
            {
                __result = __instance.MaxLightRadius;
                return false;
            }

            var visionFactor = 1f;
            foreach (var eclipsal in Role.GetRoles(RoleEnum.Eclipsal))
            {
                var eclipsalRole = (Eclipsal) eclipsal;
                if (eclipsalRole.BlindPlayers.Contains(PlayerControl.LocalPlayer) && visionFactor > eclipsalRole.visionPerc) visionFactor = eclipsalRole.visionPerc;
            }

            var switchSystem = GameOptionsManager.Instance.currentNormalGameOptions.MapId == 5 ? null : __instance.Systems[SystemTypes.Electrical]?.TryCast<SwitchSystem>();
            if (player.IsImpostor() || player._object.Is(RoleEnum.Glitch) ||
                player._object.Is(RoleEnum.Juggernaut) || player._object.Is(RoleEnum.Pestilence) || player._object.Is(RoleEnum.SoulCollector) ||
                player._object.Is(RoleEnum.Icenberg) ||
                (player._object.Is(RoleEnum.Jester) && CustomGameOptions.JesterImpVision) ||
                (player._object.Is(RoleEnum.Vampire) && CustomGameOptions.VampImpVision) ||
                (player._object.Is(RoleEnum.Arsonist) && CustomGameOptions.ArsoImpVision))


            {
                __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentNormalGameOptions.ImpostorLightMod * visionFactor;
                return false;
            }
            else if (player._object.Is(RoleEnum.Werewolf))
            {
                var role = Role.GetRole<Werewolf>(player._object);
                if (role.Rampaged)
                {
                    __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentNormalGameOptions.ImpostorLightMod * visionFactor;
                    return false;
                }
            }
            else if (player._object.Is(RoleEnum.Captain))
            {
                var role = Role.GetRole<Captain>(player._object);
                if (role.Zooming)
                {
                    if (!role.sabotageLightsZoom())
                    {
                        __result = __instance.MaxLightRadius * 5.0f;
                    }
                    else
                    {
                        if (player._object.Is(ModifierEnum.Torch))
                            __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, 1) * GameOptionsManager.Instance.currentNormalGameOptions.CrewLightMod;
                        else
                            __result = 0.1f;
                        role.UnZoomAbility();
                    }
                    return false;
                }
            }

            if (Patches.SubmergedCompatibility.isSubmerged())
            {
                if (player._object.Is(ModifierEnum.Torch)) __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, 1) * GameOptionsManager.Instance.currentNormalGameOptions.CrewLightMod * visionFactor;
                else __result *= visionFactor;
                return false;
            }

            var t = switchSystem != null ? switchSystem.Value / 255f : 1;

            if (player._object.Is(ModifierEnum.Torch)) t = 1;

            __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, t) *
                       GameOptionsManager.Instance.currentNormalGameOptions.CrewLightMod * visionFactor;
            return false;
        }
    }
}