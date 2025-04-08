#nullable enable

namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a request to support mapping of a legacy catalog entry.
    /// </summary>
    public class LegacyCatalogMappingSupportRequest
    {
        /// <summary>
        /// Gets or sets the artifact ID of the legacy catalog item.
        /// </summary>
        [JsonProperty("artifactId")]
        public string ArtifactId { get; set; }

        /// <summary>
        /// Gets or sets the content type of the artifact (e.g., protocol, automation).
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the legacy artifact.
        /// </summary>
        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        /// <summary>
        /// Gets or sets the version number of the artifact.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the display name of the artifact.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the branch associated with the artifact.
        /// </summary>
        [JsonProperty("branch")]
        public string Branch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the version is a prerelease.
        /// </summary>
        [JsonProperty("isPrerelease")]
        public string IsPrerelease { get; set; }

        /// <summary>
        /// Gets or sets the developer or publisher of the artifact.
        /// </summary>
        [JsonProperty("developer")]
        public string Developer { get; set; }

        /// <summary>
        /// Gets or sets the release path or location of the artifact binary.
        /// </summary>
        [JsonProperty("releasePath")]
        public string ReleasePath { get; set; }
    }
}
