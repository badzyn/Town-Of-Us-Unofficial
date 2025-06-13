using HarmonyLib;
using TownOfUs.NeutralRoles.ExecutionerMod;
using TownOfUs.NeutralRoles.GuardianAngelMod;
using TownOfUs.Roles;
using TownOfUs.Roles.Modifiers;
using Reactor.Utilities.Extensions;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using TownOfUs.Extensions;
using System.Diagnostics;

namespace TownOfUs.Patches
{
    [HarmonyPatch]
    public static class ChatCommands
    {
        public static bool JailorMessage = false;

        public static bool system = false;
        public static bool error = false;
        public static bool noaccess = false;

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        public static class PrivateJaileeChat
        {
            public static bool Prefix(ChatController __instance, [HarmonyArgument(0)] ref PlayerControl sourcePlayer, ref string chatText)
            {
                if (sourcePlayer == PlayerControl.LocalPlayer)
                {
                    if (chatText.ToLower().StartsWith("/note") || chatText.ToLower().StartsWith("/ note"))
                    {
                        string note = chatText.Substring(5).Trim();
                        var sourcePlayerRole = Role.GetRole(sourcePlayer);
                        sourcePlayerRole.PlayerNotes += "\n" + note;
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"<b>New Note:</b>\n{note}");
                        return false;
                    }
                    if (chatText.ToLower().StartsWith("/seenote") || chatText.ToLower().StartsWith("/ seenote"))
                    {
                        var sourcePlayerRole = Role.GetRole(sourcePlayer);
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"<b>Notes:</b>\n {sourcePlayerRole.PlayerNotes}");
                        return false;
                    }
                    if (chatText.ToLower().StartsWith("/id"))
                    {
                        if (GameData.Instance.GetHost() == sourcePlayer.Data && sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            string message = "All players ids:\n";
                            foreach (var player in PlayerControl.AllPlayerControls.ToArray().OrderBy(x => x.PlayerId))
                            {
                                message += $"{player.GetDefaultOutfit().PlayerName}: {player.PlayerId}\n";
                            }
                            message.Remove(message.Length - 1, 1);
                            chatText = message;
                            system = true;
                        }
                        else if (sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            chatText = "You don't have access to this command!";
                            noaccess = true;
                        }
                        return sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId;
                    }

                    if (chatText.ToLower().StartsWith("/kick "))
                    {
                        if (GameData.Instance.GetHost() == sourcePlayer.Data && sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            if (chatText[6..].IsNullOrWhiteSpace())
                            {
                                chatText = "You must specify a player id as parameter, use /id to get the list of all players ids.";
                                error = true;
                            }
                            if (Convert.ToByte(chatText[6..]) == PlayerControl.LocalPlayer.PlayerId)
                            {
                                chatText = "You can't kick yourself!";
                                error = true;
                            }
                            else
                            {
                                var id = Convert.ToByte(chatText[6..]);
                                var playerWithId = PlayerControl.AllPlayerControls.ToArray().Where(x => x.PlayerId == id).ToList();
                                if (!playerWithId.Any())
                                {
                                    chatText = "You must give a valid player id, use /id to get the list of all players ids.";
                                    error = true;
                                }
                                else
                                {
                                    foreach (var player in playerWithId)
                                    {
                                        AmongUsClient.Instance.KickWithReason(player.OwnerId, "You were kicked from the lobby.");
                                    }
                                    chatText = "The player was kicked successfully.";
                                    system = true;
                                }
                            }
                        }
                        else if (sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            chatText = "You don't have access to this command!";
                            noaccess = true;
                        }
                        return sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId;
                    }

                    if (chatText.ToLower().StartsWith("/ban "))
                    {
                        if (GameData.Instance.GetHost() == sourcePlayer.Data && sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            if (chatText[5..].IsNullOrWhiteSpace())
                            {
                                chatText = "You must specify a player id as parameter, use /id to get the list of all players ids.";
                                error = true;
                            }
                            if (Convert.ToByte(chatText[5..]) == PlayerControl.LocalPlayer.PlayerId)
                            {
                                chatText = "You can't ban yourself!";
                                error = true;
                            }
                            else
                            {
                                var id = Convert.ToByte(chatText[5..]);
                                var playerWithId = PlayerControl.AllPlayerControls.ToArray().Where(x => x.PlayerId == id).ToList();
                                if (!playerWithId.Any())
                                {
                                    chatText = "You must give a valid player id, use /id to get the list of all players ids.";
                                    error = true;
                                }
                                else
                                {
                                    foreach (var player in playerWithId)
                                    {
                                        AmongUsClient.Instance.Ban(player.OwnerId);
                                    }
                                    chatText = "The player was banned successfully.";
                                    system = true;
                                }
                            }
                        }
                        else if (sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            chatText = "You don't have access to this command!";
                            noaccess = true;
                        }
                        return sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId;
                    }
                    if (chatText.ToLower().StartsWith("/secret") && sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId) // Real secret fr fr
                    {
                        chatText = ":)";
                        System.Diagnostics.Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&ab_channel=RickAstley",
                            UseShellExecute = true
                        });
                    }
                    if (chatText.ToLower().StartsWith("/crew") || chatText.ToLower().StartsWith("/ crew"))
                    {
                        AddRoleMessage(RoleEnum.Crewmate);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/imp") || chatText.ToLower().StartsWith("/ imp"))
                    {
                        AddRoleMessage(RoleEnum.Impostor);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/roles") || chatText.ToLower().StartsWith("/ roles"))
                    {
                        AddRoleListMessage();
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/modi") || chatText.ToLower().StartsWith("/ modi "))
                    {
                        AddModiListMessage();
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/alt") || chatText.ToLower().StartsWith("/ alt"))
                    {
                        AddRoleMessage(RoleEnum.Altruist);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/engi") || chatText.ToLower().StartsWith("/ engi"))
                    {
                        AddRoleMessage(RoleEnum.Engineer);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/invest") || chatText.ToLower().StartsWith("/ invest"))
                    {
                        AddRoleMessage(RoleEnum.Investigator);
                        return false;
                    }
                    /*else if (chatText.ToLower().StartsWith("/mayor") || chatText.ToLower().StartsWith("/ mayor"))
                    {
                        AddRoleMessage(RoleEnum.Mayor);
                        return false;
                    }*/
                    else if (chatText.ToLower().StartsWith("/presi") || chatText.ToLower().StartsWith("/ presi"))
                    {
                        AddRoleMessage(RoleEnum.President);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/medic") || chatText.ToLower().StartsWith("/ medic"))
                    {
                        AddRoleMessage(RoleEnum.Medic);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/sher") || chatText.ToLower().StartsWith("/ sher"))
                    {
                        AddRoleMessage(RoleEnum.Sheriff);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/swap") || chatText.ToLower().StartsWith("/ swap"))
                    {
                        AddRoleMessage(RoleEnum.Swapper);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/seer") || chatText.ToLower().StartsWith("/ seer"))
                    {
                        AddRoleMessage(RoleEnum.Seer);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/sni") || chatText.ToLower().StartsWith("/ sni"))
                    {
                        AddRoleMessage(RoleEnum.Snitch);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/spy") || chatText.ToLower().StartsWith("/ spy"))
                    {
                        AddRoleMessage(RoleEnum.Spy);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/vig") || chatText.ToLower().StartsWith("/ vig"))
                    {
                        AddRoleMessage(RoleEnum.Vigilante);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/hunt") || chatText.ToLower().StartsWith("/ hunt"))
                    {
                        AddRoleMessage(RoleEnum.Hunter);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/arso") || chatText.ToLower().StartsWith("/ arso"))
                    {
                        AddRoleMessage(RoleEnum.Arsonist);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/exe") || chatText.ToLower().StartsWith("/ exe"))
                    {
                        AddRoleMessage(RoleEnum.Executioner);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/glitch") || chatText.ToLower().StartsWith("/ glitch") ||
                        chatText.ToLower().StartsWith("/theglitch") || chatText.ToLower().StartsWith("/ theglitch") ||
                        chatText.ToLower().StartsWith("/the glitch") || chatText.ToLower().StartsWith("/ the glitch"))
                    {
                        AddRoleMessage(RoleEnum.Glitch);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/ice") || chatText.ToLower().StartsWith("/ ice"))
                    {
                        AddRoleMessage(RoleEnum.Icenberg);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/jest") || chatText.ToLower().StartsWith("/ jest"))
                    {
                        AddRoleMessage(RoleEnum.Jester);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/phan") || chatText.ToLower().StartsWith("/ phan"))
                    {
                        AddRoleMessage(RoleEnum.Phantom);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/gren") || chatText.ToLower().StartsWith("/ gren"))
                    {
                        AddRoleMessage(RoleEnum.Grenadier);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/jan") || chatText.ToLower().StartsWith("/ jan"))
                    {
                        AddRoleMessage(RoleEnum.Janitor);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/mini") || chatText.ToLower().StartsWith("/ mini"))
                    {
                        AddModifierMessage(ModifierEnum.Mini);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/miner") || chatText.ToLower().StartsWith("/ miner"))
                    {
                        AddRoleMessage(RoleEnum.Miner);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/morph") || chatText.ToLower().StartsWith("/ morph"))
                    {
                        AddRoleMessage(RoleEnum.Morphling);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/swoop") || chatText.ToLower().StartsWith("/ swoop"))
                    {
                        AddRoleMessage(RoleEnum.Swooper);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/utaker") || chatText.ToLower().StartsWith("/ utaker") ||
                        chatText.ToLower().StartsWith("/undertaker") || chatText.ToLower().StartsWith("/ undertaker"))
                    {
                        AddRoleMessage(RoleEnum.Undertaker);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/haunt") || chatText.ToLower().StartsWith("/ haunt"))
                    {
                        AddRoleMessage(RoleEnum.Haunter);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/vet") || chatText.ToLower().StartsWith("/ vet"))
                    {
                        AddRoleMessage(RoleEnum.Veteran);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/amne") || chatText.ToLower().StartsWith("/ amne"))
                    {
                        AddRoleMessage(RoleEnum.Amnesiac);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/jugg") || chatText.ToLower().StartsWith("/ jugg"))
                    {
                        AddRoleMessage(RoleEnum.Juggernaut);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/track") || chatText.ToLower().StartsWith("/ track"))
                    {
                        AddRoleMessage(RoleEnum.Tracker);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/trans") || chatText.ToLower().StartsWith("/ trans"))
                    {
                        AddRoleMessage(RoleEnum.Transporter);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/trait") || chatText.ToLower().StartsWith("/ trait"))
                    {
                        AddRoleMessage(RoleEnum.Traitor);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/med") || chatText.ToLower().StartsWith("/ med"))
                    {
                        AddRoleMessage(RoleEnum.Medium);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/trap") || chatText.ToLower().StartsWith("/ trap"))
                    {
                        AddRoleMessage(RoleEnum.Trapper);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/surv") || chatText.ToLower().StartsWith("/ surv"))
                    {
                        AddRoleMessage(RoleEnum.Survivor);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/ga") || chatText.ToLower().StartsWith("/ ga") ||
                        chatText.ToLower().StartsWith("/guardian") || chatText.ToLower().StartsWith("/ guardian"))
                    {
                        AddRoleMessage(RoleEnum.GuardianAngel);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/myst") || chatText.ToLower().StartsWith("/ myst"))
                    {
                        AddRoleMessage(RoleEnum.Mystic);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/bmer") || chatText.ToLower().StartsWith("/ bmer") ||
                        chatText.ToLower().StartsWith("/black") || chatText.ToLower().StartsWith("/ black"))
                    {
                        AddRoleMessage(RoleEnum.Blackmailer);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/pb") || chatText.ToLower().StartsWith("/ pb") ||
                        chatText.ToLower().StartsWith("/plague") || chatText.ToLower().StartsWith("/ plague"))
                    {
                        AddRoleMessage(RoleEnum.Plaguebearer);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/pest") || chatText.ToLower().StartsWith("/ pest"))
                    {
                        AddRoleMessage(RoleEnum.Pestilence);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/ww") || chatText.ToLower().StartsWith("/ ww") ||
                        chatText.ToLower().StartsWith("/were") || chatText.ToLower().StartsWith("/ were"))
                    {
                        AddRoleMessage(RoleEnum.Werewolf);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/detec") || chatText.ToLower().StartsWith("/ detec"))
                    {
                        AddRoleMessage(RoleEnum.Detective);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/escap") || chatText.ToLower().StartsWith("/ escap"))
                    {
                        AddRoleMessage(RoleEnum.Escapist);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/imitat") || chatText.ToLower().StartsWith("/ imitat"))
                    {
                        AddRoleMessage(RoleEnum.Imitator);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/bomb") || chatText.ToLower().StartsWith("/ bomb"))
                    {
                        AddRoleMessage(RoleEnum.Bomber);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/fore") || chatText.ToLower().StartsWith("/ fore"))
                    {
                        AddRoleMessage(RoleEnum.Foreteller);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/vamp") || chatText.ToLower().StartsWith("/ vamp"))
                    {
                        AddRoleMessage(RoleEnum.Vampire);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/pros") || chatText.ToLower().StartsWith("/ pros"))
                    {
                        AddRoleMessage(RoleEnum.Prosecutor);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/ora") || chatText.ToLower().StartsWith("/ ora"))
                    {
                        AddRoleMessage(RoleEnum.Oracle);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/ven") || chatText.ToLower().StartsWith("/ ven"))
                    {
                        AddRoleMessage(RoleEnum.Venerer);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/aur") || chatText.ToLower().StartsWith("/ aur"))
                    {
                        AddRoleMessage(RoleEnum.Aurial);
                        return false;
                    }
                    /*else if (chatText.ToLower().StartsWith("/poli") || chatText.ToLower().StartsWith("/ poli"))
                    {
                        AddRoleMessage(RoleEnum.Politician);
                        return false;
                    }*/
                    else if (chatText.ToLower().StartsWith("/ward") || chatText.ToLower().StartsWith("/ ward"))
                    {
                        AddRoleMessage(RoleEnum.Warden);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/war") || chatText.ToLower().StartsWith("/ war"))
                    {
                        AddRoleMessage(RoleEnum.Warlock);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/hypno") || chatText.ToLower().StartsWith("/ hypno"))
                    {
                        AddRoleMessage(RoleEnum.Hypnotist);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/jailor") || chatText.ToLower().StartsWith("/ jailor"))
                    {
                        AddRoleMessage(RoleEnum.Jailor);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/scav") || chatText.ToLower().StartsWith("/ scav"))
                    {
                        AddRoleMessage(RoleEnum.Scavenger);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/sc") || chatText.ToLower().StartsWith("/ sc") ||
                        chatText.ToLower().StartsWith("/soul") || chatText.ToLower().StartsWith("/ soul"))
                    {
                        AddRoleMessage(RoleEnum.SoulCollector);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/dep") || chatText.ToLower().StartsWith("/ dep"))
                    {
                        AddRoleMessage(RoleEnum.Deputy);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/plum") || chatText.ToLower().StartsWith("/ plum"))
                    {
                        AddRoleMessage(RoleEnum.Plumber);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/eclip") || chatText.ToLower().StartsWith("/ eclip"))
                    {
                        AddRoleMessage(RoleEnum.Eclipsal);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/time") || chatText.ToLower().StartsWith("/ time")
                    || chatText.ToLower().StartsWith("/tl") || chatText.ToLower().StartsWith("/ tl"))
                    {
                        AddRoleMessage(RoleEnum.TimeLord);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/merc") || chatText.ToLower().StartsWith("/ merc"))
                    {
                        AddRoleMessage(RoleEnum.Mercenary);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/cler") || chatText.ToLower().StartsWith("/ cler"))
                    {
                        AddRoleMessage(RoleEnum.Cleric);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/wrai") || chatText.ToLower().StartsWith("/ wrai"))
                    {
                        AddRoleMessage(RoleEnum.Wraith);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/kami") || chatText.ToLower().StartsWith("/ kami"))
                    {
                        AddRoleMessage(RoleEnum.Kamikaze);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/capt") || chatText.ToLower().StartsWith("/ capt"))
                    {
                        AddRoleMessage(RoleEnum.Captain);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/lover") || chatText.ToLower().StartsWith("/ lover"))
                    {
                        AddModifierMessage(ModifierEnum.Lover);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/lo") || chatText.ToLower().StartsWith("/ lo"))
                    {
                        AddRoleMessage(RoleEnum.Lookout);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/giant") || chatText.ToLower().StartsWith("/ giant"))
                    {
                        AddModifierMessage(ModifierEnum.Giant);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/button") || chatText.ToLower().StartsWith("/ button"))
                    {
                        AddModifierMessage(ModifierEnum.ButtonBarry);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/after") || chatText.ToLower().StartsWith("/ after"))
                    {
                        AddModifierMessage(ModifierEnum.Aftermath);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/bait") || chatText.ToLower().StartsWith("/ bait"))
                    {
                        AddModifierMessage(ModifierEnum.Bait);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/disp") || chatText.ToLower().StartsWith("/ disp"))
                    {
                        AddModifierMessage(ModifierEnum.Disperser);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/dis") || chatText.ToLower().StartsWith("/ dis"))
                    {
                        AddModifierMessage(ModifierEnum.Diseased);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/flash") || chatText.ToLower().StartsWith("/ flash"))
                    {
                        AddModifierMessage(ModifierEnum.Flash);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/tie") || chatText.ToLower().StartsWith("/ tie"))
                    {
                        AddModifierMessage(ModifierEnum.Tiebreaker);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/torch") || chatText.ToLower().StartsWith("/ torch"))
                    {
                        AddModifierMessage(ModifierEnum.Torch);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/sleuth") || chatText.ToLower().StartsWith("/ sleuth"))
                    {
                        AddModifierMessage(ModifierEnum.Sleuth);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/radar") || chatText.ToLower().StartsWith("/ radar"))
                    {
                        AddModifierMessage(ModifierEnum.Radar);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/multi") || chatText.ToLower().StartsWith("/ multi"))
                    {
                        AddModifierMessage(ModifierEnum.Multitasker);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/double") || chatText.ToLower().StartsWith("/ double"))
                    {
                        AddModifierMessage(ModifierEnum.DoubleShot);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/udog") || chatText.ToLower().StartsWith("/ udog") ||
                        chatText.ToLower().StartsWith("/underdog") || chatText.ToLower().StartsWith("/ underdog"))
                    {
                        AddModifierMessage(ModifierEnum.Underdog);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/frost") || chatText.ToLower().StartsWith("/ frost"))
                    {
                        AddModifierMessage(ModifierEnum.Frosty);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/sense") || chatText.ToLower().StartsWith("/ sense") ||
                        chatText.ToLower().StartsWith("/sixth") || chatText.ToLower().StartsWith("/ sixth"))
                    {
                        AddModifierMessage(ModifierEnum.SixthSense);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/shy") || chatText.ToLower().StartsWith("/ shy"))
                    {
                        AddModifierMessage(ModifierEnum.Shy);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/sab") || chatText.ToLower().StartsWith("/ sab"))
                    {
                        AddModifierMessage(ModifierEnum.Saboteur);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/cel") || chatText.ToLower().StartsWith("/ cel"))
                    {
                        AddModifierMessage(ModifierEnum.Celebrity);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/task") || chatText.ToLower().StartsWith("/ task"))
                    {
                        AddModifierMessage(ModifierEnum.Taskmaster);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/imm") || chatText.ToLower().StartsWith("/ imm"))
                    {
                        AddModifierMessage(ModifierEnum.Immovable);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/sat") || chatText.ToLower().StartsWith("/ sat"))
                    {
                        AddModifierMessage(ModifierEnum.Satellite);
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/ass") || chatText.ToLower().StartsWith("/ ass"))
                    {
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                            "The Assassin is an ability which is given to killers to guess other player's roles during mettings. If they guess correctly they kill the other player, if not, they die instead.");
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/me") || chatText.ToLower().StartsWith("/ me"))
                    {
                        var role = Role.GetRole(PlayerControl.LocalPlayer);
                        if (role != null) AddRoleMessage(role.RoleType);
                        else if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "You do not have a role.");
                        else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "Invalid Command.");
                        var modifiers = Modifier.GetModifiers(PlayerControl.LocalPlayer);
                        if (modifiers.Length == 0)
                        {
                            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "You do not have a modifier.");
                            else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "Invalid Command.");
                        }
                        else
                        {
                            foreach (var modifier in modifiers)
                            {
                                AddModifierMessage(modifier.ModifierType);
                            }
                        }
                        return false;
                    }
                    else if (chatText.ToLower().StartsWith("/help") || chatText.ToLower().StartsWith("/ help"))
                    {
                        string mess =
                            $"<color=#6DFFFA><b>Everyone:</b></color>\n\n" +
                            $"<color=#6DFFFA>/me</color> - Information about me.\n" +
                            $"<color=#6DFFFA>/roles</color> - Information about active roles.\n" +
                            $"<color=#6DFFFA>/modifiers</color> - Information about active modifiers.\n" +
                            $"<color=#6DFFFA>/note [note]</color> - Write your own note.\n" +
                            $"<color=#6DFFFA>/seenote</color> - Your saved notes.\n" +
                            $"<color=#6DFFFA>/(rolename)</color> - Role description.\n\n" +
                            $"<color=#D91919><b>Host Only:</b></color>\n\n" +
                            $"<color=#D91919>Shift + G + Enter</color> - Force the game to end.\n" +
                            $"<color=#D91919>/id</color> - See players ids.\n" +
                            $"<color=#D91919>/kick [id]</color> - Kick a player by its id.\n" +
                            $"<color=#D91919>/ban [id]</color> - Ban a player by its id.\n";
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, mess);
                        return false;
                    }
                }
                // Jailor nie musi pisac /jail.
                if (sourcePlayer.Is(RoleEnum.Jailor) && MeetingHud.Instance && sourcePlayer.IsAnyJailed())
                {
                    if (chatText.ToLower().StartsWith("/all"))
                    {
                        chatText = chatText.Substring(4).Trim();
                        if (sourcePlayer != PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.IsJailed() && !sourcePlayer.Data.IsDead)
                            sourcePlayer = PlayerControl.LocalPlayer;
                        return true;
                    }
                    else if (PlayerControl.LocalPlayer.Is(RoleEnum.Jailor) || PlayerControl.LocalPlayer.IsJailed())
                    {
                        JailorMessage = true;
                        if (sourcePlayer != PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.IsJailed() && !sourcePlayer.Data.IsDead)
                            sourcePlayer = PlayerControl.LocalPlayer;
                        return true;
                    }
                    else return false;
                }
                if (chatText.ToLower().StartsWith("/"))
                {
                    if (sourcePlayer == PlayerControl.LocalPlayer) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "Invalid Command.");
                    return false;
                }
                if (sourcePlayer.IsJailed() && MeetingHud.Instance)
                {
                    if (PlayerControl.LocalPlayer == sourcePlayer || PlayerControl.LocalPlayer.Is(RoleEnum.Jailor)) return true;
                    else return false;
                }
                if (PlayerControl.LocalPlayer.IsJailed() && MeetingHud.Instance) return false;
                return true;
            }

            public static void AddRoleListMessage()
            {
                Dictionary<string, Color> ColorMapping = new Dictionary<string, Color>();

                ColorMapping.Add("<b>Crewmate:</b>\n", Color.cyan);
                //if (CustomGameOptions.PoliticianOn > 0) ColorMapping.Add("Politician", Colors.Politician);
                if (CustomGameOptions.SheriffOn > 0) ColorMapping.Add("Sheriff", Colors.Sheriff);
                if (CustomGameOptions.EngineerOn > 0) ColorMapping.Add("Engineer", Colors.Engineer);
                if (CustomGameOptions.SwapperOn > 0) ColorMapping.Add("Swapper", Colors.Swapper);
                if (CustomGameOptions.MedicOn > 0) ColorMapping.Add("Medic", Colors.Medic);
                if (CustomGameOptions.SeerOn > 0) ColorMapping.Add("Seer", Colors.Seer);
                if (CustomGameOptions.SpyOn > 0) ColorMapping.Add("Spy", Colors.Spy);
                if (CustomGameOptions.SnitchOn > 0) ColorMapping.Add("Snitch", Colors.Snitch);
                if (CustomGameOptions.AltruistOn > 0) ColorMapping.Add("Altruist", Colors.Altruist);
                if (CustomGameOptions.VigilanteOn > 0) ColorMapping.Add("Vigilante", Colors.Vigilante);
                if (CustomGameOptions.VeteranOn > 0) ColorMapping.Add("Veteran", Colors.Veteran);
                if (CustomGameOptions.HunterOn > 0) ColorMapping.Add("Hunter", Colors.Hunter);
                if (CustomGameOptions.TrackerOn > 0) ColorMapping.Add("Tracker", Colors.Tracker);
                if (CustomGameOptions.TrapperOn > 0) ColorMapping.Add("Trapper", Colors.Trapper);
                if (CustomGameOptions.TransporterOn > 0) ColorMapping.Add("Transporter", Colors.Transporter);
                if (CustomGameOptions.MediumOn > 0) ColorMapping.Add("Medium", Colors.Medium);
                if (CustomGameOptions.MysticOn > 0) ColorMapping.Add("Mystic", Colors.Mystic);
                if (CustomGameOptions.DetectiveOn > 0) ColorMapping.Add("Detective", Colors.Detective);
                if (CustomGameOptions.ImitatorOn > 0) ColorMapping.Add("Imitator", Colors.Imitator);
                if (CustomGameOptions.ProsecutorOn > 0) ColorMapping.Add("Prosecutor", Colors.Prosecutor);
                if (CustomGameOptions.OracleOn > 0) ColorMapping.Add("Oracle", Colors.Oracle);
                if (CustomGameOptions.AurialOn > 0) ColorMapping.Add("Aurial", Colors.Aurial);
                if (CustomGameOptions.WardenOn > 0) ColorMapping.Add("Warden", Colors.Warden);
                if (CustomGameOptions.JailorOn > 0) ColorMapping.Add("Jailor", Colors.Jailor);
                if (CustomGameOptions.LookoutOn > 0) ColorMapping.Add("Lookout", Colors.Lookout);
                if (CustomGameOptions.DeputyOn > 0) ColorMapping.Add("Deputy", Colors.Deputy);
                if (CustomGameOptions.TimeLordOn > 0) ColorMapping.Add("Time Lord", Colors.TimeLord);
                if (CustomGameOptions.PresidentOn > 0) ColorMapping.Add("President", Colors.Mayor);
                if (CustomGameOptions.ClericOn > 0) ColorMapping.Add("Cleric", Colors.Cleric);
                if (CustomGameOptions.PlumberOn > 0) ColorMapping.Add("Plumber", Colors.Plumber);
                if (CustomGameOptions.CaptainOn > 0) ColorMapping.Add("Captain", Colors.Captain);
                if (CustomGameOptions.HaunterOn > 0) ColorMapping.Add("Haunter", Colors.Haunter);

                ColorMapping.Add("\n<b>Neutral:</b>\n", Color.gray);
                if (CustomGameOptions.AmnesiacOn > 0 || (CustomGameOptions.ExecutionerOn > 0 && CustomGameOptions.OnTargetDead == OnTargetDead.Amnesiac) || (CustomGameOptions.GuardianAngelOn > 0 && CustomGameOptions.GaOnTargetDeath == BecomeOptions.Amnesiac)) ColorMapping.Add("Amnesiac", Colors.Amnesiac);
                if (CustomGameOptions.GuardianAngelOn > 0) ColorMapping.Add("Guardian Angel", Colors.GuardianAngel);
                if (CustomGameOptions.MercenaryOn > 0 || (CustomGameOptions.ExecutionerOn > 0 && CustomGameOptions.OnTargetDead == OnTargetDead.Mercenary) || (CustomGameOptions.GuardianAngelOn > 0 && CustomGameOptions.GaOnTargetDeath == BecomeOptions.Mercenary)) ColorMapping.Add("Mercenary", Colors.Mercenary);
                if (CustomGameOptions.SurvivorOn > 0 || (CustomGameOptions.ExecutionerOn > 0 && CustomGameOptions.OnTargetDead == OnTargetDead.Survivor) || (CustomGameOptions.GuardianAngelOn > 0 && CustomGameOptions.GaOnTargetDeath == BecomeOptions.Survivor)) ColorMapping.Add("Survivor", Colors.Survivor);

                if (CustomGameOptions.ForetellerOn > 0) ColorMapping.Add("Foreteller", Colors.Foreteller);
                if (CustomGameOptions.ExecutionerOn > 0) ColorMapping.Add("Executioner", Colors.Executioner);
                if (CustomGameOptions.JesterOn > 0 || (CustomGameOptions.ExecutionerOn > 0 && CustomGameOptions.OnTargetDead == OnTargetDead.Jester) || (CustomGameOptions.GuardianAngelOn > 0 && CustomGameOptions.GaOnTargetDeath == BecomeOptions.Jester)) ColorMapping.Add("Jester", Colors.Jester);
                if (CustomGameOptions.PhantomOn > 0) ColorMapping.Add("Phantom", Colors.Phantom);
                //if (CustomGameOptions.VultureOn > 0) ColorMapping.Add("Vulture", Colors.Vulture);

                if (CustomGameOptions.ArsonistOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.Arsonist)) ColorMapping.Add("Arsonist", Colors.Arsonist);
                if (CustomGameOptions.GlitchOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.Glitch)) ColorMapping.Add("Glitch", Colors.Glitch);
                if (CustomGameOptions.IcenbergOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.Icenberg)) ColorMapping.Add("Icenberg", Colors.Icenberg);
                if (CustomGameOptions.PlaguebearerOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.Plaguebearer)) ColorMapping.Add("Plaguebearer", Colors.Plaguebearer);
                if (CustomGameOptions.VampireOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.Vampire)) ColorMapping.Add("Vampire", Colors.Vampire);
                if (CustomGameOptions.WerewolfOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.Werewolf)) ColorMapping.Add("Werewolf", Colors.Werewolf);
                if (CustomGameOptions.JuggernautOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.Juggernaut)) ColorMapping.Add("Juggernaut", Colors.Juggernaut);
                if (CustomGameOptions.SoulCollectorOn > 0 && !PlayerControl.LocalPlayer.Is(RoleEnum.SoulCollector)) ColorMapping.Add("Soul Collector", Colors.SoulCollector);

                ColorMapping.Add("\n<b>Impostor:</b>\n", Colors.Impostor);
                if (CustomGameOptions.JanitorOn > 0) ColorMapping.Add("Janitor", Colors.Impostor);
                if (CustomGameOptions.MorphlingOn > 0) ColorMapping.Add("Morphling", Colors.Impostor);
                if (CustomGameOptions.MinerOn > 0) ColorMapping.Add("Miner", Colors.Impostor);
                if (CustomGameOptions.SwooperOn > 0) ColorMapping.Add("Swooper", Colors.Impostor);
                if (CustomGameOptions.UndertakerOn > 0) ColorMapping.Add("Undertaker", Colors.Impostor);
                if (CustomGameOptions.EscapistOn > 0) ColorMapping.Add("Escapist", Colors.Impostor);
                if (CustomGameOptions.GrenadierOn > 0) ColorMapping.Add("Grenadier", Colors.Impostor);
                if (CustomGameOptions.TraitorOn > 0) ColorMapping.Add("Traitor", Colors.Impostor);
                if (CustomGameOptions.BlackmailerOn > 0) ColorMapping.Add("Blackmailer", Colors.Impostor);
                if (CustomGameOptions.BomberOn > 0) ColorMapping.Add("Bomber", Colors.Impostor);
                if (CustomGameOptions.KamikazeOn > 0) ColorMapping.Add("Kamikaze", Colors.Impostor);
                if (CustomGameOptions.WarlockOn > 0) ColorMapping.Add("Warlock", Colors.Impostor);
                if (CustomGameOptions.WraithOn > 0) ColorMapping.Add("Wraith", Colors.Impostor);
                if (CustomGameOptions.VenererOn > 0) ColorMapping.Add("Venerer", Colors.Impostor);
                if (CustomGameOptions.HypnotistOn > 0) ColorMapping.Add("Hypnotist", Colors.Impostor);
                if (CustomGameOptions.ScavengerOn > 0) ColorMapping.Add("Scavenger", Colors.Impostor);
                if (CustomGameOptions.EclipsalOn > 0) ColorMapping.Add("Eclipsal", Colors.Impostor);

                string mess = "";
                foreach (var rola in ColorMapping)
                {
                    mess += $"<color=#{rola.Value.ToHtmlStringRGBA()}> {rola.Key} </color>|";
                }

                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, mess);
            }
            public static void AddModiListMessage()
            {
                Dictionary<string, Color> ColorMapping = new Dictionary<string, Color>();

                ColorMapping.Add("<b>Crewmate Modifiers:</b>\n", Color.cyan);
                if (CustomGameOptions.AftermathOn > 0) ColorMapping.Add("Aftermath", Colors.Aftermath);
                if (CustomGameOptions.BaitOn > 0) ColorMapping.Add("Bait", Colors.Bait);
                if (CustomGameOptions.CelebrityOn > 0) ColorMapping.Add("Celebrity", Colors.Celebrity);
                if (CustomGameOptions.DiseasedOn > 0) ColorMapping.Add("Diseased", Colors.Diseased);
                if (CustomGameOptions.FrostyOn > 0) ColorMapping.Add("Frosty", Colors.Frosty);
                if (CustomGameOptions.MultitaskerOn > 0) ColorMapping.Add("Multitasker", Colors.Multitasker);
                if (CustomGameOptions.TaskmasterOn > 0) ColorMapping.Add("Taskmaster", Colors.Taskmaster);
                if (CustomGameOptions.TorchOn > 0) ColorMapping.Add("Torch", Colors.Torch);

                ColorMapping.Add("\n<b>Global Modifiers:</b>\n", Color.gray);
                if (CustomGameOptions.ButtonBarryOn > 0) ColorMapping.Add("Button Barry", Colors.ButtonBarry);
                //if (CustomGameOptions.DrunkOn > 0) ColorMapping.Add("Drunk", Colors.Drunk);
                if (CustomGameOptions.FlashOn > 0) ColorMapping.Add("Flash", Colors.Flash);
                if (CustomGameOptions.GiantOn > 0) ColorMapping.Add("Giant", Colors.Giant);
                if (CustomGameOptions.ImmovableOn > 0) ColorMapping.Add("Immovable", Colors.Immovable);
                if (CustomGameOptions.LoversOn > 0) ColorMapping.Add("Lovers", Colors.Lovers);
                if (CustomGameOptions.MiniOn > 0) ColorMapping.Add("Mini", Colors.Mini);
                if (CustomGameOptions.RadarOn > 0) ColorMapping.Add("Radar", Colors.Radar);
                if (CustomGameOptions.SatelliteOn > 0) ColorMapping.Add("Satellite", Colors.Satellite);
                if (CustomGameOptions.ShyOn > 0) ColorMapping.Add("Shy", Colors.Shy);
                if (CustomGameOptions.SixthSenseOn > 0) ColorMapping.Add("SixthSense", Colors.SixthSense);
                if (CustomGameOptions.SleuthOn > 0) ColorMapping.Add("Sleuth", Colors.Sleuth);
                if (CustomGameOptions.TiebreakerOn > 0) ColorMapping.Add("Tiebreaker", Colors.Tiebreaker);

                ColorMapping.Add("\n<b>Impostor Modifiers:</b>\n", Colors.Impostor);
                if (CustomGameOptions.DoubleShotOn > 0) ColorMapping.Add("Double Shot", Colors.Impostor);
                if (CustomGameOptions.UnderdogOn > 0) ColorMapping.Add("Underdog", Colors.Impostor);
                if (CustomGameOptions.DisperserOn > 0) ColorMapping.Add("Disperser", Colors.Impostor);
                if (CustomGameOptions.SaboteurOn > 0) ColorMapping.Add("Saboteur", Colors.Impostor);

                string mess = "";
                foreach (var modi in ColorMapping)
                {
                    mess += $"<color=#{modi.Value.ToHtmlStringRGBA()}> {modi.Key} </color>|";
                }

                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, mess);
            }
            public static void AddRoleMessage(RoleEnum role)
            {
                if (role == RoleEnum.Crewmate) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "The Crewmate is a vanilla Crewmate.");
                if (role == RoleEnum.Impostor) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "The Impostor a vanilla Impostor.");
                if (role == RoleEnum.Altruist) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Altruist is a crewmate with the ability to revive other players. In order to do so they must be near a dead body, when they start reviving, all killers are alerted about the Altruist and where they are to give them a chance to prevent the revival.");
                if (role == RoleEnum.Engineer) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Engineer is a crewmate with the ability to vent and fix sabotages.");
                if (role == RoleEnum.Investigator) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Investigator is a crewmate with the ability to see other player's footsteps for a limited time.");
                if (role == RoleEnum.Mayor) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Mayor is a crewmate with 3 votes and their role is revealed to everyone.");
                if (role == RoleEnum.Medic) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Medic is a crewmate who can place a shield on another player, preventing them from dying.");
                if (role == RoleEnum.Sheriff) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Sheriff is a crewmate who can kill other players. If the other player is good, they will self-kill instead.");
                if (role == RoleEnum.Swapper) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Swapper is a crewmate who can swap the votes of 2 players during meetings.");
                if (role == RoleEnum.President) HudManager.Instance.Chat.AddChat(
                   PlayerControl.LocalPlayer, "The President is a crewmate who can abstain its vote for the next round.");
                if (role == RoleEnum.Seer) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Seer is a crewmate who can reveal the alliance of other players.");
                if (role == RoleEnum.Snitch) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Snitch is a crewmate who can see who the Impostors are once they complete all their tasks.");
                if (role == RoleEnum.Spy) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Spy is a crewmate who can see the colours of players on the admin table.");
                if (role == RoleEnum.Vigilante) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Vigilante is a crewmate who can guess other people's roles during meetings. If they guess correctly they kill the other player, if not, they die instead.");
                if (role == RoleEnum.Hunter) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Hunter is a crewmate who can stalk other players. If the stalked player uses an ability, the Hunter will then be permitted to kill them. The Hunter has no punishment for killing incorrectly.");
                if (role == RoleEnum.Arsonist) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Arsonist is a neutral killer with the goal to kill everyone. To do so they must douse players and once enough people are doused they can ignite, killing all doused players immediately.");
                if (role == RoleEnum.Executioner) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Executioner is a neutral evil role with the goal to vote out a specific player.");
                if (role == RoleEnum.Glitch) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Glitch is a neutral killer with the goal to kill everyone. In addition to killing, they can also hack players, disabling abilities and mimic players, changing their appearance to look like others.");
                if (role == RoleEnum.Icenberg) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Icenberg is a neutral killer with the goal to kill everyone. In addition to killing, they can also freeze players for a certain time.");
                if (role == RoleEnum.Jester) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Jester is a neutral evil role with the goal to be voted out.");
                if (role == RoleEnum.Phantom) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Phantom is a neutral evil role with the goal to complete all their tasks without being clicked.");
                if (role == RoleEnum.Grenadier) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Grenadier is an impostor who can use smoke grenades to blind other players.");
                if (role == RoleEnum.Janitor) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Janitor is an impostor who can remove bodies from the map.");
                if (role == RoleEnum.Miner) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Miner is an impostor who can place new vents to create a new vent network.");
                if (role == RoleEnum.Morphling) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Morphling is an impostor who can morph into other players.");
                if (role == RoleEnum.Swooper) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Swooper is an impostor who can turn invisible.");
                if (role == RoleEnum.Undertaker) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Undertaker is an impostor who can drag bodies to different locations.");
                if (role == RoleEnum.Haunter) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Haunter is a crewmate who can reveal all Impostors on completion of their tasks.");
                if (role == RoleEnum.Veteran) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Veteran is a crewmate who can alert to kill anyone who interacts with them.");
                if (role == RoleEnum.Amnesiac) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Amnesiac is a neutral benign role that needs to find a body in order to remember a new role.");
                if (role == RoleEnum.Juggernaut) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Juggernaut is a neutral killer with the goal to kill everyone. Every kill they make reduces their kill cooldown.");
                if (role == RoleEnum.Tracker) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Tracker is a crewmate who can track multiple other players.");
                if (role == RoleEnum.Transporter) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Transporter is a crewmate who can swap the locations of 2 other players.");
                if (role == RoleEnum.Traitor) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Traitor is an impostor who was originally a Crewmate but switched sides.");
                if (role == RoleEnum.Medium) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Medium is a crewmate who can see dead players the round that they die.");
                if (role == RoleEnum.Trapper) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Trapper is a crewmate who can place traps around the map. All players who stand in these traps will reveal their role to the Trapper as long as enough players trigger the trap.");
                if (role == RoleEnum.Survivor) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Survivor is a neutral benign role that needs to live to win.");
                if (role == RoleEnum.GuardianAngel) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Guardian Angel is a neutral benign role that needs their target to win to win themselves.");
                if (role == RoleEnum.Mystic) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Mystic is a crewmate who gets an alert when a player dies.");
                if (role == RoleEnum.Blackmailer) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Blackmailer is an impostor who can silence other players.");
                if (role == RoleEnum.Plaguebearer) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Plaguebearer is a neutral killer with the goal to kill everyone. To do this they must infect everyone to turn into Pestilence.");
                if (role == RoleEnum.Pestilence) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Pestilence is a neutral killer with the goal to kill everyone. In addition to being able to kill, they are invincible and anyone who interacts with them will die.");
                if (role == RoleEnum.Werewolf) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Werewolf is a neutral killer with the goal to kill everyone. In order to kill, they must rampage which gives them a short kill cooldown to kill people in bursts.");
                if (role == RoleEnum.Detective) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Detective is a crewmate that can inspect crime scenes. Any player who has walked over this crime scene is suspicious. They can then examine players to see who has been at the crime scene.");
                if (role == RoleEnum.Escapist) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Escapist is an impostor who can mark a location and recall to it later.");
                if (role == RoleEnum.Imitator) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Imitator is a crewmate who can select dead crew roles to use during meetings. The following round they become this new role.");
                if (role == RoleEnum.Bomber) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Bomber is an impostor who can place bombs, these kill anyone in the area a short duration later.");
                if (role == RoleEnum.Foreteller) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, $"The Foreteller is a neutral evil role with the goal to guess {CustomGameOptions.ForetellerGuessesToWin} other player's roles.");
                if (role == RoleEnum.Vampire) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Vampire is a neutral killer with the goal to kill everyone. The first crewmate the original Vampire bites will turn into a Vampire, the rest will die.");
                if (role == RoleEnum.Prosecutor) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Prosecutor is a crewmate who can exile a player of their choosing in a meeting.");
                if (role == RoleEnum.Warlock) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Warlock is an impostor who can charge their kill button to kill multiple people at once.");
                if (role == RoleEnum.Oracle) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Oracle is a crewmate who can make a player confess and bless players. Making a player confess allows the Oracle to learn 1 of 3 players is evil, as well as revealing their alignment on the Oracle's death. A blessed player cannot die during a meeting.");
                if (role == RoleEnum.Venerer) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Venerer is an impostor who improves their ability with each kill. First kill is camouflage, second is speed and third is a slow around the Venerer.");
                if (role == RoleEnum.Aurial) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Aurial is a crewmate who can sense ability uses nearby.");
                if (role == RoleEnum.Politician) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Politician is a crewmate who can campaign in order to become the Mayor. During meetings they can attempt to reveal as the Mayor, if they have campaigned over half the crewmates they will be successful, if not they are unable to campaign the following round.");
                if (role == RoleEnum.Warden) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Warden is a crewmate who can fortify other players. Fortified players cannot be interacted with.");
                if (role == RoleEnum.Hypnotist) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Hypnotist is an impostor who can hypnotise other players. During meetings they can then release mass hysteria which makes all hypnotised players see everyone else as either morphed as themself, camouflaged or invisible.");
                if (role == RoleEnum.Jailor) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Jailor is a crewmate who can jail other players. Jailed players cannot have meeting abilities used on them and cannot use meeting abilities themself. The Jailor and jailee may also privately talk to each other and the Jailor may also execute their jailee. If they execute a crewmate they lose the ability to jail players.");
                if (role == RoleEnum.SoulCollector) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Soul Collector is a neutral killer with the goal to kill everyone. Reaping a player instantly kills them however, instead of leaving behind a body they leave a soul behind instead.");
                if (role == RoleEnum.Lookout) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Lookout is a crewmate who can watch other players. They will see all players who interact with each player they watch.");
                if (role == RoleEnum.Scavenger) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Scavenger is an impostor who must hunt down prey. Once their kill cooldown is up they are given a target to kill and being their scavenge. If they kill that target they get a reduced kill cooldown and regenerate their scavenge duration. If they don't kill their target they are given an increased kill cooldown.");
                if (role == RoleEnum.Deputy) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Deputy is a crewmate who can camp other players. If the player is killed they will receive an alert notifying them of their death. During the following meeting they may then shoot anyone. If they shoot the killer, they die unless fortified or invincible, if they are wrong nothing happens.");
                if (role == RoleEnum.Plumber) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Plumber is a crewmate who can flush vents, ejecting everyone currently in a vent and block vents, preventing their use for the remainder of the game.");
                if (role == RoleEnum.Eclipsal) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Eclipsal is an impostor who can blind other players. Blinded players have no vision and their report button doesn't light up (but can still be used).");
                if (role == RoleEnum.Mercenary) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Mercenary is a neutral benign who can guard other players. Guarded players who are interacted with gain currency for the Mercenary to use to bribe players. The Mercenary wins if any bribed player lives and wins.");
                if (role == RoleEnum.Cleric) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Cleric is a crewmate who can barrier other players temporarily or cleanse players. Barriered players cannot be killed. Cleansing a player removes all negative effects (e.g. blackmail, douse).");
                if (role == RoleEnum.Wraith) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Wraith is an impostor who can walk trought walls.");
                if (role == RoleEnum.Captain) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Captain is a crewmate who can zoom out map to see what's happening.");
                if (role == RoleEnum.TimeLord) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Time Lord is a crewmate who can rewind time.");
            }

            public static void AddModifierMessage(ModifierEnum modifier)
            {
                if (modifier == ModifierEnum.Giant) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Giant is a global modifier that increases the size of a player.");
                if (modifier == ModifierEnum.Mini) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Mini is a global modifier that decreases the size of a player.");
                if (modifier == ModifierEnum.ButtonBarry) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Button Barry is a global modifier that allows the player to button from anywhere on the map.");
                if (modifier == ModifierEnum.Aftermath) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Aftermath is a crewmate modifier that forces their killer to instantly use their ability.");
                if (modifier == ModifierEnum.Bait) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Bait is a crewmate modifier that forces their killer to report their body.");
                if (modifier == ModifierEnum.Diseased) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Diseased is a crewmate modifier that increases their killer's kill cooldown.");
                if (modifier == ModifierEnum.Flash) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Flash is a global modifier that increases the speed of a player.");
                if (modifier == ModifierEnum.Tiebreaker) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Tiebreaker is a global modifier that allows a player's vote to break ties in meetings.");
                if (modifier == ModifierEnum.Torch) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Torch is a crewmate modifier that allows them to see when lights are off.");
                if (modifier == ModifierEnum.Lover) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Lover is a global modifier that life links 2 players. They also gain an extra win condition of surviving until final 3 together.");
                if (modifier == ModifierEnum.Sleuth) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Sleuth is a global modifier that allows a player to see roles of dead bodies that they report.");
                if (modifier == ModifierEnum.Radar) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Radar is a global modifier that shows an arrow pointing to the closest player.");
                if (modifier == ModifierEnum.Disperser) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer,
                    "The Disperser is an impostor modifier that gives an extra ability to Impostors. This being once per game sending every player to a random vent on the map.");
                if (modifier == ModifierEnum.Multitasker) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Multitasker is a crewmate modifier that makes their tasks slightly transparent.");
                if (modifier == ModifierEnum.DoubleShot) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Double Shot is an impostor modifier that gives Assassins an extra life when assassinating.");
                if (modifier == ModifierEnum.Underdog) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Underdog is an impostor modifier that grants Impostors a reduced kill cooldown when alone.");
                if (modifier == ModifierEnum.Frosty) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Frosty is a crewmate modifier that reduces the speed of their killer temporarily.");
                if (modifier == ModifierEnum.SixthSense) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Sixth Sense is a global modifier that alerts players to when someone interacts with them.");
                if (modifier == ModifierEnum.Shy) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Shy is a global modifier that makes the player slightly transparent when they stand still.");
                if (modifier == ModifierEnum.Saboteur) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Saboteur is an impostor modifier that passively reduces non-door sabotage cooldowns.");
                if (modifier == ModifierEnum.Celebrity) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Celebrity is a crewmate modifier that will send a message showing where, when, and how you died when the next meeting starts.");
                if (modifier == ModifierEnum.Taskmaster) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Taskmaster is a crewmate modifier that will automatically complete a random task after each meeting.");
                if (modifier == ModifierEnum.Immovable) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Immovable is a global modifier that makes the player unable to move via abilities and meetings.");
                if (modifier == ModifierEnum.Satellite) HudManager.Instance.Chat.AddChat(
                    PlayerControl.LocalPlayer, "The Satellite is a global modifier that gives a 1 time use ability to detect where all dead bodies are.");
            }
        }

        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public static class SetName
        {
            public static void Postfix(ChatBubble __instance, [HarmonyArgument(0)] string playerName)
            {
                if (PlayerControl.LocalPlayer.Is(RoleEnum.Jailor) && MeetingHud.Instance)
                {
                    var jailor = Role.GetRole<Jailor>(PlayerControl.LocalPlayer);
                    if (jailor.Jailed != null && jailor.Jailed.Data.PlayerName == playerName)
                    {
                        __instance.NameText.color = jailor.Color;
                        __instance.NameText.text = playerName + " (Jailed)";
                    }
                    else if (JailorMessage)
                    {
                        __instance.NameText.color = jailor.Color;
                        __instance.NameText.text = "Jailor";
                        JailorMessage = false;
                    }
                }
                if (PlayerControl.LocalPlayer.IsJailed() && MeetingHud.Instance)
                {
                    if (JailorMessage)
                    {
                        __instance.NameText.color = Colors.Jailor;
                        __instance.NameText.text = "Jailor";
                        JailorMessage = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public class ChatFixName
        {
            public static bool Prefix(ChatBubble __instance)
            {
                if (system)
                {
                    __instance.NameText.text = "System Message";
                    __instance.NameText.color = Palette.CrewmateBlue;
                    __instance.NameText.ForceMeshUpdate(true, true);
                    __instance.Xmark.enabled = false;
                    __instance.votedMark.enabled = false;
                    system = false;
                    return false;
                }
                else if (error)
                {
                    __instance.NameText.text = "Error";
                    __instance.NameText.color = Palette.ImpostorRed;
                    __instance.NameText.ForceMeshUpdate(true, true);
                    __instance.Xmark.enabled = true;
                    __instance.votedMark.enabled = false;
                    error = false;
                    return false;
                }
                else if (noaccess)
                {
                    __instance.NameText.text = "No Access";
                    __instance.NameText.color = Palette.Blue;
                    __instance.NameText.ForceMeshUpdate(true, true);
                    __instance.Xmark.enabled = true;
                    __instance.votedMark.enabled = false;
                    noaccess = false;
                    return false;
                }
                else return true;
            }
        }
    }
}