namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.CatalogService
{
	using System.Collections.Generic;
	public class CatalogYamlOwner
	{
        public string Name { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
    }

	public class CatalogYaml
	{
        public string Type { get; set; }

        public string Id { get; set; }

		public string Title { get; set; }

		public string Short_description { get; set; }

		public string Source_code_url { get; set; }

		public string Documentation_url { get; set; }

        public List<CatalogYamlOwner> Owners { get; set; }

        public List<string> Tags { get; set; }
    }
}
