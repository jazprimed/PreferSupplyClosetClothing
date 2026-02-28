using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static WearableAccessorizer;

namespace PreferSupplyClosetClothing
{
    [HarmonyPatch(typeof(WearableAccessorizer), "GetHighestAccessory")]
    internal class OverwriteAccessoryPriority
    {
        static WearableAccessorizer.WearableType Postfix(WearableAccessorizer.WearableType __result, WearableAccessorizer __instance)
        {
            Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable> wearables = Traverse.Create(__instance).Field("wearables").GetValue<Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable>>();
            if (__result == WearableAccessorizer.WearableType.Outfit & wearables.Keys.ToList().Contains(WearableAccessorizer.WearableType.CustomClothing))
            {
                return WearableAccessorizer.WearableType.CustomClothing;
            }
            else
            {
                return __result;
            }
        }
    }


    [HarmonyPatch(typeof(WearableAccessorizer), "ApplyWearable")]
    internal class OverwriteApplyWearable
    {
        static void Prefix(WearableAccessorizer __instance)
        {
            Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable> wearables = Traverse.Create(__instance).Field("wearables").GetValue<Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable>>();
            foreach (WearableAccessorizer.WearableType key in wearables.Keys)
            {
                Wearable value = wearables[key];
                if (key == WearableAccessorizer.WearableType.CustomClothing)
                {
                    value.buildOverridePriority = 6;
                }
            }
        }
    }
}
