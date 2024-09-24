namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.IO;
	using System.Net;
	using System.Net.Http;
	using System.Security.Authentication;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Newtonsoft.Json;

	internal sealed class HttpCatalogService : ICatalogService, IDisposable
	{
		private const string VolatileUploadPath = "api/key-artifact-upload/v1-0/private/artifact";
		private const string VersionUploadPathStart = "https://api.dataminer.services/api/key-catalog/v1-0/catalog/";
		private const string VersionUploadPathEnd = "/register/version";
		private const string RegistrationPath = "api/key-catalog/v1-0/catalog/register";

		private readonly HttpClient _httpClient;
		private readonly ILogger _logger;

		public HttpCatalogService(HttpClient httpClient, ILogger logger)
		{
			_logger = logger;
			_httpClient = httpClient;
		}

		public async Task<ArtifactUploadResult> RegisterCatalogAsync(byte[] catalogDetailsZip, string key, CancellationToken cancellationToken)
		{
			using var formData = new MultipartFormDataContent();
			formData.Headers.Add("Ocp-Apim-Subscription-Key", key);

			MemoryStream ms = new MemoryStream();
			ms.Write(catalogDetailsZip, 0, catalogDetailsZip.Length);

			// Reset position so it can be read out again.
			ms.Position = 0;
			formData.Add(new StreamContent(ms), "file", "catalogDetails.zip");

			string logInfo = $"catalogDetails.zip ({ms.Length} bytes)";
			_logger.LogDebug("HTTP Post with info:" + logInfo);

			var response = await _httpClient.PostAsync(RegistrationPath, formData, cancellationToken);

			if (response.IsSuccessStatusCode)
			{
				_logger.LogDebug($"The registration api returned a {response.StatusCode} response. Body: {response.Content}");
				return JsonConvert.DeserializeObject<ArtifactUploadResult>(await response.Content.ReadAsStringAsync(cancellationToken));
			}

			_logger.LogError($"The registration api returned a {response.StatusCode} response. Body: {response.Content}");
			if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
			{
				throw new AuthenticationException($"The registration api returned a {response.StatusCode} response. Body: {response.Content}");
			}

			throw new InvalidOperationException($"The registration api returned a {response.StatusCode} response. Body: {response.Content}");

		}

		public async Task<ArtifactUploadResult> UploadVersionAsync(byte[] package, string fileName, string key, string catalogId, string version, string description, CancellationToken cancellationToken)
		{
			string versionUploadPath = $"{VersionUploadPathStart}{catalogId}{VersionUploadPathEnd}";
			using var formData = new MultipartFormDataContent();
			formData.Headers.Add("Ocp-Apim-Subscription-Key", key);

			MemoryStream ms = new MemoryStream();
			ms.Write(package, 0, package.Length);

			// Reset position so it can be read out again.
			ms.Position = 0;
			formData.Add(new StreamContent(ms), "file", fileName);
			formData.Add(new StringContent(version), "versionNumber");
			formData.Add(new StringContent(description), "versionDescription");
			string logInfo = $"--versionNumber {version} --versionDescription {description}";
			_logger.LogDebug("HTTP Post with info:" + logInfo);

			var response = await _httpClient.PostAsync(versionUploadPath, formData, cancellationToken);

			if (response.IsSuccessStatusCode)
			{
				_logger.LogDebug($"The version upload api returned a {response.StatusCode} response. Body: {response.Content}");
				return JsonConvert.DeserializeObject<ArtifactUploadResult>(await response.Content.ReadAsStringAsync(cancellationToken));
			}

			_logger.LogError($"The version upload api returned a {response.StatusCode} response. Body: {response.Content}");
			if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
			{
				throw new AuthenticationException($"The version upload api returned a {response.StatusCode} response. Body: {response.Content}");
			}

			throw new InvalidOperationException($"The version upload api returned a {response.StatusCode} response. Body: {response.Content}");
		}

		public async Task<ArtifactUploadResult> VolatileArtifactUploadAsync(byte[] package, string key, CatalogMetaData catalog, CancellationToken cancellationToken)
		{
			using var formData = new MultipartFormDataContent();
			formData.Headers.Add("Ocp-Apim-Subscription-Key", key);
			formData.Add(new StringContent(catalog.Name), "name");
			formData.Add(new StringContent(catalog.Version.Value), "version");
			formData.Add(new StringContent(catalog.ContentType), "contentType");

			MemoryStream ms = new MemoryStream();
			ms.Write(package, 0, package.Length);

			// Reset position so it can be read out again.
			ms.Position = 0;
			formData.Add(new StreamContent(ms), "file", catalog.Name);

			string logInfo = $"--name {catalog.Name} --version {catalog.Version} --contentType {catalog.ContentType}  --file {catalog.Name}";

			_logger.LogDebug("HTTP Post with info:" + logInfo);

			var response = await _httpClient.PostAsync(VolatileUploadPath, formData, cancellationToken);

			if (response.IsSuccessStatusCode)
			{
				_logger.LogDebug($"The upload api returned a {response.StatusCode} response. Body: {response.Content}");
				return JsonConvert.DeserializeObject<ArtifactUploadResult>(await response.Content.ReadAsStringAsync(cancellationToken));
			}

			_logger.LogError($"The upload api returned a {response.StatusCode} response. Body: {response.Content}");
			if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
			{
				throw new AuthenticationException($"The upload api returned a {response.StatusCode} response. Body: {response.Content}");
			}

			throw new InvalidOperationException($"The upload api returned a {response.StatusCode} response. Body: {response.Content}");
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}
	}
}