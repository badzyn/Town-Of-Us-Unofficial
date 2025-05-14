using HarmonyLib;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using System.Linq;
using TownOfUs.Roles;
using TownOfUs.Roles.Modifiers;
using TownOfUs.CrewmateRoles.AltruistMod;
using UnityEngine;

namespace TownOfUs.NeutralRoles.PirateMod
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    public static class VotingComplete
    {
        public static void Postfix(MeetingHud __instance)
        {
            var pirate = (Pirate)Role.GetRoles(RoleEnum.Pirate).FirstOrDefault();
            if (pirate != null && pirate.DueledPlayer != null)
            {
                var dueled = Role.GetRole(pirate.DueledPlayer);
                pirate.DefenseButton.Destroy();
                dueled.DefenseButton.Destroy();
                if (pirate.Defense == dueled.Defense && !pirate.Player.Data.IsDead && !pirate.Player.Data.Disconnected && !dueled.Player.Data.IsDead && !dueled.Player.Data.Disconnected)
                {
                    if (PlayerControl.LocalPlayer == pirate.DueledPlayer)
                    {
                        Coroutines.Start(Utils.FlashCoroutine(Color.red));
                        NotificationPatch.Notification("You Lost The Duel!", 1000 * CustomGameOptions.NotificationDuration);
                    }
                    var voteArea = MeetingHud.Instance.playerStates.First(x => x.TargetPlayerId == pirate.DueledPlayer.PlayerId);
                    if (!pirate.DueledPlayer.Is(RoleEnum.Pestilence))
                    {
                        var hudManager = HudManager.Instance;
                        pirate.DueledPlayer.Exiled();
                        voteArea.AmDead = true;
                        voteArea.Overlay.gameObject.SetActive(true);
                        voteArea.Overlay.color = Color.white;
                        voteArea.XMark.gameObject.SetActive(true);
                        voteArea.XMark.transform.localScale = Vector3.one;
                        SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.8f);
                        if (pirate.DueledPlayer.Is(ModifierEnum.Lover) && CustomGameOptions.BothLoversDie)
                        {
                            var lover = Modifier.GetModifier<Lover>(pirate.DueledPlayer).OtherLover.Player;
                            lover.Exiled();
                            voteArea = MeetingHud.Instance.playerStates.First(x => x.TargetPlayerId == lover.PlayerId);
                            voteArea.AmDead = true;
                            voteArea.Overlay.gameObject.SetActive(true);
                            voteArea.Overlay.color = Color.white;
                            voteArea.XMark.gameObject.SetActive(true);
                            voteArea.XMark.transform.localScale = Vector3.one;
                        }
                    }
                    pirate.DueledPlayer = null;
                    pirate.DuelsWon += 1;
                    if (pirate.Player == PlayerControl.LocalPlayer)
                    {
                        Coroutines.Start(Utils.FlashCoroutine(Color.green));
                        NotificationPatch.Notification("Ya Won Th' Duel!", 1000 * CustomGameOptions.NotificationDuration);
                    }
                        if (pirate.DuelsWon >= CustomGameOptions.PirateDuelsToWin)
                        {
                            pirate.WonByDuel = true;
                            if (!CustomGameOptions.PirateWinEndsGame)
                            {
                                pirate.Player.Exiled();
                            }
                        }
                }
                else if (!pirate.Player.Data.IsDead && !pirate.Player.Data.Disconnected)
                {
                    if (pirate.Player == PlayerControl.LocalPlayer)
                    {
                        Coroutines.Start(Utils.FlashCoroutine(Color.red));
                        NotificationPatch.Notification("Ya Lost Th' Duel!", 1000 * CustomGameOptions.NotificationDuration);
                    }
                    if (pirate.DueledPlayer == PlayerControl.LocalPlayer)
                    {
                        Coroutines.Start(Utils.FlashCoroutine(Color.green));
                        NotificationPatch.Notification("You Won The Duel!", 1000 * CustomGameOptions.NotificationDuration);
                    }
                    pirate.DueledPlayer = null;
                }
            }
        }
    }
}