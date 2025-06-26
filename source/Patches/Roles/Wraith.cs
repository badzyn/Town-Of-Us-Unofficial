using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using System;
using TownOfUs.Patches;
using UnityEngine;

namespace TownOfUs.Roles
{
    public class Wraith : Role
    {
        private KillButton _noclipButton;
        public DateTime LastNoclip;
        public bool Enabled;
        public Vector3 NoclipSafePoint = new();
        public float TimeRemaining;

        public Wraith(PlayerControl player) : base(player)
        {
            Name = "Wraith";
            ImpostorText = () => "Walk Through Walls and Kill Your Enemies";
            TaskText = () => "Walk through walls like a ghost";
            Color = Patches.Colors.Impostor;
            RoleType = RoleEnum.Wraith;
            AddToRoleHistory(RoleType);
            Faction = Faction.Impostors;

        }

        public KillButton NoclipButton
        {
            get => _noclipButton;
            set
            {
                _noclipButton = value;
                ExtraButtons.Clear();
                ExtraButtons.Add(value);
            }
        }
        public bool Noclipped => TimeRemaining > 0f;
        public float NoclipTimer()
        {
            var utcNow = DateTime.UtcNow;
            var timeSpan = utcNow - LastNoclip;
            var cooldown = CustomGameOptions.WraithCd * 1000f;
            var flag2 = cooldown - (float)timeSpan.TotalMilliseconds < 0f;
            if (flag2) return 0;
            return (cooldown - (float)timeSpan.TotalMilliseconds) / 1000f;
        }

        public void WallWalk()
        {
            Enabled = true;
            TimeRemaining -= Time.deltaTime;
            Player.Collider.enabled = false;
            if (Player.Data.IsDead)
            {
                TimeRemaining = 0f;
            }
        }
        public void UnWallWalk()
        {
            Enabled = false;
            LastNoclip = DateTime.UtcNow;
            Player.Collider.enabled = true;
        }
    }
}