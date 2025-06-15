using System.Linq;
using HarmonyLib;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using TownOfUs.Roles.Modifiers;
using UnityEngine;
using AmongUs.GameOptions;

namespace TownOfUs.NeutralRoles.ArsonistMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdate
    {
        public static Sprite IgniteSprite => TownOfUs.IgniteSprite;

        public static void Postfix(HudManager __instance)
        {
            if (PlayerControl.AllPlayerControls.Count <= 1) return;
            if (PlayerControl.LocalPlayer?.Data == null) return;
            if (!PlayerControl.LocalPlayer.Is(RoleEnum.Arsonist)) return;
            var role = Role.GetRole<Arsonist>(PlayerControl.LocalPlayer);

            if (!PlayerControl.LocalPlayer.IsHypnotised() && !Utils.CommsCamouflaged())
            {
                foreach (var playerId in role.DousedPlayers)
                {
                    var player = Utils.PlayerById(playerId);
                    if (player?.Data == null || player.Data.Disconnected || player.Data.IsDead) continue;

                    player.myRend().material.SetColor("_VisorColor", role.Color);
                    var nameCol = Color.black;
                    if (player.Is(ModifierEnum.Shy)) nameCol.a = Modifier.GetModifier<Shy>(player).Opacity;
                    player.nameText().color = nameCol;
                }
            }

            if (role.IgniteButton == null)
            {
                role.IgniteButton = Object.Instantiate(__instance.KillButton, __instance.KillButton.transform.parent);
                role.IgniteButton.graphic.enabled = true;
                role.IgniteButton.gameObject.SetActive(false);
            }

            role.IgniteButton.graphic.sprite = IgniteSprite;
            role.IgniteButton.transform.localPosition = new Vector3(-2f, 0f, 0f);
            if (PlayerControl.LocalPlayer.Data.IsDead) role.IgniteButton.SetTarget(null);

            bool canShow = (__instance.UseButton.isActiveAndEnabled || __instance.PetButton.isActiveAndEnabled)
                           && !MeetingHud.Instance
                           && !PlayerControl.LocalPlayer.Data.IsDead
                           && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started;

            __instance.KillButton.gameObject.SetActive(canShow);
            role.IgniteButton.gameObject.SetActive(canShow);

            __instance.KillButton.SetCoolDown(role.DouseTimer(), CustomGameOptions.DouseCd);
            role.IgniteButton.SetCoolDown(role.DouseTimer(), CustomGameOptions.DouseCd);

            var notDoused = PlayerControl.AllPlayerControls.ToArray().Where(p => !role.DousedPlayers.Contains(p.PlayerId)).ToList();
            if ((CamouflageUnCamouflage.IsCamoed && CustomGameOptions.CamoCommsKillAnyone) || PlayerControl.LocalPlayer.IsHypnotised())
                Utils.SetTarget(ref role.ClosestPlayer, __instance.KillButton, float.NaN, notDoused);
            else if (role.Player.IsLover())
                Utils.SetTarget(ref role.ClosestPlayer, __instance.KillButton, float.NaN,
                    PlayerControl.AllPlayerControls.ToArray().Where(x => !x.IsLover() && !role.DousedPlayers.Contains(x.PlayerId)).ToList());
            else
                Utils.SetTarget(ref role.ClosestPlayer, __instance.KillButton, float.NaN, notDoused);

            var doused = PlayerControl.AllPlayerControls.ToArray().Where(p => role.DousedPlayers.Contains(p.PlayerId)).ToList();
            var closestTarget = Utils.GetClosestPlayer(PlayerControl.LocalPlayer, doused, true);

            foreach (var player in doused)
            {
                var rend = player.myRend();
                if (rend != null)
                    rend.material.SetFloat("_Outline", 0f);
            }

            
            if (closestTarget != null)
            {
                var killDist = LegacyGameOptions.KillDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
                float dist = Vector2.Distance(closestTarget.transform.localPosition, PlayerControl.LocalPlayer.transform.localPosition);

                if (dist <= killDist)
                {
                    role.CanIgnite = true;
                    var rend = closestTarget.myRend();
                    if (rend != null)
                    {
                        rend.material.SetColor("_OutlineColor", new Color(1f, 0.5f, 0f, 1f));
                        rend.material.SetFloat("_Outline", 1f);
                    }
                }
                else
                {
                    role.CanIgnite = false;
                }
            }
            else
            {
                role.CanIgnite = false;
            }

            
            var btnGraphic = role.IgniteButton.graphic;
            if (role.CanIgnite && PlayerControl.LocalPlayer.moveable && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                btnGraphic.color = Palette.EnabledColor;
                btnGraphic.material.SetFloat("_Desat", 0f);
            }
            else
            {
                btnGraphic.color = Palette.DisabledClear;
                btnGraphic.material.SetFloat("_Desat", 1f);
            }
        }
    }
}
