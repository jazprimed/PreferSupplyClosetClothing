using HarmonyLib;
using KMod;
using KSerialization;
using OutfitOverride;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PreferSupplyClosetClothing
{

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary(false);
            new POptions().RegisterOptions(this, typeof(Config));
            base.OnLoad(harmony);
        }
    }


    [HarmonyPatch(typeof(WearableAccessorizer), "GetHighestAccessory")]
    internal class OverwriteAccessoryPriority
    {
        // Anim names of the different outfit types
        const string snazzyAnim = "body_shirt_decor01_kanim";
        const string warmCoatAnim = "body_shirt_hot_shearling_kanim";
        const string pajamasAnim = "body_pajamas_kanim";
        const string swimwearAnim = "body_wetsuit_kanim";

        static WearableAccessorizer.WearableType Postfix(WearableAccessorizer.WearableType __result, WearableAccessorizer __instance)
        {
            Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable> wearables = Traverse.Create(__instance).Field("wearables").GetValue<Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable>>();

            // Compute the current highest accessory directly from the wearables dictionary
            // instead of trusting the incoming __result. This prevents an early exit when __result
            // doesn't equal Outfit (for example during a toggle-triggered refresh).
            var currentHighest = wearables.Keys.Any() ? wearables.Keys.Max() : WearableAccessorizer.WearableType.Basic;

            // Only consider overwriting when the current highest accessory is Outfit and CustomClothing exists
            bool overrideOutfit = currentHighest == WearableAccessorizer.WearableType.Outfit && wearables.ContainsKey(WearableAccessorizer.WearableType.CustomClothing);

            if (!overrideOutfit)
                return __result;

            if (!wearables.TryGetValue(WearableAccessorizer.WearableType.Outfit, out var outfitWearable) || outfitWearable == null)
                return __result;

            // If we have the Outfit wearable, detect the specific anims.
            bool hasSnazzy = outfitWearable.AnimNames != null && outfitWearable.AnimNames.Contains(snazzyAnim);
            bool hasWarmCoat = outfitWearable.AnimNames != null && outfitWearable.AnimNames.Contains(warmCoatAnim);
            bool hasPajamas = outfitWearable.AnimNames != null && outfitWearable.AnimNames.Contains(pajamasAnim);
            bool hasSwimwear = outfitWearable.AnimNames != null && outfitWearable.AnimNames.Contains(swimwearAnim);

            // Decide whether the normal override logic would want to override
            bool shouldOverride = false;

            // Snazzy suit, always override
            if (hasSnazzy)
            {
                shouldOverride = true;
            }
            else if (hasWarmCoat)
            {
                shouldOverride = Config.Instance.OverrideWarmCoats;
            }
            else if (hasPajamas)
            {
                shouldOverride = Config.Instance.OverridePajamas;
            }
            else if (hasSwimwear)
            {
                shouldOverride = Config.Instance.OverrideSwimwear;
            }
            else
            {
                shouldOverride = Config.Instance.OverridePrimoGarb;
            }

            // Honor an explicit per-duplicant disabled state: if the duplicant has a saved state that
            // explicitly disables the override, cancel any override decision.
            try
            {
                var minion = __instance.GetComponent<MinionIdentity>();
                if (minion != null)
                {
                    var state = minion.GetComponent<OverrideSaveState>();
                    if (state != null && !state.IsOverrideEnabled)
                    {
                        return __result;
                    }
                    // If there is no saved state, we can also check the global config default and skip override if the default is to disable.
                    else if (state == null)
                    {
                        shouldOverride = Config.Instance.DefaultIndividualOverride;
                    }
                }
            }
            catch { }

            return shouldOverride ? WearableAccessorizer.WearableType.CustomClothing : __result;
        }
    }


    [HarmonyPatch(typeof(WearableAccessorizer), "ApplyWearable")]
    internal class OverwriteApplyWearable
    {
        static void Prefix(WearableAccessorizer __instance)
        {
            Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable> wearables = Traverse.Create(__instance).Field("wearables").GetValue<Dictionary<WearableAccessorizer.WearableType, WearableAccessorizer.Wearable>>();

            if (wearables.TryGetValue(WearableAccessorizer.WearableType.CustomClothing, out var customWearable))
            {
                customWearable.buildOverridePriority = 6;
            }
        }
    }

    // Persisted per-duplicant override state. Saved with the game's save system.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class OverrideSaveState : KMonoBehaviour, ISaveLoadable
    {
        // When true the override is enabled for this duplicant.
        [Serialize]
        public bool IsOverrideEnabled = Config.Instance.DefaultIndividualOverride;
    }

    [HarmonyPatch(typeof(MinionConfig), "CreatePrefab")]
    public static class MinionConfig_CreatePrefab_Patch
    {
        public static void Postfix(GameObject __result)
        {
            __result.AddOrGet<OverrideSaveState>();
        }
    }

    // Patch that ensures a new toggle-button is created next to the existing edit button in the CosmeticsPanel.
    // The new button is instantiated once and re-used; it's shown/hidden to match the edit button state.
    [HarmonyPatch(typeof(CosmeticsPanel), nameof(CosmeticsPanel.Refresh))]
    public static class CosmeticsPanel_AddButton
    {
        // Keep a reference so we don't create multiple clones
        private static KButton overrideButton;

        public static void Postfix(CosmeticsPanel __instance)
        {
            // Get the original edit button (private field)
            var editButton = Traverse.Create(__instance).Field("editButton").GetValue<KButton>();
            if (editButton == null)
                return;

            // Get current selected target (may be null or a building)
            var selectedTarget = Traverse.Create(__instance).Field("selectedTarget").GetValue<GameObject>();

            // Create and cache the override button by cloning the edit button's GameObject
            if (overrideButton == null || overrideButton.gameObject == null)
            {
                try
                {
                    GameObject clone = Util.KInstantiateUI(editButton.gameObject, editButton.transform.parent.gameObject, true);
                    clone.name = "OverrideToggleButton";
                    overrideButton = clone.GetComponent<KButton>();

                    // Clear any inherited callbacks and add our own toggle behavior
                    overrideButton.ClearOnClick();
                    // Query the current selected target at click time to avoid a stale captured value.
                    overrideButton.onClick += () =>
                    {
                        var currentSelected = Traverse.Create(__instance).Field("selectedTarget").GetValue<GameObject>();
                        ToggleForSelectedTarget(currentSelected);
                    };

                    // Position the button in the panel
                    PositionButtonRelativeTo(editButton, overrideButton);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create override toggle button: {e}");
                    return;
                }
            }
            else
            {
                // Re-position on each refresh in case layout changed
                PositionButtonRelativeTo(editButton, overrideButton);
            }

            // Show/hide the override button to match the edit button visibility and current outfit tab.
            try
            {
                // Read the private selectedOutfitCategory from CosmeticsPanel to decide visibility.
                var showButton = false;
                try
                {
                    var selectedCategory = Traverse.Create(__instance).Field("selectedOutfitCategory").GetValue<ClothingOutfitUtility.OutfitType>();
                    showButton = editButton.gameObject.activeSelf && selectedCategory == ClothingOutfitUtility.OutfitType.Clothing;
                }
                catch
                {
                }

                overrideButton.gameObject.SetActive(showButton);
            }
            catch { }

            // Update label according to selected target's saved state
            UpdateToggleLabelForSelectedTarget();

            // Local helpers
            void UpdateToggleLabelForSelectedTarget()
            {
                try
                {
                    // Default to the global config default if no target or no saved state, otherwise reflect the saved state.
                    bool enabled = Config.Instance.DefaultIndividualOverride;
                    if (selectedTarget != null)
                    {
                        var state = selectedTarget.GetComponent<OverrideSaveState>();
                        if (state != null)
                            enabled = state.IsOverrideEnabled;
                    }

                    var label = overrideButton.GetComponentInChildren<LocText>();
                    label?.SetText(enabled ? "Disable Override" : "Enable Override");
                }
                catch { }
            }

            void ToggleForSelectedTarget(GameObject target)
            {
                try
                {
                    if (target == null)
                        return;

                    var state = target.GetComponent<OverrideSaveState>();
                    if (state == null)
                    {
                        // There was no saved state. Clicking should create a saved state and toggle the override based on the global config default.
                        state = target.AddComponent<OverrideSaveState>();
                        state.IsOverrideEnabled = !Config.Instance.DefaultIndividualOverride;
                    }
                    else
                    {
                        state.IsOverrideEnabled = !state.IsOverrideEnabled;
                    }

                    var label = overrideButton.GetComponentInChildren<LocText>();
                    label?.SetText(state.IsOverrideEnabled ? "Disable Override" : "Enable Override");

                    // Refresh visuals by invoking ApplyWearable via Traverse.
                    try
                    {
                        var wearableAccessorizer = target.GetComponent<WearableAccessorizer>();
                        if (wearableAccessorizer != null)
                        {
                            Traverse.Create(wearableAccessorizer).Method("ApplyWearable").GetValue();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error refreshing wearables after toggle: {e}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error toggling override state: {e}");
                }
            }

            void PositionButtonRelativeTo(KButton reference, KButton target)
            {
                try
                {
                    var editRect = reference.GetComponent<RectTransform>();
                    var overrideRect = target.GetComponent<RectTransform>();
                    if (editRect != null && overrideRect != null)
                    {
                        Transform parentContainer = editRect.parent.parent;
                        if (parentContainer != null)
                        {
                            overrideRect.SetParent(parentContainer, worldPositionStays: false);

                            var layoutElement = target.GetComponent<LayoutElement>() ?? target.FindOrAddComponent<LayoutElement>();
                            layoutElement.preferredWidth = 120f;

                            overrideRect.SetAsLastSibling();
                            overrideRect.sizeDelta = new Vector2(120f, 30f);
                        }
                        else
                        {
                            target.transform.SetParent(reference.transform.parent);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error positioning override button: {e}");
                }
            }
        }
    }
}
