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
		private const string UploadPath = "api/key-artifact-upload/v1-0/private/artifact";
		private readonly HttpClient _httpClient;
		private readonly ILogger _logger;

		public HttpCatalogService(HttpClient httpClient, ILogger logger)
		{
			_logger = logger;
			_httpClient = httpClient;
		}

		public async Task<ArtifactUploadResult> ArtifactUploadAsync(byte[] package, string key, CatalogMetaData catalog, CancellationToken cancellationToken)
		{
			using var formData = new MultipartFormDataContent();
			formData.Headers.Add("Ocp-Apim-Subscription-Key", key);
			formData.Add(new StringContent(catalog.Name), "name");
			formData.Add(new StringContent(catalog.Version), "version");
			formData.Add(new StringContent(catalog.ContentType), "contentType");
			formData.Add(new StringContent(catalog.Branch), "branch");
			formData.Add(new StringContent(catalog.Identifier), "identifier");
			formData.Add(new StringContent(catalog.IsPreRelease() ? "true" : "false"), "isprerelease");
			formData.Add(new StringContent(catalog.CommitterMail), "developer");
			formData.Add(new StringContent(catalog.ReleaseUri), "releasepath");

			MemoryStream ms = new MemoryStream();
			ms.Write(package, 0, package.Length);

			// Reset position so it can be read out again.
			ms.Position = 0;
			formData.Add(new StreamContent(ms), "file", catalog.Name);

			string logInfo = $"--name {catalog.Name} --version {catalog.Version} --contentType {catalog.ContentType} --branch {catalog.Branch} --identifier {catalog.Identifier} --isPrerelease {catalog.IsPreRelease} --developer {catalog.CommitterMail} --releasepath {catalog.ReleaseUri} --file {catalog.Name}";

			_logger.LogDebug("HTTP Post with info:" + logInfo);

			var response = await _httpClient.PostAsync(UploadPath, formData, cancellationToken);

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