using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
