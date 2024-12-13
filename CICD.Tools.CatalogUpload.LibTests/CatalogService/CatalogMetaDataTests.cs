namespace CICD.Tools.CatalogUpload.LibTests.CatalogService
{
    using System.IO.Compression;

    using FluentAssertions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;
    using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.CatalogService;

    using YamlDotNet.Serialization;

    [TestClass]
    public class CatalogMetaDataTests
    {
        [TestMethod]
        public void FromArtifactTest_FromDMProtocol_BuildVersion()
        {
            // Arrange
            string pathToArtifact = "TestData/Arris_E6000_2_0_0_17_B3.dmprotocol";

            // Act		
            CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

            // Assert

            CatalogMetaData expected = new CatalogMetaData
            {
                ArtifactHadBuildNumber = true,
                ContentType = "Connector",
                Name = "Arris E6000",
                Version = new CatalogVersionMetaData
                {
                    Value = "2.0.0.17_B3",
                    VersionDescription = "Fix: Fixed snmp instance ID appearing in the Total PATs TX column of the QAM Streams Status table.\r\nNewFeature: Added Video Counts table.\r\nNewFeature: Added Global Video Input Streams table.\r\nNewFeature: Added Passthrough Type, Network ID, Original Network ID, NIT PID ID, Network Name, PAT Generation for Broadcast, Force PAT NIT Entry to QAM Streams Filtered table.\r\nNewFeature: Added Video Streams Table Instance, Time Activated, Time Deactivated, Packet Count, Channel Container, PID Passthrou..."
                }
            };

            result.Should().Be(expected);
            result.IsPreRelease().Should().BeTrue();
        }

        [TestMethod]
        public void FromArtifactTest_FromDMProtocol_Prerelease()
        {
            // Arrange
            string pathToArtifact = "TestData/BOOST-Test-SDK-Connector 1.0.0.1-alpha1.dmprotocol";

            // Act		
            CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

            // Assert

            CatalogMetaData expected = new CatalogMetaData
            {
                ArtifactHadBuildNumber = false,
                ContentType = "Connector",
                Name = "Skyline BOOST Test Connector",
                Version = new CatalogVersionMetaData
                {
                    Value = "1.0.0.1-alpha1",
                    VersionDescription = "NewFeature: Initial version"
                }
            };

            result.Should().Be(expected);
            result.IsPreRelease().Should().BeTrue();
        }

        [TestMethod]
        public void FromArtifactTest_BuildPreReleaseDmappWithProtocols()
        {
            // Arrange
            string pathToArtifact = "TestData/withProtocols.dmapp";

            // Act		
            CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

            // Assert

            CatalogMetaData expected = new CatalogMetaData
            {
                ContentType = "Custom Solution",
                Name = "SLNetSubscriptionsBenchmarking",
                Version = new CatalogVersionMetaData
                {
                    Value = "1.0.1-B15",
                    VersionDescription = "Pre-Release (Unofficial) version.\r\nMinimum DataMiner Version: 10.0.10.0-9414\r\n---------------------------------\r\nPackage creation time: 2023-11-15 13:04:54\r\n---------------------------------\r\nFile Versions:\r\nProtocol\\Metrics Subscription Event Generator:1.0.0.1_B77\r\nProtocol\\Metrics Subscription Monitor A:1.0.0.1_B74\r\nProtocol\\Metrics Subscription Monitor B:1.0.0.1_B62\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\newtonsoft.json\\13.0.2\\lib\\net45\\Newtonsoft.Json.dll for protocol:Me...",
                    //VersionDescription = "Pre-Release (Unofficial) version.\r\nMinimum DataMiner Version: 10.0.10.0-9414\r\n---------------------------------\r\nPackage creation time: 2023-11-15 13:04:54\r\n---------------------------------\r\nFile Versions:\r\nProtocol\\Metrics Subscription Event Generator:1.0.0.1_B77\r\nProtocol\\Metrics Subscription Monitor A:1.0.0.1_B74\r\nProtocol\\Metrics Subscription Monitor B:1.0.0.1_B62\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\newtonsoft.json\\13.0.2\\lib\\net45\\Newtonsoft.Json.dll for protocol:Metrics Subscription Event Generator\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\system.threading.tasks.dataflow\\7.0.0\\lib\\net462\\System.Threading.Tasks.Dataflow.dll for protocol:Metrics Subscription Event Generator\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\skyline.dataminer.core.dataminersystem.common\\1.0.0-dev.monitorpartialtablesupport.6\\lib\\net462\\Skyline.DataMiner.Core.DataMinerSystem.Common.dll for protocol:Metrics Subscription Event Generator\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\skyline.dataminer.core.dataminersystem.protocol\\1.0.0-dev.monitorpartialtablesupport.6\\lib\\net462\\Skyline.DataMiner.Core.DataMinerSystem.Protocol.dll for protocol:Metrics Subscription Event Generator\r\n",
                }
            };

            result.Should().Be(expected);
            result.IsPreRelease().Should().BeTrue();
        }

        [TestMethod]
        public void FromArtifactTest_ReleaseAutomation()
        {
            // Arrange
            string pathToArtifact = "TestData/withAutomation.dmapp";

            // Act
            CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

            // Assert

            CatalogMetaData expected = new CatalogMetaData
            {
                ContentType = "Automation",
                Name = "Demo InterAppCalls",
                Version = new CatalogVersionMetaData
                {
                    Value = "1.0.0-CU1",
                    VersionDescription = "Minimum DataMiner Version: 10.0.9.0-9312\r\n---------------------------------\r\nPackage creation time: 2023-07-24 17:00:19\r\n---------------------------------\r\nFile Versions:\r\nScript\\MWCore-InterAppDemo-Streams:1.0.0-CU1\r\nScript\\MWCore-InterAppDemo-InputsOutputs:1.0.0-CU1\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\newtonsoft.json\\13.0.3\\lib\\net45\\Newtonsoft.Json.dll for automationscript:MWCore-InterAppDemo-Streams\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\skyline.data...",
                    //VersionDescription = "Minimum DataMiner Version: 10.0.9.0-9312\r\n---------------------------------\r\nPackage creation time: 2023-07-24 17:00:19\r\n---------------------------------\r\nFile Versions:\r\nScript\\MWCore-InterAppDemo-Streams:1.0.0-CU1\r\nScript\\MWCore-InterAppDemo-InputsOutputs:1.0.0-CU1\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\newtonsoft.json\\13.0.3\\lib\\net45\\Newtonsoft.Json.dll for automationscript:MWCore-InterAppDemo-Streams\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\skyline.dataminer.core.dataminersystem.common\\1.0.0.2\\lib\\net462\\Skyline.DataMiner.Core.DataMinerSystem.Common.dll for automationscript:MWCore-InterAppDemo-Streams\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\skyline.dataminer.core.interappcalls.common\\1.0.0.2\\lib\\net462\\Skyline.DataMiner.Core.InterAppCalls.Common.dll for automationscript:MWCore-InterAppDemo-Streams\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\skyline.dataminer.utils.connectorapi.techex.mwcore\\1.0.1\\lib\\net462\\Skyline.DataMiner.Utils.ConnectorAPI.Techex.MWCore.dll for automationscript:MWCore-InterAppDemo-Streams\r\nAssembly\\C:\\Skyline DataMiner\\ProtocolScripts\\DllImport\\newtonsoft.json\\13.0.2\\lib\\net45\\Newtonsoft.Json.dll for automationscript:MWCore-InterAppDemo-Streams\r\n",
                }
            };

            result.Should().Be(expected);
            result.IsPreRelease().Should().BeFalse();
        }

        [TestMethod]
        public void FromArtifactTest_ReleaseDashboard()
        {
            // Arrange
            string pathToArtifact = "TestData/withDashboard.dmapp";

            // Act
            CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

            // Assert

            CatalogMetaData expected = new CatalogMetaData
            {
                ContentType = "dashboard",
                Name = "Tandberg RX1290",
                Version = new CatalogVersionMetaData
                {
                    Value = "1.0.0-CU1",
                    VersionDescription = "Minimum DataMiner Version: 10.0.9.0-9312\r\n---------------------------------\r\nPackage creation time: 2023-09-28 15:37:14\r\n---------------------------------\r\nFile Versions:\r\nDashboard\\AppInstallContent\\Dashboards\\Tandberg KPI Overview.dmadb.json\r\nDashboard\\AppInstallContent\\Dashboards\\Tandberg KPI.dmadb.json\r\nDashboard\\AppInstallContent\\Dashboards\\Tandberg Status Overview.dmadb.json\r\n",
                },
            };

            result.Should().Be(expected);
            result.IsPreRelease().Should().BeFalse();
        }

        [TestMethod]
        public void FromArtifactTest_ReleaseProtocolVisio()
        {
            // Arrange
            string pathToArtifact = "TestData/withVisio.dmapp";

            // Act
            CatalogMetaData result = new CatalogMetaDataFactory().FromArtifact(pathToArtifact);

            // Assert

            CatalogMetaData expected = new CatalogMetaData
            {
                ContentType = "Visual Overview",
                Name = "Microsoft Platform",
                Version = new CatalogVersionMetaData
                {
                    Value = "1.0.0-CU4",
                    VersionDescription = "Minimum DataMiner Version: 10.0.9.0-9312\r\n---------------------------------\r\nPackage creation time: 2023-11-28 10:30:46\r\n---------------------------------\r\nFile Versions:\r\nVisio\\skyline_Microsoft Platform:1.0.0-CU4\r\n",
                },
            };

            result.Should().Be(expected);
            result.IsPreRelease().Should().BeFalse();
        }

        [TestMethod]
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

        [TestMethod]
        public void FromCatalogYaml_MissingYamlFile_ShouldThrowException()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.IsDirectory(It.IsAny<string>())).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(Array.Empty<string>());

            // Act
            Action act = () => new CatalogMetaDataFactory().FromCatalogYaml(mockFileSystem.Object, "test/path");

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("Unable to locate a catalog.yml or manifest.yml file within the provided directory/file or up to 5 parent directories.");
        }

        [TestMethod]
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
			var success = metaData.SearchAndApplyCatalogYamlAndReadMe(mockFileSystem.Object, "test/path");

            // Assert
            success.Should().BeTrue();
            metaData.CatalogIdentifier.Should().Be("catalog-id-5678");
            metaData.ContentType.Should().Be("automation");
            metaData.Name.Should().Be("AutomationScript");
            metaData.Owners.Should().ContainSingle(owner => owner.Name == "Owner2" && owner.Email == "owner2@example.com");
            metaData.Tags.Should().BeEquivalentTo(new[] { "auto", "deploy" });
        }

        [TestMethod]
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
			var success = metaData.SearchAndApplyCatalogYamlAndReadMe(mockFileSystem.Object, "test/path");

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


        [TestMethod]
        public void SearchAndApplyCatalogYaml_NoYamlFile_ShouldReturnFalse()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.IsDirectory(It.IsAny<string>())).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(Array.Empty<string>());

            var metaData = new CatalogMetaData();

			// Act
			var success = metaData.SearchAndApplyCatalogYamlAndReadMe(mockFileSystem.Object, "test/path");

            // Assert
            success.Should().BeFalse();
        }

        [TestMethod]
        public void RecursiveFindClosestCatalogYaml_FoundInParentDirectory_ShouldReturnFilePath()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();

            mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(Array.Empty<string>());

            mockFileSystem.Setup(fs => fs.File.ReadAllText("catalog.yml")).Returns("");
            mockFileSystem.Setup(fs => fs.File.GetParentDirectory("test/path")).Returns("parentDir");
            mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles("parentDir")).Returns(new[] { "catalog.yml" });
            mockFileSystem.Setup(fs => fs.Path.GetFileName("catalog.yml")).Returns("catalog.yml");
            var metaData = new CatalogMetaData();

			// Act
			var foundFile = metaData.SearchAndApplyCatalogYamlAndReadMe(mockFileSystem.Object, "test/path");

            // Assert
            foundFile.Should().BeTrue();
        }

        [TestMethod]
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
            using var zipStream = new MemoryStream(result);
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // Check manifest.yml exists
            var manifestEntry = zip.GetEntry("manifest.yml");
            manifestEntry.Should().NotBeNull();
            if (manifestEntry == null) return;
            using (var reader = new StreamReader(manifestEntry.Open()))
            {
                var content = await reader.ReadToEndAsync();
                content.Should().Contain("id: catalog-id-1234");
                content.Should().Contain("title: MyCatalogPackage");
            }

            // Check README.md exists
            var readmeEntry = zip.GetEntry("README.md");
            readmeEntry.Should().NotBeNull();
            if (readmeEntry == null) return;
            using (var reader = new StreamReader(readmeEntry.Open()))
            {
                var content = await reader.ReadToEndAsync();
                content.Should().Be("Readme content");
            }

            // Check Images folder and files exist
            var image1Entry = zip.GetEntry("Images/image1.png");
            image1Entry.Should().NotBeNull();

            var image2Entry = zip.GetEntry("Images/image2.png");
            image2Entry.Should().NotBeNull();
        }

        [TestMethod]
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
            using var zipStream = new MemoryStream(result);
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // Check manifest.yml exists
            var manifestEntry = zip.GetEntry("manifest.yml");
            manifestEntry.Should().NotBeNull();
            if (manifestEntry == null) return;
            using (var reader = new StreamReader(manifestEntry.Open()))
            {
                var content = await reader.ReadToEndAsync();
                content.Should().Contain("id: catalog-id-1234");
                content.Should().Contain("title: MyCatalogPackage");
            }

            // Check README.md does not exist
            var readmeEntry = zip.GetEntry("README.md");
            readmeEntry.Should().BeNull();

            // Check Images folder and files exist
            var image1Entry = zip.GetEntry("Images/image1.png");
            image1Entry.Should().NotBeNull();
        }

        [TestMethod]
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
            using var zipStream = new MemoryStream(result);
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

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

        [TestMethod]
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
            using var zipStream = new MemoryStream(result);
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

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

        [TestMethod]
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
            using var zipStream = new MemoryStream(result);
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // Check that all 1000 images are included
            for (int i = 0; i < 1000; i++)
            {
                var imageEntry = zip.GetEntry($"Images/image{i}.png");
                imageEntry.Should().NotBeNull();
            }
        }
    }
}