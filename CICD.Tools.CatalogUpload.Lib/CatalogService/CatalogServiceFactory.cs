namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System.Net.Http;

	using Microsoft.Extensions.Logging;

	/// <summary>
	/// Creates instances of <see cref="ICatalogService"/> to communicate with the Skyline DataMiner Catalog (https://catalog.dataminer.services/).
	/// </summary>
	public static class CatalogServiceFactory
	{
		/// <summary>
		/// Creates instances of <see cref="ICatalogService"/> to communicate with the Skyline DataMiner Catalog (https://catalog.dataminer.services/) using HTTP for communication.
		/// </summary>
		/// <param name="httpClient">An instance of <see cref="HttpClient"/> used for communication with the catalog.</param>
		/// <param name="logger">An instance of <see cref="ILogger"/> for handling debug and error logging.</param>
		/// <returns>An instance of <see cref="ICatalogService"/> to communicate with the Skyline DataMiner Catalog (https://catalog.dataminer.services/).</returns>
		public static ICatalogService CreateWithHttp(HttpClient httpClient, ILogger logger)
		{
			return new HttpCatalogService(httpClient, logger);
		}
	}
}