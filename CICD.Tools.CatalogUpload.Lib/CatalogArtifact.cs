namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Newtonsoft.Json;

	using Skyline.DataMiner.CICD.FileSystem;
	using YamlDotNet.Serialization.NamingConventions;
	using YamlDotNet.Serialization;

	/// <summary>
	/// Allows Uploading an artifact to the Catalog using one of the below in order of priority:
	///  <para>- provided key in upload argument (Unix/Windows)</para>
	///  <para>- key stored as an Environment Variable called "DATAMINER_CATALOG_TOKEN". (Unix/Windows)</para>
	///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "DATAMINER_CATALOG_TOKEN_ENCRYPTED" (Windows only)</para>
	/// </summary>
	public class CatalogArtifact
	{
		private readonly ILogger _logger;
		private readonly CatalogMetaData metaData;
		private readonly ISerializer serializer;

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
			Fs = fileSystem;
			Cts = new CancellationTokenSource();
			CatalogService = service;
			PathToArtifact = pathToArtifact;
			serializer = new SerializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
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

		private ICatalogService CatalogService { get; set; }

		private CancellationTokenSource Cts { get; set; }

		private IFileSystem Fs { get; set; }

		private string KeyFromEnv { get; set; }

		/// <summary>
		/// Cancels an ongoing upload. Create a new CatalogArtifact to attempt a new upload.
		/// </summary>
		public void CancelUpload()
		{
			_logger.LogDebug($"Upload cancellation requested for {PathToArtifact}");
			Cts.Cancel();
		}

		/// <summary>
		/// Registers the catalog metadata asynchronously using the provided catalog token.
		/// </summary>
		/// <param name="dmCatalogToken">The token for authenticating with the catalog service.</param>
		/// <returns>An <see cref="ArtifactUploadResult"/> containing the result of the registration.</returns>
		public async Task<ArtifactUploadResult> RegisterAsync(string dmCatalogToken)
		{
			CheckCatalogIdentifier(metaData.CatalogIdentifier);

			var zipArray = await metaData.ToCatalogZipAsync(Fs, serializer).ConfigureAwait(false);
			return await CatalogService.RegisterCatalogAsync(zipArray, dmCatalogToken, Cts.Token).ConfigureAwait(false);
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
			if (String.IsNullOrWhiteSpace(KeyFromEnv))
			{
				throw new InvalidOperationException("Registration failed, missing token in environment variable DATAMINER_CATALOG_TOKEN or DATAMINER_CATALOG_TOKEN_ENCRYPTED.");
			}

			CheckCatalogIdentifier(metaData.CatalogIdentifier);
			var zipArray = await metaData.ToCatalogZipAsync(Fs, serializer).ConfigureAwait(false);
			return await CatalogService.RegisterCatalogAsync(zipArray, KeyFromEnv, Cts.Token).ConfigureAwait(false);
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
			byte[] packageData = Fs.File.ReadAllBytes(PathToArtifact);

			CheckCatalogIdentifier(metaData.CatalogIdentifier);

			var uploadResult = await CatalogService.UploadVersionAsync(packageData, Fs.Path.GetFileName(PathToArtifact), dmCatalogToken, metaData.CatalogIdentifier, metaData.Version.Value, metaData.Version.VersionDescription, Cts.Token).ConfigureAwait(false);
			// Register the new version on the catalog.
			await RegisterAsync(dmCatalogToken).ConfigureAwait(false);

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

			if (String.IsNullOrWhiteSpace(KeyFromEnv))
			{
				throw new InvalidOperationException("Uploading failed, missing token in environment variable DATAMINER_CATALOG_TOKEN or DATAMINER_CATALOG_TOKEN_ENCRYPTED.");
			}

			_logger.LogDebug($"Attempting upload with Environment Variable as token for artifact: {PathToArtifact}...");
			return await UploadAndRegisterAsync(KeyFromEnv).ConfigureAwait(false);
		}

		/// <summary>
		/// Uploads to the private catalog using the provided dmCatalogToken. Does not perform registration on the catalog.
		/// </summary>
		/// <param name="dmCatalogToken">A provided token for the agent or organization as defined in https://admin.dataminer.services/.</param>
		/// <returns>If the upload was successful or not.</returns>
		public async Task<ArtifactUploadResult> VolatatileUploadAsync(string dmCatalogToken)
		{
			if (PathToArtifact == null) throw new InvalidOperationException($"{nameof(PathToArtifact)} cannot be null.");

			if (dmCatalogToken != KeyFromEnv)
			{
				_logger.LogDebug($"Attempting upload with provided argument as token for artifact: {PathToArtifact}...");
			}

			_logger.LogDebug($"Uploading {PathToArtifact}...");

			byte[] packageData = Fs.File.ReadAllBytes(PathToArtifact);
			var result = await CatalogService.VolatileArtifactUploadAsync(packageData, dmCatalogToken, metaData, Cts.Token).ConfigureAwait(false);
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
		public async Task<ArtifactUploadResult> VolatatileUploadAsync()
		{
			if (PathToArtifact == null) throw new InvalidOperationException($"{nameof(PathToArtifact)} cannot be null.");

			if (String.IsNullOrWhiteSpace(KeyFromEnv))
			{
				throw new InvalidOperationException("Uploading failed, missing token in environment variable DATAMINER_CATALOG_TOKEN or DATAMINER_CATALOG_TOKEN_ENCRYPTED.");
			}

			_logger.LogDebug($"Attempting upload with Environment Variable as token for artifact: {PathToArtifact}...");
			return await VolatatileUploadAsync(KeyFromEnv).ConfigureAwait(false);
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
						string keyFromWinEncryptedKeys = new System.Net.NetworkCredential(string.Empty, encryptedKey).Password;

						if (!String.IsNullOrWhiteSpace(keyFromWinEncryptedKeys))
						{
							_logger.LogDebug("OK: Found token in Env Variable: 'DATAMINER_CATALOG_TOKEN_ENCRYPTED' created by WinEncryptedKeys.");
							KeyFromEnv = keyFromWinEncryptedKeys;
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
				if (!String.IsNullOrWhiteSpace(KeyFromEnv))
				{
					_logger.LogDebug("OK: Overriding 'DATAMINER_CATALOG_TOKEN_ENCRYPTED' with found token in Env Variable: 'DATAMINER_CATALOG_TOKEN'.");
				}
				else
				{
					_logger.LogDebug("OK: Found token in Env Variable: 'DATAMINER_CATALOG_TOKEN'.");
				}

				KeyFromEnv = keyFromEnvironment;
			}
		}
	}
}