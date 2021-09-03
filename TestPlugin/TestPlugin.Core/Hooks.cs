using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Chara;
using System.Linq;
#if AI || HS2
    using AIChara;
#elif KK
    using KKAPI.MainGame;
#endif


namespace KK_TestPlugin
{
    public partial class TestPlugin
    {

        private static class Hooks
        {
            public static void InitHooks(Harmony harmonyInstance)
            {
                harmonyInstance.PatchAll(typeof(Hooks));
            }

            #if KK

                // /// <summary>
                // /// Trigger the ClothesStateChangeEvent for toggling on and off a clothing item
                // /// </summary>
                // [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
                // private static void AssetLoading_hook(ChaControl __instance, int clothesKind)
                // {
                //     TestPlugin.Logger.LogWarning($" AssetLoading_hook()");                               
                // }

            #endif

        }
    }
}
