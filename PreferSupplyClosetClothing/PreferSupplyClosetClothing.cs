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

            Debug.Log("__GetHighestAccessory__");
            Debug.Log("Dupe: " + __instance.GetProperName());
            Debug.Log("initial result?: " + __result.ToString());


            foreach (ClothingOutfitUtility.OutfitType key in __instance.GetCustomClothingItems().Keys)
            {
                Debug.Log("clothing types: " + key.ToString());
                for (int i = 0; i < __instance.GetCustomClothingItems()[key].Count; i++)
                {
                    Debug.Log("GetCustomClothingItems: " + __instance.GetCustomClothingItems()[key][i].Get().Name);
                }
            }


            foreach (WearableAccessorizer.WearableType key in wearables.Keys)
            {
                Debug.Log("wearables: " + key.ToString());
                Wearable value = wearables[key];
                Debug.Log("buildOverridePriority: " + value.buildOverridePriority);
            }

            if (__result == WearableAccessorizer.WearableType.Outfit & wearables.Keys.ToList().Contains(WearableAccessorizer.WearableType.CustomClothing))
            {
                Debug.Log("return CustomClothing");
                return WearableAccessorizer.WearableType.CustomClothing;
            }
            else
            {
                Debug.Log("return " + __result.ToString());
                return __result;
            }
        }
    }


    [HarmonyPatch(typeof(WearableAccessorizer), "ApplyWearable")]
    internal class OverwriteApplyWearable
    {
        static void Prefix(WearableAccessorizer __instance)
        {

            Debug.Log("__ApplyWearable__");
            Debug.Log("GetProperName: " + __instance.GetProperName());

            Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable> wearables = Traverse.Create(__instance).Field("wearables").GetValue<Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable>>();
            foreach (WearableAccessorizer.WearableType key in wearables.Keys)
            {
                Debug.Log("wearables: " + key.ToString());
                Wearable value = wearables[key];
                Debug.Log("buildOverridePriority: " + value.buildOverridePriority);
                if (key == WearableAccessorizer.WearableType.CustomClothing)
                {
                    value.buildOverridePriority = 6;
                }
            }

        }
    }
}
