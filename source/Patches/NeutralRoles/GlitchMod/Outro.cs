using System.Linq;
using HarmonyLib;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using UnityEngine;

namespace TownOfUs.NeutralRoles.GlitchMod
{
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
    public class Outro
    {
        public static void Postfix(EndGameManager __instance)
        {
            if (Role.GetRoles(RoleEnum.Jester).Any(x => ((Jester)x).VotedOut) && CustomGameOptions.JesterWin == ExecutionerMod.WinEndsGame.EndsGame) return;
            if (Role.GetRoles(RoleEnum.Executioner).Any(x => ((Executioner)x).TargetVotedOut) && CustomGameOptions.ExecutionerWin == ExecutionerMod.WinEndsGame.EndsGame) return;
            if (Role.GetRoles(RoleEnum.Foreteller).Any(x => ((Foreteller)x).WonByGuessing) && CustomGameOptions.ForetellerWinEndsGame) return;
            if (Role.GetRoles(RoleEnum.Pirate).Any(x => ((Pirate)x).WonByDuel) && CustomGameOptions.PirateWinEndsGame) return;
            var role = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Glitch && ((Glitch) x).GlitchWins);
            if (role == null) return;
            PoolablePlayer[] array = Object.FindObjectsOfType<PoolablePlayer>();
            foreach (var player in array) player.NameText().text = role.ColorString + player.NameText().text + "</color>";
            __instance.BackgroundBar.material.color = role.Color;
            var text = Object.Instantiate(__instance.WinText);
            text.text = "Glitch Wins!";
            text.color = role.Color;
            var pos = __instance.WinText.transform.localPosition;
            pos.y = 1.5f;
            text.transform.position = pos;
            text.text = $"<size=4>{text.text}</size>";
        }
    }
}