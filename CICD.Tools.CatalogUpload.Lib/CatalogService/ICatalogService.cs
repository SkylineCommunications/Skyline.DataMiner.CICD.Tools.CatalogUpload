namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System.IO;
	using System.Net.Http;
	using System.Net;
	using System.Security.Authentication;
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	/// <summary>
	/// Service interface used to actually upload and artifact.
	/// </summary>
	public interface ICatalogService
	{

		Task<ArtifactUploadResult> RegisterCatalogAsync(byte[] catalogDetailsZip, string key, CancellationToken cancellationToken);

		Task<ArtifactUploadResult> UploadVersionAsync(byte[] package, string fileName, string key, string catalogId, string version, string description, CancellationToken cancellationToken);

		/// <summary>
		/// Uploads an artifact to an external store without any registration.
		/// </summary>
		/// <param name="package">A byte array with the package content.</param>
		/// <param name="key">A unique token used for communications.</param>
		/// <param name="catalog">An instance of <see cref="CatalogMetaData"/> containing additional data for upload.</param>
		/// <param name="cancellationToken">An instance of <see cref="CancellationToken"/> to cancel an ongoing upload.</param>
		/// <returns></returns>
		Task<ArtifactUploadResult> VolatileArtifactUploadAsync(byte[] package, string key, CatalogMetaData catalog, CancellationToken cancellationToken);
	}
}