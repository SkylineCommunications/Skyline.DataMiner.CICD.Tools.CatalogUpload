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
        /// Registers the catalog by uploading the catalog details as a zip file.
        /// </summary>
        /// <param name="catalogDetailsZip">A byte array containing the zipped catalog details.</param>
        /// <param name="key">A unique token used for authentication.</param>
        /// <param name="cancellationToken">A token used to cancel the ongoing registration if needed.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, returning an <see cref="ArtifactUploadResult"/>.</returns>
        Task<ArtifactUploadResult> RegisterCatalogAsync(byte[] catalogDetailsZip, string key, CancellationToken cancellationToken);

        /// <summary>
        /// Uploads legacy catalog mapping support data.
        /// This method is intended for the Skyline Communications Organization to facilitate the migration from internal flows to GitHub.
        /// </summary>
        /// <param name="key">The API subscription key used for authentication.</param>
        /// <param name="payload">The legacy catalog mapping support request payload.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UploadLegacyCatalogMappingSupport(string key, LegacyCatalogMappingSupportRequest payload, CancellationToken cancellationToken);

        /// <summary>
        /// Uploads a specific version of the artifact to the catalog.
        /// </summary>
        /// <param name="package">A byte array containing the package content.</param>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <param name="key">A unique token used for authentication.</param>
        /// <param name="catalogId">The unique catalog identifier for the artifact.</param>
        /// <param name="version">The version of the artifact being uploaded.</param>
        /// <param name="description">A description of the artifact version.</param>
        /// <param name="cancellationToken">A token used to cancel the ongoing upload if needed.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, returning an <see cref="ArtifactUploadResult"/>.</returns>
        Task<ArtifactUploadResult> UploadVersionAsync(byte[] package, string fileName, string key, string catalogId, string version, string description, CancellationToken cancellationToken);

        /// <summary>
        /// Uploads a .dmapp artifact to an external store without registering it in the catalog.
        /// </summary>
        /// <param name="package">A byte array containing the package content.</param>
        /// <param name="key">A unique token used for authentication.</param>
        /// <param name="catalog">An instance of <see cref="CatalogMetaData"/> containing additional metadata for the upload.</param>
        /// <param name="cancellationToken">A token used to cancel the ongoing upload if needed.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, returning an <see cref="ArtifactUploadResult"/>.</returns>
        Task<ArtifactUploadResult> VolatileArtifactUploadAsync(byte[] package, string key, CatalogMetaData catalog, CancellationToken cancellationToken);


        /// <summary>
        /// Uploads either a .dmapp or .dmprotocol to an external store without registering it in the catalog.
        /// </summary>
        /// <param name="package">A byte array containing the package content.</param>
        /// <param name="type">The type of item getting uploaded: DmsScript or Connector</param>
        /// <param name="key">A unique token used for authentication.</param>
        /// <param name="catalog">An instance of <see cref="CatalogMetaData"/> containing additional metadata for the upload.</param>
        /// <param name="cancellationToken">A token used to cancel the ongoing upload if needed.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, returning an <see cref="ArtifactUploadResult"/>.</returns>
        Task<ArtifactUploadResult> VolatileArtifactUploadAsync(byte[] package, VolatileContentType type, string key, CatalogMetaData catalog, CancellationToken cancellationToken);
    }
}