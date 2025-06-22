using HarmonyLib;

namespace TownOfUs 
{
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    public static class PingShowerPatch
    {
        public static void Postfix(PingTracker __instance)
        {
            __instance.text.text += $"<line-height=50%><indent=25%>\n<size=70%><align=center><color=#EE9D01FF><i>Town of Us</color></i> <color=#D91919FF><b>Unofficial</b></color><color=#FFFFFFFF> v{TownOfUs.CompilationString}</color></align></indent>\n\n\n\n";
        }
    }
}