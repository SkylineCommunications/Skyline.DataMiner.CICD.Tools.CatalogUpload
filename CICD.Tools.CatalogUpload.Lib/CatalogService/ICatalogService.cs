namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.HttpArtifactUploadModels;

	public interface ICatalogService
	{
		Task<ArtifactModel> ArtifactUploadAsync(byte[] package, string key, CatalogMetaData catalog, CancellationToken cancellationToken);
	}
}