#nullable enable

namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
    using Newtonsoft.Json;

    public class CatalogItemInfo
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("isPrivate")]
        public bool? IsPrivate { get; set; }
    }
}