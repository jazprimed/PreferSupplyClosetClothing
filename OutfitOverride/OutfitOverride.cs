using HarmonyLib;
using KMod;
using OutfitOverride;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core.Tokens;
using static STRINGS.ROOMS.TYPES;
using static WearableAccessorizer;

namespace PreferSupplyClosetClothing
{

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary(false);
            new POptions().RegisterOptions(this, typeof(Config));
            base.OnLoad(harmony);
            Debug.Log("OnLoad");
        }
    }


    [HarmonyPatch(typeof(WearableAccessorizer), "GetHighestAccessory")]
    internal class OverwriteAccessoryPriority
    {
        // Anim names of the different outfit types
        const string snazzyAnim = "body_shirt_decor01_kanim";
        const string warmCoatAnim = "body_shirt_hot_shearling_kanim";

        static WearableAccessorizer.WearableType Postfix(WearableAccessorizer.WearableType __result, WearableAccessorizer __instance)
        {
            Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable> wearables = Traverse.Create(__instance).Field("wearables").GetValue<Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable>>();

            // Only consider overwriting when the current highest accessory is Outfit and CustomClothing exists
            bool overrideOutfit = __result == WearableAccessorizer.WearableType.Outfit && wearables.ContainsKey(WearableAccessorizer.WearableType.CustomClothing);

            if (!overrideOutfit)
                return __result;

            if (!wearables.TryGetValue(WearableAccessorizer.WearableType.Outfit, out var outfitWearable) || outfitWearable == null)
                return __result;

            // If we have the Outfit wearable, detect the specific anims.
            bool hasSnazzy = outfitWearable.AnimNames != null && outfitWearable.AnimNames.Contains(snazzyAnim);
            bool hasWarmCoat = outfitWearable.AnimNames != null && outfitWearable.AnimNames.Contains(warmCoatAnim);

            // Snazzy suit, always override
            if (hasSnazzy)
            {
                return WearableAccessorizer.WearableType.CustomClothing;
            }

            // If warm coat equipped, check the config
            if (hasWarmCoat)
            {
                // Only override warm coats if enabled
                if (Config.Instance.OverrideWarmCoats)
                {
                    return WearableAccessorizer.WearableType.CustomClothing;
                }

                return __result;
            }

            // Outfit isn't snazzy or warm coat, only override if Primo is enabled
            if (Config.Instance.OverridePrimoGarb)
            {
                return WearableAccessorizer.WearableType.CustomClothing;
            }

            return __result;
        }
    }


    [HarmonyPatch(typeof(WearableAccessorizer), "ApplyWearable")]
    internal class OverwriteApplyWearable
    {
        static void Prefix(WearableAccessorizer __instance)
        {
            Debug.Log("ApplyWearable");
            Debug.Log("Dupe: " + __instance.GetProperName());
            Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable> wearables = Traverse.Create(__instance).Field("wearables").GetValue<Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable>>();

            if (wearables.TryGetValue(WearableAccessorizer.WearableType.CustomClothing, out var customWearable))
            {
                customWearable.buildOverridePriority = 6;
            }
        }
    }
}
