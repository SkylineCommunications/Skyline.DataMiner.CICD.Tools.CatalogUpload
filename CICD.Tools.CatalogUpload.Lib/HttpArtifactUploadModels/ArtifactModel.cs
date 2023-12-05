namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.HttpArtifactUploadModels
{
	using Newtonsoft.Json;

	/// <summary>
	/// Artifact information returned from uploading an artifact to the catalog.
	/// </summary>
	public class ArtifactModel
	{
		/// <summary>
		/// The GUID that represents the ID of the artifact in our cloud storage database. Can be used to download or deploy the artifact.
		/// </summary>
		[JsonProperty("artifactId")]
		public string? ArtifactId { get; set; }
	}
}