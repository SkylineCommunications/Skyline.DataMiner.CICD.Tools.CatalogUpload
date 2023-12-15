# Skyline.DataMiner.CICD.Tools.CatalogUpload

## About

Uploads and/or makes visible, artifacts to the Skyline DataMiner catalog (https://catalog.dataminer.services).

### About DataMiner

DataMiner is a transformational platform that provides vendor-independent control and monitoring of devices and services. Out of the box and by design, it addresses key challenges such as security, complexity, multi-cloud, and much more. It has a pronounced open architecture and powerful capabilities enabling users to evolve easily and continuously.

The foundation of DataMiner is its powerful and versatile data acquisition and control layer. With DataMiner, there are no restrictions to what data users can access. Data sources may reside on premises, in the cloud, or in a hybrid setup.

A unique catalog of 7000+ connectors already exist. In addition, you can leverage DataMiner Development Packages to build you own connectors (also known as "protocols" or "drivers").

> **Note**
> See also: [About DataMiner](https://aka.dataminer.services/about-dataminer).

### About Skyline Communications

At Skyline Communications, we deal in world-class solutions that are deployed by leading companies around the globe. Check out [our proven track record](https://aka.dataminer.services/about-skyline) and see how we make our customers' lives easier by empowering them to take their operations to the next level.

## Getting Started
In commandline:

```console
dotnet tool install -g Skyline.DataMiner.CICD.Tools.CatalogUpload
```

Then run the command

```console
dataminer-catalog-upload help
```
## Common Commands

### Volatile Uploads
The most basic command will upload but not register a package. 
This allows further usage only with the returned Artifact ID. The package will not show up in the Catalog. 
Nothing will be registered but your cloud-connected agent will be able to get deployed with the package using the returned identifier.

```console
dataminer-catalog-upload --path-to-artifact "pathToPackage.dmapp" --dm-catalog-token "cloudConnectedToken"
```

### Authentication and Tokens

You can choose to add the DATAMINER_CATALOG_TOKEN to an environment variable instead and skip having to pass along the secure token.
```console
 dataminer-catalog-upload --path-to-artifact "pathToPackage.dmapp"
```
 
 There are 2 options to store the key in an environment variable:
- key stored as an Environment Variable called "DATAMINER_CATALOG_TOKEN". (unix/win)
- key configured one-time using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "DATAMINER_CATALOG_TOKEN_ENCRYPTED" (windows only)

The first option is commonplace for environment setups in cloud-based CI/CD Pipelines (github, gitlab, azure, ...)
The second option can be beneficial on a static server such as Jenkins or your local machine (windows only). It adds additional encryption to the environment variable only allowing decryption on the same machine. 

Running as Administrator:
```console
dotnet tool install -g Skyline.DataMiner.CICD.Tools.WinEncryptedKeys
WinEncryptedKeys --name "DATAMINER_CATALOG_TOKEN_ENCRYPTED" --value "MyTokenHere"
```

> **Note**
> Make sure you close your commandline tool so it clears the history.
> This only works on windows machines.

You can review and make suggestions to the sourcecode of this encryption tool here: 
https://github.com/SkylineCommunications/Skyline.DataMiner.CICD.Tools.WinEncryptedKeys

 ### Registered Uploads

If you want to make your package visible on the catalog and provide the ability to create combined Installation Packages (Currently only available through internal tools at Skyline Communications) you'll need to provide additional registration meta-data.

The most basic command will be default anonymous and try to use the 'main' branch and the version defined in the artifact (either protocol version or dmapp version)

```console
 dataminer-catalog-upload with-registration --path-to-artifact "pathToPackage.dmapp" --sourcecode "https://github.com/SkylineCommunications/MyTestRepo"
```

Though optional, it is however highly recommended (due to current restrictions to the internal dmapp version syntax) to provide your own version tag.

```console
 dataminer-catalog-upload with-registration --path-to-artifact "pathToPackage.dmapp" --sourcecode "https://github.com/SkylineCommunications/MyTestRepo" --version "1.0.1-alpha1"
```

In addition you can provide additional optional information:

```console
 dataminer-catalog-upload with-registration --path-to-artifact "pathToPackage.dmapp" --sourcecode "https://github.com/SkylineCommunications/MyTestRepo" --version "1.0.1-alpha1" --branch "dev/MyFeature" --author-mail "thunder@skyline.be" --release-notes "https://github.com/SkylineCommunications/MyTestRepo/releases/tag/1.0.3"
```
