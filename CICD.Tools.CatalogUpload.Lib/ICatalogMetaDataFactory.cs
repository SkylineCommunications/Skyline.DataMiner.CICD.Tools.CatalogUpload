namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using Skyline.DataMiner.CICD.FileSystem;

	using System;

	/// <summary>
	/// A factory class responsible for creating instances of <see cref="CatalogMetaData"/>.
	/// </summary>
	public interface ICatalogMetaDataFactory
	{
		/// <summary>
		/// Creates a partial CataLogMetaData using any information it can from the artifact itself. Check the items for null and complete.
		/// </summary>
		/// <param name="pathToArtifact">Path to the artifact.</param>
		/// <param name="pathToReadme"></param>
		/// <param name="pathToImages"></param>
		/// <returns>An instance of <see cref="CatalogMetaData"/>.></returns>
		/// <exception cref="ArgumentNullException">Provided path should not be null</exception>
		/// <exception cref="InvalidOperationException">Expected data was not present in the Artifact.</exception>
		public CatalogMetaData FromArtifact(string pathToArtifact, string pathToReadme = null, string pathToImages = null);
		
		/// <summary>
		/// Creates a <see cref="CatalogMetaData"/> instance using information from a catalog YAML file.
		/// </summary>
		/// <param name="fs">The file system interface used for accessing files and directories.</param>
		/// <param name="startPath">The starting directory or file path for the catalog YAML file.</param>
		/// <param name="pathToReadme">Optional. The path to the README file. If null, the method will attempt to locate the README.</param>
		/// <param name="pathToImages">Optional. The path to the images directory. If null, no images path will be set.</param>
		/// <returns>A <see cref="CatalogMetaData"/> instance populated with the catalog YAML data.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a catalog.yml or manifest.yml file cannot be found within the provided directory, file, or parent directories.
		/// </exception>
		public CatalogMetaData FromCatalogYaml(IFileSystem fs, string startPath, string pathToReadme = null, string pathToImages = null);

	}
}