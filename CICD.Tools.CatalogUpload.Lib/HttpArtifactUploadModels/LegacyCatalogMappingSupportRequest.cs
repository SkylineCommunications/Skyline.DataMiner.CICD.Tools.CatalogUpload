#nullable enable

namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
    using System.Text.Json.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// Represents a request to support mapping of a legacy catalog entry.
    /// </summary>
    public class LegacyCatalogMappingSupportRequest
    {
        [JsonProperty("artifactId")]
        /// <summary>
        /// Gets or sets the artifact ID of the legacy catalog item.
        /// </summary>
        public string ArtifactId { get; set; }

        [JsonProperty("contentType")]
        /// <summary>
        /// Gets or sets the content type of the artifact (e.g., protocol, automation).
        /// </summary>
        public string ContentType { get; set; }

        [JsonProperty("identifier")]
        /// <summary>
        /// Gets or sets the unique identifier of the legacy artifact.
        /// </summary>
        public string Identifier { get; set; }

        [JsonProperty("version")]
        /// <summary>
        /// Gets or sets the version number of the artifact.
        /// </summary>
        public string Version { get; set; }

        [JsonProperty("name")]
        /// <summary>
        /// Gets or sets the display name of the artifact.
        /// </summary>
        public string Name { get; set; }

        [JsonProperty("branch")]
        /// <summary>
        /// Gets or sets the name of the branch associated with the artifact.
        /// </summary>
        public string Branch { get; set; }

        [JsonProperty("isPrerelease")]
        /// <summary>
        /// Gets or sets a value indicating whether the version is a prerelease.
        /// </summary>
        public string IsPrerelease { get; set; }

        [JsonProperty("developer")]
        /// <summary>
        /// Gets or sets the developer or publisher of the artifact.
        /// </summary>
        public string Developer { get; set; }

        [JsonProperty("releasePath")]
        /// <summary>
        /// Gets or sets the release path or location of the artifact binary.
        /// </summary>
        public string ReleasePath { get; set; }
    }
}
