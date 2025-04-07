namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    using Skyline.DataMiner.CICD.FileSystem;

    using YamlDotNet.Serialization;

    using YamlDotNet.Serialization.NamingConventions;

    /// <summary>
    /// Allows Uploading an artifact to the Catalog using one of the below in order of priority:
    ///  <para>- provided key in upload argument (Unix/Windows)</para>
    ///  <para>- key stored as an Environment Variable called "DATAMINER_CATALOG_TOKEN". (Unix/Windows)</para>
    ///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "DATAMINER_CATALOG_TOKEN_ENCRYPTED" (Windows only)</para>
    /// </summary>
    public class CatalogArtifact : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ICatalogService catalogService;
        private readonly CancellationTokenSource cts;
        private readonly IFileSystem fs;
        private readonly CatalogMetaData metaData;
        private readonly ISerializer serializer;
        private string keyFromEnv;

        /// <summary>
        /// Creates an instance of <see cref="CatalogArtifact"/>.
        /// It searches for an optional dmCatalogToken in the "DATAMINER_CATALOG_TOKEN" or "DATAMINER_CATALOG_TOKEN_ENCRYPTED" Environment Variable.
        /// </summary>
        /// <param name="pathToArtifact">Path to the application package (.dmapp) or protocol package (.dmprotocol).</param>
        /// <param name="service">An instance of <see cref="ICatalogService"/> used for communication.</param>
        /// <param name="fileSystem">An instance of <see cref="IFileSystem"/> to access the filesystem. e.g. Skyline.DataMiner.CICD.FileSystem.Instance.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/> that will hold error, debug and other information.</param>
        /// <param name="metaData">Contains package metadata.</param>
        public CatalogArtifact(string pathToArtifact, ICatalogService service, IFileSystem fileSystem, ILogger logger, CatalogMetaData metaData)
        {
            this.metaData = metaData;
            _logger = logger;
            fs = fileSystem;
            cts = new CancellationTokenSource();
            catalogService = service;
            PathToArtifact = pathToArtifact;
            serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            TryFindEnvironmentKey();
        }

        /// <summary>
        /// Creates an instance of <see cref="CatalogArtifact"/> using a default HttpCatalogService with a new HttpClient for communication.
        /// It searches for an optional dmCatalogToken in the "DATAMINER_CATALOG_TOKEN" or "DATAMINER_CATALOG_TOKEN_ENCRYPTED" Environment Variable for authentication.
        /// </summary>
        /// <remarks>WARNING: when wishing to upload several Artifacts it's recommended to use the CatalogArtifact(string pathToArtifact, ICatalogService service, IFileSystem fileSystem, ILogger logger).</remarks>
        /// <param name="pathToArtifact">Path to the application package (.dmapp) or protocol package (.dmprotocol).</param>
        /// <param name="logger">An instance of <see cref="ILogger"/> that will hold error, debug and other information.</param>
        /// <param name="metaData">Contains package metadata.</param>
        public CatalogArtifact(string pathToArtifact, ILogger logger, CatalogMetaData metaData) : this(pathToArtifact, CatalogServiceFactory.CreateWithHttp(new System.Net.Http.HttpClient(), logger), FileSystem.Instance, logger, metaData)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="CatalogArtifact"/> using a default HttpCatalogService with a new HttpClient for communication.
        /// It searches for an optional dmCatalogToken in the "DATAMINER_CATALOG_TOKEN" or "DATAMINER_CATALOG_TOKEN_ENCRYPTED" Environment Variable for authentication.
        /// </summary>
        /// <remarks>WARNING: when wishing to upload several Artifacts it's recommended to use the CatalogArtifact(string pathToArtifact, ICatalogService service, IFileSystem fileSystem, ILogger logger).</remarks>
        /// <param name="logger">An instance of <see cref="ILogger"/> that will hold error, debug and other information.</param>
        /// <param name="metaData">Contains package metadata.</param>
        public CatalogArtifact(ILogger logger, CatalogMetaData metaData) : this(null, CatalogServiceFactory.CreateWithHttp(new System.Net.Http.HttpClient(), logger), FileSystem.Instance, logger, metaData)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="CatalogArtifact"/>.
        /// It searches for an optional dmCatalogToken in the "DATAMINER_CATALOG_TOKEN" or "DATAMINER_CATALOG_TOKEN_ENCRYPTED" Environment Variable.
        /// </summary>
        /// <param name="service">An instance of <see cref="ICatalogService"/> used for communication.</param>
        /// <param name="fileSystem">An instance of <see cref="IFileSystem"/> to access the filesystem. e.g. Skyline.DataMiner.CICD.FileSystem.Instance.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/> that will hold error, debug and other information.</param>
        /// <param name="metaData">Contains package metadata.</param>
        public CatalogArtifact(ICatalogService service, IFileSystem fileSystem, ILogger logger, CatalogMetaData metaData) : this(null, service, fileSystem, logger, metaData)
        {
        }

        /// <summary>
        /// Path to the application package (.dmapp) or protocol package (.dmprotocol).
        /// </summary>
        public string PathToArtifact { get; private set; }

        /// <summary>
        /// Cancels an ongoing upload. Create a new CatalogArtifact to attempt a new upload.
        /// </summary>
        public void CancelUpload()
        {
            _logger.LogDebug($"Upload cancellation requested for {PathToArtifact}");
            cts.Cancel();
        }

        /// <summary>
        /// Registers the catalog metadata asynchronously using the provided catalog token.
        /// </summary>
        /// <param name="dmCatalogToken">The token for authenticating with the catalog service.</param>
        /// <returns>An <see cref="ArtifactUploadResult"/> containing the result of the registration.</returns>
        public async Task<ArtifactUploadResult> RegisterAsync(string dmCatalogToken)
        {
            CheckCatalogIdentifier(metaData.CatalogIdentifier);

            var zipArray = await metaData.ToCatalogZipAsync(fs, serializer, _logger).ConfigureAwait(false);
            var result = await catalogService.RegisterCatalogAsync(zipArray, dmCatalogToken, cts.Token).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Registers the catalog metadata asynchronously using the token retrieved from environment variables.
        /// </summary>
        /// <returns>An <see cref="ArtifactUploadResult"/> containing the result of the registration.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the required environment variable for the catalog token is missing.
        /// </exception>
        public async Task<ArtifactUploadResult> RegisterAsync()
        {
            if (String.IsNullOrWhiteSpace(keyFromEnv))
            {
                throw new InvalidOperationException("Registration failed, missing token in environment variable DATAMINER_CATALOG_TOKEN or DATAMINER_CATALOG_TOKEN_ENCRYPTED.");
            }

            CheckCatalogIdentifier(metaData.CatalogIdentifier);
            var zipArray = await metaData.ToCatalogZipAsync(fs, serializer, _logger).ConfigureAwait(false);
            return await catalogService.RegisterCatalogAsync(zipArray, keyFromEnv, cts.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads the artifact and registers the catalog metadata asynchronously using the provided catalog token.
        /// </summary>
        /// <param name="dmCatalogToken">The token for authenticating with the catalog service.</param>
        /// <returns>An <see cref="ArtifactUploadResult"/> containing the result of the upload and registration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the path to the artifact is null.</exception>
        public async Task<ArtifactUploadResult> UploadAndRegisterAsync(string dmCatalogToken)
        {
            if (PathToArtifact == null) throw new InvalidOperationException($"{nameof(PathToArtifact)} cannot be null.");

            // Upload the version to the cloud.
            byte[] packageData = fs.File.ReadAllBytes(PathToArtifact);

            CheckCatalogIdentifier(metaData.CatalogIdentifier);
            // Register the new version on the catalog.
            await RegisterAsync(dmCatalogToken).ConfigureAwait(false);

            var uploadResult = await catalogService.UploadVersionAsync(packageData, fs.Path.GetFileName(PathToArtifact), dmCatalogToken, metaData.CatalogIdentifier, metaData.Version.Value, metaData.Version.VersionDescription, cts.Token).ConfigureAwait(false);

            string isForSkyline = Environment.GetEnvironmentVariable("IsForSkyline");
            if (isForSkyline != null && isForSkyline.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                LegacyCatalogMappingSupportRequest payload = new LegacyCatalogMappingSupportRequest()
                {
                    ArtifactId = metaData.CatalogIdentifier,
                    ContentType = metaData.ContentType,
                    Identifier = metaData.SourceCodeUri,
                    Name = metaData.Name,
                    Version = metaData.Version.Value,
                    Branch = metaData.Version.Branch,
                    Developer = metaData.Version.CommitterMail,
                    IsPrerelease = metaData.IsPreRelease() ? "true" : "false",
                    ReleasePath = uploadResult.ArtifactId
                };

                await catalogService.UploadLegacyCatalogMappingSupport(dmCatalogToken, cts.Token, payload);
            }

            _logger.LogInformation(JsonConvert.SerializeObject(uploadResult));
            return uploadResult;
        }

        /// <summary>
        /// Uploads the artifact and registers the catalog metadata asynchronously using the token retrieved from environment variables.
        /// </summary>
        /// <returns>An <see cref="ArtifactUploadResult"/> containing the result of the upload and registration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the path to the artifact is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the required environment variable for the catalog token is missing.
        /// </exception>
        public async Task<ArtifactUploadResult> UploadAndRegisterAsync()
        {
            if (PathToArtifact == null) throw new InvalidOperationException($"{nameof(PathToArtifact)} cannot be null.");

            if (String.IsNullOrWhiteSpace(keyFromEnv))
            {
                throw new InvalidOperationException("Uploading failed, missing token in environment variable DATAMINER_CATALOG_TOKEN or DATAMINER_CATALOG_TOKEN_ENCRYPTED.");
            }

            _logger.LogDebug($"Attempting upload with Environment Variable as token for artifact: {PathToArtifact}...");
            return await UploadAndRegisterAsync(keyFromEnv).ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads to the private catalog using the provided dmCatalogToken. Does not perform registration on the catalog.
        /// </summary>
        /// <param name="dmCatalogToken">A provided token for the agent or organization as defined in https://admin.dataminer.services/.</param>
        /// <returns>If the upload was successful or not.</returns>
        public async Task<ArtifactUploadResult> VolatileUploadAsync(string dmCatalogToken)
        {
            if (PathToArtifact == null) throw new InvalidOperationException($"{nameof(PathToArtifact)} cannot be null.");

            if (dmCatalogToken != keyFromEnv)
            {
                _logger.LogDebug($"Attempting upload with provided argument as token for artifact: {PathToArtifact}...");
            }

            _logger.LogDebug($"Uploading {PathToArtifact}...");

            byte[] packageData = fs.File.ReadAllBytes(PathToArtifact);

            VolatileContentType volatileType;
            if (PathToArtifact.EndsWith(".dmprotocol", StringComparison.InvariantCultureIgnoreCase))
            {
                volatileType = VolatileContentType.Connector;
            }
            else
            {
                volatileType = VolatileContentType.DmScript;
            }

            var result = await catalogService.VolatileArtifactUploadAsync(packageData, volatileType, dmCatalogToken, metaData, cts.Token).ConfigureAwait(false);
            _logger.LogDebug($"Finished Uploading {PathToArtifact}");

            _logger.LogInformation(JsonConvert.SerializeObject(result));
            return result;
        }

        /// <summary>
        /// Uploads to the private catalog using the DATAMINER_CATALOG_TOKEN or DATAMINER_CATALOG_TOKEN environment variable as the token.
        /// </summary>
        /// <returns>If the upload was successful or not.</returns>
        /// <exception cref="InvalidOperationException">Uploading failed.</exception>
        /// <exception cref="UnauthorizedAccessException">Uploading failed due to invalid Token.</exception>
        public async Task<ArtifactUploadResult> VolatileUploadAsync()
        {
            if (PathToArtifact == null) throw new InvalidOperationException($"{nameof(PathToArtifact)} cannot be null.");

            if (String.IsNullOrWhiteSpace(keyFromEnv))
            {
                throw new InvalidOperationException("Uploading failed, missing token in environment variable DATAMINER_CATALOG_TOKEN or DATAMINER_CATALOG_TOKEN_ENCRYPTED.");
            }

            _logger.LogDebug($"Attempting upload with Environment Variable as token for artifact: {PathToArtifact}...");
            return await VolatileUploadAsync(keyFromEnv).ConfigureAwait(false);
        }

        /// <summary>
        /// Validates the provided catalog identifier.
        /// </summary>
        /// <param name="id">The catalog identifier (GUID) to check.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the catalog identifier is null, empty, or consists only of whitespace.
        /// A valid catalog ID (GUID) must be provided either through the '--catalog-identifier' argument
        /// or the 'id' variable in the 'catalog.yml'.
        /// </exception>
        private static void CheckCatalogIdentifier(string id)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Please provide either an existing or a new catalog ID (GUID) through the '--catalog-identifier' argument or the 'id' variable in the 'catalog.yml'.");
            }
        }

        /// <summary>
        ///  Attempts to find the necessary API key in Environment Variables. In order of priority:
        ///  <para>- key stored as an Environment Variable called "DATAMINER_CATALOG_TOKEN". (unix/win)</para>
        ///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "DATAMINER_CATALOG_TOKEN_ENCRYPTED" (windows only)</para>
        /// </summary>
        private void TryFindEnvironmentKey()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var encryptedKey = WinEncryptedKeys.Lib.Keys.RetrieveKey("DATAMINER_CATALOG_TOKEN_ENCRYPTED");
                    if (encryptedKey != null)
                    {
                        string keyFromWinEncryptedKeys = new System.Net.NetworkCredential(String.Empty, encryptedKey).Password;

                        if (!String.IsNullOrWhiteSpace(keyFromWinEncryptedKeys))
                        {
                            _logger.LogDebug("OK: Found token in Env Variable: 'DATAMINER_CATALOG_TOKEN_ENCRYPTED' created by WinEncryptedKeys.");
                            keyFromEnv = keyFromWinEncryptedKeys;
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // Gobble up, no key means we try the next thing.
                }
            }

            string keyFromEnvironment = Environment.GetEnvironmentVariable("DATAMINER_CATALOG_TOKEN");

            if (!String.IsNullOrWhiteSpace(keyFromEnvironment))
            {
                if (!String.IsNullOrWhiteSpace(keyFromEnv))
                {
                    _logger.LogDebug("OK: Overriding 'DATAMINER_CATALOG_TOKEN_ENCRYPTED' with found token in Env Variable: 'DATAMINER_CATALOG_TOKEN'.");
                }
                else
                {
                    _logger.LogDebug("OK: Found token in Env Variable: 'DATAMINER_CATALOG_TOKEN'.");
                }

                keyFromEnv = keyFromEnvironment;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            cts?.Dispose();
        }
    }
}