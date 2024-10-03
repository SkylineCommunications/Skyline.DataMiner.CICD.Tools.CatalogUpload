# Skyline.DataMiner.CICD.Tools.CatalogUpload

## About

Uploads and/or makes visible, artifacts to the Skyline DataMiner catalog (https://catalog.dataminer.services).

> **Note**
> Usage of this tool is tracked through non-personal metrics provided through a single https call on each use.
>
> These metrics may include, but are not limited to, the frequency of use and the primary purposes for which the Software is employed (e.g., automation, protocol analysis, visualization, etc.). By using the Software, you agree to allow Skyline to collect and analyze such metrics for the purpose of improving and enhancing the Software.
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

## Creating a dataminer.services Key

A `dataminer.services` key can be scoped either to a specific DMS or to an organization. Both types of keys can be used to upload and register items.

**Note:** For volatile uploads, you can only upload and deploy using the same DMS-scoped key.

For more details on how to create a `dataminer.services` key, refer to the documentation [here](https://docs.dataminer.services/user-guide/Cloud_Platform/CloudAdminApp/Managing_DCP_keys.html).

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

To make your package visible on the catalog and enable the creation of combined Installation Packages (currently only available through internal tools at Skyline Communications), you need to provide additional registration metadata.

#### Important Changes Since Version 3.0.1
- The **--uri-sourcecode** argument is no longer required. Instead, you must provide the **catalog-identifier**, which is the GUID identifying the catalog item on [catalog.dataminer.services](https://catalog.dataminer.services/). If not provided through the `catalog-identifier` argument, it must be specified in a `catalog.yml` file as described [here](https://docs.dataminer.services/user-guide/Cloud_Platform/Catalog/Register_Catalog_Item.html#manifest-file).
- If a `README.md` file or an `Images` folder is present in the same directory (or a parent directory) as the `.dmapp` or `.dmprotocol` file, they will be registered alongside the package.

#### Basic Command
The most basic command is anonymous by default and will try to use the `main` branch and the version defined in the artifact (either protocol version or DMAPP version).

```console
dataminer-catalog-upload with-registration --path-to-artifact "pathToPackage.dmapp" --catalog-identifier "123-aaa-123-123-abc"
```

#### Version Tag Recommendation
Though optional, it is highly recommended to provide your own **version tag** due to current restrictions in the internal DMAPP version syntax.

```console
dataminer-catalog-upload with-registration --path-to-artifact "pathToPackage.dmapp" --catalog-identifier "123-aaa-123-123-abc" --version "1.0.1-alpha1"
```

#### Optional Additional Information
You can also provide additional optional metadata:

```console
dataminer-catalog-upload with-registration --path-to-artifact "pathToPackage.dmapp" --catalog-identifier "123-aaa-123-123-abc" --uri-sourcecode "https://github.com/SkylineCommunications/MyTestRepo" --version "1.0.1-alpha1" --branch "dev/MyFeature" --author-mail "thunder@skyline.be" --release-notes "https://github.com/SkylineCommunications/MyTestRepo/releases/tag/1.0.3"
```

Alternatively, you can rely on a **catalog.yml file** located next to the `.dmapp` or `.dmprotocol` file, as described [here](https://docs.dataminer.services/user-guide/Cloud_Platform/Catalog/Register_Catalog_Item.html#manifest-file).

```console
dataminer-catalog-upload with-registration --path-to-artifact "pathToPackage.dmapp"
```

### Update Catalog Details

You can update or create only the catalog registration details by providing a `.yml` file containing the required metadata (as described [here](https://docs.dataminer.services/user-guide/Cloud_Platform/Catalog/Register_Catalog_Item.html#manifest-file)), along with an optional `README.md` and `Images` folder.

```console
dataminer-catalog-upload update-catalog-details --path-to-catalog-yml "catalog.yml" --path-to-readme "README.md" --path-to-images "resources/images"
```