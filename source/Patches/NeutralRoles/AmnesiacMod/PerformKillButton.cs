using HarmonyLib;
using TownOfUs.CrewmateRoles.InvestigatorMod;
using TownOfUs.CrewmateRoles.SnitchMod;
using TownOfUs.CrewmateRoles.TrapperMod;
using TownOfUs.Roles;
using UnityEngine;
using System;
using TownOfUs.Extensions;
using TownOfUs.CrewmateRoles.ImitatorMod;
using AmongUs.GameOptions;
using TownOfUs.Roles.Modifiers;
using TownOfUs.ImpostorRoles.BomberMod;
using TownOfUs.Patches;

namespace TownOfUs.NeutralRoles.AmnesiacMod
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    public class PerformKillButton
    {
        public static Sprite Sprite => TownOfUs.Arrow;
        public static bool Prefix(KillButton __instance)
        {
            if (__instance != HudManager.Instance.KillButton) return true;
            var flag = PlayerControl.LocalPlayer.Is(RoleEnum.Amnesiac);
            if (!flag) return true;
            if (!PlayerControl.LocalPlayer.CanMove) return false;
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;
            var role = Role.GetRole<Amnesiac>(PlayerControl.LocalPlayer);

            var flag2 = __instance.isCoolingDown;
            if (flag2) return false;
            if (!__instance.enabled) return false;
            var maxDistance = LegacyGameOptions.KillDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
            if (role == null)
                return false;
            if (role.CurrentTarget == null)
                return false;
            if (Vector2.Distance(role.CurrentTarget.TruePosition,
                PlayerControl.LocalPlayer.GetTruePosition()) > maxDistance) return false;
            var playerId = role.CurrentTarget.ParentId;
            var player = Utils.PlayerById(playerId);
            var abilityUsed = Utils.AbilityUsed(PlayerControl.LocalPlayer);
            if (!abilityUsed) return false;
            if ((player.IsInfected() || role.Player.IsInfected()) && !player.Is(RoleEnum.Plaguebearer))
            {
                foreach (var pb in Role.GetRoles(RoleEnum.Plaguebearer)) ((Plaguebearer)pb).RpcSpreadInfection(player, role.Player);
            }

            if (AmongUsClient.Instance.AmHost)
            {
                Utils.Rpc(CustomRPC.Remember, PlayerControl.LocalPlayer.PlayerId, playerId, (byte)1);
                Remember(role, player);
            }
            else Utils.Rpc(CustomRPC.Remember, PlayerControl.LocalPlayer.PlayerId, playerId, (byte)0);

            return false;
        }

        public static void Remember(Amnesiac amneRole, PlayerControl other)
        {
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Lookout))
            {
                var lookout = Role.GetRole<Lookout>(PlayerControl.LocalPlayer);
                if (lookout.Watching.ContainsKey(other.PlayerId))
                {
                    if (!lookout.Watching[other.PlayerId].Contains(RoleEnum.Amnesiac)) lookout.Watching[other.PlayerId].Add(RoleEnum.Amnesiac);
                }
            }

            var role = Utils.GetRole(other);
            var amnesiac = amneRole.Player;

            var rememberImp = true;
            var rememberNeut = true;

            Role newRole;

            if (PlayerControl.LocalPlayer == amnesiac)
            {
                var amnesiacRole = Role.GetRole<Amnesiac>(amnesiac);
                amnesiacRole.BodyArrows.Values.DestroyAll();
                amnesiacRole.BodyArrows.Clear();
                try
                {
                    foreach (var body in amnesiacRole.CurrentTarget.bodyRenderers) body.material.SetFloat("_Outline", 0f);
                }
                catch
                {

                }
            }

            switch (role)
            {
                case RoleEnum.Sheriff:
                case RoleEnum.Engineer:
                case RoleEnum.Mayor:
                case RoleEnum.President:
                case RoleEnum.Swapper:
                case RoleEnum.Investigator:
                case RoleEnum.Medic:
                case RoleEnum.Seer:
                case RoleEnum.Spy:
                case RoleEnum.Snitch:
                case RoleEnum.Altruist:
                case RoleEnum.Vigilante:
                case RoleEnum.Veteran:
                case RoleEnum.Crewmate:
                case RoleEnum.Tracker:
                case RoleEnum.Hunter:
                case RoleEnum.Transporter:
                case RoleEnum.Medium:
                case RoleEnum.Mystic:
                case RoleEnum.Trapper:
                case RoleEnum.Detective:
                case RoleEnum.Imitator:
                case RoleEnum.Prosecutor:
                case RoleEnum.Oracle:
                case RoleEnum.Aurial:
                case RoleEnum.Politician:
                case RoleEnum.Warden:
                case RoleEnum.Jailor:
                case RoleEnum.Lookout:
                case RoleEnum.Deputy:
                case RoleEnum.Plumber:
                case RoleEnum.Cleric:
                case RoleEnum.Captain:
                case RoleEnum.TimeLord:

                    rememberImp = false;
                    rememberNeut = false;

                    break;

                case RoleEnum.Jester:
                case RoleEnum.Executioner:
                case RoleEnum.Arsonist:
                case RoleEnum.Amnesiac:
                case RoleEnum.Glitch:
                case RoleEnum.Icenberg:
                case RoleEnum.Juggernaut:
                case RoleEnum.Survivor:
                case RoleEnum.GuardianAngel:
                case RoleEnum.Plaguebearer:
                case RoleEnum.Pestilence:
                case RoleEnum.Werewolf:
                case RoleEnum.Foreteller:
                case RoleEnum.Vampire:
                case RoleEnum.SoulCollector:
                case RoleEnum.Mercenary:

                    rememberImp = false;

                    break;
            }

            newRole = Role.GetRole(other);
            newRole.Player = amnesiac;

            if ((role == RoleEnum.Glitch || role == RoleEnum.Icenberg || role == RoleEnum.Juggernaut || role == RoleEnum.Pestilence ||
                role == RoleEnum.Werewolf) && PlayerControl.LocalPlayer == other)
            {
                HudManager.Instance.KillButton.buttonLabelText.gameObject.SetActive(false);
            }

            if ((role == RoleEnum.Arsonist || role == RoleEnum.Plaguebearer || role == RoleEnum.Pestilence
                 || role == RoleEnum.Grenadier || role == RoleEnum.Eclipsal) && PlayerControl.LocalPlayer == other)
            {
                foreach (var visor in PlayerControl.AllPlayerControls)
                {
                    ShowShield.ClearVisor(visor);
                }
            }

            if (role == RoleEnum.Investigator) Footprint.DestroyAll(Role.GetRole<Investigator>(other));

            if (role == RoleEnum.Snitch) CompleteTask.Postfix(amnesiac);

            if (role == RoleEnum.Detective && PlayerControl.LocalPlayer == other)
            {
                var detecRole = Role.GetRole<Detective>(other);
                foreach (GameObject scene in detecRole.CrimeScenes)
                {
                    UnityEngine.Object.Destroy(scene);
                }
            }

            if (role == RoleEnum.Bomber && PlayerControl.LocalPlayer.Data.IsImpostor())
            {
                if (BombTeammate.TempBomb != null)
                {
                    try { BombExtentions.ClearBomb(BombTeammate.TempBomb); }
                    catch { }
                }
            }

            Role.RoleDictionary.Remove(amnesiac.PlayerId);
            Role.RoleDictionary.Remove(other.PlayerId);
            Role.RoleDictionary.Add(amnesiac.PlayerId, newRole);

            newRole.RegenTask();

            if (StartImitate.ImitatingPlayers.Contains(other.PlayerId))
            {
                StartImitate.ImitatingPlayers.Remove(other.PlayerId);
                StartImitate.ImitatingPlayers.Add(amneRole.Player.PlayerId);
                newRole.AddToRoleHistory(RoleEnum.Imitator);
            }
            else newRole.AddToRoleHistory(newRole.RoleType);

            if (rememberImp == false)
            {
                if (rememberNeut == false)
                {
                    new Crewmate(other);
                }
                else
                {
                    if (role == RoleEnum.Amnesiac || role == RoleEnum.GuardianAngel || role == RoleEnum.Mercenary || role == RoleEnum.Survivor)
                    {
                        var survivor = new Survivor(other);
                        survivor.RegenTask();
                    }
                    else if (role == RoleEnum.Foreteller || role == RoleEnum.Executioner || role == RoleEnum.Jester || role == RoleEnum.SoulCollector)
                    {
                        var jester = new Jester(other);
                        jester.RegenTask();
                    }
                    else
                    {
                        var mercenary = new Mercenary(other);
                        mercenary.Bribed.Add(amnesiac.PlayerId);
                        if (PlayerControl.LocalPlayer == amnesiac) mercenary.Alert = true;
                        mercenary.RegenTask();
                        if (CustomGameOptions.AmneTurnNeutAssassin) new Assassin(amnesiac);
                        if (other.Is(AbilityEnum.Assassin)) Ability.AbilityDictionary.Remove(other.PlayerId);
                    }
                }
            }
            else if (rememberImp == true)
            {
                new Impostor(other);
                amnesiac.Data.Role.TeamType = RoleTeamTypes.Impostor;
                RoleManager.Instance.SetRole(amnesiac, RoleTypes.Impostor);
                amnesiac.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player.Data.IsImpostor() && PlayerControl.LocalPlayer.Data.IsImpostor())
                    {
                        player.nameText().color = Patches.Colors.Impostor;
                    }
                }
                if (CustomGameOptions.AmneTurnImpAssassin) new Assassin(amnesiac);
            }

            if (role == RoleEnum.Snitch)
            {
                var snitchRole = Role.GetRole<Snitch>(amnesiac);
                snitchRole.ImpArrows.DestroyAll();
                snitchRole.SnitchArrows.Values.DestroyAll();
                snitchRole.SnitchArrows.Clear();
                CompleteTask.Postfix(amnesiac);
                if (other.AmOwner)
                    foreach (var player in PlayerControl.AllPlayerControls)
                        player.nameText().color = Color.white;
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Sheriff)
            {
                var sheriffRole = Role.GetRole<Sheriff>(amnesiac);
                sheriffRole.LastKilled = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Engineer)
            {
                var engiRole = Role.GetRole<Engineer>(amnesiac);
                engiRole.UsesLeft = CustomGameOptions.MaxFixes;
            }

            else if (role == RoleEnum.Medic)
            {
                var medicRole = Role.GetRole<Medic>(amnesiac);
                medicRole.ShieldedPlayer = null;
                medicRole.StartingCooldown = medicRole.StartingCooldown.AddSeconds(-10f);
            }

            else if (role == RoleEnum.Mayor)
            {
                var mayorRole = Role.GetRole<Mayor>(amnesiac);
                mayorRole.Revealed = false;
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Politician)
            {
                var pnRole = Role.GetRole<Politician>(amnesiac);
                pnRole.CampaignedPlayers.RemoveRange(0, pnRole.CampaignedPlayers.Count);
                pnRole.LastCampaigned = DateTime.UtcNow;
            }

            else if (role == RoleEnum.President)
            {
                var presidentRole = Role.GetRole<President>(amnesiac);
                presidentRole.VoteBank = CustomGameOptions.PresidentVoteBank;
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Prosecutor)
            {
                var prosRole = Role.GetRole<Prosecutor>(amnesiac);
                prosRole.Prosecuted = false;
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Vigilante)
            {
                var vigiRole = Role.GetRole<Vigilante>(amnesiac);
                vigiRole.RemainingKills = CustomGameOptions.VigilanteKills;
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Veteran)
            {
                var vetRole = Role.GetRole<Veteran>(amnesiac);
                vetRole.UsesLeft = CustomGameOptions.MaxAlerts;
                vetRole.LastAlerted = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Altruist)
            {
                var altruistRole = Role.GetRole<Altruist>(amnesiac);
                altruistRole.UsesLeft = CustomGameOptions.ReviveUses;
                altruistRole.CurrentlyReviving = false;
                altruistRole.UsedThisRound = false;
            }

            else if (role == RoleEnum.Hunter)
            {
                var hunterRole = Role.GetRole<Hunter>(amnesiac);
                hunterRole.UsesLeft = CustomGameOptions.HunterStalkUses;
                hunterRole.LastStalked = DateTime.UtcNow;
                hunterRole.LastKilled = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Cleric)
            {
                var clericRole = Role.GetRole<Cleric>(amnesiac);
                clericRole.LastBarriered = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Tracker)
            {
                var trackerRole = Role.GetRole<Tracker>(amnesiac);
                trackerRole.TrackerArrows.Values.DestroyAll();
                trackerRole.TrackerArrows.Clear();
                trackerRole.UsesLeft = CustomGameOptions.MaxTracks;
                trackerRole.LastTracked = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Captain)
            {
                var capRole = Role.GetRole<Captain>(amnesiac);
                capRole.UsesLeft = CustomGameOptions.ZoomMaxUses;
                capRole.Cooldown = CustomGameOptions.ZoomCooldown;
            }

            else if (role == RoleEnum.TimeLord)
            {
                var timeLord = Role.GetRole<TimeLord>(amnesiac);
                timeLord.UsesLeft = CustomGameOptions.RewindMaxUses;
            }

            else if (role == RoleEnum.Lookout)
            {
                var loRole = Role.GetRole<Lookout>(amnesiac);
                loRole.UsesLeft = CustomGameOptions.MaxWatches;
                loRole.Watching.Clear();
                loRole.LastWatched = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Plumber)
            {
                var plumberRole = Role.GetRole<Plumber>(amnesiac);
                plumberRole.UsesLeft = CustomGameOptions.MaxBarricades;
                plumberRole.FutureBlocks.Clear();
                plumberRole.LastFlushed = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Aurial)
            {
                var aurialRole = Role.GetRole<Aurial>(amnesiac);
                aurialRole.SenseArrows.Values.DestroyAll();
                aurialRole.SenseArrows.Clear();
                if (PlayerControl.LocalPlayer == aurialRole.Player) HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Warden)
            {
                var wardenRole = Role.GetRole<Warden>(amnesiac);
                wardenRole.Fortified = null;
                wardenRole.StartingCooldown = wardenRole.StartingCooldown.AddSeconds(-10f);
            }

            else if (role == RoleEnum.Deputy)
            {
                var deputyRole = Role.GetRole<Deputy>(amnesiac);
                deputyRole.Camping = null;
                deputyRole.Killer = null;
                deputyRole.CampedThisRound = false;
                deputyRole.StartingCooldown = deputyRole.StartingCooldown.AddSeconds(-10f);
            }

            else if (role == RoleEnum.Detective)
            {
                var detectiveRole = Role.GetRole<Detective>(amnesiac);
                detectiveRole.LastExamined = DateTime.UtcNow;
                detectiveRole.CurrentTarget = null;
            }

            else if (role == RoleEnum.SoulCollector)
            {
                var scRole = Role.GetRole<SoulCollector>(amnesiac);
                scRole.LastReaped = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Mystic)
            {
                var mysticRole = Role.GetRole<Mystic>(amnesiac);
                mysticRole.BodyArrows.Values.DestroyAll();
                mysticRole.BodyArrows.Clear();
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Transporter)
            {
                var tpRole = Role.GetRole<Transporter>(amnesiac);
                tpRole.TransportPlayer1 = null;
                tpRole.TransportPlayer2 = null;
                tpRole.LastTransported = DateTime.UtcNow;
                tpRole.UsesLeft = CustomGameOptions.TransportMaxUses;
            }

            else if (role == RoleEnum.Medium)
            {
                var medRole = Role.GetRole<Medium>(amnesiac);
                medRole.MediatedPlayers.Values.DestroyAll();
                medRole.MediatedPlayers.Clear();
                medRole.LastMediated = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Seer)
            {
                var seerRole = Role.GetRole<Seer>(amnesiac);
                seerRole.Investigated.RemoveRange(0, seerRole.Investigated.Count);
                seerRole.LastInvestigated = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Jailor)
            {
                var jailorRole = Role.GetRole<Jailor>(amnesiac);
                jailorRole.LastJailed = DateTime.UtcNow;
                jailorRole.Jailed = null;
                jailorRole.Executes = CustomGameOptions.MaxExecutes;
                jailorRole.CanJail = true;
            }

            else if (role == RoleEnum.Oracle)
            {
                var oracleRole = Role.GetRole<Oracle>(amnesiac);
                oracleRole.Confessor = null;
                oracleRole.Blessed = null;
                oracleRole.LastConfessed = DateTime.UtcNow;
                oracleRole.LastBlessed = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Arsonist)
            {
                var arsoRole = Role.GetRole<Arsonist>(amnesiac);
                arsoRole.DousedPlayers.RemoveRange(0, arsoRole.DousedPlayers.Count);
                arsoRole.LastDoused = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Survivor)
            {
                var survRole = Role.GetRole<Survivor>(amnesiac);
                survRole.LastVested = DateTime.UtcNow;
                survRole.UsesLeft = CustomGameOptions.MaxVests;
                survRole.LastMoved = DateTime.UtcNow;
                survRole.Locations.Clear();
            }

            else if (role == RoleEnum.Jester)
            {
                var jestRole = Role.GetRole<Jester>(amnesiac);
                jestRole.LastMoved = DateTime.UtcNow;
                jestRole.Locations.Clear();
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            else if (role == RoleEnum.Mercenary)
            {
                var mercRole = Role.GetRole<Mercenary>(amnesiac);
                mercRole.LastGuarded = DateTime.UtcNow;
                mercRole.Guarded.Clear();
                mercRole.Bribed.Clear();
                mercRole.Alert = false;
            }

            else if (role == RoleEnum.GuardianAngel)
            {
                var gaRole = Role.GetRole<GuardianAngel>(amnesiac);
                gaRole.LastProtected = DateTime.UtcNow;
                gaRole.UsesLeft = CustomGameOptions.MaxProtects;
            }

            else if (role == RoleEnum.Glitch)
            {
                var glitchRole = Role.GetRole<Glitch>(amnesiac);
                glitchRole.LastKill = DateTime.UtcNow;
                glitchRole.LastHack = DateTime.UtcNow;
                glitchRole.LastMimic = DateTime.UtcNow;
                glitchRole.Hacked = null;
            }

            else if (role == RoleEnum.Icenberg)
            {
                var icenbergRole = Role.GetRole<Icenberg>(amnesiac);
                icenbergRole.LastKill = DateTime.UtcNow;
                icenbergRole.LastFreeze = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Juggernaut)
            {
                var juggRole = Role.GetRole<Juggernaut>(amnesiac);
                juggRole.JuggKills = 0;
                juggRole.LastKill = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Grenadier)
            {
                var grenadeRole = Role.GetRole<Grenadier>(amnesiac);
                grenadeRole.LastFlashed = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Morphling)
            {
                var morphlingRole = Role.GetRole<Morphling>(amnesiac);
                morphlingRole.LastMorphed = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Escapist)
            {
                var escapistRole = Role.GetRole<Escapist>(amnesiac);
                escapistRole.LastEscape = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Eclipsal)
            {
                var eclipsalRole = Role.GetRole<Eclipsal>(amnesiac);
                eclipsalRole.LastBlind = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Swooper)
            {
                var swooperRole = Role.GetRole<Swooper>(amnesiac);
                swooperRole.LastSwooped = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Venerer)
            {
                var venererRole = Role.GetRole<Venerer>(amnesiac);
                venererRole.LastCamouflaged = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Wraith)
            {
                var wraithRole = Role.GetRole<Wraith>(amnesiac);
                wraithRole.LastNoclip = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Blackmailer)
            {
                var blackmailerRole = Role.GetRole<Blackmailer>(amnesiac);
                blackmailerRole.LastBlackmailed = DateTime.UtcNow;
                blackmailerRole.Blackmailed = null;
            }

            else if (role == RoleEnum.Hypnotist)
            {
                var hypnotistRole = Role.GetRole<Hypnotist>(amnesiac);
                hypnotistRole.LastHypnotised = DateTime.UtcNow;
                hypnotistRole.HypnotisedPlayers.RemoveRange(0, hypnotistRole.HypnotisedPlayers.Count);
                hypnotistRole.HysteriaActive = false;
            }

            else if (role == RoleEnum.Miner)
            {
                var minerRole = Role.GetRole<Miner>(amnesiac);
                minerRole.LastMined = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Undertaker)
            {
                var dienerRole = Role.GetRole<Undertaker>(amnesiac);
                dienerRole.LastDragged = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Werewolf)
            {
                var wwRole = Role.GetRole<Werewolf>(amnesiac);
                wwRole.LastRampaged = DateTime.UtcNow;
                wwRole.LastKilled = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Foreteller)
            {
                var foreRole = Role.GetRole<Foreteller>(amnesiac);
                foreRole.LastObserved = DateTime.UtcNow;
                foreRole.LastObservedPlayer = null;
            }

            else if (role == RoleEnum.Plaguebearer)
            {
                var plagueRole = Role.GetRole<Plaguebearer>(amnesiac);
                plagueRole.InfectedPlayers.RemoveRange(0, plagueRole.InfectedPlayers.Count);
                plagueRole.InfectedPlayers.Add(amnesiac.PlayerId);
                plagueRole.LastInfected = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Pestilence)
            {
                var pestRole = Role.GetRole<Pestilence>(amnesiac);
                pestRole.LastKill = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Vampire)
            {
                var vampRole = Role.GetRole<Vampire>(amnesiac);
                vampRole.LastBit = DateTime.UtcNow;
            }

            else if (role == RoleEnum.Trapper)
            {
                var trapperRole = Role.GetRole<Trapper>(amnesiac);
                trapperRole.LastTrapped = DateTime.UtcNow;
                trapperRole.UsesLeft = CustomGameOptions.MaxTraps;
                trapperRole.trappedPlayers.Clear();
                trapperRole.traps.ClearTraps();
            }

            else if (role == RoleEnum.Bomber)
            {
                var bomberRole = Role.GetRole<Bomber>(amnesiac);
                bomberRole.Bomb.ClearBomb();
            }

            else if (!(amnesiac.Is(RoleEnum.Amnesiac) || amnesiac.Is(Faction.Impostors)))
            {
                HudManager.Instance.KillButton.gameObject.SetActive(false);
            }

            var killsList = (newRole.Kills, newRole.CorrectKills, newRole.IncorrectKills, newRole.CorrectAssassinKills, newRole.IncorrectAssassinKills);
            var otherRole = Role.GetRole(other);
            newRole.Kills = otherRole.Kills;
            newRole.CorrectKills = otherRole.CorrectKills;
            newRole.IncorrectKills = otherRole.IncorrectKills;
            newRole.CorrectAssassinKills = otherRole.CorrectAssassinKills;
            newRole.IncorrectAssassinKills = otherRole.IncorrectAssassinKills;
            otherRole.Kills = killsList.Kills;
            otherRole.CorrectKills = killsList.CorrectKills;
            otherRole.IncorrectKills = killsList.IncorrectKills;
            otherRole.CorrectAssassinKills = killsList.CorrectAssassinKills;
            otherRole.IncorrectAssassinKills = killsList.IncorrectAssassinKills;

            if (amnesiac.Is(Faction.Impostors) && (Role.GetRole(amnesiac).formerRole == RoleEnum.None || CustomGameOptions.SnitchSeesTraitor))
            {
                foreach (var snitch in Role.GetRoles(RoleEnum.Snitch))
                {
                    var snitchRole = (Snitch)snitch;
                    if (snitchRole.TasksDone && PlayerControl.LocalPlayer.Is(RoleEnum.Snitch))
                    {
                        var gameObj = new GameObject();
                        var arrow = gameObj.AddComponent<ArrowBehaviour>();
                        gameObj.transform.parent = PlayerControl.LocalPlayer.gameObject.transform;
                        var renderer = gameObj.AddComponent<SpriteRenderer>();
                        renderer.sprite = Sprite;
                        arrow.image = renderer;
                        gameObj.layer = 5;
                        snitchRole.SnitchArrows.Add(amnesiac.PlayerId, arrow);
                    }
                    else if (snitchRole.Revealed && PlayerControl.LocalPlayer == amnesiac)
                    {
                        var gameObj = new GameObject();
                        var arrow = gameObj.AddComponent<ArrowBehaviour>();
                        gameObj.transform.parent = PlayerControl.LocalPlayer.gameObject.transform;
                        var renderer = gameObj.AddComponent<SpriteRenderer>();
                        renderer.sprite = Sprite;
                        arrow.image = renderer;
                        gameObj.layer = 5;
                        snitchRole.ImpArrows.Add(arrow);
                    }
                }
            }
        }
    }
}
