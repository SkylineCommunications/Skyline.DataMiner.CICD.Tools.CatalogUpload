namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.CatalogService
{
	using System.Collections.Generic;

	/// <summary>
	/// Represents the metadata of a catalog entry in YAML format.
	/// </summary>
	public class CatalogYaml
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CatalogYaml"/> class.
		/// </summary>
		public CatalogYaml()
		{
			Owners = new List<CatalogYamlOwner>();
			Tags = new List<string>();
		}

		/// <summary>
		/// Gets or sets the URL to the documentation related to the catalog entry.
		/// </summary>
		/// <value>A string representing the documentation URL.</value>
		public string Documentation_url { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for the catalog entry.
		/// </summary>
		/// <value>A string representing the unique ID of the catalog entry.</value>
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the owners of the catalog entry.
		/// </summary>
		/// <value>A list of <see cref="CatalogYamlOwner"/> objects representing the owners.</value>
		public List<CatalogYamlOwner> Owners { get; set; }

		/// <summary>
		/// Gets or sets the short description of the catalog entry.
		/// </summary>
		/// <value>A string representing the short description of the entry.</value>
		public string Short_description { get; set; }

		/// <summary>
		/// Gets or sets the URL to the source code related to the catalog entry.
		/// </summary>
		/// <value>A string representing the source code URL.</value>
		public string Source_code_url { get; set; }

		/// <summary>
		/// Gets or sets the tags associated with the catalog entry.
		/// </summary>
		/// <value>A list of strings representing the tags.</value>
		public List<string> Tags { get; set; }

		/// <summary>
		/// Gets or sets the title of the catalog entry.
		/// </summary>
		/// <value>A string representing the title of the entry.</value>
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets the type of the catalog entry.
		/// </summary>
		/// <value>A string representing the type of the entry.</value>
		public string Type { get; set; }
	}

	/// <summary>
	/// Represents an owner of the catalog entry in the YAML format.
	/// </summary>
	public class CatalogYamlOwner
	{
		/// <summary>
		/// Gets or sets the email of the owner.
		/// </summary>
		/// <value>A string representing the owner's email address.</value>
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the name of the owner.
		/// </summary>
		/// <value>A string representing the owner's name.</value>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the URL of the owner's profile or webpage.
		/// </summary>
		/// <value>A string representing the URL of the owner.</value>
		public string Url { get; set; }
	}
}