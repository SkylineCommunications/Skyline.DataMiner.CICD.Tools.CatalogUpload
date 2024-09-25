using System.IO.Compression;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Skyline.DataMiner.CICD.FileSystem;
using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.CatalogService;

using YamlDotNet.Serialization;

namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.Tests
{
	[TestClass()]
	public class CatalogMetaDataTests
	{
		[TestMethod()]
		public void FromArtifactTest_BuildPreReleaseDmappWithProtocols()
		{
			// Arrange
			string pathToArtifact = "TestData/withProtocols.dmapp";

			// Act		
			CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "solution",
				Name = "SLNetSubscriptionsBenchmarking",
				Version = new CatalogVersionMetaData()
				{
					Value = "1.0.1-B15",
				}
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeTrue();
		}

		[TestMethod()]
		public void FromArtifactTest_ReleaseAutomation()
		{
			// Arrange
			string pathToArtifact = "TestData/withAutomation.dmapp";

			// Act
			CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "automationscript",
				Name = "Demo InterAppCalls",
				Version = new CatalogVersionMetaData()
				{
					Value = "1.0.0-CU1",
				}
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeFalse();
		}

		[TestMethod()]
		public void FromArtifactTest_ReleaseDashboard()
		{
			// Arrange
			string pathToArtifact = "TestData/withDashboard.dmapp";

			// Act
			CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "dashboard",
				Name = "Tandberg RX1290",
				Version = new CatalogVersionMetaData() { Value = "1.0.0-CU1" },
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeFalse();
		}

		[TestMethod()]
		public void FromArtifactTest_ReleaseProtocolVisio()
		{
			// Arrange
			string pathToArtifact = "TestData/withVisio.dmapp";

			// Act
			CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "visio",
				Name = "Microsoft Platform",
				Version = new CatalogVersionMetaData() { Value = "1.0.0-CU4" },
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeFalse();
		}

		[TestMethod()]
		public void FromCatalogYaml_ValidYamlFile_ShouldParseCorrectly()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			string yamlContent = @"
                id: catalog-id-1234
                type: solution
                title: MyCatalogPackage
                short_description: A package description
                source_code_url: https://example.com/source-code
                owners:
                  - name: Owner1
                    email: owner1@example.com
                    url: https://owner1.com
                tags: [tag1, tag2]
            ";

			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Returns(yamlContent);
			mockFileSystem.Setup(fs => fs.Directory.IsDirectory(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(new[] { "catalog.yml" });
			mockFileSystem.Setup(fs => fs.Path.GetFileName(It.IsAny<string>())).Returns("catalog.yml");
			// Act
			var result = new CatalogMetaDataFactory().FromCatalogYaml(mockFileSystem.Object, "test/path");

			// Assert
			result.CatalogIdentifier.Should().Be("catalog-id-1234");
			result.ContentType.Should().Be("solution");
			result.Name.Should().Be("MyCatalogPackage");
			result.ShortDescription.Should().Be("A package description");
			result.SourceCodeUri.Should().Be("https://example.com/source-code");
			result.Owners.Should().ContainSingle(owner => owner.Name == "Owner1" && owner.Email == "owner1@example.com");
			result.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
		}

		[TestMethod()]
		public void FromCatalogYaml_MissingYamlFile_ShouldThrowException()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			mockFileSystem.Setup(fs => fs.Directory.IsDirectory(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(new string[] { });

			// Act
			Action act = () => new CatalogMetaDataFactory().FromCatalogYaml(mockFileSystem.Object, "test/path");

			// Assert
			act.Should().Throw<InvalidOperationException>().WithMessage("Unable to locate a catalog.yml or manifest.yml file within the provided directory/file or up to 5 parent directories.");
		}

		[TestMethod()]
		public void SearchAndApplyCatalogYaml_ValidFile_ShouldApplyYamlData()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			string yamlContent = @"
                id: catalog-id-5678
                type: automation
                title: AutomationScript
                owners:
                  - name: Owner2
                    email: owner2@example.com
                tags: [auto, deploy]
            ";

			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Returns(yamlContent);
			mockFileSystem.Setup(fs => fs.Directory.IsDirectory(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(new[] { "catalog.yml" });
			mockFileSystem.Setup(fs => fs.Path.GetFileName(It.IsAny<string>())).Returns("catalog.yml");

			var metaData = new CatalogMetaData();

			// Act
			var success = metaData.SearchAndApplyCatalogYaml(mockFileSystem.Object, "test/path");

			// Assert
			success.Should().BeTrue();
			metaData.CatalogIdentifier.Should().Be("catalog-id-5678");
			metaData.ContentType.Should().Be("automation");
			metaData.Name.Should().Be("AutomationScript");
			metaData.Owners.Should().ContainSingle(owner => owner.Name == "Owner2" && owner.Email == "owner2@example.com");
			metaData.Tags.Should().BeEquivalentTo(new[] { "auto", "deploy" });
		}

		[TestMethod()]
		public void SearchAndApplyCatalogYaml_ValidFile_ShouldOverwriteOrPreserveExistingData()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			string yamlContent = @"
        id: catalog-id-5678
        type: automation
        title: AutomationScript
        tags: [auto, deploy]
    ";

			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Returns(yamlContent);
			mockFileSystem.Setup(fs => fs.Directory.IsDirectory(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(new[] { "catalog.yml" });
			mockFileSystem.Setup(fs => fs.Path.GetFileName(It.IsAny<string>())).Returns("catalog.yml");

			// Pre-populate CatalogMetaData with some values
			var metaData = new CatalogMetaData
			{
				CatalogIdentifier = "pre-existing-id",
				ContentType = "solution",
				Name = "PreExistingScript",
				ShortDescription = "Pre-existing description",
				Owners = new List<CatalogOwner>
		{
			new CatalogOwner { Name = "Owner1", Email = "owner1@example.com" }
		},
				Tags = new List<string> { "pre-existing-tag" }
			};

			// Act
			var success = metaData.SearchAndApplyCatalogYaml(mockFileSystem.Object, "test/path");

			// Assert
			success.Should().BeTrue();

			// Ensure the YAML overrides existing data
			metaData.CatalogIdentifier.Should().Be("catalog-id-5678"); // Overwritten by YAML
			metaData.ContentType.Should().Be("automation"); // Overwritten by YAML
			metaData.Name.Should().Be("AutomationScript"); // Overwritten by YAML

			// Ensure pre-existing data is preserved when not overridden by YAML
			metaData.ShortDescription.Should().Be("Pre-existing description"); // Not overwritten, so it should remain
			metaData.Owners.Should().ContainSingle(owner => owner.Name == "Owner1" && owner.Email == "owner1@example.com"); // No owners in YAML, so it should remain
			metaData.Tags.Should().BeEquivalentTo(new[] { "pre-existing-tag", "auto", "deploy" }); // Extended by YAML
		}


		[TestMethod()]
		public void SearchAndApplyCatalogYaml_NoYamlFile_ShouldReturnFalse()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			mockFileSystem.Setup(fs => fs.Directory.IsDirectory(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(new string[] { });

			var metaData = new CatalogMetaData();

			// Act
			var success = metaData.SearchAndApplyCatalogYaml(mockFileSystem.Object, "test/path");

			// Assert
			success.Should().BeFalse();
		}

		[TestMethod()]
		public void RecursiveFindClosestCatalogYaml_FoundInParentDirectory_ShouldReturnFilePath()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();

			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(new string[] { });

			mockFileSystem.Setup(fs => fs.File.ReadAllText("catalog.yml")).Returns("");
			mockFileSystem.Setup(fs => fs.File.GetParentDirectory("test/path")).Returns("parentDir");
			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles("parentDir")).Returns(new[] { "catalog.yml" });
			mockFileSystem.Setup(fs => fs.Path.GetFileName("catalog.yml")).Returns("catalog.yml");
			var metaData = new CatalogMetaData();

			// Act
			var foundFile = metaData.SearchAndApplyCatalogYaml(mockFileSystem.Object, "test/path");

			// Assert
			foundFile.Should().BeTrue();
		}

		[TestMethod()]
		public async Task ToCatalogZipAsync_ShouldCreateValidZipFile()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			var mockSerializer = new Mock<ISerializer>();

			// Mock YAML serialization
			mockSerializer.Setup(s => s.Serialize(It.IsAny<CatalogYaml>()))
				.Returns("id: catalog-id-1234\ntitle: MyCatalogPackage");

			// Mock file system for README.md and Images folder
			mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>()))
				.Returns("Readme content"); // Mock README content

			mockFileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.GetFiles(It.IsAny<string>()))
				.Returns(new[] { "image1.png", "image2.png" }); // Mock image files
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.Is<string>(path => path == "image1.png")))
				.Returns("Image1 content"); // Mock Image1 content
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.Is<string>(path => path == "image2.png")))
				.Returns("Image2 content"); // Mock Image2 content

			mockFileSystem.Setup(fs => fs.Path.GetFileName("image1.png")).Returns("image1.png");
			mockFileSystem.Setup(fs => fs.Path.GetFileName("image2.png")).Returns("image2.png");
			mockFileSystem.Setup(fs => fs.Path.GetFileName("README.md")).Returns("README.md");

			var catalogMetaData = new CatalogMetaData
			{
				CatalogIdentifier = "catalog-id-1234",
				Name = "MyCatalogPackage",
				PathToReadme = "README.md",
				PathToImages = "Images"
			};

			// Act
			byte[] result = await catalogMetaData.ToCatalogZipAsync(mockFileSystem.Object, mockSerializer.Object);

			// Assert
			using (var zipStream = new MemoryStream(result))
			{
				using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
				{
					// Check manifest.yml exists
					var manifestEntry = zip.GetEntry("manifest.yml");
					manifestEntry.Should().NotBeNull();
					using (var reader = new StreamReader(manifestEntry.Open()))
					{
						var content = reader.ReadToEnd();
						content.Should().Contain("id: catalog-id-1234");
						content.Should().Contain("title: MyCatalogPackage");
					}

					// Check README.md exists
					var readmeEntry = zip.GetEntry("README.md");
					readmeEntry.Should().NotBeNull();
					using (var reader = new StreamReader(readmeEntry.Open()))
					{
						var content = reader.ReadToEnd();
						content.Should().Be("Readme content");
					}

					// Check Images folder and files exist
					var image1Entry = zip.GetEntry("Images/image1.png");
					image1Entry.Should().NotBeNull();
					using (var reader = new StreamReader(image1Entry.Open()))
					{
						var content = reader.ReadToEnd();
						content.Should().Be("Image1 content");
					}

					var image2Entry = zip.GetEntry("Images/image2.png");
					image2Entry.Should().NotBeNull();
					using (var reader = new StreamReader(image2Entry.Open()))
					{
						var content = reader.ReadToEnd();
						content.Should().Be("Image2 content");
					}
				}
			}
		}

		[TestMethod()]
		public async Task ToCatalogZipAsync_NoReadmeFile_ShouldNotIncludeReadme()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			var mockSerializer = new Mock<ISerializer>();

			// Mock YAML serialization
			mockSerializer.Setup(s => s.Serialize(It.IsAny<CatalogYaml>()))
				.Returns("id: catalog-id-1234\ntitle: MyCatalogPackage");

			// Mock file system without README.md
			mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(false); // No README file

			mockFileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.GetFiles(It.IsAny<string>()))
				.Returns(new[] { "image1.png" }); // Mock image files
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.Is<string>(path => path == "image1.png")))
				.Returns("Image1 content"); // Mock Image1 content

			mockFileSystem.Setup(fs => fs.Path.GetFileName("image1.png")).Returns("image1.png");

			var catalogMetaData = new CatalogMetaData
			{
				CatalogIdentifier = "catalog-id-1234",
				Name = "MyCatalogPackage",
				PathToImages = "Images"
			};

			// Act
			byte[] result = await catalogMetaData.ToCatalogZipAsync(mockFileSystem.Object, mockSerializer.Object);

			// Assert
			using (var zipStream = new MemoryStream(result))
			{
				using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
				{
					// Check manifest.yml exists
					var manifestEntry = zip.GetEntry("manifest.yml");
					manifestEntry.Should().NotBeNull();
					using (var reader = new StreamReader(manifestEntry.Open()))
					{
						var content = reader.ReadToEnd();
						content.Should().Contain("id: catalog-id-1234");
						content.Should().Contain("title: MyCatalogPackage");
					}

					// Check README.md does not exist
					var readmeEntry = zip.GetEntry("README.md");
					readmeEntry.Should().BeNull();

					// Check Images folder and files exist
					var image1Entry = zip.GetEntry("Images/image1.png");
					image1Entry.Should().NotBeNull();
					using (var reader = new StreamReader(image1Entry.Open()))
					{
						var content = reader.ReadToEnd();
						content.Should().Be("Image1 content");
					}
				}
			}
		}

		[TestMethod()]
		public async Task ToCatalogZipAsync_ImagesFolderEmpty_ShouldNotIncludeImages()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			var mockSerializer = new Mock<ISerializer>();

			// Mock YAML serialization
			mockSerializer.Setup(s => s.Serialize(It.IsAny<CatalogYaml>()))
				.Returns("id: catalog-id-1234\ntitle: MyCatalogPackage");

			// Mock file system without images
			mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true); // README exists
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Returns("Readme content"); // Mock README content

			mockFileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.GetFiles(It.IsAny<string>())).Returns(new string[] { }); // No image files

			mockFileSystem.Setup(fs => fs.Path.GetFileName(It.IsAny<string>())).Returns((string s) => s);

			var catalogMetaData = new CatalogMetaData
			{
				CatalogIdentifier = "catalog-id-1234",
				Name = "MyCatalogPackage",
				PathToReadme = "README.md",
				PathToImages = "Images"
			};

			// Act
			byte[] result = await catalogMetaData.ToCatalogZipAsync(mockFileSystem.Object, mockSerializer.Object);

			// Assert
			using (var zipStream = new MemoryStream(result))
			{
				using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
				{
					// Check manifest.yml exists
					var manifestEntry = zip.GetEntry("manifest.yml");
					manifestEntry.Should().NotBeNull();

					// Check README.md exists
					var readmeEntry = zip.GetEntry("README.md");
					readmeEntry.Should().NotBeNull();

					// Check Images folder does not exist
					var imageEntry = zip.GetEntry("Images/image1.png");
					imageEntry.Should().BeNull();
				}
			}
		}

		[TestMethod()]
		public async Task ToCatalogZipAsync_NoReadmeOrImages_ShouldOnlyContainManifest()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			var mockSerializer = new Mock<ISerializer>();

			// Mock YAML serialization
			mockSerializer.Setup(s => s.Serialize(It.IsAny<CatalogYaml>()))
				.Returns("id: catalog-id-1234\ntitle: MyCatalogPackage");

			// Mock file system without README and images
			mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(false); // No README
			mockFileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(false); // No images folder

			var catalogMetaData = new CatalogMetaData
			{
				CatalogIdentifier = "catalog-id-1234",
				Name = "MyCatalogPackage"
			};

			// Act
			byte[] result = await catalogMetaData.ToCatalogZipAsync(mockFileSystem.Object, mockSerializer.Object);

			// Assert
			using (var zipStream = new MemoryStream(result))
			{
				using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
				{
					// Check manifest.yml exists
					var manifestEntry = zip.GetEntry("manifest.yml");
					manifestEntry.Should().NotBeNull();

					// Check README.md does not exist
					var readmeEntry = zip.GetEntry("README.md");
					readmeEntry.Should().BeNull();

					// Check Images folder does not exist
					var imageEntry = zip.GetEntry("Images/image1.png");
					imageEntry.Should().BeNull();
				}
			}
		}

		[TestMethod()]
		public async Task ToCatalogZipAsync_LargeNumberOfImageFiles_ShouldIncludeAllImages()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			var mockSerializer = new Mock<ISerializer>();

			// Mock YAML serialization
			mockSerializer.Setup(s => s.Serialize(It.IsAny<CatalogYaml>()))
				.Returns("id: catalog-id-1234\ntitle: MyCatalogPackage");

			// Mock file system for README.md and a large number of image files
			mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Returns("Readme content");

			mockFileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
			var largeImageList = new List<string>();
			for (int i = 0; i < 1000; i++)
			{
				largeImageList.Add($"image{i}.png");
			}
			mockFileSystem.Setup(fs => fs.Directory.GetFiles(It.IsAny<string>())).Returns(largeImageList.ToArray());
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Returns("Image content");

			mockFileSystem.Setup(fs => fs.Path.GetFileName(It.IsAny<string>())).Returns((string s) => s);

			var catalogMetaData = new CatalogMetaData
			{
				CatalogIdentifier = "catalog-id-1234",
				Name = "MyCatalogPackage",
				PathToReadme = "README.md",
				PathToImages = "Images"
			};

			// Act
			byte[] result = await catalogMetaData.ToCatalogZipAsync(mockFileSystem.Object, mockSerializer.Object);

			// Assert
			using (var zipStream = new MemoryStream(result))
			{
				using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
				{
					// Check that all 1000 images are included
					for (int i = 0; i < 1000; i++)
					{
						var imageEntry = zip.GetEntry($"Images/image{i}.png");
						imageEntry.Should().NotBeNull();
					}
				}
			}
		}
	}
}