using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using Reactor.Networking.Extensions;
using TownOfUs.CrewmateRoles.MedicMod;
using TownOfUs.CrewmateRoles.SwapperMod;
using TownOfUs.CrewmateRoles.VigilanteMod;
using TownOfUs.CrewmateRoles.JailorMod;
using TownOfUs.NeutralRoles.ForetellerMod;
using TownOfUs.CustomOption;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.AssassinMod;
using TownOfUs.NeutralRoles.ExecutionerMod;
using TownOfUs.NeutralRoles.GuardianAngelMod;
using TownOfUs.ImpostorRoles.MinerMod;
using TownOfUs.CrewmateRoles.HaunterMod;
using TownOfUs.NeutralRoles.PhantomMod;
using TownOfUs.ImpostorRoles.TraitorMod;
using TownOfUs.CrewmateRoles.ImitatorMod;
using TownOfUs.Roles;
using TownOfUs.Roles.Modifiers;
using UnityEngine;
using Coroutine = TownOfUs.ImpostorRoles.JanitorMod.Coroutine;
using Object = UnityEngine.Object;
using PerformKillButton = TownOfUs.NeutralRoles.AmnesiacMod.PerformKillButton;
using Random = UnityEngine.Random;
using TownOfUs.Patches;
using AmongUs.GameOptions;
using TownOfUs.NeutralRoles.VampireMod;
using TownOfUs.CrewmateRoles.MayorMod;
using System.Reflection;
using TownOfUs.ImpostorRoles.BomberMod;
using TownOfUs.CrewmateRoles.HunterMod;
using Il2CppSystem.Linq;
using TownOfUs.CrewmateRoles.DeputyMod;
using TownOfUs.CrewmateRoles.ClericMod;
using TownOfUs.ImpostorRoles.BlackmailerMod;
using TownOfUs.NeutralRoles.IcenbergMod;
using TownOfUs.CrewmateRoles.TimeLordMod;

namespace TownOfUs
{
    public static class RpcHandling
    {
        private static readonly List<(Type, int, bool)> CrewmateInvestigativeRoles = new();
        private static readonly List<(Type, int, bool)> CrewmateKillingRoles = new();
        private static readonly List<(Type, int, bool)> CrewmatePowerRoles = new();
        private static readonly List<(Type, int, bool)> CrewmateProtectiveRoles = new();
        private static readonly List<(Type, int, bool)> CrewmateSupportRoles = new();
        private static readonly List<(Type, int, bool)> NeutralBenignRoles = new();
        private static readonly List<(Type, int, bool)> NeutralEvilRoles = new();
        private static readonly List<(Type, int, bool)> NeutralKillingRoles = new();
        private static readonly List<(Type, int, bool)> ImpostorConcealingRoles = new();
        private static readonly List<(Type, int, bool)> ImpostorKillingRoles = new();
        private static readonly List<(Type, int, bool)> ImpostorSupportRoles = new();
        private static readonly List<(Type, int)> CrewmateModifiers = new();
        private static readonly List<(Type, int)> GlobalModifiers = new();
        private static readonly List<(Type, int)> ImpostorModifiers = new();
        private static readonly List<(Type, int)> AssassinModifiers = new();
        private static readonly List<(Type, CustomRPC, int)> AssassinAbility = new();

        internal static bool Check(int probability)
        {
            if (probability == 0) return false;
            if (probability == 100) return true;
            var num = Random.RandomRangeInt(1, 101);
            return num <= probability;
        }

        private static (Type, int, bool) SelectRole(List<(Type, int, bool)> roles)
        {
            var chosenRoles = roles.Where(x => x.Item2 == 100).ToList();
            if (chosenRoles.Count > 0)
            {
                chosenRoles.Shuffle();
                return chosenRoles[0];
            }

            chosenRoles = roles.Where(x => x.Item2 < 100).ToList();
            int total = chosenRoles.Sum(x => x.Item2);
            int random = Random.RandomRangeInt(1, total + 1);

            int cumulative = 0;
            (Type, int, bool) selectedRole = default;

            foreach (var role in chosenRoles)
            {
                cumulative += role.Item2;
                if (random <= cumulative)
                {
                    selectedRole = role;
                    break;
                }
            }
            return selectedRole;
        }

        private static void GenEachRole(List<NetworkedPlayerInfo> infected)
        {
            var impostors = Utils.GetImpostors(infected);
            var crewmates = Utils.GetCrewmates(impostors);

            var crewRoles = new List<(Type, int, bool)>();
            var impRoles = new List<(Type, int, bool)>();

            // sort out bad lists
            var players = impostors.Count + crewmates.Count;
            List<RoleOptions> crewNKBuckets = [RoleOptions.CrewInvest, RoleOptions.CrewKilling, RoleOptions.CrewPower, RoleOptions.CrewProtective,
                RoleOptions.CrewSupport, RoleOptions.CrewCommon, RoleOptions.CrewSpecial, RoleOptions.CrewRandom, RoleOptions.NeutKilling];
            List<RoleOptions> impBuckets = [RoleOptions.ImpConceal, RoleOptions.ImpKilling, RoleOptions.ImpSupport, RoleOptions.ImpCommon, RoleOptions.ImpRandom];
            List<RoleOptions> buckets = [CustomGameOptions.Slot1, CustomGameOptions.Slot2, CustomGameOptions.Slot3, CustomGameOptions.Slot4];
            var impCount = 0;
            var anySlots = 0;

            if (players > 4) buckets.Add(CustomGameOptions.Slot5);
            if (players > 5) buckets.Add(CustomGameOptions.Slot6);
            if (players > 6) buckets.Add(CustomGameOptions.Slot7);
            if (players > 7) buckets.Add(CustomGameOptions.Slot8);
            if (players > 8) buckets.Add(CustomGameOptions.Slot9);
            if (players > 9) buckets.Add(CustomGameOptions.Slot10);
            if (players > 10) buckets.Add(CustomGameOptions.Slot11);
            if (players > 11) buckets.Add(CustomGameOptions.Slot12);
            if (players > 12) buckets.Add(CustomGameOptions.Slot13);
            if (players > 13) buckets.Add(CustomGameOptions.Slot14);
            if (players > 14) buckets.Add(CustomGameOptions.Slot15);
            if (players > 15)
            {
                for (int i = 0; i < players - 15; i++)
                {
                    int random = Random.RandomRangeInt(0, 4);
                    if (random == 0) buckets.Add(RoleOptions.CrewRandom);
                    else buckets.Add(RoleOptions.NonImp);
                }
            }

            // imp issues
            foreach (var roleOption in buckets)
            {
                if (impBuckets.Contains(roleOption)) impCount += 1;
                else if (roleOption == RoleOptions.Any) anySlots += 1;
            }
            while (impCount > impostors.Count)
            {
                buckets.Shuffle();
                buckets.Remove(buckets.FindLast(x => impBuckets.Contains(x)));
                buckets.Add(RoleOptions.NonImp);
                impCount -= 1;
            }
            while (impCount + anySlots < impostors.Count)
            {
                buckets.Shuffle();
                buckets.RemoveAt(0);
                buckets.Add(RoleOptions.ImpRandom);
                impCount += 1;
            }
            while (buckets.Contains(RoleOptions.Any))
            {
                buckets.Shuffle();
                buckets.Remove(buckets.FindLast(x => x == RoleOptions.Any));
                if (impCount < impostors.Count)
                {
                    buckets.Add(RoleOptions.ImpRandom);
                    impCount += 1;
                }
                else buckets.Add(RoleOptions.NonImp);
            }

            // crew and neut issues
            var noChange = false;
            var nonImp = false;
            var randNeut = false;
            foreach (var roleOption in buckets)
            {
                if (crewNKBuckets.Contains(roleOption))
                {
                    noChange = true;
                    break;
                }
                else if (roleOption == RoleOptions.NeutRandom)
                {
                    randNeut = true;
                    break;
                }
                else if (roleOption == RoleOptions.NonImp)
                {
                    nonImp = true;
                    break;
                }
            }
            if (!noChange)
            {
                List<RoleOptions> add = [RoleOptions.CrewRandom, RoleOptions.NeutKilling];
                add.Shuffle();
                if (randNeut)
                {
                    buckets.Remove(RoleOptions.NeutRandom);
                    buckets.Add(RoleOptions.NeutKilling);
                }
                else if (nonImp)
                {
                    buckets.Remove(RoleOptions.NonImp);
                    buckets.Add(add[0]);
                }
                else
                {
                    buckets.Remove(buckets.FindLast(x => !impBuckets.Contains(x)));
                    buckets.Add(add[0]);
                }
            }

            // imp buckets
            while (buckets.Contains(RoleOptions.ImpConceal))
            {
                if (ImpostorConcealingRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.ImpConceal))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.ImpConceal));
                        buckets.Add(RoleOptions.ImpCommon);
                    }
                    break;
                }
                var addedRole = SelectRole(ImpostorConcealingRoles);
                impRoles.Add(addedRole);
                ImpostorConcealingRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) ImpostorConcealingRoles.Add(addedRole);
                buckets.Remove(RoleOptions.ImpConceal);
            }
            var commonImpRoles = ImpostorConcealingRoles;
            while (buckets.Contains(RoleOptions.ImpSupport))
            {
                if (ImpostorSupportRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.ImpSupport))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.ImpSupport));
                        buckets.Add(RoleOptions.ImpCommon);
                    }
                    break;
                }
                var addedRole = SelectRole(ImpostorSupportRoles);
                impRoles.Add(addedRole);
                ImpostorSupportRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) ImpostorSupportRoles.Add(addedRole);
                buckets.Remove(RoleOptions.ImpSupport);
            }
            commonImpRoles.AddRange(ImpostorSupportRoles);
            while (buckets.Contains(RoleOptions.ImpKilling))
            {
                if (ImpostorKillingRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.ImpKilling))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.ImpKilling));
                        buckets.Add(RoleOptions.ImpRandom);
                    }
                    break;
                }
                var addedRole = SelectRole(ImpostorKillingRoles);
                impRoles.Add(addedRole);
                ImpostorKillingRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) ImpostorKillingRoles.Add(addedRole);
                buckets.Remove(RoleOptions.ImpKilling);
            }
            var randomImpRoles = ImpostorKillingRoles;
            while (buckets.Contains(RoleOptions.ImpCommon))
            {
                if (commonImpRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.ImpCommon))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.ImpCommon));
                        buckets.Add(RoleOptions.ImpRandom);
                    }
                    break;
                }
                var addedRole = SelectRole(commonImpRoles);
                impRoles.Add(addedRole);
                commonImpRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) commonImpRoles.Add(addedRole);
                buckets.Remove(RoleOptions.ImpCommon);
            }
            randomImpRoles.AddRange(commonImpRoles);
            while (buckets.Contains(RoleOptions.ImpRandom))
            {
                if (randomImpRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.ImpRandom))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.ImpRandom));
                    }
                    break;
                }
                var addedRole = SelectRole(randomImpRoles);
                impRoles.Add(addedRole);
                randomImpRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) randomImpRoles.Add(addedRole);
                buckets.Remove(RoleOptions.ImpRandom);
            }

            // crew buckets
            while (buckets.Contains(RoleOptions.CrewInvest))
            {
                if (CrewmateInvestigativeRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewInvest))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewInvest));
                        buckets.Add(RoleOptions.CrewCommon);
                    }
                    break;
                }
                var addedRole = SelectRole(CrewmateInvestigativeRoles);
                crewRoles.Add(addedRole);
                CrewmateInvestigativeRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) CrewmateInvestigativeRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewInvest);
            }
            var commonCrewRoles = CrewmateInvestigativeRoles;
            while (buckets.Contains(RoleOptions.CrewProtective))
            {
                if (CrewmateProtectiveRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewProtective))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewProtective));
                        buckets.Add(RoleOptions.CrewCommon);
                    }
                    break;
                }
                var addedRole = SelectRole(CrewmateProtectiveRoles);
                crewRoles.Add(addedRole);
                CrewmateProtectiveRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) CrewmateProtectiveRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewProtective);
            }
            commonCrewRoles.AddRange(CrewmateProtectiveRoles);
            while (buckets.Contains(RoleOptions.CrewSupport))
            {
                if (CrewmateSupportRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewSupport))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewSupport));
                        buckets.Add(RoleOptions.CrewCommon);
                    }
                    break;
                }
                var addedRole = SelectRole(CrewmateSupportRoles);
                crewRoles.Add(addedRole);
                CrewmateSupportRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) CrewmateSupportRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewSupport);
            }
            commonCrewRoles.AddRange(CrewmateSupportRoles);
            while (buckets.Contains(RoleOptions.CrewKilling))
            {
                if (CrewmateKillingRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewKilling))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewKilling));
                        buckets.Add(RoleOptions.CrewSpecial);
                    }
                    break;
                }
                var addedRole = SelectRole(CrewmateKillingRoles);
                crewRoles.Add(addedRole);
                CrewmateKillingRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) CrewmateKillingRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewKilling);
            }
            var specialCrewRoles = CrewmateKillingRoles;
            while (buckets.Contains(RoleOptions.CrewPower))
            {
                if (CrewmatePowerRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewPower))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewPower));
                        buckets.Add(RoleOptions.CrewSpecial);
                    }
                    break;
                }
                var addedRole = SelectRole(CrewmatePowerRoles);
                crewRoles.Add(addedRole);
                CrewmatePowerRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) CrewmatePowerRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewPower);
            }
            specialCrewRoles.AddRange(CrewmatePowerRoles);
            while (buckets.Contains(RoleOptions.CrewCommon))
            {
                if (commonCrewRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewCommon))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewCommon));
                        buckets.Add(RoleOptions.CrewRandom);
                    }
                    break;
                }
                var addedRole = SelectRole(commonCrewRoles);
                crewRoles.Add(addedRole);
                commonCrewRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) commonCrewRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewCommon);
            }
            var randomCrewRoles = commonCrewRoles;
            while (buckets.Contains(RoleOptions.CrewSpecial))
            {
                if (specialCrewRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewSpecial))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewSpecial));
                        buckets.Add(RoleOptions.CrewRandom);
                    }
                    break;
                }
                var addedRole = SelectRole(specialCrewRoles);
                crewRoles.Add(addedRole);
                specialCrewRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) specialCrewRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewSpecial);
            }
            randomCrewRoles.AddRange(specialCrewRoles);
            while (buckets.Contains(RoleOptions.CrewRandom))
            {
                if (randomCrewRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.CrewRandom))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.CrewRandom));
                    }
                    break;
                }
                var addedRole = SelectRole(randomCrewRoles);
                crewRoles.Add(addedRole);
                randomCrewRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) randomCrewRoles.Add(addedRole);
                buckets.Remove(RoleOptions.CrewRandom);
            }
            var randomNonImpRoles = randomCrewRoles;

            // neutral buckets
            while (buckets.Contains(RoleOptions.NeutBenign))
            {
                if (NeutralBenignRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.NeutBenign))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.NeutBenign));
                        buckets.Add(RoleOptions.NeutCommon);
                    }
                    break;
                }
                var addedRole = SelectRole(NeutralBenignRoles);
                crewRoles.Add(addedRole);
                NeutralBenignRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) NeutralBenignRoles.Add(addedRole);
                buckets.Remove(RoleOptions.NeutBenign);
            }
            var commonNeutRoles = NeutralBenignRoles;
            while (buckets.Contains(RoleOptions.NeutEvil))
            {
                if (NeutralEvilRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.NeutEvil))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.NeutEvil));
                        buckets.Add(RoleOptions.NeutCommon);
                    }
                    break;
                }
                var addedRole = SelectRole(NeutralEvilRoles);
                crewRoles.Add(addedRole);
                NeutralEvilRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) NeutralEvilRoles.Add(addedRole);
                buckets.Remove(RoleOptions.NeutEvil);
            }
            commonNeutRoles.AddRange(NeutralEvilRoles);
            while (buckets.Contains(RoleOptions.NeutKilling))
            {
                if (NeutralKillingRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.NeutKilling))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.NeutKilling));
                        buckets.Add(RoleOptions.NeutRandom);
                    }
                    break;
                }
                var addedRole = SelectRole(NeutralKillingRoles);
                crewRoles.Add(addedRole);
                NeutralKillingRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) NeutralKillingRoles.Add(addedRole);
                buckets.Remove(RoleOptions.NeutKilling);
            }
            var randomNeutRoles = NeutralKillingRoles;
            while (buckets.Contains(RoleOptions.NeutCommon))
            {
                if (commonNeutRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.NeutCommon))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.NeutCommon));
                        buckets.Add(RoleOptions.NeutRandom);
                    }
                    break;
                }
                var addedRole = SelectRole(commonNeutRoles);
                crewRoles.Add(addedRole);
                commonNeutRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) commonNeutRoles.Add(addedRole);
                buckets.Remove(RoleOptions.NeutCommon);
            }
            randomNeutRoles.AddRange(commonNeutRoles);
            while (buckets.Contains(RoleOptions.NeutRandom))
            {
                if (randomNeutRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.NeutRandom))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.NeutRandom));
                        buckets.Add(RoleOptions.NonImp);
                    }
                    break;
                }
                var addedRole = SelectRole(randomNeutRoles);
                crewRoles.Add(addedRole);
                randomNeutRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) randomNeutRoles.Add(addedRole);
                buckets.Remove(RoleOptions.NeutRandom);
            }
            randomNonImpRoles.AddRange(randomNeutRoles);
            while (buckets.Contains(RoleOptions.NonImp))
            {
                if (randomNonImpRoles.Count == 0)
                {
                    while (buckets.Contains(RoleOptions.NonImp))
                    {
                        buckets.Remove(buckets.FindLast(x => x == RoleOptions.NonImp));
                    }
                    break;
                }
                var addedRole = SelectRole(randomNonImpRoles);
                crewRoles.Add(addedRole);
                randomNonImpRoles.Remove(addedRole);
                if (addedRole.Item2 >= 100) addedRole.Item2 -= 5;
                if (addedRole.Item2 > 0 && !addedRole.Item3) randomNonImpRoles.Add(addedRole);
                buckets.Remove(RoleOptions.NonImp);
            }

            // Shuffle roles before handing them out.
            // This should ensure a statistically equal chance of all permutations of roles.
            crewRoles.Shuffle();
            impRoles.Shuffle();

            // Hand out appropriate roles to crewmates and impostors.
            foreach (var (type, _, unique) in crewRoles)
            {
                Role.GenRole<Role>(type, crewmates);
            }
            foreach (var (type, _, unique) in impRoles)
            {
                Role.GenRole<Role>(type, impostors);
            }

            // Assign vanilla roles to anyone who did not receive a role.
            foreach (var crewmate in crewmates)
                Role.GenRole<Role>(typeof(Crewmate), crewmate);

            foreach (var impostor in impostors)
                Role.GenRole<Role>(typeof(Impostor), impostor);

            // Hand out assassin ability to killers according to the settings.
            var canHaveAbility = PlayerControl.AllPlayerControls.ToArray().Where(player => player.Is(Faction.Impostors)).ToList();
            canHaveAbility.Shuffle();
            var canHaveAbility2 = PlayerControl.AllPlayerControls.ToArray().Where(player => player.Is(Faction.NeutralKilling)).ToList();
            canHaveAbility2.Shuffle();

            var assassinConfig = new (List<PlayerControl>, NumAssassins)[]
            {
                (canHaveAbility, CustomGameOptions.NumberOfImpostorAssassins),
                (canHaveAbility2, CustomGameOptions.NumberOfNeutralAssassins)
            };
            foreach ((var abilityList, NumAssassins maxNumber) in assassinConfig)
            {
                if (maxNumber == NumAssassins.None) continue;
                while (abilityList.Count > 0)
                {
                    var (type, rpc, _) = AssassinAbility.Ability();
                    Role.Gen<Ability>(type, abilityList.TakeFirst(), rpc);
                    if (maxNumber == NumAssassins.One) break;
                }
            }

            // Hand out assassin modifiers, if enabled, to impostor assassins.
            var canHaveAssassinModifier = PlayerControl.AllPlayerControls.ToArray().Where(player => player.Is(Faction.Impostors) && player.Is(AbilityEnum.Assassin)).ToList();
            canHaveAssassinModifier.Shuffle();
            AssassinModifiers.Shuffle();

            foreach (var (type, _) in AssassinModifiers)
            {
                if (canHaveAssassinModifier.Count == 0) break;
                Role.GenModifier<Modifier>(type, canHaveAssassinModifier);
            }

            // Hand out impostor modifiers.
            var canHaveImpModifier = PlayerControl.AllPlayerControls.ToArray().Where(player => player.Is(Faction.Impostors) && !player.Is(ModifierEnum.DoubleShot)).ToList();
            canHaveImpModifier.Shuffle();
            ImpostorModifiers.Shuffle();

            foreach (var (type, _) in ImpostorModifiers)
            {
                if (canHaveImpModifier.Count == 0) break;
                Role.GenModifier<Modifier>(type, canHaveImpModifier);
            }


                // Now hand out Crewmate Modifiers to all remaining eligible players.
                var canHaveCrewModifier = PlayerControl.AllPlayerControls.ToArray().Where(player => player.Is(Faction.Crewmates)).ToList();
            canHaveCrewModifier.Shuffle();
            CrewmateModifiers.Shuffle();

            foreach (var (type, _) in CrewmateModifiers)
            {
                if (canHaveCrewModifier.Count == 0) break;
                Role.GenModifier<Modifier>(type, canHaveCrewModifier);
            }

            // Hand out global modifiers.
            var canHaveModifier = PlayerControl.AllPlayerControls.ToArray().ToList();
            canHaveModifier.Shuffle();
            GlobalModifiers.Shuffle();

            foreach (var (type, id) in GlobalModifiers)
            {
                if (type.FullName.Contains("Lover")) Lover.Gen(PlayerControl.AllPlayerControls.ToArray().ToList());
                else if (canHaveModifier.Count > 0) Role.GenModifier<Modifier>(type, canHaveModifier);
            }

            var exeTargets = PlayerControl.AllPlayerControls.ToArray().Where(x => x.Is(Faction.Crewmates) && !x.Is(RoleEnum.President) && !x.Is(ModifierEnum.Lover) && !x.Is(RoleEnum.Politician) && !x.Is(RoleEnum.Prosecutor) && !x.Is(RoleEnum.Swapper) && !x.Is(RoleEnum.Vigilante) && !x.Is(RoleEnum.Jailor)).ToList();
            foreach (var role in Role.GetRoles(RoleEnum.Executioner))
            {
                var exe = (Executioner)role;
                if (exeTargets.Count > 0)
                {
                    exe.target = exeTargets[Random.RandomRangeInt(0, exeTargets.Count)];
                    exeTargets.Remove(exe.target);

                    Utils.Rpc(CustomRPC.SetTarget, role.Player.PlayerId, exe.target.PlayerId);
                }
            }

            var goodGATargets = PlayerControl.AllPlayerControls.ToArray().Where(x => x.Is(Faction.Crewmates) && !x.Is(ModifierEnum.Lover)).ToList();
            var evilGATargets = PlayerControl.AllPlayerControls.ToArray().Where(x => (x.Is(Faction.Impostors) || x.Is(Faction.NeutralKilling)) && !x.Is(ModifierEnum.Lover)).ToList();
            foreach (var role in Role.GetRoles(RoleEnum.GuardianAngel))
            {
                var ga = (GuardianAngel)role;
                if (!(goodGATargets.Count == 0 && CustomGameOptions.EvilTargetPercent == 0) ||
                    (evilGATargets.Count == 0 && CustomGameOptions.EvilTargetPercent == 100) ||
                    goodGATargets.Count == 0 && evilGATargets.Count == 0)
                {
                    if (goodGATargets.Count == 0)
                    {
                        ga.target = evilGATargets[Random.RandomRangeInt(0, evilGATargets.Count)];
                        evilGATargets.Remove(ga.target);
                    }
                    else if (evilGATargets.Count == 0 || !Check(CustomGameOptions.EvilTargetPercent))
                    {
                        ga.target = goodGATargets[Random.RandomRangeInt(0, goodGATargets.Count)];
                        goodGATargets.Remove(ga.target);
                    }
                    else
                    {
                        ga.target = evilGATargets[Random.RandomRangeInt(0, evilGATargets.Count)];
                        evilGATargets.Remove(ga.target);
                    }

                    Utils.Rpc(CustomRPC.SetGATarget, role.Player.PlayerId, ga.target.PlayerId);
                }
            }
        }



        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class HandleRpc
        {
            public static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
            {
                Assembly asm = typeof(Role).Assembly;

                byte readByte, readByte1, readByte2;
                sbyte readSByte, readSByte2;
                switch ((CustomRPC) callId)
                {
                    case CustomRPC.SetRole:
                        var player = Utils.PlayerById(reader.ReadByte());
                        var rstring = reader.ReadString();
                        Activator.CreateInstance(asm.GetType(rstring), new object[] { player });
                        break;
                    case CustomRPC.SetModifier:
                        var player2 = Utils.PlayerById(reader.ReadByte());
                        var mstring = reader.ReadString();
                        Activator.CreateInstance(asm.GetType(mstring), new object[] { player2 });
                        break;

                    case CustomRPC.LoveWin:
                        var winnerlover = Utils.PlayerById(reader.ReadByte());
                        Modifier.GetModifier<Lover>(winnerlover).Win();
                        break;

                    case CustomRPC.NobodyWins:
                        Role.NobodyWinsFunc();
                        break;

                    case CustomRPC.SurvivorOnlyWin:
                        Role.SurvOnlyWin();
                        break;

                    case CustomRPC.VampireWin:
                        Role.VampWin();
                        break;

                    case CustomRPC.SetCouple:
                        var id = reader.ReadByte();
                        var id2 = reader.ReadByte();
                        var lover1 = Utils.PlayerById(id);
                        var lover2 = Utils.PlayerById(id2);

                        var modifierLover1 = new Lover(lover1);
                        var modifierLover2 = new Lover(lover2);

                        modifierLover1.OtherLover = modifierLover2;
                        modifierLover2.OtherLover = modifierLover1;

                        break;

                    case CustomRPC.Start:
                        readByte = reader.ReadByte();
                        Utils.ShowDeadBodies = false;
                        ShowShield.FirstRoundShielded = readByte == byte.MaxValue ? null : Utils.PlayerById(readByte);
                        ShowShield.DiedFirst = "";
                        Murder.KilledPlayers.Clear();
                        Role.NobodyWins = false;
                        Role.ForceGameEnd = false;
                        Role.SurvOnlyWins = false;
                        Role.VampireWins = false;
                        ExileControllerPatch.lastExiled = null;
                        PatchKillTimer.GameStarted = false;
                        StartImitate.ImitatingPlayers.Clear();
                        ChatCommands.JailorMessage = false;
                        RecordRewind.points.Clear();
                        AddHauntPatch.AssassinatedPlayers.Clear();
                        HudUpdate.Zooming = false;
                        HudUpdate.ZoomStart();
                        break;

                    case CustomRPC.JanitorClean:
                        readByte1 = reader.ReadByte();
                        var janitorPlayer = Utils.PlayerById(readByte1);
                        var janitorRole = Role.GetRole<Janitor>(janitorPlayer);
                        readByte = reader.ReadByte();
                        var deadBodies = Object.FindObjectsOfType<DeadBody>();
                        foreach (var body in deadBodies)
                            if (body.ParentId == readByte)
                                Coroutines.Start(Coroutine.CleanCoroutine(body, janitorRole));

                        break;
                    case CustomRPC.ForceEndGame:
                        if (Role.ForceGameEnd != true) Role.ForceGameEnd = true;
                        break;
                    case CustomRPC.EngineerFix:
                        if (ShipStatus.Instance.Systems.ContainsKey(SystemTypes.MushroomMixupSabotage))
                        {
                            var mushroom = ShipStatus.Instance.Systems[SystemTypes.MushroomMixupSabotage].Cast<MushroomMixupSabotageSystem>();
                            if (mushroom.IsActive) mushroom.currentSecondsUntilHeal = 0.1f;
                        }
                        break;

                    case CustomRPC.FixLights:
                        var lights = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                        lights.ActualSwitches = lights.ExpectedSwitches;
                        break;

                    case CustomRPC.Reveal:
                        var mayor = Utils.PlayerById(reader.ReadByte());
                        var mayorRole = Role.GetRole<Mayor>(mayor);
                        mayorRole.Revealed = true;
                        AddRevealButton.RemoveAssassin(mayorRole);
                        break;

                    case CustomRPC.Elect:
                        var politician = Utils.PlayerById(reader.ReadByte());
                        Role.RoleDictionary.Remove(politician.PlayerId);
                        var mayorRole2 = new Mayor(politician);
                        mayorRole2.Revealed = true;
                        AddRevealButton.RemoveAssassin(mayorRole2);
                        break;

                    case CustomRPC.Prosecute:
                        var host = reader.ReadBoolean();
                        if (host && AmongUsClient.Instance.AmHost)
                        {
                            var prosecutor = Utils.PlayerById(reader.ReadByte());
                            var prosRole = Role.GetRole<Prosecutor>(prosecutor);
                            prosRole.ProsecuteThisMeeting = true;
                        }
                        else if (!host && !AmongUsClient.Instance.AmHost)
                        {
                            var prosecutor = Utils.PlayerById(reader.ReadByte());
                            var prosRole = Role.GetRole<Prosecutor>(prosecutor);
                            prosRole.ProsecuteThisMeeting = true;
                        }
                        break;
                    case CustomRPC.Douse:
                        var arso = Utils.PlayerById(reader.ReadByte());
                        var arsoRole = Role.GetRole<Arsonist>(arso);
                        var doused = reader.ReadByte();
                        if (!arsoRole.DousedPlayers.Contains(doused)) arsoRole.DousedPlayers.Add(doused);
                        break;
                    case CustomRPC.Bite:
                        var newVamp = Utils.PlayerById(reader.ReadByte());
                        Bite.Convert(newVamp);
                        break;
                    case CustomRPC.SetSwaps:
                        readSByte = reader.ReadSByte();
                        SwapVotes.Swap1 =
                            MeetingHud.Instance.playerStates.FirstOrDefault(x => x.TargetPlayerId == readSByte);
                        readSByte2 = reader.ReadSByte();
                        SwapVotes.Swap2 =
                            MeetingHud.Instance.playerStates.FirstOrDefault(x => x.TargetPlayerId == readSByte2);
                        PluginSingleton<TownOfUs>.Instance.Log.LogMessage("Bytes received - " + readSByte + " - " +
                                                                          readSByte2);
                        break;

                    case CustomRPC.Imitate:
                        var imitator = Utils.PlayerById(reader.ReadByte());
                        var imitatorRole = Role.GetRole<Imitator>(imitator);
                        var imitateTarget = Utils.PlayerById(reader.ReadByte());
                        imitatorRole.ImitatePlayer = imitateTarget;
                        break;
                    case CustomRPC.StartImitate:
                        var imitator2 = Utils.PlayerById(reader.ReadByte());
                        if (imitator2.Is(RoleEnum.Traitor)) break;
                        var imitatorRole2 = Role.GetRole<Imitator>(imitator2);
                        StartImitate.Imitate(imitatorRole2);
                        break;
                    case CustomRPC.Remember:
                        readByte1 = reader.ReadByte();
                        readByte2 = reader.ReadByte();
                        var amnesiac = Utils.PlayerById(readByte1);
                        var other = Utils.PlayerById(readByte2);
                        switch (reader.ReadByte()) {
                            case 0: // start
                                if (AmongUsClient.Instance.AmHost && amnesiac.Is(RoleEnum.Amnesiac))
                                {
                                    Utils.Rpc(CustomRPC.Remember, amnesiac.PlayerId, other.PlayerId, (byte)1);
                                    PerformKillButton.Remember(Role.GetRole<Amnesiac>(amnesiac), other);
                                }
                                break;
                            case 1: // end
                            default:
                                PerformKillButton.Remember(Role.GetRole<Amnesiac>(amnesiac), other);
                                break;
                        }
                        break;


                    case CustomRPC.Rewind:
                        readByte = reader.ReadByte();
                        var TimeLordPlayer = Utils.PlayerById(readByte);
                        var TimeLordRole = Role.GetRole<TimeLord>(TimeLordPlayer);
                        StartStop.StartRewind(TimeLordRole);
                        break;
                    case CustomRPC.RewindRevive:
                        readByte = reader.ReadByte();
                        RecordRewind.ReviveBody(Utils.PlayerById(readByte));
                        break;


                    case CustomRPC.Protect:
                        readByte1 = reader.ReadByte();
                        readByte2 = reader.ReadByte();

                        var medic = Utils.PlayerById(readByte1);
                        var shield = Utils.PlayerById(readByte2);
                        Role.GetRole<Medic>(medic).ShieldedPlayer = shield;
                        break;
                    case CustomRPC.AttemptSound:
                        var medicId = reader.ReadByte();
                        readByte = reader.ReadByte();
                        StopKill.BreakShield(medicId, readByte, CustomGameOptions.ShieldBreaks);
                        break;
                    case CustomRPC.BypassKill:
                        var killer = Utils.PlayerById(reader.ReadByte());
                        var target = Utils.PlayerById(reader.ReadByte());

                        Utils.MurderPlayer(killer, target, true);
                        break;
                    case CustomRPC.BypassMultiKill:
                        var killer2 = Utils.PlayerById(reader.ReadByte());
                        var target2 = Utils.PlayerById(reader.ReadByte());

                        Utils.MurderPlayer(killer2, target2, false);
                        break;
                    case CustomRPC.AssassinKill:
                        var toDie = Utils.PlayerById(reader.ReadByte());
                        var assassin = Utils.PlayerById(reader.ReadByte());
                        AssassinKill.MurderPlayer(toDie);
                        AssassinKill.AssassinKillCount(toDie, assassin);
                        break;
                    case CustomRPC.VigilanteKill:
                        var toDie2 = Utils.PlayerById(reader.ReadByte());
                        var vigi = Utils.PlayerById(reader.ReadByte());
                        VigilanteKill.MurderPlayer(toDie2);
                        VigilanteKill.VigiKillCount(toDie2, vigi);
                        break;
                    case CustomRPC.ForetellerKill:
                        var toDie4 = Utils.PlayerById(reader.ReadByte());
                        var fore = Utils.PlayerById(reader.ReadByte());
                        ForetellerKill.DoomKillCount(toDie4, fore);
                        if (!toDie4.IsBlessed()) ForetellerKill.MurderPlayer(toDie4);
                        else if (PlayerControl.LocalPlayer.Is(RoleEnum.Oracle) && toDie4.IsBlessed() && toDie4.GetOracle().Contains(Role.GetRole<Oracle>(PlayerControl.LocalPlayer))) Coroutines.Start(Utils.FlashCoroutine(Colors.Oracle));
                        break;
                    case CustomRPC.SetMimic:
                        var glitchPlayer = Utils.PlayerById(reader.ReadByte());
                        var mimicPlayer = Utils.PlayerById(reader.ReadByte());
                        var glitchRole = Role.GetRole<Glitch>(glitchPlayer);
                        glitchRole.MimicTarget = mimicPlayer;
                        glitchRole.IsUsingMimic = true;
                        glitchRole.Enabled = true;
                        Utils.Morph(glitchPlayer, mimicPlayer);
                        break;
                    case CustomRPC.RpcResetAnim:
                        var animPlayer = Utils.PlayerById(reader.ReadByte());
                        var theGlitchRole = Role.GetRole<Glitch>(animPlayer);
                        theGlitchRole.MimicTarget = null;
                        theGlitchRole.IsUsingMimic = false;
                        Utils.Unmorph(theGlitchRole.Player);
                        break;
                    case CustomRPC.GlitchWin:
                        var theGlitch = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Glitch);
                        ((Glitch) theGlitch)?.Wins();
                        break;
                    case CustomRPC.IcenbergWin:
                        var icenberg = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Icenberg);
                        ((Icenberg)icenberg)?.Wins();
                        break;
                    case CustomRPC.JuggernautWin:
                        var juggernaut = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Juggernaut);
                        ((Juggernaut)juggernaut)?.Wins();
                        break;
                    case CustomRPC.SetHacked:
                        var hackingPlayer = Utils.PlayerById(reader.ReadByte());
                        var hackPlayer = Utils.PlayerById(reader.ReadByte());
                        var glitch = Role.GetRole<Glitch>(hackingPlayer);
                        glitch.SetHacked(hackPlayer);
                        break;
                    case CustomRPC.Morph:
                        var morphling = Utils.PlayerById(reader.ReadByte());
                        var morphTarget = Utils.PlayerById(reader.ReadByte());
                        var morphRole = Role.GetRole<Morphling>(morphling);
                        morphRole.TimeRemaining = CustomGameOptions.MorphlingDuration;
                        morphRole.MorphedPlayer = morphTarget;
                        break;
                    case CustomRPC.SetTarget:
                        var exe = Utils.PlayerById(reader.ReadByte());
                        var exeTarget = Utils.PlayerById(reader.ReadByte());
                        var exeRole = Role.GetRole<Executioner>(exe);
                        exeRole.target = exeTarget;
                        break;
                    case CustomRPC.SetGATarget:
                        var ga = Utils.PlayerById(reader.ReadByte());
                        var gaTarget = Utils.PlayerById(reader.ReadByte());
                        var gaRole = Role.GetRole<GuardianAngel>(ga);
                        gaRole.target = gaTarget;
                        break;
                    case CustomRPC.Blackmail:
                        var blackmailer = Role.GetRole<Blackmailer>(Utils.PlayerById(reader.ReadByte()));
                        var blackmailed = Utils.PlayerById(reader.ReadByte());
                        switch (reader.ReadByte())
                        {
                            default:
                            case 0:
                                if (AmongUsClient.Instance.AmHost && !MeetingHud.Instance)
                                {
                                    blackmailer.Blackmailed = blackmailed;
                                    Utils.Rpc(CustomRPC.Blackmail, blackmailer.Player.PlayerId, blackmailer.Blackmailed.PlayerId, (byte)1);
                                }
                                break;
                            case 1:
                                if (PlayerControl.LocalPlayer == blackmailer.Player && blackmailer.Blackmailed != null)
                                {
                                    blackmailer.Blackmailed?.myRend().material.SetFloat("_Outline", 0f);
                                    if (blackmailer.Blackmailed.Data.IsImpostor())
                                    {
                                        if (blackmailer.Blackmailed.GetCustomOutfitType() != CustomPlayerOutfitType.Camouflage &&
                                            blackmailer.Blackmailed.GetCustomOutfitType() != CustomPlayerOutfitType.Swooper)
                                            blackmailer.Blackmailed.nameText().color = Patches.Colors.Impostor;
                                        else blackmailer.Blackmailed.nameText().color = Color.clear;
                                    }
                                }
                                blackmailer.Blackmailed = blackmailed;
                                break;
                        }
                        break;
                    case CustomRPC.Fortify:
                        switch (reader.ReadByte())
                        {
                            default:
                            case 0: //set fortify
                                var warden = Utils.PlayerById(reader.ReadByte());
                                var fortified = Utils.PlayerById(reader.ReadByte());
                                var wardenRole = Role.GetRole<Warden>(warden);
                                wardenRole.Fortified = fortified;
                                break;
                            case 1: //fortify alert
                                var wardenPlayer = Utils.PlayerById(reader.ReadByte());
                                if (PlayerControl.LocalPlayer == wardenPlayer) Coroutines.Start(Utils.FlashCoroutine(Colors.Warden));
                                break;
                        }
                        break;
                    case CustomRPC.Camp:
                        var deputy = Utils.PlayerById(reader.ReadByte());
                        var deputyRole = Role.GetRole<Deputy>(deputy);
                        switch (reader.ReadByte())
                        {
                            default: // the reason why I do both is in case of desync
                            case 0: //camp
                                var camp = Utils.PlayerById(reader.ReadByte());
                                deputyRole.Camping = camp;
                                break;
                            case 1: //camp trigger
                                var killerTarget = Utils.PlayerById(reader.ReadByte());
                                deputyRole.Killer = killerTarget;
                                deputyRole.Camping = null;
                                break;
                            case 2: //shoot
                                var shot = Utils.PlayerById(reader.ReadByte());
                                if (shot == deputyRole.Killer && !shot.Is(RoleEnum.Pestilence) && !shot.IsBlessed())
                                {
                                    AddButtonDeputy.Shoot(deputyRole, shot);
                                    if (shot.Is(Faction.Crewmates)) deputyRole.IncorrectKills += 1;
                                    else deputyRole.CorrectKills += 1;
                                }
                                deputyRole.Killer = null;
                                break;
                        }
                        break;
                    case CustomRPC.Hypnotise:
                        var hypnotist = Utils.PlayerById(reader.ReadByte());
                        var hypnotistRole = Role.GetRole<Hypnotist>(hypnotist);
                        switch (reader.ReadByte())
                        {
                            default:
                            case 0: //set hypnosis
                                var hypnotised = Utils.PlayerById(reader.ReadByte());
                                hypnotistRole.HypnotisedPlayers.Add(hypnotised.PlayerId);
                                break;
                            case 1: //trigger hysteria
                                hypnotistRole.HysteriaActive = true;
                                hypnotistRole.Hysteria();
                                break;
                        }
                        break;
                    case CustomRPC.Jail:
                        var jailor = Utils.PlayerById(reader.ReadByte());
                        var jailorRole = Role.GetRole<Jailor>(jailor);
                        switch (reader.ReadByte())
                        {
                            default:
                            case 0: //jail
                                var jailed = Utils.PlayerById(reader.ReadByte());
                                if (!MeetingHud.Instance && AmongUsClient.Instance.AmHost)
                                {
                                    jailorRole.Jailed = jailed;
                                    Utils.Rpc(CustomRPC.Jail, jailor.PlayerId, (byte)2, jailed.PlayerId);
                                }
                                break;
                            case 1: //execute
                                if (jailorRole.Jailed.Is(Faction.Crewmates))
                                {
                                    jailorRole.IncorrectKills += 1;
                                    jailorRole.Executes = 0;
                                }
                                else jailorRole.CorrectKills += 1;
                                jailorRole.JailCell.Destroy();
                                AddJailButtons.ExecuteKill(jailorRole, jailorRole.Jailed);
                                jailorRole.Jailed = null;
                                break;
                            case 2:
                                readByte = reader.ReadByte();
                                if (readByte == byte.MaxValue) jailorRole.Jailed = null;
                                else
                                {
                                    var jailed2 = Utils.PlayerById(readByte);
                                    jailorRole.Jailed = jailed2;
                                }
                                break;
                        }
                        break;
                    case CustomRPC.Confess:
                        var oracle = Role.GetRole<Oracle>(Utils.PlayerById(reader.ReadByte()));
                        oracle.Confessor = Utils.PlayerById(reader.ReadByte());
                        var faction = reader.ReadInt32();
                        if (faction == 0) oracle.RevealedFaction = Faction.Crewmates;
                        else if (faction == 1) oracle.RevealedFaction = Faction.NeutralEvil;
                        else oracle.RevealedFaction = Faction.Impostors;
                        break;
                    case CustomRPC.Bless:
                        var oracle2 = Role.GetRole<Oracle>(Utils.PlayerById(reader.ReadByte()));
                        readByte = reader.ReadByte();
                        switch (readByte)
                        {
                            case 0:
                                oracle2.SavedBlessed = true;
                                break;
                            case 1:
                                var blessed = Utils.PlayerById(reader.ReadByte());
                                oracle2.Blessed = blessed;
                                break;
                            case 2:
                                if (PlayerControl.LocalPlayer == oracle2.Player) Coroutines.Start(Utils.FlashCoroutine(Colors.Oracle));
                                break;
                        }
                        break;
                    case CustomRPC.Flush:
                        readByte = reader.ReadByte();
                        switch (readByte)
                        {
                            case 0:
                                if (PlayerControl.LocalPlayer.inVent)
                                {
                                    PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                                    PlayerControl.LocalPlayer.MyPhysics.ExitAllVents();
                                    Coroutines.Start(Utils.FlashCoroutine(Patches.Colors.Plumber));
                                }
                                break;
                            case 1:
                                var plumber = Utils.PlayerById(reader.ReadByte());
                                var plumberRole = Role.GetRole<Plumber>(plumber);
                                readByte = reader.ReadByte();
                                if (!plumberRole.FutureBlocks.Contains(readByte)) plumberRole.FutureBlocks.Add(readByte);
                                break;
                        }
                        break;
                    case CustomRPC.Retribution:
                        var hunter2 = Role.GetRole<Hunter>(Utils.PlayerById(reader.ReadByte()));
                        var hunterLastVoted = Utils.PlayerById(reader.ReadByte());
                        Retribution.MurderPlayer(hunter2, hunterLastVoted);
                        break;
                    case CustomRPC.ExecutionerToJester:
                        TargetColor.ExeToJes(Utils.PlayerById(reader.ReadByte()));
                        break;
                    case CustomRPC.GAToSurv:
                        GATargetColor.GAToSurv(Utils.PlayerById(reader.ReadByte()));
                        break;
                    case CustomRPC.Mine:
                        var ventId = reader.ReadInt32();
                        var miner = Utils.PlayerById(reader.ReadByte());
                        var minerRole = Role.GetRole<Miner>(miner);
                        var pos = reader.ReadVector2();
                        var zAxis = reader.ReadSingle();
                        PlaceVent.SpawnVent(ventId, minerRole, pos, zAxis);
                        break;
                    case CustomRPC.Swoop:
                        var swooper = Utils.PlayerById(reader.ReadByte());
                        var swooperRole = Role.GetRole<Swooper>(swooper);
                        swooperRole.TimeRemaining = CustomGameOptions.SwoopDuration;
                        swooperRole.Swoop();
                        break;
                    case CustomRPC.Camouflage:
                        var venerer = Utils.PlayerById(reader.ReadByte());
                        var venererRole = Role.GetRole<Venerer>(venerer);
                        venererRole.TimeRemaining = CustomGameOptions.AbilityDuration;
                        venererRole.Ability();
                        break;
                    case CustomRPC.Alert:
                        var veteran = Utils.PlayerById(reader.ReadByte());
                        var veteranRole = Role.GetRole<Veteran>(veteran);
                        veteranRole.UsesLeft -= 1;
                        veteranRole.TimeRemaining = CustomGameOptions.AlertDuration;
                        veteranRole.Alert();
                        break;


                    case CustomRPC.Freeze:
                        var icenbergPlayer = Utils.PlayerById(reader.ReadByte());
                        var freezePlayer = Utils.PlayerById(reader.ReadByte());
                        var icenbergRole = Role.GetRole<Icenberg>(icenbergPlayer);
                        icenbergRole.FreezeTarget = freezePlayer;
                        icenbergRole.IsUsingFreeze = true;
                        Utils.Freeze(icenbergPlayer, freezePlayer);
                        break;





                    case CustomRPC.SetExtraVotes:

                        var president = Utils.PlayerById(reader.ReadByte());
                        var presidentRole = Role.GetRole<President>(president);
                        presidentRole.ExtraVotes = reader.ReadBytesAndSize().ToList();
                        if (!president.Is(RoleEnum.President)) presidentRole.VoteBank -= presidentRole.ExtraVotes.Count;

                        break;
                    case CustomRPC.AddPresidentVoteBank:
                        Role.GetRole<President>(Utils.PlayerById(reader.ReadByte())).VoteBank += reader.ReadInt32();
                        break;




                    case CustomRPC.Vest:
                        var surv = Utils.PlayerById(reader.ReadByte());
                        var survRole = Role.GetRole<Survivor>(surv);
                        survRole.TimeRemaining = CustomGameOptions.VestDuration;
                        survRole.Vest();
                        break;
                    case CustomRPC.GAProtect:
                        var ga2 = Utils.PlayerById(reader.ReadByte());
                        var ga2Role = Role.GetRole<GuardianAngel>(ga2);
                        ga2Role.TimeRemaining = CustomGameOptions.ProtectDuration;
                        ga2Role.Protect();
                        break;
                    case CustomRPC.Transport:
                        Coroutines.Start(Transporter.TransportPlayers(reader.ReadByte(), reader.ReadByte(), reader.ReadBoolean()));
                        break;
                    case CustomRPC.SetUntransportable:
                        if (PlayerControl.LocalPlayer.Is(RoleEnum.Transporter))
                        {
                            Role.GetRole<Transporter>(PlayerControl.LocalPlayer).UntransportablePlayers.Add(reader.ReadByte(), DateTime.UtcNow);
                        }
                        break;
                    case CustomRPC.Mediate:
                        var mediatedPlayer = Utils.PlayerById(reader.ReadByte());
                        var medium = Role.GetRole<Medium>(Utils.PlayerById(reader.ReadByte()));
                        if (PlayerControl.LocalPlayer.PlayerId != mediatedPlayer.PlayerId) break;
                        medium.AddMediatePlayer(mediatedPlayer.PlayerId);
                        break;
                    case CustomRPC.FlashGrenade:
                        var grenadier = Utils.PlayerById(reader.ReadByte());
                        var grenadierRole = Role.GetRole<Grenadier>(grenadier);
                        grenadierRole.TimeRemaining = CustomGameOptions.GrenadeDuration;
                        byte playersFlashed = reader.ReadByte();
                        Il2CppSystem.Collections.Generic.List<PlayerControl> playerControlList = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                        for (int i = 0; i < playersFlashed; i++)
                        {
                            playerControlList.Add(Utils.PlayerById(reader.ReadByte()));
                        }
                        grenadierRole.flashedPlayers = playerControlList;
                        grenadierRole.Flash();
                        break;
                    case CustomRPC.Blind:
                        var eclipsal = Utils.PlayerById(reader.ReadByte());
                        var eclipsalRole = Role.GetRole<Eclipsal>(eclipsal);
                        eclipsalRole.TimeRemaining = CustomGameOptions.BlindDuration;
                        byte playersBlinded = reader.ReadByte();
                        Il2CppSystem.Collections.Generic.List<PlayerControl> blinded = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                        for (int i = 0; i < playersBlinded; i++)
                        {
                            blinded.Add(Utils.PlayerById(reader.ReadByte()));
                        }
                        eclipsalRole.BlindPlayers = blinded;
                        eclipsalRole.Blind();
                        break;
                    case CustomRPC.Bribe:
                        var mercenary = Utils.PlayerById(reader.ReadByte());
                        var mercenaryRole = Role.GetRole<Mercenary>(mercenary);
                        mercenaryRole.Bribed.Add(reader.ReadByte());
                        break;
                    case CustomRPC.ArsonistWin:
                        var theArsonistTheRole = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Arsonist);
                        ((Arsonist) theArsonistTheRole)?.Wins();
                        break;
                    case CustomRPC.WerewolfWin:
                        var theWerewolfTheRole = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Werewolf);
                        ((Werewolf)theWerewolfTheRole)?.Wins();
                        break;
                    case CustomRPC.PlaguebearerWin:
                        var thePlaguebearerTheRole = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Plaguebearer);
                        ((Plaguebearer)thePlaguebearerTheRole)?.Wins();
                        break;
                    case CustomRPC.SoulCollectorWin:
                        var soulCollector = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.SoulCollector);
                        ((SoulCollector)soulCollector)?.Wins();
                        break;
                    case CustomRPC.Infect:
                        var pb = Role.GetRole<Plaguebearer>(Utils.PlayerById(reader.ReadByte()));
                        pb.SpreadInfection(Utils.PlayerById(reader.ReadByte()), Utils.PlayerById(reader.ReadByte()));
                        break;
                    case CustomRPC.TurnPestilence:
                        Role.GetRole<Plaguebearer>(Utils.PlayerById(reader.ReadByte())).TurnPestilence();
                        break;
                    case CustomRPC.PestilenceWin:
                        var thePestilenceTheRole = Role.AllRoles.FirstOrDefault(x => x.RoleType == RoleEnum.Pestilence);
                        ((Pestilence)thePestilenceTheRole)?.Wins();
                        break;
                    case CustomRPC.SyncCustomSettings:
                        Rpc.ReceiveRpc(reader, reader.BytesRemaining > 8);
                        break;
                    case CustomRPC.AltruistRevive:
                        readByte1 = reader.ReadByte();
                        var altruistPlayer = Utils.PlayerById(readByte1);
                        switch (reader.ReadByte())
                        {
                            default:
                            case 0: //start
                                var altruistRole = Role.GetRole<Altruist>(altruistPlayer);
                                Coroutines.Start(global::TownOfUs.CrewmateRoles.AltruistMod.Coroutine.AltruistRevive(altruistRole));
                                break;
                            case 1: //end non-host
                                byte revivesCount = reader.ReadByte();
                                Dictionary<byte, Vector2> revives = new Dictionary<byte, Vector2>();
                                for (int i = 0; i < revivesCount; i++)
                                {
                                    byte playerId = reader.ReadByte();
                                    Vector2 location = reader.ReadVector2();
                                    revives.Add(playerId, location);
                                }
                                global::TownOfUs.CrewmateRoles.AltruistMod.Coroutine.AltruistReviveEnd(altruistPlayer, revives);
                                break;
                            case 2: //end host
                                byte revivesCountHost = reader.ReadByte();
                                Dictionary<byte, Vector2> revivesHost = new Dictionary<byte, Vector2>();
                                for (int i = 0; i < revivesCountHost; i++)
                                {
                                    byte playerId = reader.ReadByte();
                                    Vector2 location = reader.ReadVector2();
                                    revivesHost.Add(playerId, location);
                                }
                                if (AmongUsClient.Instance.AmHost && !MeetingHud.Instance) global::TownOfUs.CrewmateRoles.AltruistMod.Coroutine.ReviveRPC(altruistPlayer, revivesHost);
                                break;
                        }
                        break;
                    case CustomRPC.BarryButton:
                        var buttonBarry = Utils.PlayerById(reader.ReadByte());

                        if (AmongUsClient.Instance.AmHost)
                        {
                            MeetingRoomManager.Instance.reporter = buttonBarry;
                            MeetingRoomManager.Instance.target = null;
                            AmongUsClient.Instance.DisconnectHandlers.AddUnique(MeetingRoomManager.Instance
                                .Cast<IDisconnectHandler>());
                            if (GameManager.Instance.CheckTaskCompletion()) return;

                            HudManager.Instance.OpenMeetingRoom(buttonBarry);
                            buttonBarry.RpcStartMeeting(null);
                        }
                        break;
                    case CustomRPC.Disperse:
                        byte teleports = reader.ReadByte();
                        Dictionary<byte, Vector2> coordinates = new Dictionary<byte, Vector2>();
                        for (int i = 0; i < teleports; i++)
                        {
                            byte playerId = reader.ReadByte();
                            Vector2 location = reader.ReadVector2();
                            coordinates.Add(playerId, location);
                        }
                        Disperser.DispersePlayersToCoordinates(coordinates);
                        break;
                    case CustomRPC.BaitReport:
                        var baitKiller = Utils.PlayerById(reader.ReadByte());
                        var bait = Utils.PlayerById(reader.ReadByte());
                        baitKiller.ReportDeadBody(bait.Data);
                        break;
                    case CustomRPC.CheckMurder:
                        var murderKiller = Utils.PlayerById(reader.ReadByte());
                        var murderTarget = Utils.PlayerById(reader.ReadByte());
                        murderKiller.CheckMurder(murderTarget);
                        break;
                    case CustomRPC.Drag:
                        readByte1 = reader.ReadByte();
                        var dienerPlayer = Utils.PlayerById(readByte1);
                        var dienerRole = Role.GetRole<Undertaker>(dienerPlayer);
                        readByte = reader.ReadByte();
                        var dienerBodies = Object.FindObjectsOfType<DeadBody>();
                        foreach (var body in dienerBodies)
                        {
                            if (body.ParentId == readByte)
                            {
                                dienerRole.CurrentlyDragging = body;

                                if (PlayerControl.LocalPlayer.Is(RoleEnum.Lookout))
                                {
                                    var lookout = Role.GetRole<Lookout>(PlayerControl.LocalPlayer);
                                    if (lookout.Watching.ContainsKey(body.ParentId))
                                    {
                                        if (!lookout.Watching[body.ParentId].Contains(RoleEnum.Undertaker)) lookout.Watching[body.ParentId].Add(RoleEnum.Undertaker);
                                    }
                                }
                            }
                        }

                        break;
                    case CustomRPC.Drop:
                        readByte1 = reader.ReadByte();
                        var v2 = reader.ReadVector2();
                        var v2z = reader.ReadSingle();
                        var dienerPlayer2 = Utils.PlayerById(readByte1);
                        var dienerRole2 = Role.GetRole<Undertaker>(dienerPlayer2);
                        var body2 = dienerRole2.CurrentlyDragging;
                        dienerRole2.CurrentlyDragging = null;

                        body2.transform.position = new Vector3(v2.x, v2.y, v2z);

                        break;
                    case CustomRPC.SetAssassin:
                        new Assassin(Utils.PlayerById(reader.ReadByte()));
                        break;
                    case CustomRPC.SetPhantom:
                        readByte = reader.ReadByte();
                        SetPhantom.WillBePhantom = readByte == byte.MaxValue ? null : Utils.PlayerById(readByte);
                        SetPhantom.ChangeToPhantom();
                        Coroutines.Start(SetPhantom.WaitForMeeting());
                        break;
                    case CustomRPC.CatchPhantom:
                        var phantomPlayer = Utils.PlayerById(reader.ReadByte());
                        Role.GetRole<Phantom>(phantomPlayer).Caught = true;
                        if (PlayerControl.LocalPlayer == phantomPlayer) HudManager.Instance.AbilityButton.gameObject.SetActive(true);
                        phantomPlayer.Exiled();
                        break;
                    case CustomRPC.PhantomWin:
                        var phantomWinner = Role.GetRole<Phantom>(Utils.PlayerById(reader.ReadByte()));
                        phantomWinner.CompletedTasks = true;
                        if (!CustomGameOptions.PhantomWinEndsGame)
                        {
                            phantomWinner.Caught = true;
                            if (PlayerControl.LocalPlayer == phantomWinner.Player) HudManager.Instance.AbilityButton.gameObject.SetActive(true);
                        }
                        break;
                    case CustomRPC.SetHaunter:
                        readByte = reader.ReadByte();
                        SetHaunter.WillBeHaunter = readByte == byte.MaxValue ? null : Utils.PlayerById(readByte);
                        SetHaunter.ChangeToHaunter();
                        Coroutines.Start(SetHaunter.WaitForMeeting());
                        break;
                    case CustomRPC.CatchHaunter:
                        var haunterPlayer = Utils.PlayerById(reader.ReadByte());
                        Role.GetRole<Haunter>(haunterPlayer).Caught = true;
                        if (PlayerControl.LocalPlayer == haunterPlayer) HudManager.Instance.AbilityButton.gameObject.SetActive(true);
                        haunterPlayer.Exiled();
                        break;
                    case CustomRPC.TraitorSpawn:
                        readByte = reader.ReadByte();
                        SetTraitor.WillBeTraitor = Utils.PlayerById(readByte);
                        SetTraitor.TurnImp();
                        break;
                    case CustomRPC.Escape:
                        var escapist = Utils.PlayerById(reader.ReadByte());
                        var escapistRole = Role.GetRole<Escapist>(escapist);
                        var escapePos = reader.ReadVector2();
                        escapistRole.EscapePoint = escapePos;
                        Escapist.Escape(escapist);
                        break;
                    case CustomRPC.RemoveAllBodies:
                        var buggedBodies = Object.FindObjectsOfType<DeadBody>();
                        foreach (var body in buggedBodies)
                            body.gameObject.Destroy();
                        break;
                    case CustomRPC.CelebDied:
                        string message = reader.ReadString();
                        if (!PlayerControl.LocalPlayer.Data.IsDead) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, message);
                        break;
                    case CustomRPC.SubmergedFixOxygen:
                        Patches.SubmergedCompatibility.RepairOxygen();
                        break;
                    case CustomRPC.SetPos:
                        var setplayer = Utils.PlayerById(reader.ReadByte());
                        var pos2 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        setplayer.transform.position = pos2;
                        setplayer.NetTransform.SnapTo(pos2);
                        break;
                    case CustomRPC.AddTraitorRole:
                        var traitor = Utils.PlayerById(reader.ReadByte());
                        var traitorRole = Role.GetRole<Traitor>(traitor);
                        var selectedRole = (RoleEnum)reader.ReadByte();
                        SetTraitor.ChangeRole(traitorRole, selectedRole);
                        break;
                    case CustomRPC.Plant:
                        if (PlayerControl.LocalPlayer.Data.IsImpostor()) Coroutines.Start(BombTeammate.BombShowTeammate(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
                        break;
                    case CustomRPC.SetSettings:
                        readByte = reader.ReadByte();
                        GameOptionsManager.Instance.currentNormalGameOptions.MapId = readByte == byte.MaxValue ? (byte)0 : readByte;
                        GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                        GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                        GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
                        GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
                        GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
                        GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                        GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
                        RandomMap.AdjustSettings(readByte);
                        break;
                    case CustomRPC.SetPostRoles:
                        if (reader.ReadByte() == 0) SetPhantom.PhantomOn = true;
                        else SetPhantom.PhantomOn = false;
                        SetPhantom.WillBePhantom = null;
                        if (reader.ReadByte() == 0) SetHaunter.HaunterOn = true;
                        else SetHaunter.HaunterOn = false;
                        SetHaunter.WillBeHaunter = null;
                        if (reader.ReadByte() == 0) SetTraitor.TraitorOn = true;
                        else SetTraitor.TraitorOn = false;
                        SetTraitor.WillBeTraitor = null;
                        break;
                    case CustomRPC.HunterStalk:
                        var stalker = Utils.PlayerById(reader.ReadByte());
                        var stalked = Utils.PlayerById(reader.ReadByte());
                        Hunter hunterRole = Role.GetRole<Hunter>(stalker);
                        hunterRole.StalkDuration = CustomGameOptions.HunterStalkDuration;
                        hunterRole.StalkedPlayer = stalked;
                        hunterRole.UsesLeft -= 1;
                        hunterRole.Stalk();
                        break;
                    case CustomRPC.Barrier:
                        var cleric = Utils.PlayerById(reader.ReadByte());
                        var clericRole = Role.GetRole<Cleric>(cleric);
                        switch (reader.ReadByte())
                        {
                            default:
                            case 0: //barrier
                                var barriered = Utils.PlayerById(reader.ReadByte());
                                clericRole.Barriered = barriered;
                                clericRole.TimeRemaining = CustomGameOptions.BarrierCd;
                                break;
                            case 2: //attack
                                if (PlayerControl.LocalPlayer == cleric) Coroutines.Start(Utils.FlashCoroutine(Color.blue));
                                break;
                            case 1: //cleanse
                                var cleansed = Utils.PlayerById(reader.ReadByte());
                                var count = reader.ReadByte();
                                var effects = new List<EffectType>();
                                for (int i = 0; i < count; i++)
                                {
                                    effects.Add((EffectType)reader.ReadByte());
                                }
                                if (clericRole.CleansedPlayers.ContainsKey(cleansed.PlayerId))
                                {
                                    foreach (var effect in effects)
                                    {
                                        if (!clericRole.CleansedPlayers[cleansed.PlayerId].Contains(effect))
                                        {
                                            clericRole.CleansedPlayers[cleansed.PlayerId].Add(effect);
                                        }
                                    }
                                }
                                else clericRole.CleansedPlayers[cleansed.PlayerId] = effects;

                                if (PlayerControl.LocalPlayer == cleansed)
                                {
                                    if (effects.Contains(EffectType.Blind))
                                    {
                                        foreach (var e in Role.GetRoles(RoleEnum.Eclipsal))
                                        {
                                            var eRole = (Eclipsal)e;
                                            if (eRole.TimeRemaining > CustomGameOptions.BlindDuration - 1f) eRole.TimeRemaining = (eRole.TimeRemaining - CustomGameOptions.BlindDuration) * -1f;
                                            else if (eRole.TimeRemaining > 1f) eRole.TimeRemaining = 1f;
                                        }
                                    }
                                    if (effects.Contains(EffectType.Flash))
                                    {
                                        foreach (var gr in Role.GetRoles(RoleEnum.Grenadier))
                                        {
                                            var grRole = (Grenadier)gr;
                                            if (grRole.TimeRemaining > CustomGameOptions.GrenadeDuration - 0.5f) grRole.TimeRemaining = (grRole.TimeRemaining - CustomGameOptions.GrenadeDuration) * -1f;
                                            else if (grRole.TimeRemaining > 0.5f) grRole.TimeRemaining = 0.5f;
                                        }
                                    }
                                    if (effects.Contains(EffectType.Hack))
                                    {
                                        foreach (var g in Role.GetRoles(RoleEnum.Glitch))
                                        {
                                            var gRole = (Glitch)g;
                                            if (gRole.Hacked == PlayerControl.LocalPlayer) gRole.Hacked = null;
                                        }
                                    }
                                }
                                if (MeetingHud.Instance)
                                {
                                    if (effects.Contains(EffectType.Blackmail) && cleansed.IsBlackmailed())
                                    {
                                        try
                                        {
                                            foreach (var role in Role.GetRoles(RoleEnum.Blackmailer))
                                            {
                                                foreach (var voteArea in MeetingHud.Instance.playerStates)
                                                {
                                                    var bmer = (Blackmailer)role;
                                                    if (voteArea.TargetPlayerId == bmer.Blackmailed.PlayerId)
                                                    {
                                                        if (BlackmailMeetingUpdate.PrevXMark != null && BlackmailMeetingUpdate.PrevOverlay != null)
                                                        {
                                                            voteArea.XMark.sprite = BlackmailMeetingUpdate.PrevXMark;
                                                            voteArea.Overlay.sprite = BlackmailMeetingUpdate.PrevOverlay;
                                                            voteArea.XMark.transform.localPosition = new Vector3(
                                                                voteArea.XMark.transform.localPosition.x - BlackmailMeetingUpdate.LetterXOffset,
                                                                voteArea.XMark.transform.localPosition.y - BlackmailMeetingUpdate.LetterYOffset,
                                                                voteArea.XMark.transform.localPosition.z);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    AddMeetingButtons.CleansePlayer(cleansed.PlayerId, effects);
                                }
                                break;
                        }
                        break;
                    case CustomRPC.AbilityTrigger:
                        var abilityUser = Utils.PlayerById(reader.ReadByte());
                        var abilitytargetId = reader.ReadByte();
                        var abilitytarget = abilitytargetId == byte.MaxValue ? null : Utils.PlayerById(abilitytargetId);
                        if (PlayerControl.LocalPlayer.Is(ModifierEnum.SixthSense) && !PlayerControl.LocalPlayer.Data.IsDead && abilitytarget == PlayerControl.LocalPlayer)
                        {
                            Coroutines.Start(Utils.FlashCoroutine(Colors.SixthSense));
                        }
                        foreach (Role hunterRole2 in Role.GetRoles(RoleEnum.Hunter))
                        {
                            Hunter hunter = (Hunter)hunterRole2;
                            if (hunter.StalkedPlayer == abilityUser) hunter.RpcCatchPlayer(abilityUser);
                        }
                        if (PlayerControl.LocalPlayer.Is(RoleEnum.Aurial) && !PlayerControl.LocalPlayer.Data.IsDead)
                        {
                            var aurial = Role.GetRole<Aurial>(PlayerControl.LocalPlayer);
                            Coroutines.Start(aurial.Sense(abilityUser));
                        }
                        else if (PlayerControl.LocalPlayer.Is(RoleEnum.Lookout) && abilitytarget != null)
                        {
                            var lookout = Role.GetRole<Lookout>(PlayerControl.LocalPlayer);
                            if (lookout.Watching.ContainsKey(abilitytargetId))
                            {
                                RoleEnum playerRole = Role.GetRole(Utils.PlayerById(abilityUser.PlayerId)).RoleType;
                                if (!lookout.Watching[abilitytargetId].Contains(playerRole)) lookout.Watching[abilitytargetId].Add(playerRole);
                            }
                        }
                        else if (PlayerControl.LocalPlayer.Is(RoleEnum.Mercenary) && abilitytarget != null)
                        {
                            var merc = Role.GetRole<Mercenary>(PlayerControl.LocalPlayer);
                            if (merc.Guarded.Contains(abilitytargetId))
                            {
                                merc.Gold += 1;
                            }
                        }
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
        public static class RpcSetRole
        {
            public static void Postfix()
            {
                PluginSingleton<TownOfUs>.Instance.Log.LogMessage("RPC SET ROLE");
                var infected = GameData.Instance.AllPlayers.ToArray().Where(o => o.IsImpostor());

                Utils.ShowDeadBodies = false;
                if (ShowShield.DiedFirst != null && CustomGameOptions.FirstDeathShield)
                {
                    var shielded = false;
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (player.name == ShowShield.DiedFirst)
                        {
                            ShowShield.FirstRoundShielded = player;
                            shielded = true;
                        }
                    }
                    if (!shielded) ShowShield.FirstRoundShielded = null;
                }
                else ShowShield.FirstRoundShielded = null;
                ShowShield.DiedFirst = "";
                Role.NobodyWins = false;
                Role.ForceGameEnd = false;
                Role.SurvOnlyWins = false;
                Role.VampireWins = false;
                ExileControllerPatch.lastExiled = null;
                PatchKillTimer.GameStarted = false;
                StartImitate.ImitatingPlayers.Clear();
                ChatCommands.JailorMessage = false;
                AddHauntPatch.AssassinatedPlayers.Clear();
                CrewmateInvestigativeRoles.Clear();
                CrewmateKillingRoles.Clear();
                CrewmatePowerRoles.Clear();
                CrewmateProtectiveRoles.Clear();
                CrewmateSupportRoles.Clear();
                NeutralBenignRoles.Clear();
                NeutralEvilRoles.Clear();
                NeutralKillingRoles.Clear();
                ImpostorConcealingRoles.Clear();
                ImpostorKillingRoles.Clear();
                ImpostorSupportRoles.Clear();
                CrewmateModifiers.Clear();
                GlobalModifiers.Clear();
                ImpostorModifiers.Clear();
                AssassinModifiers.Clear();
                AssassinAbility.Clear();

                RecordRewind.points.Clear();
                Murder.KilledPlayers.Clear();
                HudUpdate.Zooming = false;
                HudUpdate.ZoomStart();

                if (ShowShield.FirstRoundShielded != null)
                {
                    Utils.Rpc(CustomRPC.Start, ShowShield.FirstRoundShielded.PlayerId);
                }
                else
                {
                    Utils.Rpc(CustomRPC.Start, byte.MaxValue);
                }

                if (GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek) return;

                SetPhantom.PhantomOn = Check(CustomGameOptions.PhantomOn);
                SetHaunter.HaunterOn = Check(CustomGameOptions.HaunterOn);
                SetTraitor.TraitorOn = Check(CustomGameOptions.TraitorOn);

                Utils.Rpc(CustomRPC.SetPostRoles, SetPhantom.PhantomOn ? (byte)0 : (byte)1,
                    SetHaunter.HaunterOn ? (byte)0 : (byte)1, SetTraitor.TraitorOn ? (byte)0 : (byte)1);

                #region Crewmate Roles
                if (CustomGameOptions.PoliticianOn > 0)
                    CrewmatePowerRoles.Add((typeof(Politician), CustomGameOptions.PoliticianOn, true));
                
                if (CustomGameOptions.PresidentOn > 0)
                    CrewmatePowerRoles.Add((typeof(President), CustomGameOptions.PresidentOn, true));

                if (CustomGameOptions.SheriffOn > 0)
                    CrewmateKillingRoles.Add((typeof(Sheriff), CustomGameOptions.SheriffOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.EngineerOn > 0)
                    CrewmateSupportRoles.Add((typeof(Engineer), CustomGameOptions.EngineerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.SwapperOn > 0)
                    CrewmatePowerRoles.Add((typeof(Swapper), CustomGameOptions.SwapperOn, true));

                if (CustomGameOptions.InvestigatorOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Investigator), CustomGameOptions.InvestigatorOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.MedicOn > 0)
                    CrewmateProtectiveRoles.Add((typeof(Medic), CustomGameOptions.MedicOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.SeerOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Seer), CustomGameOptions.SeerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.SpyOn > 0 && GameOptionsManager.Instance.currentNormalGameOptions.MapId != 5)
                    CrewmateInvestigativeRoles.Add((typeof(Spy), CustomGameOptions.SpyOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.SnitchOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Snitch), CustomGameOptions.SnitchOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.AltruistOn > 0)
                    CrewmateProtectiveRoles.Add((typeof(Altruist), CustomGameOptions.AltruistOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.VigilanteOn > 0)
                    CrewmateKillingRoles.Add((typeof(Vigilante), CustomGameOptions.VigilanteOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.VeteranOn > 0)
                    CrewmateKillingRoles.Add((typeof(Veteran), CustomGameOptions.VeteranOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.HunterOn > 0)
                    CrewmateKillingRoles.Add((typeof(Hunter), CustomGameOptions.HunterOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.TrackerOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Tracker), CustomGameOptions.TrackerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.TransporterOn > 0)
                    CrewmateSupportRoles.Add((typeof(Transporter), CustomGameOptions.TransporterOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.MediumOn > 0)
                    CrewmateSupportRoles.Add((typeof(Medium), CustomGameOptions.MediumOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.MysticOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Mystic), CustomGameOptions.MysticOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.TrapperOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Trapper), CustomGameOptions.TrapperOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.DetectiveOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Detective), CustomGameOptions.DetectiveOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.ImitatorOn > 0)
                    CrewmateSupportRoles.Add((typeof(Imitator), CustomGameOptions.ImitatorOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.ProsecutorOn > 0)
                    CrewmatePowerRoles.Add((typeof(Prosecutor), CustomGameOptions.ProsecutorOn, true));

                if (CustomGameOptions.OracleOn > 0)
                    CrewmateProtectiveRoles.Add((typeof(Oracle), CustomGameOptions.OracleOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.AurialOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Aurial), CustomGameOptions.AurialOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.WardenOn > 0)
                    CrewmateProtectiveRoles.Add((typeof(Warden), CustomGameOptions.WardenOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.JailorOn > 0)
                    CrewmatePowerRoles.Add((typeof(Jailor), CustomGameOptions.JailorOn, true));

                if (CustomGameOptions.LookoutOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Lookout), CustomGameOptions.LookoutOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.DeputyOn > 0)
                    CrewmateKillingRoles.Add((typeof(Deputy), CustomGameOptions.DeputyOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.PlumberOn > 0)
                    CrewmateSupportRoles.Add((typeof(Plumber), CustomGameOptions.PlumberOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.ClericOn > 0)
                    CrewmateProtectiveRoles.Add((typeof(Cleric), CustomGameOptions.ClericOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.CaptainOn > 0)
                    CrewmateInvestigativeRoles.Add((typeof(Captain), CustomGameOptions.CaptainOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.TimeLordOn > 0)
                    CrewmatePowerRoles.Add((typeof(TimeLord), CustomGameOptions.TimeLordOn, true));

                #endregion
                #region Neutral Roles
                if (CustomGameOptions.JesterOn > 0)
                    NeutralEvilRoles.Add((typeof(Jester), CustomGameOptions.JesterOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.AmnesiacOn > 0)
                    NeutralBenignRoles.Add((typeof(Amnesiac), CustomGameOptions.AmnesiacOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.ExecutionerOn > 0)
                    NeutralEvilRoles.Add((typeof(Executioner), CustomGameOptions.ExecutionerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.ForetellerOn > 0)
                    NeutralEvilRoles.Add((typeof(Foreteller), CustomGameOptions.ForetellerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.SoulCollectorOn > 0)
                    NeutralKillingRoles.Add((typeof(SoulCollector), CustomGameOptions.SoulCollectorOn, true));

                if (CustomGameOptions.SurvivorOn > 0)
                    NeutralBenignRoles.Add((typeof(Survivor), CustomGameOptions.SurvivorOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.GuardianAngelOn > 0)
                    NeutralBenignRoles.Add((typeof(GuardianAngel), CustomGameOptions.GuardianAngelOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.GlitchOn > 0)
                    NeutralKillingRoles.Add((typeof(Glitch), CustomGameOptions.GlitchOn, true));

                if (CustomGameOptions.IcenbergOn > 0)
                    NeutralKillingRoles.Add((typeof(Icenberg), CustomGameOptions.IcenbergOn, true));

                if (CustomGameOptions.ArsonistOn > 0)
                    NeutralKillingRoles.Add((typeof(Arsonist), CustomGameOptions.ArsonistOn, true));

                if (CustomGameOptions.PlaguebearerOn > 0)
                    NeutralKillingRoles.Add((typeof(Plaguebearer), CustomGameOptions.PlaguebearerOn, true));

                if (CustomGameOptions.WerewolfOn > 0)
                    NeutralKillingRoles.Add((typeof(Werewolf), CustomGameOptions.WerewolfOn, true));

                if (CustomGameOptions.VampireOn > 0)
                    NeutralKillingRoles.Add((typeof(Vampire), CustomGameOptions.VampireOn, true));

                if (CustomGameOptions.JuggernautOn > 0)
                    NeutralKillingRoles.Add((typeof(Juggernaut), CustomGameOptions.JuggernautOn, true));

                if (CustomGameOptions.MercenaryOn > 0)
                    NeutralBenignRoles.Add((typeof(Mercenary), CustomGameOptions.MercenaryOn, false || CustomGameOptions.UniqueRoles));

               
                #endregion
                #region Impostor Roles
                if (CustomGameOptions.UndertakerOn > 0)
                    ImpostorSupportRoles.Add((typeof(Undertaker), CustomGameOptions.UndertakerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.MorphlingOn > 0)
                    ImpostorConcealingRoles.Add((typeof(Morphling), CustomGameOptions.MorphlingOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.BlackmailerOn > 0)
                    ImpostorSupportRoles.Add((typeof(Blackmailer), CustomGameOptions.BlackmailerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.MinerOn > 0)
                    ImpostorSupportRoles.Add((typeof(Miner), CustomGameOptions.MinerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.SwooperOn > 0)
                    ImpostorConcealingRoles.Add((typeof(Swooper), CustomGameOptions.SwooperOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.JanitorOn > 0)
                    ImpostorSupportRoles.Add((typeof(Janitor), CustomGameOptions.JanitorOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.GrenadierOn > 0)
                    ImpostorConcealingRoles.Add((typeof(Grenadier), CustomGameOptions.GrenadierOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.EscapistOn > 0)
                    ImpostorConcealingRoles.Add((typeof(Escapist), CustomGameOptions.EscapistOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.BomberOn > 0)
                    ImpostorKillingRoles.Add((typeof(Bomber), CustomGameOptions.BomberOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.WarlockOn > 0)
                    ImpostorKillingRoles.Add((typeof(Warlock), CustomGameOptions.WarlockOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.VenererOn > 0)
                    ImpostorConcealingRoles.Add((typeof(Venerer), CustomGameOptions.VenererOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.HypnotistOn > 0)
                    ImpostorSupportRoles.Add((typeof(Hypnotist), CustomGameOptions.HypnotistOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.ScavengerOn > 0)
                    ImpostorKillingRoles.Add((typeof(Scavenger), CustomGameOptions.ScavengerOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.EclipsalOn > 0)
                    ImpostorConcealingRoles.Add((typeof(Eclipsal), CustomGameOptions.EclipsalOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.WraithOn > 0)
                    ImpostorConcealingRoles.Add((typeof(Wraith), CustomGameOptions.WraithOn, false || CustomGameOptions.UniqueRoles));

                if (CustomGameOptions.KamikazeOn > 0)
                    ImpostorSupportRoles.Add((typeof(Kamikaze), CustomGameOptions.KamikazeOn, true));
                #endregion
                #region Crewmate Modifiers
                if (Check(CustomGameOptions.TorchOn) && GameOptionsManager.Instance.currentNormalGameOptions.MapId != 5)
                    CrewmateModifiers.Add((typeof(Torch), CustomGameOptions.TorchOn));

                if (Check(CustomGameOptions.DiseasedOn))
                    CrewmateModifiers.Add((typeof(Diseased), CustomGameOptions.DiseasedOn));

                if (Check(CustomGameOptions.BaitOn))
                    CrewmateModifiers.Add((typeof(Bait), CustomGameOptions.BaitOn));

                if (Check(CustomGameOptions.AftermathOn))
                    CrewmateModifiers.Add((typeof(Aftermath), CustomGameOptions.AftermathOn));

                if (Check(CustomGameOptions.MultitaskerOn))
                    CrewmateModifiers.Add((typeof(Multitasker), CustomGameOptions.MultitaskerOn));

                if (Check(CustomGameOptions.FrostyOn))
                    CrewmateModifiers.Add((typeof(Frosty), CustomGameOptions.FrostyOn));

                if (Check(CustomGameOptions.CelebrityOn))
                    CrewmateModifiers.Add((typeof(Celebrity), CustomGameOptions.CelebrityOn));

                if (Check(CustomGameOptions.TaskmasterOn) && !SubmergedCompatibility.isSubmerged())
                    CrewmateModifiers.Add((typeof(Taskmaster), CustomGameOptions.TaskmasterOn));
                #endregion
                #region Global Modifiers
                if (Check(CustomGameOptions.TiebreakerOn))
                    GlobalModifiers.Add((typeof(Tiebreaker), CustomGameOptions.TiebreakerOn));

                if (Check(CustomGameOptions.FlashOn))
                    GlobalModifiers.Add((typeof(Flash), CustomGameOptions.FlashOn));

                if (Check(CustomGameOptions.GiantOn))
                    GlobalModifiers.Add((typeof(Giant), CustomGameOptions.GiantOn));

                if (Check(CustomGameOptions.ButtonBarryOn) && GameOptionsManager.Instance.currentNormalGameOptions.NumEmergencyMeetings > 0)
                    GlobalModifiers.Add((typeof(ButtonBarry), CustomGameOptions.ButtonBarryOn));

                if (Check(CustomGameOptions.LoversOn))
                    GlobalModifiers.Add((typeof(Lover), CustomGameOptions.LoversOn));

                if (Check(CustomGameOptions.SleuthOn))
                    GlobalModifiers.Add((typeof(Sleuth), CustomGameOptions.SleuthOn));

                if (Check(CustomGameOptions.RadarOn))
                    GlobalModifiers.Add((typeof(Radar), CustomGameOptions.RadarOn));

                if (Check(CustomGameOptions.SixthSenseOn))
                    GlobalModifiers.Add((typeof(SixthSense), CustomGameOptions.SixthSenseOn));

                if (Check(CustomGameOptions.ShyOn))
                    GlobalModifiers.Add((typeof(Shy), CustomGameOptions.ShyOn));

                if (Check(CustomGameOptions.MiniOn))
                    GlobalModifiers.Add((typeof(Mini), CustomGameOptions.MiniOn));

                if (Check(CustomGameOptions.ImmovableOn) && !SubmergedCompatibility.isSubmerged())
                    GlobalModifiers.Add((typeof(Immovable), CustomGameOptions.ImmovableOn));

                if (Check(CustomGameOptions.SatelliteOn))
                    GlobalModifiers.Add((typeof(Satellite), CustomGameOptions.SatelliteOn));
                #endregion
                #region Impostor Modifiers
                if (Check(CustomGameOptions.DisperserOn) && GameOptionsManager.Instance.currentNormalGameOptions.MapId < 4)
                    ImpostorModifiers.Add((typeof(Disperser), CustomGameOptions.DisperserOn));

                if (Check(CustomGameOptions.DoubleShotOn))
                    AssassinModifiers.Add((typeof(DoubleShot), CustomGameOptions.DoubleShotOn));

                if (Check(CustomGameOptions.SaboteurOn))
                    ImpostorModifiers.Add((typeof(Saboteur), CustomGameOptions.SaboteurOn));

                if (Check(CustomGameOptions.UnderdogOn))
                    ImpostorModifiers.Add((typeof(Underdog), CustomGameOptions.UnderdogOn));
                #endregion
                #region Assassin Ability
                AssassinAbility.Add((typeof(Assassin), CustomRPC.SetAssassin, 100));
                #endregion

                GenEachRole(infected.ToList());
            }
        }
    }
}
