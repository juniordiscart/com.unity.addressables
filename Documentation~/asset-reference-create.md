# Create asset reference fields

To add an [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) or one of its subclasses, to a `MonoBehaviour` or `ScriptableObject`, declare it as a serializable field in the class:

[!code-cs[sample](../Tests/Editor/DocExampleCode/DeclaringReferences.cs#doc_DeclaringReferences)]

> [!NOTE]
> Before Unity 2020.1, the Inspector window couldn't display generic fields by default. In earlier versions of Unity, you must make your own non-generic subclass of `AssetReferenceT` instead. For more information, refer to [Create a concrete subclass](#create-a-concrete-subclass).

## Load and release asset references

The [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) class provides its own methods to load, instantiate, and release a referenced asset. You can also use an AssetReference instance as a key to any `Addressables` class method that loads assets.

The following example instantiates an AssetReference as a child of the current GameObject and releases it when the parent is destroyed:

[!code-cs[sample](../Tests/Editor/DocExampleCode/InstantiateReference.cs#doc_InstantiateReference)]

Refer to [Load an AssetReference](LoadingAssetReferences.md) for more information and examples about loading assets using AssetReferences.

## Use labels with asset references

Use the [`AssetReferenceUILabelRestriction`](xref:UnityEngine.AssetReferenceUILabelRestriction) attribute to restrict the assets you can assign to an `AssetReference` field to those with specific [labels](Labels.md). You can use this attribute reference and `AssetReference` subclasses to restrict assignment by both type and label.

The following example prevents someone from assigning an Addressable asset to a reference that doesn't have either the label, `animals`, or the label, `characters`:

[!code-cs[sample](../Tests/Editor/DocExampleCode/DeclaringReferences.cs#doc_RestrictionAttribute)]

This attribute only prevents assigning assets without the specified label using an Inspector in the Unity Editor. You can still assign an asset without the label to the field using a script.

You can't drag non-Addressable assets to a field with the `AssetReferenceUILabelRestriction` attribute.

## Create a concrete subclass

If you can't use the generic form of the `AssetReference` class directly, such as in versions of Unity prior to Unity 2020.1 or when using the `CustomPropertyDrawer` attribute, you can create a concrete subclass.

To create a concrete subclass, inherit from the [`AssetReferenceT`](xref:UnityEngine.AddressableAssets.AssetReferenceT`1) class and specify the asset type. You must also pass the GUID string to the base class constructor:

[!code-cs[sample](../Tests/Editor/DocExampleCode/DeclaringReferences.cs#doc_ConcreteSubclass)]


You can use your custom AssetReference subclass in another script the same way as other AssetReference types:

[!code-cs[sample](../Tests/Editor/DocExampleCode/DeclaringReferences.cs#doc_UseConcreteSubclass)]
