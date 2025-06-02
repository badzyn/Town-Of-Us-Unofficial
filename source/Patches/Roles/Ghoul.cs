using System;
using System.Linq;
using UnityEngine;
using TownOfUs.Extensions;
using AmongUs.GameOptions;

namespace TownOfUs.Roles
{
    public class Ghoul : Role
    {
        private KillButton _eatButton;

        public DeadBody CurrentTarget;
        public DateTime LastEaten { get; set; }
        public int EatenCount { get; set; } = 0;
        public float CurrentEatCd { get; set; }
        public bool GhoulWins { get; set; }
        public bool CanKill { get; set; } = false;

        public Ghoul(PlayerControl player) : base(player)
        {
            Name = "Ghoul";
            ImpostorText = () => "Eat bodies, kill everyone";
            TaskText = () => "Eat bodies, kill everyone";
            Color = Patches.Colors.Ghoul;
            LastEaten = DateTime.UtcNow;
            RoleType = RoleEnum.Ghoul;
            AddToRoleHistory(RoleType);
            Faction = Faction.NeutralKilling;
            CurrentEatCd = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown; // Używa KillCooldown
            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                Player.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown); // Startowy cooldown
            }
        }

        public KillButton eatButton
        {
            get => _eatButton;
            set => _eatButton = value;
        }

        public float EatTimer()
        {
            var utcNow = DateTime.UtcNow;
            var timeSpan = utcNow - LastEaten;
            var num = CurrentEatCd * 1000f;
            if (num - (float)timeSpan.TotalMilliseconds < 0f) return 0;
            return (num - (float)timeSpan.TotalMilliseconds) / 1000f;
        }

        public float SharedCooldown()
        {
            var elapsed = (float)(DateTime.UtcNow - LastEaten).TotalSeconds;
            return Mathf.Clamp(CurrentEatCd - elapsed, 0f, CurrentEatCd);
        }

        public bool CanEat()
        {
            if (SharedCooldown() > 0f) return false;
            var truePosition = Player.GetTruePosition();
            var maxDistance = LegacyGameOptions.KillDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
            var bodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            return bodies.Any(body => Vector2.Distance(truePosition, body.TruePosition) <= maxDistance);
        }

        public void ApplyBuffs()
        {
            float minCd = 5f;
            if (CurrentEatCd > minCd)
            {
                CurrentEatCd -= 2f;
                if (CurrentEatCd < minCd) CurrentEatCd = minCd;
            }
        }

        public void Wins()
        {
            GhoulWins = true;
        }

        internal override bool GameEnd(LogicGameFlowNormal __instance)
        {
            if (Player.Data.IsDead || Player.Data.Disconnected) return true;
            if (PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead && !x.Data.Disconnected && x.IsLover()) == 2) return false;
            if (PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead && !x.Data.Disconnected && (x.Data.IsImpostor() || x.Is(Faction.NeutralKilling) || x.IsCrewKiller())) > 1) return false;

            if (PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead && !x.Data.Disconnected && (x.Data.IsImpostor() || x.Is(Faction.NeutralKilling) || x.Is(Faction.Crewmates))) == 1 ||
                PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead && !x.Data.Disconnected) <= 2)
            {
                Utils.Rpc(CustomRPC.GhoulWin, Player.PlayerId);
                Wins();
                Utils.EndGame();
                return false;
            }
            return false;
        }

        protected override void IntroPrefix(IntroCutscene._ShowTeam_d__38 __instance)
        {
            var ghoulTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            ghoulTeam.Add(PlayerControl.LocalPlayer);
            __instance.teamToShow = ghoulTeam;
        }
    }
}