# Content catalogs

Content catalogs are the data stores Addressables uses to look up an asset's physical location based on the keys provided to the system. Addressables builds a single catalog for all Addressable assets. The catalog is placed in the [StreamingAssets](xref:StreamingAssets) folder when you build your application player. The local catalog can access remote and local assets, but if you want to update content between full builds of your application, you must create a remote catalog.

## Remote catalog

The remote catalog is a separate copy of the catalog that you host along with your remote content.

Ultimately, Addressables only uses one of these catalogs. A hash file contains the hash (a mathematical fingerprint) of the catalog. If a remote catalog is built and it has a different hash than the local catalog, it is downloaded, cached, and used in place of the built-in local catalog. When you produce a [content update build](ContentUpdateWorkflow.md), the hash is updated and the new remote catalog points to the changed versions of any updated assets.

> [!NOTE]
> You must enable the remote catalog for the full player build that you publish. Otherwise, the Addressables system doesn't check for a remote catalog and can't detect any content updates. Refer to [Enabling the remote catalog](xref:addressables-remote-content-distribution) for more information.

Although Addressables produces one content catalog per project, you can load catalogs created by other projects to load Addressable assets produced by those projects. This allows you to use separate projects to develop and build some of your assets, which can make iteration and team collaboration easier on large productions. See [Managing catalogs at runtime](xref:addressables-api-load-content-catalog-async) for information about loading catalogs.

## Catalog settings

There are the following settings used for catalogs:

* [Catalog settings](xref:addressables-asset-settings): Options used to configure local and remote catalogs
* [Content update settings](xref:addressables-asset-settings): Options used to configure the remote catalog only

To minimize the catalog size, use the following settings:

* **Compress the local catalog**: If your primary concern is how big the catalog is in your build, there is an option in [Catalog settings](xref:addressables-asset-settings) called **Compress Local Catalog**. This option builds catalog that ships with your game into an AssetBundle. Compressing the catalog makes the file itself smaller, but note that this does increase catalog load time.

There are several group settings that can help reduce the catalog size, such as __Internal Asset Naming Mode__. For more information refer to [Advanced Group settings](xref:addressables-content-packing-and-loading-schema).
