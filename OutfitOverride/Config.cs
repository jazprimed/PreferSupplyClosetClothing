using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;

namespace OutfitOverride
{
    [Serializable]
    [RestartRequired]
    [ConfigFile(SharedConfigLocation: true)]
    public class Config : SingletonOptions<Config>
    {
        [JsonProperty]
        [Option("Override Primo Garb",
    "Replace the appearance of Primo Garb with the duplicant’s Supply Closet outfit.")]
        public bool OverridePrimoGarb { get; set; } = false;

        [JsonProperty]
        [Option("Override Warm Coats",
            "Replace the appearance of Warm Coats with the duplicant’s Supply Closet outfit.")]
        public bool OverrideWarmCoats { get; set; } = false;

        [JsonProperty]
        [Option("Default Individual Outfit Override",
            "Whether the outfit override is enabled by default for individual duplicants. You can still toggle the override on or off for each duplicant separately.")]
        public bool DefaultIndividualOverride { get; set; } = true;
    }
}
