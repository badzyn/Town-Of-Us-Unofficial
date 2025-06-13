using UnityEngine;
using System.Collections;

namespace TownOfUs.Patches
{
    public class HostManager
    {
        public static bool starting;
        public static IEnumerator AutoRejoin()
        {
            if (!AmongUsClient.Instance.AmHost) yield break;

            yield return new WaitForSeconds(CustomGameOptions.RejoinSeconds);

            if (LobbyBehaviour.Instance || AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started
            || AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.NotJoined) yield break;
            
            DestroyableSingleton<EndGameNavigation>.Instance.NextGame();

            yield break;
        }
    }
}