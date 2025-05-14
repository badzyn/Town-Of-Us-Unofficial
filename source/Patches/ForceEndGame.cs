using System.Linq;
using HarmonyLib;
using TownOfUs.NeutralRoles.ExecutionerMod;
using TownOfUs.Roles;
using UnityEngine;

namespace TownOfUs.Patches
{
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
    public class ForceGameEndOutro
    {
        public static void Postfix(EndGameManager __instance)
        {
            if (Role.GetRoles(RoleEnum.Jester).Any(x => ((Jester)x).VotedOut) && CustomGameOptions.JesterWin == WinEndsGame.EndsGame) return;
            if (Role.GetRoles(RoleEnum.Executioner).Any(x => ((Executioner)x).TargetVotedOut) && CustomGameOptions.ExecutionerWin == WinEndsGame.EndsGame) return;
            if (Role.GetRoles(RoleEnum.Foreteller).Any(x => ((Foreteller)x).WonByGuessing) && CustomGameOptions.ForetellerWinEndsGame) return;
            if (Role.GetRoles(RoleEnum.Pirate).Any(x => ((Pirate)x).WonByDuel) && CustomGameOptions.PirateWinEndsGame) return;
            if (!Role.ForceGameEnd) return;
            var text = Object.Instantiate(__instance.WinText);
            text.text = "The Host Ended The Game";
            text.color = Color.white;
            __instance.BackgroundBar.material.color = Color.white;
            var pos = __instance.WinText.transform.localPosition;
            pos.y = 1.5f;
            text.transform.position = pos;
            text.text = $"<size=4>{text.text}</size>";
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class ForceGameEnd
    {
        public static void Postfix()
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.G) && Input.GetKey(KeyCode.Return))
            {
                Role.ForceGameEnd = true;
                Utils.Rpc(CustomRPC.ForceEndGame);
                System.Console.WriteLine("Host Forced End Game");
                Utils.EndGame();
            }
        }
    }
}