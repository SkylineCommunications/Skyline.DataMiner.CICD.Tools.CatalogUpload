namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Service interface used to actually upload and artifact.
	/// </summary>
	public interface ICatalogService
	{
		/// <summary>
		/// Uploads an artifact to an external store.
		/// </summary>
		/// <param name="package">A byte array with the package content.</param>
		/// <param name="key">A unique token used for communications.</param>
		/// <param name="catalog">An instance of <see cref="CatalogMetaData"/> containing additional data for upload and registration.</param>
		/// <param name="cancellationToken">An instance of <see cref="CancellationToken"/> to cancel an ongoing upload.</param>
		/// <returns></returns>
		Task<ArtifactUploadResult> ArtifactUploadAsync(byte[] package, string key, CatalogMetaData catalog, CancellationToken cancellationToken);
	}
}