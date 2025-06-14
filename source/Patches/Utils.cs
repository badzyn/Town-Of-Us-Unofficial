﻿using HarmonyLib;
using Hazel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.CrewmateRoles.MedicMod;
using TownOfUs.Extensions;
using TownOfUs.Patches;
using TownOfUs.Roles;
using TownOfUs.Roles.Modifiers;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;
using PerformKill = TownOfUs.Modifiers.UnderdogMod.PerformKill;
using Random = UnityEngine.Random;
using AmongUs.GameOptions;
using TownOfUs.CrewmateRoles.TrapperMod;
using TownOfUs.ImpostorRoles.BomberMod;
using Reactor.Networking;
using Reactor.Networking.Extensions;
using TownOfUs.CrewmateRoles.DetectiveMod;
using TownOfUs.NeutralRoles.SoulCollectorMod;
using static TownOfUs.Roles.Glitch;
using TownOfUs.Patches.NeutralRoles;
using Il2CppSystem.Linq;
using TownOfUs.ImpostorRoles.TraitorMod;
using TownOfUs.Modifiers.ShyMod;
using TownOfUs.CrewmateRoles.ClericMod;
using static TownOfUs.DisableAbilities;
using static TownOfUs.Roles.Icenberg;
using TownOfUs.ImpostorRoles.KamikazeMod;
using TownOfUs.CrewmateRoles.SheriffMod;

namespace TownOfUs
{
    [HarmonyPatch]
    public static class Utils
    {
        internal static bool ShowDeadBodies = false;
        private static NetworkedPlayerInfo voteTarget = null;



        public static void Unfreeze(PlayerControl player)
        {
            Debug.Log($"SPEED to {player.name}");
            if (player.MyPhysics != null)
            {
                player.MyPhysics.enabled = true;
            }
            if (PlayerControl.LocalPlayer.MyPhysics != null)
            {
                PlayerControl.LocalPlayer.MyPhysics.enabled = true;
            }
        }

        public static void Freeze(PlayerControl killer, PlayerControl target)
        {
            Coroutines.Start(IFreezePlayer(killer, target));
        }

        public static IEnumerator IFreezePlayer(PlayerControl killer, PlayerControl target)
        {
            var icenberg = Role.GetRole<Icenberg>(killer);
            var lf = DateTime.UtcNow;
            if (PlayerControl.LocalPlayer.MyPhysics != null && PlayerControl.LocalPlayer == target)
            {
                PlayerControl.LocalPlayer.MyPhysics.enabled = false;
                Coroutines.Start(Utils.FlashCoroutine(Color.blue, CustomGameOptions.FreezeDuration));
                Coroutines.Start(DisableAbility.StopAbility(CustomGameOptions.FreezeDuration));
            }
            while (true)
            {
                var elapsedTime = (float)(DateTime.UtcNow - lf).TotalSeconds;
                var remainingTime = CustomGameOptions.FreezeDuration - elapsedTime;
                if (PlayerControl.LocalPlayer == target && remainingTime <= 0)
                {
                    PlayerControl.LocalPlayer.MyPhysics.enabled = true;
                    break;
                }
                yield return new WaitForSeconds(0.5f);
            }
            var bodies = Object.FindObjectsOfType<DeadBody>();
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var body in bodies)
                {
                    try
                    {
                        if (body.ParentId == target.PlayerId) { break; }
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                foreach (var body in bodies)
                {
                    try
                    {
                        if (body.ParentId == target.PlayerId) { break; }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void Morph(PlayerControl player, PlayerControl MorphedPlayer)
        {
            if (PlayerControl.LocalPlayer.IsHypnotised()) return;
            if (CamouflageUnCamouflage.IsCamoed) return;
            if (player.GetCustomOutfitType() != CustomPlayerOutfitType.Morph)
                player.SetOutfit(CustomPlayerOutfitType.Morph, MorphedPlayer.Data.DefaultOutfit);
        }

        public static void Swoop(PlayerControl player)
        {
            if (PlayerControl.LocalPlayer.IsHypnotised()) return;
            var color = Color.clear;
            if (PlayerControl.LocalPlayer.Data.IsImpostor() || PlayerControl.LocalPlayer.Data.IsDead) color.a = 0.1f;

            if (player.GetCustomOutfitType() != CustomPlayerOutfitType.Swooper)
            {
                player.SetOutfit(CustomPlayerOutfitType.Swooper, new NetworkedPlayerInfo.PlayerOutfit()
                {
                    ColorId = player.CurrentOutfit.ColorId,
                    HatId = "",
                    SkinId = "",
                    VisorId = "",
                    PlayerName = " ",
                    PetId = ""
                });
                player.myRend().color = color;
                player.nameText().color = Color.clear;
                player.cosmetics.colorBlindText.color = Color.clear;
            }
        }

        public static void Unmorph(PlayerControl player)
        {
            if (PlayerControl.LocalPlayer.IsHypnotised()) return;
            if (CamouflageUnCamouflage.IsCamoed)
            {
                player.SetOutfit(CustomPlayerOutfitType.Camouflage, new NetworkedPlayerInfo.PlayerOutfit()
                {
                    ColorId = player.GetDefaultOutfit().ColorId,
                    HatId = "",
                    SkinId = "",
                    VisorId = "",
                    PlayerName = " ",
                    PetId = ""
                });
                PlayerMaterial.SetColors(Color.grey, player.myRend());
                player.nameText().color = Color.clear;
                player.cosmetics.colorBlindText.color = Color.clear;
            }
            else
            {
                player.SetOutfit(CustomPlayerOutfitType.Default);
                if (!player.Is(ModifierEnum.Shy) || player.Data.IsDead || player.Data.Disconnected) return;
                player.SetHatAndVisorAlpha(1f);
                player.cosmetics.skin.layer.color = player.cosmetics.skin.layer.color.SetAlpha(1f);
                foreach (var rend in player.cosmetics.currentPet.renderers)
                    rend.color = rend.color.SetAlpha(1f);
                foreach (var shadow in player.cosmetics.currentPet.shadows)
                    shadow.color = shadow.color.SetAlpha(1f);
            }
        }

        public static void GroupCamouflage()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                Camouflage(player);
            }
        }

        public static void Camouflage(PlayerControl player)
        {
            if (PlayerControl.LocalPlayer.IsHypnotised()) return;
            if (player.GetCustomOutfitType() != CustomPlayerOutfitType.Camouflage &&
                    player.GetCustomOutfitType() != CustomPlayerOutfitType.Swooper &&
                    player.GetCustomOutfitType() != CustomPlayerOutfitType.PlayerNameOnly)
            {
                player.SetOutfit(CustomPlayerOutfitType.Camouflage, new NetworkedPlayerInfo.PlayerOutfit()
                {
                    ColorId = player.GetDefaultOutfit().ColorId,
                    HatId = "",
                    SkinId = "",
                    VisorId = "",
                    PlayerName = " ",
                    PetId = ""
                });
                PlayerMaterial.SetColors(Color.grey, player.myRend());
                player.nameText().color = Color.clear;
                player.cosmetics.colorBlindText.color = Color.clear;
            }
        }

        public static void UnCamouflage()
        {
            if (PlayerControl.LocalPlayer.IsHypnotised()) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Is(RoleEnum.Swooper))
                {
                    var swooper = Role.GetRole<Swooper>(player);
                    if (swooper.IsSwooped) continue;
                }
                else if (player.Is(RoleEnum.Venerer))
                {
                    var venerer = Role.GetRole<Venerer>(player);
                    if (venerer.IsCamouflaged) continue;
                }
                else if (player.Is(RoleEnum.Morphling))
                {
                    var morphling = Role.GetRole<Morphling>(player);
                    if (morphling.Morphed) continue;
                }
                else if (player.Is(RoleEnum.Glitch))
                {
                    var glitch = Role.GetRole<Glitch>(player);
                    if (glitch.IsUsingMimic) continue;
                }
                else if (CamouflageUnCamouflage.IsCamoed) continue;
                Unmorph(player);
            }
        }

        public static void AddUnique<T>(this Il2CppSystem.Collections.Generic.List<T> self, T item)
            where T : IDisconnectHandler
        {
            if (!self.Contains(item)) self.Add(item);
        }

        public static bool IsLover(this PlayerControl player)
        {
            return player.Is(ModifierEnum.Lover);
        }

        public static bool Is(this PlayerControl player, RoleEnum roleType)
        {
            return Role.GetRole(player)?.RoleType == roleType;
        }

        public static bool Is(this PlayerControl player, ModifierEnum modifierType)
        {
            return Modifier.GetModifiers(player).Any(x => x.ModifierType == modifierType);
        }

        public static bool Is(this PlayerControl player, AbilityEnum abilityType)
        {
            return Ability.GetAbility(player)?.AbilityType == abilityType;
        }

        public static bool Is(this PlayerControl player, Faction faction)
        {
            return Role.GetRole(player)?.Faction == faction;
        }

        public static List<PlayerControl> GetCrewmates(List<PlayerControl> impostors)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(
                player => !impostors.Any(imp => imp.PlayerId == player.PlayerId)
            ).ToList();
        }

        public static List<PlayerControl> GetImpostors(
            List<NetworkedPlayerInfo> infected)
        {
            var impostors = new List<PlayerControl>();
            foreach (var impData in infected)
                impostors.Add(impData.Object);

            return impostors;
        }

        public static RoleEnum GetRole(PlayerControl player)
        {
            if (player == null) return RoleEnum.None;
            if (player.Data == null) return RoleEnum.None;

            var role = Role.GetRole(player);
            if (role != null) return role.RoleType;

            return player.Data.IsImpostor() ? RoleEnum.Impostor : RoleEnum.Crewmate;
        }

        public static PlayerControl PlayerById(byte id)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player.PlayerId == id)
                    return player;

            return null;
        }

        public static bool CommsCamouflaged()
        {
            if (!CustomGameOptions.ColourblindComms) return false;
            if (PlayerControl.LocalPlayer.AreCommsAffected()) return true;
            return false;
        }

        public static bool IsCrewKiller(this PlayerControl player)
        {
            if (!CustomGameOptions.CrewKillersContinue) return false;
            if (player.Is(RoleEnum.Mayor) || player.Is(RoleEnum.Politician) || player.Is(RoleEnum.Swapper) ||
                (player.Is(RoleEnum.Sheriff) && CustomGameOptions.SheriffKillsNK)) return true;
            else if (player.Is(RoleEnum.Hunter))
            {
                var hunter = Role.GetRole<Hunter>(player);
                if (hunter.UsesLeft > 0 || (hunter.StalkedPlayer != null && !hunter.StalkedPlayer.Data.IsDead && !hunter.StalkedPlayer.Data.Disconnected && hunter.StalkedPlayer.Is(Faction.NeutralKilling)) ||
                hunter.CaughtPlayers.Count(player => !player.Data.IsDead && !player.Data.Disconnected && player.Is(Faction.NeutralKilling)) > 0) return true;
            }
            else if (player.Is(RoleEnum.Imitator))
            {
                if (PlayerControl.AllPlayerControls.ToArray().Count(x => x.Data.IsDead && !x.Data.Disconnected &&
                (x.Is(RoleEnum.Hunter) || x.Is(RoleEnum.Sheriff) || x.Is(RoleEnum.Veteran))) > 0) return true;
            }
            else if (player.Is(RoleEnum.Jailor))
            {
                var jailor = Role.GetRole<Jailor>(player);
                if (jailor.Executes > 0) return true;
            }
            else if (player.Is(RoleEnum.Prosecutor))
            {
                var pros = Role.GetRole<Prosecutor>(player);
                if (!pros.HasProsecuted) return true;
            }
            else if (player.Is(RoleEnum.Veteran))
            {
                var vet = Role.GetRole<Veteran>(player);
                if (vet.UsesLeft > 0 || vet.Enabled) return true;
            }
            else if (player.Is(RoleEnum.Vigilante))
            {
                var vigi = Role.GetRole<Vigilante>(player);
                if (vigi.RemainingKills > 0 && CustomGameOptions.VigilanteGuessNeutralKilling) return true;
            }
            else if (player.Is(RoleEnum.Deputy))
            {
                var dep = Role.GetRole<Deputy>(player);
                if (dep.Killer != null && !dep.Killer.Data.IsDead && !dep.Killer.Data.Disconnected) return true;
            }
            else if (player.Is(RoleEnum.Altruist))
            {
                var altruist = Role.GetRole<Altruist>(player);
                if (((altruist.UsesLeft > 0 && !altruist.UsedThisRound) || altruist.CurrentlyReviving) && GameObject.FindObjectsOfType<DeadBody>().Count > 0) return true;
            }
            return false;
        }

        public static bool IsExeTarget(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Executioner).Any(role =>
            {
                var exeTarget = ((Executioner)role).target;
                return exeTarget != null && player.PlayerId == exeTarget.PlayerId;
            });
        }

        public static bool IsShielded(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Medic).Any(role =>
            {
                var shieldedPlayer = ((Medic)role).ShieldedPlayer;
                return shieldedPlayer != null && player.PlayerId == shieldedPlayer.PlayerId;
            });
        }

        public static bool IsFortified(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Warden).Any(role =>
            {
                var warden = (Warden)role;
                var fortifiedPlayer = warden.Fortified;
                return fortifiedPlayer != null && player.PlayerId == fortifiedPlayer.PlayerId && !warden.Player.Data.IsDead && !warden.Player.Data.Disconnected;
            });
        }

        public static bool IsBlessed(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Oracle).Any(role =>
            {
                var oracle = (Oracle)role;
                var blessedPlayer = oracle.Blessed;
                return blessedPlayer != null && player.PlayerId == blessedPlayer.PlayerId && !oracle.Player.Data.IsDead && !oracle.Player.Data.Disconnected;
            });
        }

        public static bool IsBarriered(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Cleric).Any(role =>
            {
                var cleric = (Cleric)role;
                var barrieredPlayer = cleric.Barriered;
                return barrieredPlayer != null && player.PlayerId == barrieredPlayer.PlayerId && !cleric.Player.Data.Disconnected;
            });
        }

        public static bool IsHacked(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Glitch).Any(role =>
            {
                var glitch = (Glitch)role;
                var hackedPlayer = glitch.Hacked;
                return hackedPlayer != null && player.PlayerId == hackedPlayer.PlayerId && !hackedPlayer.Data.IsDead && !glitch.Player.Data.IsDead;
            });
        }

        public static bool IsFreezed(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Icenberg).Any(role =>
            {
                var icenberg = (Icenberg)role;
                var hackedPlayer = icenberg.Freezed;
                return hackedPlayer != null && player.PlayerId == hackedPlayer.PlayerId && !hackedPlayer.Data.IsDead && !icenberg.Player.Data.IsDead;
            });
        }
        public static Icenberg GetIcenberg(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Icenberg).FirstOrDefault(role =>
            {
                var icenberg = (Icenberg)role;
                return icenberg != null && player == icenberg.Player;
            }) as Icenberg;
        }

        public static bool IsHypnotised(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Hypnotist).Any(role =>
            {
                var hypnotist = (Hypnotist)role;
                return hypnotist.HypnotisedPlayers.Contains(player.PlayerId) && hypnotist.HysteriaActive && !hypnotist.Player.Data.IsDead && !hypnotist.Player.Data.Disconnected && !player.Data.IsDead;
            });
        }

        public static bool IsGhostRole(this PlayerControl player)
        {
            return player.Is(RoleEnum.Haunter) || player.Is(RoleEnum.Phantom);
        }

        public static bool IsJailed(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Jailor).Any(role =>
            {
                var jailor = (Jailor)role;
                return jailor.Jailed == player && !player.Data.IsDead && !player.Data.Disconnected;
            }) || Role.GetRoles(RoleEnum.Imitator).Any(role =>
            {
                var imitator = (Imitator)role;
                return imitator.jailedPlayer == player && !player.Data.IsDead && !player.Data.Disconnected;
            });
        }

        public static bool IsAnyJailed(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Jailor).Any(role =>
            {
                var jailor = (Jailor)role;
                return jailor.IsAnyJailed == player && !player.Data.IsDead && !player.Data.Disconnected;
            });
        }

        public static bool IsBlackmailed(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Blackmailer).Any(role =>
            {
                var bmer = (Blackmailer)role;
                return bmer.Blackmailed == player && !player.Data.IsDead && !player.Data.Disconnected;
            });
        }

        public static List<Cleric> GetCleric(this PlayerControl player)
        {
            List<Cleric> clerics = new List<Cleric>();
            foreach (var role in Role.GetRoles(RoleEnum.Cleric))
            {
                var cleric = (Cleric)role;
                if (cleric.Barriered == player) clerics.Add(cleric);
            }
            return clerics;
        }

        public static List<Medic> GetMedic(this PlayerControl player)
        {
            List<Medic> medics = new List<Medic>();
            foreach (var role in Role.GetRoles(RoleEnum.Medic))
            {
                var medic = (Medic)role;
                if (medic.ShieldedPlayer == player) medics.Add(medic);
            }
            return medics;
        }

        public static List<Warden> GetWarden(this PlayerControl player)
        {
            List<Warden> wardens = new List<Warden>();
            foreach (var role in Role.GetRoles(RoleEnum.Warden))
            {
                var warden = (Warden)role;
                if (warden.Fortified == player) wardens.Add(warden);
            }
            return wardens;
        }

        public static List<Oracle> GetOracle(this PlayerControl player)
        {
            List<Oracle> oracles = new List<Oracle>();
            foreach (var role in Role.GetRoles(RoleEnum.Oracle))
            {
                var oracle = (Oracle)role;
                if (oracle.Blessed == player) oracles.Add(oracle);
            }
            return oracles;
        }

        public static GuardianAngel GetGA(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.GuardianAngel).FirstOrDefault(role =>
            {
                var protectedPlayer = ((GuardianAngel)role).target;
                return protectedPlayer != null && player.PlayerId == protectedPlayer.PlayerId;
            }) as GuardianAngel;
        }

        public static bool IsOnAlert(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Veteran).Any(role =>
            {
                var veteran = (Veteran)role;
                return veteran != null && veteran.OnAlert && player.PlayerId == veteran.Player.PlayerId;
            });
        }

        public static bool IsVesting(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Survivor).Any(role =>
            {
                var surv = (Survivor)role;
                return surv != null && surv.Vesting && player.PlayerId == surv.Player.PlayerId;
            });
        }

        public static bool IsProtected(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.GuardianAngel).Any(role =>
            {
                var gaTarget = ((GuardianAngel)role).target;
                var ga = (GuardianAngel)role;
                return gaTarget != null && ga.Protecting && player.PlayerId == gaTarget.PlayerId;
            });
        }

        public static bool IsInfected(this PlayerControl player)
        {
            return Role.GetRoles(RoleEnum.Plaguebearer).Any(role =>
            {
                var plaguebearer = (Plaguebearer)role;
                return plaguebearer != null && (plaguebearer.InfectedPlayers.Contains(player.PlayerId) || player.PlayerId == plaguebearer.Player.PlayerId);
            });
        }

        public static List<bool> Interact(PlayerControl player, PlayerControl target, bool toKill = false)
        {
            bool fullCooldownReset = false;
            bool gaReset = false;
            bool zeroSecReset = false;
            bool abilityUsed = false;
            var checkHack = AbilityUsed(player, target);
            if (!checkHack) return new List<bool> { false, false, false, true, false };
            if (!player.Is(RoleEnum.Cleric) && (target.IsInfected() || player.IsInfected()))
            {
                foreach (var pb in Role.GetRoles(RoleEnum.Plaguebearer)) ((Plaguebearer)pb).RpcSpreadInfection(target, player);
            }

            // Arsonist auto spread
            if (target.Is(RoleEnum.Arsonist) && CustomGameOptions.DouseSpread)
            {
                var arso = Role.GetRole<Arsonist>(target);

                int livingDoused = 0;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (!p.Data.IsDead && arso.DousedPlayers.Contains(p.PlayerId))
                    {
                        livingDoused++;
                    }
                }

                if (livingDoused < CustomGameOptions.MaxDoused && !arso.DousedPlayers.Contains(player.PlayerId))
                {
                    arso.DousedPlayers.Add(player.PlayerId);
                    Rpc(CustomRPC.Douse, target.PlayerId, player.PlayerId);
                }
            }


            if (target == ShowShield.FirstRoundShielded && toKill)
            {
                zeroSecReset = true;
            }
            else if (target.IsFortified())
            {
                zeroSecReset = true;
                Coroutines.Start(FlashCoroutine(Colors.Warden));
                foreach (var warden in target.GetWarden())
                {
                    Rpc(CustomRPC.Fortify, (byte)1, warden.Player.PlayerId);
                }
            }
            else if (target.Is(RoleEnum.Pestilence))
            {
                if (player.IsShielded())
                {
                    foreach (var medic in player.GetMedic())
                    {
                        Rpc(CustomRPC.AttemptSound, medic.Player.PlayerId, player.PlayerId);
                        StopKill.BreakShield(medic.Player.PlayerId, player.PlayerId, CustomGameOptions.ShieldBreaks);
                    }

                    if (CustomGameOptions.ShieldBreaks) fullCooldownReset = true;
                    else zeroSecReset = true;
                }
                else if (player.IsProtected() || player.IsBarriered())
                {
                    gaReset = true;
                    if (player.IsBarriered())
                    {
                        foreach (var cleric in player.GetCleric())
                        {
                            StopAttack.NotifyCleric(cleric.Player.PlayerId, false);
                        }
                    }
                }
                else RpcMurderPlayer(target, player);
            }
            else if (target.IsOnAlert())
            {
                if (player.Is(RoleEnum.Pestilence)) zeroSecReset = true;
                else if (player.IsShielded())
                {
                    foreach (var medic in player.GetMedic())
                    {
                        Rpc(CustomRPC.AttemptSound, medic.Player.PlayerId, player.PlayerId);
                        StopKill.BreakShield(medic.Player.PlayerId, player.PlayerId, CustomGameOptions.ShieldBreaks);
                    }

                    if (CustomGameOptions.ShieldBreaks) fullCooldownReset = true;
                    else zeroSecReset = true;
                }
                else if (player.IsProtected() || player.IsBarriered())
                {
                    gaReset = true;
                    if (player.IsBarriered())
                    {
                        foreach (var cleric in player.GetCleric())
                        {
                            StopAttack.NotifyCleric(cleric.Player.PlayerId, false);
                        }
                    }
                }
                else RpcMurderPlayer(target, player);
                if (toKill && CustomGameOptions.KilledOnAlert)
                {
                    if (target.IsShielded())
                    {
                        foreach (var medic in target.GetMedic())
                        {
                            Rpc(CustomRPC.AttemptSound, medic.Player.PlayerId, target.PlayerId);
                            StopKill.BreakShield(medic.Player.PlayerId, target.PlayerId, CustomGameOptions.ShieldBreaks);
                        }

                        if (CustomGameOptions.ShieldBreaks) fullCooldownReset = true;
                        else zeroSecReset = true;

                        Coroutines.Start(FlashCoroutine(new Color(0f, 0.5f, 0f, 1f)));
                    }
                    else if (target.IsProtected() || target.IsBarriered())
                    {
                        gaReset = true;
                        if (target.IsBarriered())
                        {
                            foreach (var cleric in target.GetCleric())
                            {
                                StopAttack.NotifyCleric(cleric.Player.PlayerId);
                            }
                        }
                    }
                    else
                    {
                        if (player.Is(RoleEnum.Glitch))
                        {
                            var glitch = Role.GetRole<Glitch>(player);
                            glitch.LastKill = DateTime.UtcNow;
                        }
                        else if (player.Is(RoleEnum.Icenberg))
                        {
                            var icenberg = Role.GetRole<Icenberg>(player);
                            icenberg.LastKill = DateTime.UtcNow;
                        }
                        else if (player.Is(RoleEnum.Juggernaut))
                        {
                            var jugg = Role.GetRole<Juggernaut>(player);
                            jugg.JuggKills += 1;
                            jugg.LastKill = DateTime.UtcNow;
                        }
                        else if (player.Is(RoleEnum.Pestilence))
                        {
                            var pest = Role.GetRole<Pestilence>(player);
                            pest.LastKill = DateTime.UtcNow;
                        }
                        else if (player.Is(RoleEnum.Vampire))
                        {
                            var vamp = Role.GetRole<Vampire>(player);
                            vamp.LastBit = DateTime.UtcNow;
                        }
                        else if (player.Is(RoleEnum.Werewolf))
                        {
                            var ww = Role.GetRole<Werewolf>(player);
                            ww.LastKilled = DateTime.UtcNow;
                        }
                        else if (player.Is(RoleEnum.SoulCollector))
                        {
                            var sc = Role.GetRole<SoulCollector>(player);
                            sc.LastReaped = DateTime.UtcNow;
                        }
                        RpcMurderPlayer(player, target);
                        abilityUsed = true;
                        fullCooldownReset = true;
                        gaReset = false;
                        zeroSecReset = false;
                    }
                }
            }
            else if (target.IsShielded() && toKill)
            {
                foreach (var medic in target.GetMedic())
                {
                    Rpc(CustomRPC.AttemptSound, medic.Player.PlayerId, target.PlayerId);
                    StopKill.BreakShield(medic.Player.PlayerId, target.PlayerId, CustomGameOptions.ShieldBreaks);
                }

                if (CustomGameOptions.ShieldBreaks) fullCooldownReset = true;
                else zeroSecReset = true;
                Coroutines.Start(Utils.FlashCoroutine(new Color(0f, 0.5f, 0f, 1f)));
            }
            else if ((target.IsVesting() || target.IsProtected() || target.IsBarriered()) && toKill)
            {
                gaReset = true;
                if (target.IsBarriered())
                {
                    foreach (var cleric in target.GetCleric())
                    {
                        StopAttack.NotifyCleric(cleric.Player.PlayerId);
                    }
                }
            }
            else if (toKill)
            {
                if (player.Is(RoleEnum.Glitch))
                {
                    var glitch = Role.GetRole<Glitch>(player);
                    glitch.LastKill = DateTime.UtcNow;
                }
                else if (player.Is(RoleEnum.Icenberg))
                {
                    var icenberg = Role.GetRole<Icenberg>(player);
                    icenberg.LastKill = DateTime.UtcNow;
                }
                else if (player.Is(RoleEnum.Juggernaut))
                {
                    var jugg = Role.GetRole<Juggernaut>(player);
                    jugg.JuggKills += 1;
                    jugg.LastKill = DateTime.UtcNow;
                }
                else if (player.Is(RoleEnum.Pestilence))
                {
                    var pest = Role.GetRole<Pestilence>(player);
                    pest.LastKill = DateTime.UtcNow;
                }
                else if (player.Is(RoleEnum.Vampire))
                {
                    var vamp = Role.GetRole<Vampire>(player);
                    vamp.LastBit = DateTime.UtcNow;
                }
                else if (player.Is(RoleEnum.Werewolf))
                {
                    var ww = Role.GetRole<Werewolf>(player);
                    ww.LastKilled = DateTime.UtcNow;
                }
                else if (player.Is(RoleEnum.SoulCollector))
                {
                    var sc = Role.GetRole<SoulCollector>(player);
                    sc.LastReaped = DateTime.UtcNow;
                }
                RpcMurderPlayer(player, target);
                abilityUsed = true;
                fullCooldownReset = true;
            }
            else
            {
                abilityUsed = true;
                fullCooldownReset = true;
            }

            var reset = new List<bool>();
            reset.Add(fullCooldownReset);
            reset.Add(gaReset);
            reset.Add(false);
            reset.Add(zeroSecReset);
            reset.Add(abilityUsed);
            return reset;
        }

        public static bool AbilityUsed(PlayerControl player, PlayerControl target = null)
        {
            if (player.IsHacked())
            {
                Coroutines.Start(AbilityCoroutine.Hack(player));
                return false;
            }
            if (player.IsFreezed())
            {
                foreach (var role in Role.GetRoles(RoleEnum.Icenberg))
                {
                    var icenberg = (Icenberg)role;
                    Coroutines.Start(AbilityCoroutineIcenberg.Freeze(icenberg, PlayerControl.LocalPlayer));
                }
                return false;
            }
            var targetId = byte.MaxValue;
            if (target != null) targetId = target.PlayerId;
            Rpc(CustomRPC.AbilityTrigger, player.PlayerId, targetId);
            return true;
        }

        public static Il2CppSystem.Collections.Generic.List<PlayerControl> GetClosestPlayers(Vector2 truePosition, float radius, bool includeDead)
        {
            Il2CppSystem.Collections.Generic.List<PlayerControl> playerControlList = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            float lightRadius = radius * ShipStatus.Instance.MaxLightRadius;
            Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> allPlayers = GameData.Instance.AllPlayers;
            for (int index = 0; index < allPlayers.Count; ++index)
            {
                NetworkedPlayerInfo playerInfo = allPlayers[index];
                if (!playerInfo.Disconnected && (!playerInfo.Object.Data.IsDead || includeDead))
                {
                    Vector2 vector2 = new Vector2(playerInfo.Object.GetTruePosition().x - truePosition.x, playerInfo.Object.GetTruePosition().y - truePosition.y);
                    float magnitude = ((Vector2)vector2).magnitude;
                    if (magnitude <= lightRadius)
                    {
                        PlayerControl playerControl = playerInfo.Object;
                        playerControlList.Add(playerControl);
                    }
                }
            }
            return playerControlList;
        }

        public static PlayerControl GetClosestPlayer(PlayerControl refPlayer, List<PlayerControl> AllPlayers, bool blockPhysics = false)
        {
            if (!refPlayer.moveable) return null;
            var num = double.MaxValue;
            var refPosition = refPlayer.GetTruePosition();
            PlayerControl result = null;
            foreach (var player in AllPlayers)
            {
                if (player.Data.IsDead || player.PlayerId == refPlayer.PlayerId || !player.Collider.enabled || player.inVent) continue;
                var playerPosition = player.GetTruePosition();
                var distBetweenPlayers = Vector2.Distance(refPosition, playerPosition);
                var isClosest = distBetweenPlayers < num;
                if (!isClosest) continue;
                var vector = playerPosition - refPosition;
                if (!blockPhysics && PhysicsHelpers.AnyNonTriggersBetween(
                    refPosition, vector.normalized, vector.magnitude, Constants.ShipAndObjectsMask
                )) continue;
                num = distBetweenPlayers;
                result = player;
            }

            return result;
        }
        public static void SetTarget(
            ref PlayerControl closestPlayer,
            KillButton button,
            float maxDistance = float.NaN,
            List<PlayerControl> targets = null
        )
        {
            if (!button.isActiveAndEnabled) return;

            button.SetTarget(
                SetClosestPlayer(ref closestPlayer, maxDistance, targets)
            );
        }

        public static PlayerControl SetClosestPlayer(
            ref PlayerControl closestPlayer,
            float maxDistance = float.NaN,
            List<PlayerControl> targets = null
        )
        {
            if (float.IsNaN(maxDistance))
                maxDistance = LegacyGameOptions.KillDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
            var player = GetClosestPlayer(
                PlayerControl.LocalPlayer,
                targets ?? PlayerControl.AllPlayerControls.ToArray().ToList()
            );
            var closeEnough = player == null || (
                GetDistBetweenPlayers(PlayerControl.LocalPlayer, player) < maxDistance
            );
            return closestPlayer = closeEnough ? player : null;
        }

        public static double GetDistBetweenPlayers(PlayerControl player, PlayerControl refplayer)
        {
            var truePosition = refplayer.GetTruePosition();
            var truePosition2 = player.GetTruePosition();
            return Vector2.Distance(truePosition, truePosition2);
        }

        public static void RpcMurderPlayer(PlayerControl killer, PlayerControl target)
        {
            MurderPlayer(killer, target, true);
            Rpc(CustomRPC.BypassKill, killer.PlayerId, target.PlayerId);
        }

        public static void RpcMultiMurderPlayer(PlayerControl killer, PlayerControl target)
        {
            MurderPlayer(killer, target, false);
            Rpc(CustomRPC.BypassMultiKill, killer.PlayerId, target.PlayerId);
        }

        public static void MurderPlayer(PlayerControl killer, PlayerControl target, bool jumpToBody = false)
        {
            var data = target.Data;
            if (data != null && !data.IsDead)
            {
                if (ShowShield.DiedFirst == "") ShowShield.DiedFirst = target.GetDefaultOutfit().PlayerName;

                if (target.GetAppearance().SizeFactor == new Vector3(0.4f, 0.4f, 1f))
                {
                    target.transform.localPosition += new Vector3(0f, SizePatch.Radius * 0.75f, 0f);
                }
                else if (killer.GetAppearance().SizeFactor == new Vector3(0.4f, 0.4f, 1f))
                {
                    target.transform.localPosition -= new Vector3(0f, SizePatch.Radius * 0.75f, 0f);
                }

                if (killer.Is(RoleEnum.SoulCollector) && killer != target)
                {
                    var sc = Role.GetRole<SoulCollector>(killer);
                    var bodyPos = target.transform.position;
                    bodyPos.y -= 0.3f;
                    bodyPos.x -= 0.11f;
                    sc.Souls.Add(SoulExtensions.CreateSoul(bodyPos, target));
                }

                if (killer.Is(ModifierEnum.Shy) && killer.GetCustomOutfitType() == CustomPlayerOutfitType.Default)
                {
                    var shy = Modifier.GetModifier<Shy>(killer);
                    shy.Opacity = 1f;
                    ShyHudManagerUpdate.SetVisiblity(killer, shy.Opacity);
                    shy.Moving = true;
                }

                if (target.IsProtected())
                {
                    var ga = target.GetGA();
                    ga.UnProtect();
                }

                if (target.Is(RoleEnum.Jailor) && AmongUsClient.Instance.AmHost && !MeetingHud.Instance)
                {
                    var jailor = Role.GetRole<Jailor>(target);
                    jailor.Jailed = null;
                    Rpc(CustomRPC.Jail, target.PlayerId, (byte)2, byte.MaxValue);
                }

                if (target == PlayerControl.LocalPlayer && target.Is(ModifierEnum.Celebrity))
                {
                    var celeb = Modifier.GetModifier<Celebrity>(target);
                    celeb.GenMessage(killer);
                }

                // I do both cause desync sometimes
                if (PlayerControl.LocalPlayer.Is(RoleEnum.Deputy))
                {
                    var deputy = Role.GetRole<Deputy>(PlayerControl.LocalPlayer);
                    if (target == deputy.Camping)
                    {
                        deputy.Killer = killer;
                        Rpc(CustomRPC.Camp, PlayerControl.LocalPlayer.PlayerId, (byte)1, deputy.Killer.PlayerId);
                        deputy.Camping = null;
                        Coroutines.Start(FlashCoroutine(Color.red));
                    }
                }
                foreach (var role in Role.GetRoles(RoleEnum.Deputy))
                {
                    var dep = (Deputy)role;
                    if (target == dep.Camping)
                    {
                        dep.Killer = killer;
                        dep.Camping = null;
                    }
                }

                if (PlayerControl.LocalPlayer == target)
                {
                    try
                    {
                        PlayerMenu.singleton.Menu.Close();
                    }
                    catch { }
                    try
                    {
                        TraitorMenu.singleton.Menu.Close();
                    }
                    catch { }
                }

                if (target.Is(RoleEnum.Hypnotist))
                {
                    var hypno = Role.GetRole<Hypnotist>(target);
                    hypno.HysteriaActive = false;
                    if (!PlayerControl.LocalPlayer.IsHypnotised()) hypno.UnHysteria();
                }
                if (PlayerControl.LocalPlayer == target && target.IsHypnotised())
                {
                    var unhypno = false;
                    foreach (var role in Role.GetRoles(RoleEnum.Hypnotist))
                    {
                        var hypno = (Hypnotist)role;
                        hypno.HypnotisedPlayers.Remove(target.PlayerId);
                        if (!unhypno)
                        {
                            hypno.UnHysteria();
                            unhypno = true;
                        }
                    }
                }

                int currentOutfitType = 0;
                if (PlayerControl.LocalPlayer == target)
                {
                    target.SetOutfit(CustomPlayerOutfitType.Default);
                    currentOutfitType = (int)killer.CurrentOutfitType;
                    killer.CurrentOutfitType = PlayerOutfitType.Default;
                }

                if (killer == PlayerControl.LocalPlayer)
                    SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.8f);

                if (!killer.Is(Faction.Crewmates) && killer != target
                    && GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal) Role.GetRole(killer).Kills += 1;

                if (killer.Is(RoleEnum.Sheriff))
                {
                    var sheriff = Role.GetRole<Sheriff>(killer);
                    if (target.Is(Faction.Impostors) ||
                        (target.Is(Faction.NeutralEvil) && CustomGameOptions.SheriffKillsNE) ||
                        (target.Is(Faction.NeutralKilling) && CustomGameOptions.SheriffKillsNK)) sheriff.CorrectKills += 1;
                    else if (killer == target) sheriff.IncorrectKills += 1;
                }

                if (killer.Is(RoleEnum.Veteran))
                {
                    var veteran = Role.GetRole<Veteran>(killer);
                    if (!target.Is(Faction.Crewmates)) veteran.CorrectKills += 1;
                    else if (killer != target) veteran.IncorrectKills += 1;
                }

                if (killer.Is(RoleEnum.Hunter))
                {
                    var hunter = Role.GetRole<Hunter>(killer);
                    if (!target.Is(Faction.Crewmates)) hunter.CorrectKills += 1;
                    else if (killer != target) hunter.IncorrectKills += 1;
                }

                target.gameObject.layer = LayerMask.NameToLayer("Ghost");
                target.Visible = false;

                if (PlayerControl.LocalPlayer.Is(RoleEnum.Mystic) && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Coroutines.Start(FlashCoroutine(Patches.Colors.Mystic));
                }

                if (PlayerControl.LocalPlayer.Is(RoleEnum.Detective))
                {
                    var detective = Role.GetRole<Detective>(PlayerControl.LocalPlayer);
                    if (PlayerControl.LocalPlayer != target && !PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        var bodyPos = target.transform.position;
                        bodyPos.y -= 0.3f;
                        bodyPos.x -= 0.11f;
                        detective.CrimeScenes.Add(CrimeSceneExtensions.CreateCrimeScene(bodyPos, target));
                    }
                }

                if (PlayerControl.LocalPlayer == target && PlayerControl.LocalPlayer.Is(RoleEnum.Aurial))
                {
                    var aurial = Role.GetRole<Aurial>(PlayerControl.LocalPlayer);
                    aurial.SenseArrows.Values.DestroyAll();
                    aurial.SenseArrows.Clear();
                }

                if (target.AmOwner)
                {
                    try
                    {
                        if (Minigame.Instance)
                        {
                            Minigame.Instance.Close();
                            Minigame.Instance.Close();
                        }

                        if (MapBehaviour.Instance)
                        {
                            MapBehaviour.Instance.Close();
                            MapBehaviour.Instance.Close();
                        }
                    }
                    catch
                    {
                    }

                    HudManager.Instance.KillOverlay.ShowKillAnimation(killer.Data, data);
                    HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
                    target.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
                    target.RpcSetScanner(false);
                    var importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                    importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
                    if (!GameOptionsManager.Instance.currentNormalGameOptions.GhostsDoTasks)
                    {
                        //GameManager.Instance.LogicFlow.CheckEndCriteria();
                        for (var i = 0; i < target.myTasks.Count; i++)
                        {
                            var playerTask = target.myTasks.ToArray()[i];
                            playerTask.OnRemove();
                            Object.Destroy(playerTask.gameObject);
                        }

                        target.myTasks.Clear();
                        importantTextTask.Text = TranslationController.Instance.GetString(
                            StringNames.GhostIgnoreTasks,
                            new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                    }
                    else
                    {
                        importantTextTask.Text = TranslationController.Instance.GetString(
                            StringNames.GhostDoTasks,
                            new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                    }

                    target.myTasks.Insert(0, importantTextTask);
                }

                if (jumpToBody)
                {
                    killer.MyPhysics.StartCoroutine(killer.KillAnimations.Random().CoPerformKill(killer, target));
                }
                else killer.MyPhysics.StartCoroutine(killer.KillAnimations.Random().CoPerformKill(target, target));

                if (PlayerControl.LocalPlayer == target) killer.CurrentOutfitType = (PlayerOutfitType)currentOutfitType;

                if (target.Is(ModifierEnum.Frosty))
                {
                    var frosty = Modifier.GetModifier<Frosty>(target);
                    frosty.Chilled = killer;
                    frosty.LastChilled = DateTime.UtcNow;
                    frosty.IsChilled = true;
                }

                var deadBody = new DeadPlayer
                {
                    PlayerId = target.PlayerId,
                    KillerId = killer.PlayerId,
                    KillTime = DateTime.UtcNow
                };

                Murder.KilledPlayers.Add(deadBody);

                if (PlayerControl.LocalPlayer.Is(RoleEnum.Scavenger) && killer != PlayerControl.LocalPlayer)
                {
                    var scav = Role.GetRole<Scavenger>(PlayerControl.LocalPlayer);
                    if (scav.Target == target) scav.Target = scav.GetClosestPlayer();
                    scav.RegenTask();
                }

                if (killer.Is(RoleEnum.SoulCollector) && killer != target)
                {
                    foreach (var body in GameObject.FindObjectsOfType<DeadBody>())
                    {
                        if (body.ParentId == target.PlayerId)
                        {
                            if (PlayerControl.LocalPlayer == killer || PlayerControl.LocalPlayer == target) Coroutines.Start(PerformKillButton.RemoveBody(body));
                            else body.gameObject.Destroy();
                        }
                    }
                }

                if (MeetingHud.Instance) target.Exiled();

                if (!killer.AmOwner) return;

                if (target.Is(ModifierEnum.Bait) && !killer.Is(RoleEnum.SoulCollector))
                {
                    BaitReport(killer, target);
                }

                if (target.Is(ModifierEnum.Aftermath))
                {
                    Aftermath.ForceAbility(killer, target);
                }

                if (!jumpToBody) return;

                if (killer.Data.IsImpostor() && GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek)
                {
                    killer.SetKillTimer(GameOptionsManager.Instance.currentHideNSeekGameOptions.KillCooldown);
                    return;
                }

                if (killer == PlayerControl.LocalPlayer && killer.Is(RoleEnum.Warlock))
                {
                    var warlock = Role.GetRole<Warlock>(killer);
                    if (warlock.Charging)
                    {
                        warlock.UsingCharge = true;
                        warlock.ChargeUseDuration = warlock.ChargePercent * CustomGameOptions.ChargeUseDuration / 100f;
                        if (warlock.ChargeUseDuration == 0f) warlock.ChargeUseDuration += 0.01f;
                    }
                    killer.SetKillTimer(0.01f);
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Is(RoleEnum.Werewolf))
                {
                    var werewolf = Role.GetRole<Werewolf>(killer);
                    werewolf.LastKilled = DateTime.UtcNow.AddSeconds((CustomGameOptions.DiseasedMultiplier - 1f) * CustomGameOptions.RampageKillCd);
                    werewolf.Player.SetKillTimer(CustomGameOptions.RampageKillCd * CustomGameOptions.DiseasedMultiplier);
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Is(RoleEnum.SoulCollector))
                {
                    var sc = Role.GetRole<SoulCollector>(killer);
                    sc.LastReaped = DateTime.UtcNow.AddSeconds((CustomGameOptions.DiseasedMultiplier - 1f) * CustomGameOptions.ReapCd);
                    sc.Player.SetKillTimer(CustomGameOptions.ReapCd * CustomGameOptions.DiseasedMultiplier);
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Is(RoleEnum.Vampire))
                {
                    var vampire = Role.GetRole<Vampire>(killer);
                    vampire.LastBit = DateTime.UtcNow.AddSeconds((CustomGameOptions.DiseasedMultiplier - 1f) * CustomGameOptions.BiteCd);
                    vampire.Player.SetKillTimer(CustomGameOptions.BiteCd * CustomGameOptions.DiseasedMultiplier);
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Is(RoleEnum.Glitch))
                {
                    var glitch = Role.GetRole<Glitch>(killer);
                    glitch.LastKill = DateTime.UtcNow.AddSeconds((CustomGameOptions.DiseasedMultiplier - 1f) * CustomGameOptions.GlitchKillCooldown);
                    glitch.Player.SetKillTimer(CustomGameOptions.GlitchKillCooldown * CustomGameOptions.DiseasedMultiplier);
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Is(RoleEnum.Icenberg))
                {
                    var icenberg = Role.GetRole<Icenberg>(killer);
                    icenberg.LastKill = DateTime.UtcNow.AddSeconds((CustomGameOptions.DiseasedMultiplier - 1f) * CustomGameOptions.IcenbergKillCooldown);
                    icenberg.Player.SetKillTimer(CustomGameOptions.IcenbergKillCooldown * CustomGameOptions.DiseasedMultiplier);
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Is(RoleEnum.Juggernaut))
                {
                    var juggernaut = Role.GetRole<Juggernaut>(killer);
                    juggernaut.LastKill = DateTime.UtcNow.AddSeconds((CustomGameOptions.DiseasedMultiplier - 1f) * (CustomGameOptions.JuggKCd - CustomGameOptions.ReducedKCdPerKill * juggernaut.JuggKills));
                    juggernaut.Player.SetKillTimer((CustomGameOptions.JuggKCd - CustomGameOptions.ReducedKCdPerKill * juggernaut.JuggKills) * CustomGameOptions.DiseasedMultiplier);
                    return;
                }

                if (killer.Is(RoleEnum.Scavenger))
                {
                    var scav = Role.GetRole<Scavenger>(killer);
                    if (target == scav.Target)
                    {
                        if (target.Is(ModifierEnum.Diseased))
                        {
                            killer.SetKillTimer(CustomGameOptions.ScavengeCorrectKillCooldown * CustomGameOptions.DiseasedMultiplier);
                        }
                        else
                        {
                            killer.SetKillTimer(CustomGameOptions.ScavengeCorrectKillCooldown);
                        }
                        scav.Target = scav.GetClosestPlayer();
                        scav.ScavengeEnd = scav.ScavengeEnd.AddSeconds(CustomGameOptions.ScavengeIncreaseDuration);
                    }
                    else
                    {
                        if (target.Is(ModifierEnum.Diseased) && killer.Is(ModifierEnum.Underdog))
                        {
                            var lowerKC = (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown - CustomGameOptions.UnderdogKillBonus) * CustomGameOptions.DiseasedMultiplier * CustomGameOptions.ScavengeIncorrectKillCooldown;
                            var normalKC = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * CustomGameOptions.DiseasedMultiplier * CustomGameOptions.ScavengeIncorrectKillCooldown;
                            var upperKC = (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + CustomGameOptions.UnderdogKillBonus) * CustomGameOptions.DiseasedMultiplier * CustomGameOptions.ScavengeIncorrectKillCooldown;
                            killer.SetKillTimer(PerformKill.LastImp() ? lowerKC : (PerformKill.IncreasedKC() ? normalKC : upperKC));
                        }
                        else if (target.Is(ModifierEnum.Diseased))
                        {
                            killer.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * CustomGameOptions.DiseasedMultiplier * CustomGameOptions.ScavengeIncorrectKillCooldown);
                        }
                        else if (killer.Is(ModifierEnum.Underdog))
                        {
                            var lowerKC = (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown - CustomGameOptions.UnderdogKillBonus) * CustomGameOptions.ScavengeIncorrectKillCooldown;
                            var normalKC = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * CustomGameOptions.ScavengeIncorrectKillCooldown;
                            var upperKC = (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + CustomGameOptions.UnderdogKillBonus) * CustomGameOptions.ScavengeIncorrectKillCooldown;
                            killer.SetKillTimer(PerformKill.LastImp() ? lowerKC : (PerformKill.IncreasedKC() ? normalKC : upperKC));
                        }
                        else killer.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * CustomGameOptions.ScavengeIncorrectKillCooldown);
                        scav.StopScavenge();
                        scav.ScavengeEnd = scav.ScavengeEnd.AddSeconds(-3000f);
                    }
                    scav.RegenTask();
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Is(ModifierEnum.Underdog))
                {
                    var lowerKC = (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown - CustomGameOptions.UnderdogKillBonus) * CustomGameOptions.DiseasedMultiplier;
                    var normalKC = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * CustomGameOptions.DiseasedMultiplier;
                    var upperKC = (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + CustomGameOptions.UnderdogKillBonus) * CustomGameOptions.DiseasedMultiplier;
                    killer.SetKillTimer(PerformKill.LastImp() ? lowerKC : (PerformKill.IncreasedKC() ? normalKC : upperKC));
                    return;
                }

                if (target.Is(ModifierEnum.Diseased) && killer.Data.IsImpostor())
                {
                    killer.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * CustomGameOptions.DiseasedMultiplier);
                    return;
                }

                if (killer.Is(ModifierEnum.Underdog))
                {
                    var lowerKC = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown - CustomGameOptions.UnderdogKillBonus;
                    var normalKC = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
                    var upperKC = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + CustomGameOptions.UnderdogKillBonus;
                    killer.SetKillTimer(PerformKill.LastImp() ? lowerKC : (PerformKill.IncreasedKC() ? normalKC : upperKC));
                    return;
                }

                if (killer.Data.IsImpostor())
                {
                    killer.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
                    return;
                }
            }
        }

        public static void BaitReport(PlayerControl killer, PlayerControl target)
        {
            Coroutines.Start(BaitReportDelay(killer, target));
        }

        public static IEnumerator BaitReportDelay(PlayerControl killer, PlayerControl target)
        {
            var extraDelay = Random.RandomRangeInt(0, (int) (100 * (CustomGameOptions.BaitMaxDelay - CustomGameOptions.BaitMinDelay) + 1));
            if (CustomGameOptions.BaitMaxDelay <= CustomGameOptions.BaitMinDelay)
                yield return new WaitForSeconds(CustomGameOptions.BaitMaxDelay + 0.01f);
            else
                yield return new WaitForSeconds(CustomGameOptions.BaitMinDelay + 0.01f + extraDelay/100f);
            var bodies = Object.FindObjectsOfType<DeadBody>();
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var body in bodies)
                {
                    try
                    {
                        if (body.ParentId == target.PlayerId) { killer.ReportDeadBody(target.Data); break; }
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                foreach (var body in bodies)
                {
                    try
                    {
                        if (body.ParentId == target.PlayerId)
                        {
                            Rpc(CustomRPC.BaitReport, killer.PlayerId, target.PlayerId);
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static IEnumerator FlashCoroutine(Color color, float waitfor = 1f, float alpha = 0.3f)
        {
            color.a = alpha;
            if (HudManager.InstanceExists && HudManager.Instance.FullScreen)
            {
                var fullscreen = HudManager.Instance.FullScreen;
                fullscreen.enabled = true;
                fullscreen.gameObject.active = true;
                fullscreen.color = color;
            }

            yield return new WaitForSeconds(waitfor);

            if (HudManager.InstanceExists && HudManager.Instance.FullScreen)
            {
                var fullscreen = HudManager.Instance.FullScreen;
                if (fullscreen.color.Equals(color))
                {
                    fullscreen.color = new Color(1f, 0f, 0f, 0.37254903f);
                    fullscreen.gameObject.SetActive(false);
                }
            }
        }

        public static IEnumerable<(T1, T2)> Zip<T1, T2>(List<T1> first, List<T2> second)
        {
            return first.Zip(second, (x, y) => (x, y));
        }

        public static IEnumerable<GameObject> GetAllChilds(this GameObject Go)
        {
            for (var i = 0; i < Go.transform.childCount; i++)
            {
                yield return Go.transform.GetChild(i).gameObject;
            }
        }

        public static void RemoveTasks(PlayerControl player)
        {
            var totalTasks = GameOptionsManager.Instance.currentNormalGameOptions.NumCommonTasks + GameOptionsManager.Instance.currentNormalGameOptions.NumLongTasks +
                             GameOptionsManager.Instance.currentNormalGameOptions.NumShortTasks;


            foreach (var task in player.myTasks)
                if (task.TryCast<NormalPlayerTask>() != null)
                {
                    var normalPlayerTask = task.Cast<NormalPlayerTask>();

                    var updateArrow = normalPlayerTask.taskStep > 0;

                    normalPlayerTask.taskStep = 0;
                    normalPlayerTask.Initialize();
                    if (normalPlayerTask.TaskType == TaskTypes.PickUpTowels)
                        foreach (var console in Object.FindObjectsOfType<TowelTaskConsole>())
                            console.Image.color = Color.white;
                    normalPlayerTask.taskStep = 0;
                    if (normalPlayerTask.TaskType == TaskTypes.UploadData)
                        normalPlayerTask.taskStep = 1;
                    if ((normalPlayerTask.TaskType == TaskTypes.EmptyGarbage || normalPlayerTask.TaskType == TaskTypes.EmptyChute)
                        && (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 0 ||
                        GameOptionsManager.Instance.currentNormalGameOptions.MapId == 3 ||
                        GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4))
                        normalPlayerTask.taskStep = 1;
                    if (updateArrow)
                        normalPlayerTask.UpdateArrowAndLocation();

                    var taskInfo = player.Data.FindTaskById(task.Id);
                    taskInfo.Complete = false;
                }
        }

        public static void DestroyAll(this IEnumerable<Component> listie)
        {
            foreach (var item in listie)
            {
                if (item == null) continue;
                Object.Destroy(item);
                if (item.gameObject == null) return;
                Object.Destroy(item.gameObject);
            }
        }

        public static void EndGame(GameOverReason reason = GameOverReason.ImpostorsByVote, bool showAds = false)
        {
            GameManager.Instance.RpcEndGame(reason, showAds);
        }


        public static void Rpc(params object[] data)
        {
            if (data[0] is not CustomRPC) throw new ArgumentException($"first parameter must be a {typeof(CustomRPC).FullName}");

            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte)(CustomRPC)data[0], SendOption.Reliable, -1);

            if (data.Length == 1)
            {
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                return;
            }

            foreach (var item in data[1..])
            {

                if (item is bool boolean)
                {
                    writer.Write(boolean);
                }
                else if (item is int integer)
                {
                    writer.Write(integer);
                }
                else if (item is uint uinteger)
                {
                    writer.Write(uinteger);
                }
                else if (item is float Float)
                {
                    writer.Write(Float);
                }
                else if (item is byte Byte)
                {
                    writer.Write(Byte);
                }
                else if (item is sbyte sByte)
                {
                    writer.Write(sByte);
                }
                else if (item is Vector2 vector)
                {
                    writer.Write(vector);
                }
                else if (item is Vector3 vector3)
                {
                    writer.Write(vector3);
                }
                else if (item is string str)
                {
                    writer.Write(str);
                }
                else if (item is byte[] array)
                {
                    writer.WriteBytesAndSize(array);
                }
                else
                {
                    Logger<TownOfUs>.Error($"unknown data type entered for rpc write: item - {nameof(item)}, {item.GetType().FullName}, rpc - {data[0]}");
                }
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        [HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.FixedUpdate))]
        class MedScanMinigameFixedUpdatePatch
        {
            static void Prefix(MedScanMinigame __instance)
            {
                if (CustomGameOptions.ParallelMedScans)
                {
                    //Allows multiple medbay scans at once
                    __instance.medscan.CurrentUser = PlayerControl.LocalPlayer.PlayerId;
                    __instance.medscan.UsersList.Clear();
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
        class StartMeetingPatch {
            public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo meetingTarget) {
                voteTarget = meetingTarget;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        class MeetingHudUpdatePatch {
            static void Postfix(MeetingHud __instance) {
                // Deactivate skip Button if skipping on emergency meetings is disabled
                if ((voteTarget == null && CustomGameOptions.SkipButtonDisable == DisableSkipButtonMeetings.Emergency) || (CustomGameOptions.SkipButtonDisable == DisableSkipButtonMeetings.Always)) {
                    __instance.SkipVoteButton.gameObject.SetActive(false);
                }
            }
        }

        //Submerged utils
        public static object TryCast(this Il2CppObjectBase self, Type type)
        {
            return AccessTools.Method(self.GetType(), nameof(Il2CppObjectBase.TryCast)).MakeGenericMethod(type).Invoke(self, Array.Empty<object>());
        }
        public static IList createList(Type myType)
        {
            Type genericListType = typeof(List<>).MakeGenericType(myType);
            return (IList)Activator.CreateInstance(genericListType);
        }

        public static void ResetCustomTimers()
        {
            #region CrewmateRoles
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Medium))
            {
                var medium = Role.GetRole<Medium>(PlayerControl.LocalPlayer);
                medium.LastMediated = DateTime.UtcNow;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Medium))
            {
                var medium = (Medium)role;
                medium.MediatedPlayers.Values.DestroyAll();
                medium.MediatedPlayers.Clear();
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.TimeLord))
            {
                var timelord = Role.GetRole<TimeLord>(PlayerControl.LocalPlayer);
                timelord.StartRewind = DateTime.UtcNow.AddSeconds(-10.0f);
                timelord.FinishRewind = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Seer))
            {
                var seer = Role.GetRole<Seer>(PlayerControl.LocalPlayer);
                seer.LastInvestigated = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Oracle))
            {
                var oracle = Role.GetRole<Oracle>(PlayerControl.LocalPlayer);
                oracle.LastConfessed = DateTime.UtcNow;
                oracle.LastBlessed = DateTime.UtcNow;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Oracle))
            {
                var oracle = (Oracle)role;
                oracle.Blessed = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Sheriff))
            {
                var sheriff = Role.GetRole<Sheriff>(PlayerControl.LocalPlayer);
                sheriff.LastKilled = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Hunter))
            {
                var hunter = Role.GetRole<Hunter>(PlayerControl.LocalPlayer);
                hunter.LastKilled = DateTime.UtcNow;
                hunter.LastStalked = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Cleric))
            {
                var cleric = Role.GetRole<Cleric>(PlayerControl.LocalPlayer);
                cleric.LastBarriered = DateTime.UtcNow;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Cleric))
            {
                var cler = (Cleric)role;
                cler.CleansedPlayers.Clear();
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Tracker))
            {
                var tracker = Role.GetRole<Tracker>(PlayerControl.LocalPlayer);
                tracker.LastTracked = DateTime.UtcNow;
                if (CustomGameOptions.ResetOnNewRound)
                {
                    tracker.TrackerArrows.Values.DestroyAll();
                    tracker.TrackerArrows.Clear();
                    tracker.UsesLeft = CustomGameOptions.MaxTracks;
                }
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Lookout))
            {
                var lo = Role.GetRole<Lookout>(PlayerControl.LocalPlayer);
                lo.LastWatched = DateTime.UtcNow;
                if (CustomGameOptions.LoResetOnNewRound)
                {
                    lo.UsesLeft = CustomGameOptions.MaxWatches;
                    lo.Watching.Clear();
                }
                else
                {
                    List<byte> toRemove = new List<byte>();
                    foreach (var (key, value) in lo.Watching)
                    {
                        value.Clear();
                        if (PlayerById(key).Data.IsDead || PlayerById(key).Data.Disconnected) toRemove.Add(key);
                    }
                    foreach (var key in toRemove)
                    {
                        lo.Watching.Remove(key);
                    }
                }
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Transporter))
            {
                var transporter = Role.GetRole<Transporter>(PlayerControl.LocalPlayer);
                transporter.LastTransported = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Veteran))
            {
                var veteran = Role.GetRole<Veteran>(PlayerControl.LocalPlayer);
                veteran.LastAlerted = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Trapper))
            {
                var trapper = Role.GetRole<Trapper>(PlayerControl.LocalPlayer);
                trapper.LastTrapped = DateTime.UtcNow;
                trapper.trappedPlayers.Clear();
                if (CustomGameOptions.TrapsRemoveOnNewRound)
                {
                    trapper.traps.ClearTraps();
                    trapper.UsesLeft = CustomGameOptions.MaxTraps;
                }
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Detective))
            {
                var detective = Role.GetRole<Detective>(PlayerControl.LocalPlayer);
                detective.LastExamined = DateTime.UtcNow;
                detective.ClosestPlayer = null;
                detective.CurrentTarget = null;
                if (PlayerControl.LocalPlayer.Data.IsDead)
                {
                    detective.InvestigatingScene = null;
                    CrimeSceneExtensions.ClearCrimeScenes(detective.CrimeScenes);
                }
            }
            foreach (var role in Role.GetRoles(RoleEnum.Imitator))
            {
                var imitator = (Imitator)role;
                imitator.trappedPlayers = null;
                imitator.watchedPlayers = null;
                imitator.confessingPlayer = null;
                imitator.jailedPlayer = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Politician))
            {
                var politician = Role.GetRole<Politician>(PlayerControl.LocalPlayer);
                politician.LastCampaigned = DateTime.UtcNow;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Altruist))
            {
                var altruist = (Altruist)role;
                altruist.UsedThisRound = false;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Deputy))
            {
                var deputy = (Deputy)role;
                deputy.Camping = null;
                deputy.Killer = null;
                deputy.CampedThisRound = false;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Jailor))
            {
                var jailor = Role.GetRole<Jailor>(PlayerControl.LocalPlayer);
                jailor.LastJailed = DateTime.UtcNow;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Jailor))
            {
                var jailor = (Jailor)role;
                jailor.Jailed = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Plumber))
            {
                var plumber = Role.GetRole<Plumber>(PlayerControl.LocalPlayer);
                plumber.LastFlushed = DateTime.UtcNow;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Plumber))
            {
                var plumber = (Plumber)role;
                foreach (var ventId in plumber.FutureBlocks)
                {
                    plumber.VentsBlocked.Add(ventId);
                    GameObject barricade = new GameObject("Barricade");
                    Vent trueVent = null;
                    foreach (var vent in ShipStatus.Instance.AllVents)
                    {
                        if (vent.Id == ventId) trueVent = vent;
                    }
                    var pos = trueVent.transform.position;
                    if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 5) pos.y -= 0.1f;
                    else if (GameOptionsManager.Instance.currentNormalGameOptions.MapId != 2) pos.y -= 0.04f;
                    pos.z -= 0.00001f;
                    barricade.transform.localPosition = pos;
                    SpriteRenderer render = barricade.AddComponent<SpriteRenderer>();
                    render.sprite = TownOfUs.BarricadeSprite;
                    plumber.Barricades.Add(barricade);
                }
                plumber.FutureBlocks.Clear();
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Captain))
            {
                var cap = Role.GetRole<Captain>(PlayerControl.LocalPlayer);
                cap.Cooldown = CustomGameOptions.ZoomCooldown;
            }
            #endregion
            #region NeutralRoles
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Survivor))
            {
                var surv = Role.GetRole<Survivor>(PlayerControl.LocalPlayer);
                surv.LastVested = DateTime.UtcNow;
                surv.Locations.Clear();
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Mercenary))
            {
                var merc = Role.GetRole<Mercenary>(PlayerControl.LocalPlayer);
                merc.LastGuarded = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Jester))
            {
                var jest = Role.GetRole<Jester>(PlayerControl.LocalPlayer);
                jest.Locations.Clear();
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Vampire))
            {
                var vamp = Role.GetRole<Vampire>(PlayerControl.LocalPlayer);
                vamp.LastBit = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.GuardianAngel))
            {
                var ga = Role.GetRole<GuardianAngel>(PlayerControl.LocalPlayer);
                ga.LastProtected = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Arsonist))
            {
                var arsonist = Role.GetRole<Arsonist>(PlayerControl.LocalPlayer);
                arsonist.LastDoused = DateTime.UtcNow;
            }
            foreach (var role in Role.GetRoles(RoleEnum.Glitch))
            {
                var glitch = (Glitch)role;
                glitch.Hacked = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Glitch))
            {
                var glitch = Role.GetRole<Glitch>(PlayerControl.LocalPlayer);
                glitch.LastKill = DateTime.UtcNow;
                glitch.LastHack = DateTime.UtcNow;
                glitch.LastMimic = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Icenberg))
            {
                var icenberg = Role.GetRole<Icenberg>(PlayerControl.LocalPlayer);
                icenberg.LastKill = DateTime.UtcNow;
                icenberg.LastFreeze = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Juggernaut))
            {
                var juggernaut = Role.GetRole<Juggernaut>(PlayerControl.LocalPlayer);
                juggernaut.LastKill = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Werewolf))
            {
                var werewolf = Role.GetRole<Werewolf>(PlayerControl.LocalPlayer);
                werewolf.LastRampaged = DateTime.UtcNow;
                werewolf.LastKilled = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Plaguebearer))
            {
                var plaguebearer = Role.GetRole<Plaguebearer>(PlayerControl.LocalPlayer);
                plaguebearer.LastInfected = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Pestilence))
            {
                var pest = Role.GetRole<Pestilence>(PlayerControl.LocalPlayer);
                pest.LastKill = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Foreteller))
            {
                var doom = Role.GetRole<Foreteller>(PlayerControl.LocalPlayer);
                doom.LastObserved = DateTime.UtcNow;
                doom.LastObservedPlayer = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.SoulCollector))
            {
                var sc = Role.GetRole<SoulCollector>(PlayerControl.LocalPlayer);
                sc.LastReaped = DateTime.UtcNow;
                sc.ClosestPlayer = null;
            }
            foreach (var role in Role.GetRoles(RoleEnum.SoulCollector))
            {
                var sc = (SoulCollector)role;
                SoulExtensions.ClearSouls(sc.Souls);
            }
            #endregion
            #region ImposterRoles
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Escapist))
            {
                var escapist = Role.GetRole<Escapist>(PlayerControl.LocalPlayer);
                escapist.LastEscape = DateTime.UtcNow;
                escapist.EscapeButton.graphic.sprite = TownOfUs.MarkSprite;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Blackmailer))
            {
                var blackmailer = Role.GetRole<Blackmailer>(PlayerControl.LocalPlayer);
                blackmailer.LastBlackmailed = DateTime.UtcNow;
                if (blackmailer.Player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    blackmailer.Blackmailed?.myRend().material.SetFloat("_Outline", 0f);
                }
            }
            foreach (var role in Role.GetRoles(RoleEnum.Blackmailer))
            {
                var blackmailer = (Blackmailer)role;
                blackmailer.Blackmailed = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Hypnotist))
            {
                var hypnotist = Role.GetRole<Hypnotist>(PlayerControl.LocalPlayer);
                hypnotist.LastHypnotised = DateTime.UtcNow;
            }
            var hasHypnoed = false;
            foreach (var role in Role.GetRoles(RoleEnum.Hypnotist))
            {
                var hypno = (Hypnotist)role;
                if ((PlayerControl.LocalPlayer.Data.IsDead || hypno.Player.Data.IsDead) && hypno.HysteriaActive && hypno.HypnotisedPlayers.Contains(PlayerControl.LocalPlayer.PlayerId))
                {
                    hypno.HysteriaActive = false;
                    if (!PlayerControl.LocalPlayer.IsHypnotised()) hypno.UnHysteria();
                }
                else if (hypno.HysteriaActive && hypno.HypnotisedPlayers.Contains(PlayerControl.LocalPlayer.PlayerId) && !hasHypnoed)
                {
                    hypno.Hysteria();
                    hasHypnoed = true;
                }
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Bomber))
            {
                var bomber = Role.GetRole<Bomber>(PlayerControl.LocalPlayer);
                bomber.PlantButton.graphic.sprite = TownOfUs.PlantSprite;
                bomber.Bomb.ClearBomb();
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Kamikaze))
            {
                var kami = Role.GetRole<Kamikaze>(PlayerControl.LocalPlayer);
                kami.PlantButton.graphic.sprite = TownOfUs.PlantSprite;
                kami.Bomb.ClearBomb();
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Grenadier))
            {
                var grenadier = Role.GetRole<Grenadier>(PlayerControl.LocalPlayer);
                grenadier.LastFlashed = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Miner))
            {
                var miner = Role.GetRole<Miner>(PlayerControl.LocalPlayer);
                miner.LastMined = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Morphling))
            {
                var morphling = Role.GetRole<Morphling>(PlayerControl.LocalPlayer);
                morphling.LastMorphed = DateTime.UtcNow;
                morphling.MorphButton.graphic.sprite = TownOfUs.SampleSprite;
                morphling.SampledPlayer = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Swooper))
            {
                var swooper = Role.GetRole<Swooper>(PlayerControl.LocalPlayer);
                swooper.LastSwooped = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Venerer))
            {
                var venerer = Role.GetRole<Venerer>(PlayerControl.LocalPlayer);
                venerer.LastCamouflaged = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Undertaker))
            {
                var undertaker = Role.GetRole<Undertaker>(PlayerControl.LocalPlayer);
                undertaker.LastDragged = DateTime.UtcNow;
                undertaker.DragDropButton.graphic.sprite = TownOfUs.DragSprite;
                undertaker.CurrentlyDragging = null;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Eclipsal))
            {
                var eclipsal = Role.GetRole<Eclipsal>(PlayerControl.LocalPlayer);
                eclipsal.LastBlind = DateTime.UtcNow;
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Wraith))
            {
                var wraith = Role.GetRole<Wraith>(PlayerControl.LocalPlayer);
                wraith.LastNoclip = DateTime.UtcNow;
            }
            #endregion
            #region Modifiers
            foreach (var immove in Modifier.GetModifiers(ModifierEnum.Immovable))
            {
                var immovable = (Immovable)immove;
                var player = immovable.Player;
                if (immovable.Location == Vector3.zero || player == null || player.Data == null ||
                    player.Data.IsDead || player.Data.Disconnected) continue;
                player.transform.localPosition = immovable.Location;
                player.NetTransform.SnapTo(immovable.Location);
                if (SubmergedCompatibility.isSubmerged())
                {
                    SubmergedCompatibility.ChangeFloor(player.GetTruePosition().y > -7);
                    SubmergedCompatibility.CheckOutOfBoundsElevator(PlayerControl.LocalPlayer);
                }
            }
            if (PlayerControl.LocalPlayer.Is(ModifierEnum.Taskmaster) && !PlayerControl.LocalPlayer.Is(RoleEnum.Vampire) &&
                !PlayerControl.LocalPlayer.Is(RoleEnum.Traitor) && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                var taskinfos = PlayerControl.LocalPlayer.Data.Tasks.ToArray();
                var tasksLeft = taskinfos.Count(x => !x.Complete);
                if (tasksLeft != 0)
                {
                    var i = Random.RandomRangeInt(PlayerControl.LocalPlayer.myTasks.Count - taskinfos.Count, PlayerControl.LocalPlayer.myTasks.Count);
                    while (true)
                    {
                        var task = PlayerControl.LocalPlayer.myTasks[i];
                        if (task.TryCast<NormalPlayerTask>() != null)
                        {
                            var normalPlayerTask = task.Cast<NormalPlayerTask>();

                            if (normalPlayerTask.IsComplete)
                            {
                                i++;
                                if (i >= PlayerControl.LocalPlayer.myTasks.Count) i = 0;
                                continue;
                            }

                            if (normalPlayerTask.TaskType == TaskTypes.PickUpTowels)
                            {
                                normalPlayerTask.Data = new Il2CppStructArray<byte>([250, 250, 250, 250, 250, 250, 250, 250]);
                                foreach (var console in Object.FindObjectsOfType<TowelTaskConsole>())
                                    console.Image.color = Color.clear;
                            }
                            while (normalPlayerTask.taskStep < normalPlayerTask.MaxStep) normalPlayerTask.NextStep();

                            break;
                        }
                        else
                        {
                            i++;
                            if (i >= PlayerControl.LocalPlayer.myTasks.Count) i = 0;
                        }
                    }
                }
            }
            #endregion
        }

        public static Dictionary<T2, T3> TryToDictionary<T1, T2, T3>(this IEnumerable<T1> source, Func<T1, T2> keySelector, Func<T1, T3> valueSelector)
        {
            var dict = new Dictionary<T2, T3>();

            foreach (var item in source)
                dict.TryAdd(keySelector(item), valueSelector(item));

            return dict;
        }
    }
}
