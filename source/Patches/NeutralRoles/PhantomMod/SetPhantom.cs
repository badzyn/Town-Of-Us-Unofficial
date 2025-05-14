using System;
using System.Collections;
using HarmonyLib;
using TownOfUs.Roles;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using TownOfUs.Patches;
using System.Linq;
using System.Collections.Generic;

namespace TownOfUs.NeutralRoles.PhantomMod
{
    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
    public static class AirshipExileController_WrapUpAndSpawn
    {
        public static void Postfix(AirshipExileController __instance) => SetPhantom.ExileControllerPostfix(__instance);
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    public class SetPhantom
    {
        public static PlayerControl WillBePhantom;
        public static bool PhantomOn;
        public static Vector2 StartPosition;

        public static void ExileControllerPostfix(ExileController __instance)
        {
            if (!PhantomOn || !AmongUsClient.Instance.AmHost) return;

            if (WillBePhantom == null)
            {
                var exiled = __instance.initData.networkedPlayer?.Object;
                var toChooseFrom = PlayerControl.AllPlayerControls.ToArray().Where(x => !x.Is(Faction.Crewmates)
                    && !x.Is(Faction.Impostors) && !x.IsLover() && !x.Data.Disconnected && x.Data.IsDead &&
                    !(x == exiled && exiled.Is(RoleEnum.Jester)) && !(x.Is(RoleEnum.Foreteller) && Role.GetRole<Foreteller>(x).WonByGuessing) &&
                    !(x.Is(RoleEnum.Pirate) && Role.GetRole<Pirate>(x).WonByDuel) && !(x.Is(RoleEnum.Executioner) && Role.GetRole<Executioner>(x).TargetVotedOut) &&
                    !(x.Is(RoleEnum.Jester) && Role.GetRole<Jester>(x).VotedOut)).ToList();
                if (toChooseFrom.Count == 0) return;
                var rand = Random.RandomRangeInt(0, toChooseFrom.Count);
                var pc = toChooseFrom[rand];
                WillBePhantom = pc;
                Utils.Rpc(CustomRPC.SetPhantom, pc.PlayerId);
                ChangeToPhantom();
            }

            if (Role.GetRole<Phantom>(WillBePhantom).Caught) return;

            List<Vent> vents = new();
            var CleanVentTasks = WillBePhantom.myTasks.ToArray().Where(x => x.TaskType == TaskTypes.VentCleaning).ToList();
            if (CleanVentTasks != null)
            {
                var ids = CleanVentTasks.Where(x => !x.IsComplete)
                                        .ToList()
                                        .ConvertAll(x => x.FindConsoles()[0].ConsoleId);

                vents = ShipStatus.Instance.AllVents.Where(x => !ids.Contains(x.Id)).ToList();
            }
            else vents = ShipStatus.Instance.AllVents.ToList();

            var startingVent = vents[Random.RandomRangeInt(0, vents.Count)];

            Utils.Rpc(CustomRPC.SetPos, WillBePhantom.PlayerId, startingVent.transform.position.x, startingVent.transform.position.y + 0.3636f);
            var pos = new Vector2(startingVent.transform.position.x, startingVent.transform.position.y + 0.3636f);
            WillBePhantom.transform.position = pos;
            WillBePhantom.NetTransform.SnapTo(pos);
        }

        public static void ChangeToPhantom()
        {
            var oldRole = Role.GetRole(WillBePhantom);
            var killsList = (oldRole.CorrectKills, oldRole.IncorrectKills, oldRole.CorrectAssassinKills, oldRole.IncorrectAssassinKills);
            Role.RoleDictionary.Remove(WillBePhantom.PlayerId);
            if (PlayerControl.LocalPlayer == WillBePhantom)
            {
                if (SubmergedCompatibility.Loaded && GameOptionsManager.Instance.currentNormalGameOptions.MapId == 6)
                {
                    HudUpdate.Zooming = false;
                    HudUpdate.ZoomStart();
                }
                var role = new Phantom(PlayerControl.LocalPlayer);
                role.formerRole = oldRole.RoleType;
                role.CorrectKills = killsList.CorrectKills;
                role.IncorrectKills = killsList.IncorrectKills;
                role.CorrectAssassinKills = killsList.CorrectAssassinKills;
                role.IncorrectAssassinKills = killsList.IncorrectAssassinKills;
                role.RegenTask();
            }
            else
            {
                var role = new Phantom(WillBePhantom);
                role.formerRole = oldRole.RoleType;
                role.CorrectKills = killsList.CorrectKills;
                role.IncorrectKills = killsList.IncorrectKills;
                role.CorrectAssassinKills = killsList.CorrectAssassinKills;
                role.IncorrectAssassinKills = killsList.IncorrectAssassinKills;
            }

            Utils.RemoveTasks(WillBePhantom);
            if (!PlayerControl.LocalPlayer.Is(RoleEnum.Haunter)) WillBePhantom.MyPhysics.ResetMoveState();

            WillBePhantom.gameObject.layer = LayerMask.NameToLayer("Players");

            WillBePhantom.gameObject.GetComponent<PassiveButton>().OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            WillBePhantom.gameObject.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => WillBePhantom.OnClick()));
            WillBePhantom.gameObject.GetComponent<BoxCollider2D>().enabled = true;
        }

        public static IEnumerator WaitForMeeting()
        {
            var startTime = DateTime.UtcNow;
            while (true)
            {
                if ((DateTime.UtcNow - startTime).TotalSeconds > 0.2f) break;
                yield return null;
            }
            if (PlayerControl.LocalPlayer == WillBePhantom) Role.GetRole(PlayerControl.LocalPlayer).RegenTask();
            WillBePhantom.gameObject.layer = LayerMask.NameToLayer("Players");
        }

        public static void Postfix(ExileController __instance) => ExileControllerPostfix(__instance);

        [HarmonyPatch(typeof(Object), nameof(Object.Destroy), new Type[] { typeof(GameObject) })]
        public static void Prefix(GameObject obj)
        {
            if (!SubmergedCompatibility.Loaded || GameOptionsManager.Instance?.currentNormalGameOptions?.MapId != 6) return;
            if (obj.name?.Contains("ExileCutscene") == true) ExileControllerPostfix(ExileControllerPatch.lastExiled);
        }
    }
}