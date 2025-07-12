using System;
using HarmonyLib;
using TownOfUs.Patches;
using TownOfUs.Roles;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfUs
{
    #region OpenDoorConsole
    [HarmonyPatch(typeof(OpenDoorConsole), nameof(OpenDoorConsole.CanUse))]
    public class OpenDoorConsoleCanUse
    {
        public static void Prefix(OpenDoorConsole __instance,
            [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo,
            ref bool __state)
        {
            __state = false;

            var playerControl = playerInfo.Object;
            if (playerControl.IsGhostRole() && !GhostRole.GetGhostRole(playerControl).Caught && playerInfo.IsDead)
            {
                playerInfo.IsDead = false;
                __state = true;
            }
        }

        public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref bool __state)
        {
            if (__state)
                playerInfo.IsDead = true;
        }
    }

    [HarmonyPatch(typeof(OpenDoorConsole), nameof(OpenDoorConsole.Use))]
    public class OpenDoorConsoleUse
    {
        public static bool Prefix(OpenDoorConsole __instance)
        {
            __instance.CanUse(PlayerControl.LocalPlayer.Data, out var canUse, out _);
            if (!canUse) return false;
            __instance.myDoor.SetDoorway(true);
            return false;
        }
    }
    #endregion

    #region DoorConsole
    [HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.CanUse))]
    public class DoorConsoleCanUse
    {
        public static void Prefix(DoorConsole __instance,
            [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo,
            ref bool __state)
        {
            __state = false;

            var playerControl = playerInfo.Object;
            if (playerControl.IsGhostRole() && !GhostRole.GetGhostRole(playerControl).Caught && playerInfo.IsDead)
            {
                playerInfo.IsDead = false;
                __state = true;
            }
        }

        public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref bool __state,
            [HarmonyArgument(1)] ref bool canUse, [HarmonyArgument(2)] ref bool couldUse)
        {
            if (__state)
                playerInfo.IsDead = true;
        }
    }

    [HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.Use))]
    public static class DoorConsoleUsePatch
    {
        public static bool Prefix(DoorConsole __instance)
        {
            __instance.CanUse(PlayerControl.LocalPlayer.Data, out var canUse, out _);
            if (!canUse) return false;
            PlayerControl.LocalPlayer.NetTransform.Halt();
            var minigame = Object.Instantiate(__instance.MinigamePrefab, Camera.main.transform);
            minigame.transform.localPosition = new Vector3(0f, 0f, -50f);

            try
            {
                minigame.Cast<IDoorMinigame>().SetDoor(__instance.MyDoor);
            }
            catch (InvalidCastException) { /* ignored */ }

            minigame.Begin(null);
            return false;
        }
    }
    #endregion

    #region Ladder
    [HarmonyPatch(typeof(Ladder), nameof(Ladder.CanUse))]
    public class LadderCanUse
    {
        public static void Prefix(Ladder __instance,
            [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo,
            ref bool __state)
        {
            __state = false;
            var playerControl = playerInfo.Object;
            if (playerControl.IsGhostRole() && !GhostRole.GetGhostRole(playerControl).Caught && playerInfo.IsDead)
            {
                playerInfo.IsDead = false;
                __state = true;
            }
        }

        public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref bool __state)
        {
            if (__state)
                playerInfo.IsDead = true;
        }
    }

    [HarmonyPatch(typeof(Ladder), nameof(Ladder.Use))]
    public class LadderUse
    {
        public static bool Prefix(Ladder __instance)
        {
            var data = PlayerControl.LocalPlayer.Data;
            __instance.CanUse(data, out var flag, out var _);
            if (flag) PlayerControl.LocalPlayer.MyPhysics.RpcClimbLadder(__instance);
            return false;
        }
    }
    #endregion

    #region PlatformConsole
    [HarmonyPatch(typeof(PlatformConsole), nameof(PlatformConsole.CanUse))]
    public class PlatformConsoleCanUse
    {
        public static void Prefix(
            PlatformConsole __instance,
            [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo,
            ref bool __state)
        {
            __state = false;
            var playerControl = playerInfo.Object;
            if (playerControl.IsGhostRole() && !GhostRole.GetGhostRole(playerControl).Caught && playerInfo.IsDead)
            {
                playerInfo.IsDead = false;
                __state = true;
            }
        }

        public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref bool __state)
        {
            if (__state)
                playerInfo.IsDead = true;
        }
    }

    [HarmonyPatch(typeof(PlatformConsole), nameof(PlatformConsole.Use))]
    public class PlatformConsoleUse
    {
        public static bool Prefix(PlatformConsole __instance)
        {
            var data = PlayerControl.LocalPlayer.Data;
            __instance.CanUse(data, out var flag, out var _);
            if (flag) __instance.Platform.Use();
            return false;
        }
    }

    [HarmonyPatch(typeof(MovingPlatformBehaviour), nameof(MovingPlatformBehaviour.Use), typeof(PlayerControl))]
    public class PMovingPlatformBehaviourUse
    {
        public static bool Prefix(MovingPlatformBehaviour __instance, [HarmonyArgument(0)] PlayerControl player)
        {
            if (player.Data.Disconnected) return true;
            if (player.IsGhostRole() && !GhostRole.GetGhostRole(player).Caught && player.Data.IsDead)
            {
                __instance.IsDirty = true;
                __instance.StartCoroutine(__instance.UsePlatform(player));
                return false;
            }
            return true;
        }
    }
    #endregion

    #region ZiplineConsole
    [HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.CanUse))]
    public class ZiplineConsoleCanUse
    {
        public static void Prefix(
            ZiplineConsole __instance,
            [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo,
            ref bool __state)
        {
            __state = false;
            var playerControl = playerInfo.Object;
            if (playerControl.IsGhostRole() && !GhostRole.GetGhostRole(playerControl).Caught && playerInfo.IsDead)
            {
                playerInfo.IsDead = false;
                __state = true;
            }
        }

        public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref bool __state)
        {
            if (__state)
                playerInfo.IsDead = true;
        }
    }

    [HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.Use))]
    public class ZiplineConsoleUse
    {
        public static bool Prefix(ZiplineConsole __instance)
        {
            if (__instance.IsCoolingDown()) return false;
            var data = PlayerControl.LocalPlayer.Data;
            __instance.CanUse(data, out var flag, out var _);
            if (flag)
            {
                __instance.zipline.Use(__instance.atTop, __instance);
                __instance.CoolDown = __instance.MaxCoolDown;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
    public class ZiplineConsoleCheck
    {
        public static bool Prefix([HarmonyArgument(0)] PlayerControl target,
        [HarmonyArgument(1)] ZiplineBehaviour ziplineBehaviour, [HarmonyArgument(2)] bool fromTop)
        {
            if (target != null && !MeetingHud.Instance && target.Data != null && !target.inMovingPlat)
            {
                if (target.IsGhostRole() && !GhostRole.GetGhostRole(target).Caught && target.Data.IsDead)
                {
                    PlayerControl.LocalPlayer.RpcUseZipline(target, ziplineBehaviour, fromTop);
                    return false;
                }
                return true;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ZiplineBehaviour), nameof(ZiplineBehaviour.Use), typeof(PlayerControl), typeof(bool))]
    public class ZiplineBehaviourUse
    {
        public static bool Prefix(ZiplineBehaviour __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] bool fromTop)
        {
            if (player.Data.Disconnected) return true;
            if (player.IsGhostRole() && !GhostRole.GetGhostRole(player).Caught && player.Data.IsDead)
            {
                Transform start;
                Transform end;
                Transform landing;
                if (fromTop)
                {
                    start = __instance.handleTop;
                    end = __instance.handleBottom;
                    landing = __instance.landingPositionBottom;
                }
                else
                {
                    start = __instance.handleBottom;
                    end = __instance.handleTop;
                    landing = __instance.landingPositionTop;
                }
                __instance.StopAllCoroutinesForPlayer(player);
                __instance.playerIdUseZiplineCoroutines[player.PlayerId] = __instance.StartCoroutine(__instance.CoUseZipline(player, start, end, landing, fromTop));
                return false;
            }
            return true;
        }
    }
    #endregion

    #region DeconControl
    [HarmonyPatch(typeof(DeconControl), nameof(DeconControl.CanUse))]
    public class DeconControlCanUse
    {
        [HarmonyBefore(LevelImpostorCompatibility.LiGuid)]
        public static void Prefix(DeconControl __instance,
            [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo,
            ref bool __state)
        {
            __state = false;

            var playerControl = playerInfo.Object;
            if (playerControl.IsGhostRole() && !GhostRole.GetGhostRole(playerControl).Caught && playerInfo.IsDead)
            {
                playerInfo.IsDead = false;
                __state = true;
            }
        }

        [HarmonyAfter(LevelImpostorCompatibility.LiGuid)]
        public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref bool __state)
        {
            if (__state)
                playerInfo.IsDead = true;
        }
    }

    [HarmonyPatch(typeof(DeconControl), nameof(DeconControl.Use))]
    public class DeconControlUse
    {
        public static bool Prefix(DeconControl __instance)
        {
            var data = PlayerControl.LocalPlayer.Data;
            __instance.CanUse(data, out var flag, out var _);
            if (flag)
            {
                __instance.cooldown = 6f;
                if (Constants.ShouldPlaySfx())
                {
                    SoundManager.Instance.PlaySound(__instance.UseSound, false, 1f, null);
                }
                __instance.OnUse.Invoke();
            }
            return false;
        }
    }
    #endregion

    #region global::Console
    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    public class ConsoleCanUsePatch
    {
        public static void Prefix(Console __instance,
            [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo,
            ref bool __state)
        {
            __state = false;

            var playerControl = playerInfo.Object;
            if (playerControl.IsGhostRole() && !GhostRole.GetGhostRole(playerControl).Caught && playerInfo.IsDead)
            {
                playerInfo.IsDead = false;
                __state = true;
            }
        }

        public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref bool __state)
        {
            if (__state)
                playerInfo.IsDead = true;
        }
    }

    [HarmonyPatch(typeof(Console), nameof(Console.Use))]
    public class ConsoleUsePatch
    {
        public static bool Prefix(Console __instance)
        {
            __instance.CanUse(PlayerControl.LocalPlayer.Data, out var canUse, out var couldUse);
            if (canUse)
            {
                PlayerTask playerTask = __instance.FindTask(PlayerControl.LocalPlayer);
                if (playerTask.MinigamePrefab)
                {
                    var minigame = Object.Instantiate(playerTask.GetMinigamePrefab());
                    minigame.transform.SetParent(Camera.main.transform, false);
                    minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
                    minigame.Console = __instance;
                    minigame.Begin(playerTask);
                }
            }

            return false;
        }
    }
    #endregion

    #region global::PetPos
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetPetPosition))]
    public class PetPos
    {
        public static bool Prefix(PlayerControl __instance)
        {
            if (__instance.IsGhostRole() && !GhostRole.GetGhostRole(__instance).Caught) return false;
            return true;
        }
    }
    #endregion
}