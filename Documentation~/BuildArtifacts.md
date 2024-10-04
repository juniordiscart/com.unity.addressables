---
uid: addressables-build-artifacts
---
# Build artifacts

A [content build]( xref:addressables-builds) creates files in several locations and Unity doesn't include every file in a built player. Typically, Unity includes files associated with local content in the built player and excludes files associated with remote content.

Most of the files associated with local content are in the `Library/com.unity.addressables` folder. This is a special subfolder in the `Library` folder which Unity uses to store Addressables files. For more information about the `Library` folder, refer to [Importing assets](xref:ImportingAssets).
