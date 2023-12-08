namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System.Collections.Generic;
	using System.IO.Compression;
	using System.Linq;

	/// <summary>
	/// As known by the Azure Artifact Uploader
	/// </summary>
	internal enum ArtifactContentType
	{
		Unknown = 0,
		DmScript = 1,
		Package = 2,
		Visio = 3,
		Function = 4,
		Dashboard = 5,
		CustomSolution = 6,
		Example = 7,
		CompanionFile = 8,
		ProfileLoadScript = 9,
		GQIOperator = 10,
		ProcessActivity = 11,
		DataGrabber = 12,
		AdHocDataSource = 13,
	}

	[System.Flags]
	internal enum PackageTypes
	{
		None = 0b_0000_0000, // 0
		HasAutomation = 0b_0000_0001, // 1
		HasDashboards = 0b_0000_0010, // 2
		HasProtocols = 0b_0000_0100, // 4
		HasOtherAppPackages = 0b_0000_1000, // 8
		HasCompanionFiles = 0b_0001_0000, // 16
		HasFunctions = 0b_0010_0000, // 32
		HasVisios = 0b_0100_0000  // 64
	}

	internal class ContentType
	{
		private readonly IEnumerable<ZipArchiveEntry> allContentFiles;

		private readonly FileSystem.IPathIO path;

		public ContentType(ZipArchive zipFile)
		{
			path = FileSystem.FileSystem.Instance.Path;
			this.allContentFiles = zipFile.Entries.Where(p => p.FullName.StartsWith("AppInstallContent"));

			// Consider this a best effort currently.
			PackageTypes content = PackageTypes.None;

			if (HasAutomationScripts()) content |= PackageTypes.HasAutomation;
			if (HasDashboards()) content |= PackageTypes.HasDashboards;
			if (HasProtocols()) content |= PackageTypes.HasProtocols;
			if (HasOtherAppPackages()) content |= PackageTypes.HasOtherAppPackages;
			if (HasCompanionFiles()) content |= PackageTypes.HasCompanionFiles;
			if (HasFunctions()) content |= PackageTypes.HasFunctions;
			if (HasVisios()) content |= PackageTypes.HasVisios;

			switch (content)
			{
				case PackageTypes.HasAutomation:
				case PackageTypes.HasAutomation | PackageTypes.HasCompanionFiles:
					Value = ArtifactContentType.DmScript.ToString();
					break;

				case PackageTypes.HasDashboards:
				case PackageTypes.HasDashboards | PackageTypes.HasCompanionFiles:
					Value = ArtifactContentType.Dashboard.ToString();
					break;

				case PackageTypes.HasOtherAppPackages:
					Value = ArtifactContentType.Package.ToString();
					break;

				case PackageTypes.HasCompanionFiles:
					Value = ArtifactContentType.CompanionFile.ToString();
					break;

				case PackageTypes.HasFunctions:
				case PackageTypes.HasFunctions | PackageTypes.HasCompanionFiles:
					Value = ArtifactContentType.Function.ToString();
					break;

				case PackageTypes.HasVisios:
				case PackageTypes.HasVisios | PackageTypes.HasCompanionFiles:
					Value = ArtifactContentType.Visio.ToString();
					break;

				case PackageTypes.HasProtocols:
				case PackageTypes.HasProtocols | PackageTypes.HasCompanionFiles:
					Value = ArtifactContentType.Package.ToString();
					break;

				default:
					// Everything else is going to be a combination of more than one item so we can consider that to be a "package"
					Value = ArtifactContentType.Package.ToString();
					break;
			}
		}

		public string Value { get; set; }

		private bool HasAutomationScripts()
		{
			// Note: ongoing discussions on better defining the different automationscripts ProfileLoadScript, GQIOperator, ProcessActivity, DataGrabber, AdHocDataSource, ...
			return allContentFiles.FirstOrDefault(p => p.FullName.StartsWith(path.Combine("AppInstallContent", "Scripts"))) != null;
		}

		private bool HasCompanionFiles()
		{
			return allContentFiles.FirstOrDefault(p => p.FullName.StartsWith(path.Combine("AppInstallContent", "CompanionFiles"))) != null;
		}

		private bool HasDashboards()
		{
			return allContentFiles.FirstOrDefault(p => p.FullName.StartsWith(path.Combine("AppInstallContent", "Dashboards"))) != null;
		}

		private bool HasFunctions()
		{
			return allContentFiles.FirstOrDefault(p => p.FullName.StartsWith(path.Combine("AppInstallContent", "Functions"))) != null;
		}

		private bool HasOtherAppPackages()
		{
			return allContentFiles.FirstOrDefault(p => p.FullName.StartsWith(path.Combine("AppInstallContent", "AppPackages"))) != null;
		}

		private bool HasProtocols()
		{
			// Need to check deeper to make sure it doesn't only have a .vsdx. That would turn it into a Visio.
			return allContentFiles.FirstOrDefault(p => p.FullName.EndsWith(".xml") && p.FullName.StartsWith(path.Combine("AppInstallContent", "Protocols"))) != null;
		}

		private bool HasVisios()
		{
			return allContentFiles.FirstOrDefault(p => p.Name.EndsWith(".vsdx")) != null;
		}
	}
}