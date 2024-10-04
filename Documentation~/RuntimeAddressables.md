---
uid: addressable-runtime
---

# Use Addressables at runtime

Once you have Addressable assets organized into groups and built into AssetBundles, you must load, instantiate, and release them at runtime.

Addressables uses a reference counting system to make sure that assets are only kept in memory while they're needed.

|**Topic**|**Description**|
|---|---|
|[Addressables initialization](InitializeAsync.md)|Understand how and when Addressables are initialized.|
|[Memory management overview](MemoryManagement.md)|Understand how Unity manages Addressables memory.|
|[Manage catalogs at runtime](LoadContentCatalogAsync.md)|How to manage the catalogs in your project at runtime.|
|[Get addresses at runtime](GetRuntimeAddress.md)|How to get and use addresses at runtime.|
|[Modification events](ModificationEvents.md)|Understand modification events, which signal when data is manipulated.|