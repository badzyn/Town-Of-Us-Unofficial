using HarmonyLib;
using TownOfUs.Extensions;
using TownOfUs.NeutralRoles.ExecutionerMod;
using TownOfUs.Roles;
using TownOfUs.Roles.Modifiers;
using UnityEngine;

namespace TownOfUs.NeutralRoles.GuardianAngelMod
{
    public enum BecomeOptions
    {
        Crew,
        Amnesiac,
        Mercenary,
        Survivor,
        Jester
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class GATargetColor
    {
        private static void UpdateMeeting(MeetingHud __instance, GuardianAngel role)
        {
            if (CustomGameOptions.GAKnowsTargetRole) return;
            foreach (var player in __instance.playerStates)
                if (player.TargetPlayerId == role.target.PlayerId)
                    player.NameText.color = new Color(1f, 0.85f, 0f, 1f);
        }

        private static void Postfix(HudManager __instance)
        {
            if (PlayerControl.AllPlayerControls.Count <= 1) return;
            if (PlayerControl.LocalPlayer == null) return;
            if (PlayerControl.LocalPlayer.Data == null) return;
            if (!PlayerControl.LocalPlayer.Is(RoleEnum.GuardianAngel)) return;
            if (PlayerControl.LocalPlayer.Data.IsDead) return;

            var role = Role.GetRole<GuardianAngel>(PlayerControl.LocalPlayer);

            if (MeetingHud.Instance != null) UpdateMeeting(MeetingHud.Instance, role);

            if (!PlayerControl.LocalPlayer.IsHypnotised() && !Utils.CommsCamouflaged())
            {
                if (!CustomGameOptions.GAKnowsTargetRole)
                {
                    var colour = new Color(1f, 0.85f, 0f);
                    if (role.target.Is(ModifierEnum.Shy)) colour.a = Modifier.GetModifier<Shy>(role.target).Opacity;
                    role.target.nameText().color = colour;
                }
            }

            if (!role.target.Data.IsDead && !role.target.Data.Disconnected) return;

            Utils.Rpc(CustomRPC.GAToSurv, PlayerControl.LocalPlayer.PlayerId);

            Object.Destroy(role.UsesText);
            HudManager.Instance.KillButton.gameObject.SetActive(false);

            GAToSurv(PlayerControl.LocalPlayer);
        }

        public static void GAToSurv(PlayerControl player)
        {
            player.myTasks.RemoveAt(0);
            Role.RoleDictionary.Remove(player.PlayerId);

            if (CustomGameOptions.GaOnTargetDeath == BecomeOptions.Jester)
            {
                var jester = new Jester(player);
                jester.SpawnedAs = false;
                jester.RegenTask();
            }
            else if (CustomGameOptions.GaOnTargetDeath == BecomeOptions.Amnesiac)
            {
                var amnesiac = new Amnesiac(player);
                amnesiac.SpawnedAs = false;
                amnesiac.RegenTask();
            }
            else if (CustomGameOptions.OnTargetDead == OnTargetDead.Mercenary)
            {
                var merc = new Mercenary(player);
                merc.SpawnedAs = false;
                merc.Gold = CustomGameOptions.GoldToBribe;
                merc.RegenTask();
            }
            else if (CustomGameOptions.GaOnTargetDeath == BecomeOptions.Survivor)
            {
                var surv = new Survivor(player);
                surv.SpawnedAs = false;
                surv.RegenTask();
            }
            else
            {
                new Crewmate(player);
            }
        }
    }
}