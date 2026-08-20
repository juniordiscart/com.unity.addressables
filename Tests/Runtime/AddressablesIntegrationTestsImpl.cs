using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using NUnit.Framework.Internal;
using UnityEngine.AddressableAssets.ResourceProviders.Tests;
using UnityEngine.AddressableAssets.Tests;
using UnityEngine.Lumin;
using UnityEngine.Networking;
using UnityEngine.U2D;
using Logger = UnityEngine.Logger;
using Object = UnityEngine.Object;
using Texture2D = UnityEngine.Texture2D;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace AddressableAssetsIntegrationTests
{
    internal abstract partial class AddressablesIntegrationTests : IPrebuildSetup
    {
        class FakeTypedOperation : AsyncOperationBase<GameObject>
        {
            public FakeTypedOperation()
            {
                m_RM = new ResourceManager();
            }

            public object GetResultAsObject()
            {
                return null;
            }

            protected override void Execute()
            {
            }
        }

        [UnityTest]
        public IEnumerator AsyncCache_IsCleaned_OnFailedOperation()
        {
           yield return Init();

           AsyncOperationHandle<GameObject> op = default;
           try {
               using (new IgnoreFailingLogMessage())
                   op = m_Addressables.LoadAssetAsync<GameObject>("notARealKey");
               yield return op;
               Assert.AreEqual(0, m_Addressables.ResourceManager.CachedOperationCount());
           } finally {
               if (op.IsValid())
                   m_Addressables.Release(op);
           }
        }

        [UnityTest]
        public IEnumerator LoadResourceLocations_InvalidKeyDoesNotThrow()
        {
            //Setup
            yield return Init();

            //Test
            Assert.DoesNotThrow(() =>
            {
                var handle = m_Addressables.LoadResourceLocationsAsync("noSuchLabel", typeof(object));
                handle.WaitForCompletion();
                handle.Release();
            });
        }

        [UnityTest]
        public IEnumerator LoadResourceLocations_ValidKeyDoesNotThrow()
        {
            //Setup
            yield return Init();

            //Test
            Assert.DoesNotThrow(() => {
                var handle = m_Addressables.LoadResourceLocationsAsync(AddressablesTestUtility.GetPrefabLabel(BuildSuffix), typeof(GameObject));
                handle.Release();
            });

            yield return null; //< Process deferred callback
        }

        [UnityTest]
        public IEnumerator LoadResourceLocations_SubObjects_ReturnsCorrectResourceTypes()
        {
            yield return Init();
            string subObjectsAddress = "assetWithDifferentTypedSubAssets";

            var loadLocsHandle = m_Addressables.LoadResourceLocationsAsync(subObjectsAddress, typeof(object));
            yield return loadLocsHandle;

            Assert.AreEqual(3, loadLocsHandle.Result.Count);
            Assert.IsTrue(loadLocsHandle.Result.Any(loc => loc.ResourceType == typeof(Mesh)));
            Assert.IsTrue(loadLocsHandle.Result.Any(loc => loc.ResourceType == typeof(Material)));
            Assert.IsTrue(loadLocsHandle.Result.Any(loc => loc.ResourceType == typeof(TestObject)));

            loadLocsHandle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocations_ReturnsCorrectResourceTypes()
        {
            yield return Init();
            string spriteAddress = "sprite";

            var loadLocsHandle = m_Addressables.LoadResourceLocationsAsync(spriteAddress, typeof(object));
            yield return loadLocsHandle;

            Assert.AreEqual(2, loadLocsHandle.Result.Count);
            Assert.IsTrue(loadLocsHandle.Result.Any(loc => loc.ResourceType == typeof(Texture2D)));
            Assert.IsTrue(loadLocsHandle.Result.Any(loc => loc.ResourceType == typeof(Sprite)));

            loadLocsHandle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocations_SpecificType_ReturnsCorrectResourceTypes()
        {
            yield return Init();
            string spriteAddress = "sprite";

            var loadLocsHandle = m_Addressables.LoadResourceLocationsAsync(spriteAddress, typeof(Sprite));
            yield return loadLocsHandle;

            Assert.AreEqual(1, loadLocsHandle.Result.Count);
            Assert.AreEqual(typeof(Sprite), loadLocsHandle.Result[0].ResourceType);

            loadLocsHandle.Release();

            loadLocsHandle = m_Addressables.LoadResourceLocationsAsync(spriteAddress, typeof(Texture2D));
            yield return loadLocsHandle;

            Assert.AreEqual(1, loadLocsHandle.Result.Count);
            Assert.AreEqual(typeof(Texture2D), loadLocsHandle.Result[0].ResourceType);

            loadLocsHandle.Release();
        }

        [UnityTest]
        public IEnumerator LoadPrefabWithComponentType_Fails()
        {
            //Setup
            yield return Init();

            //Test
            AsyncOperationHandle<MeshRenderer> op = new AsyncOperationHandle<MeshRenderer>();
            using (new IgnoreFailingLogMessage())
            {
                op = m_Addressables.LoadAssetAsync<MeshRenderer>($"test0{BuildSuffix}");
            }

            yield return op;
            Assert.IsNull(op.Result);
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAsset_NoKeyFound()
        {
            //Setup
            yield return Init();
            string keyString = "noSuchLabel";
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetAsync<GameObject>(keyString);
                    yield return handle;
                }

                InvalidKeyException expected = new InvalidKeyException(keyString, typeof(GameObject));
                Assert.AreEqual(expected.FormatMessage(InvalidKeyException.Format.NoLocation), handle.OperationException.Message, "InvalidKeyException message not the same as expected for when the Location does not exist");
            }
            finally
            {
                //Cleanup
                if (handle.IsValid())
                    handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAsset_KeyFoundWithOtherType()
        {
            //Setup
            yield return Init();
            string keyString = $"test0{BuildSuffix}";
            AsyncOperationHandle<TextAsset> handle = new AsyncOperationHandle<TextAsset>();

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetAsync<TextAsset>(keyString);
                    yield return handle;
                }

                InvalidKeyException expected = new InvalidKeyException(keyString, typeof(TextAsset));
                string expectedMessage = expected.FormatMessage(InvalidKeyException.Format.TypeMismatch, typeof(GameObject).FullName);
                Assert.AreEqual(expectedMessage, handle.OperationException.Message,
                    "InvalidKeyException message not the same as expected for when a similar Location exists with same key and a different type");
            }
            finally
            {
                //Cleanup
                if (handle.IsValid())
                    handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAsset_KeyFoundWithMultipleOtherType()
        {
            //Setup
            yield return Init();
            string keyString = "mixed";
            string otherAvailableTypesForKey = "UnityEngine.GameObject, UnityEngine.AddressableAssets.Tests.TestObject";
            AsyncOperationHandle<TextAsset> handle = new AsyncOperationHandle<TextAsset>();

            try //Test
            {
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetAsync<TextAsset>(keyString);
                    yield return handle;
                }

                InvalidKeyException expected = new InvalidKeyException(keyString, typeof(TextAsset));
                string message = expected.FormatMessage(InvalidKeyException.Format.MultipleTypeMismatch, otherAvailableTypesForKey);
                bool isEqual = message == handle.OperationException.Message;
                if (!isEqual)
                {
                    // order isn't guaranteed
                    message = message.Replace("UnityEngine.GameObject, UnityEngine.AddressableAssets.Tests.TestObject", "UnityEngine.AddressableAssets.Tests.TestObject, UnityEngine.GameObject");
                    isEqual = message == handle.OperationException.Message;
                }

                Assert.IsTrue(isEqual,
                    $"InvalidKeyException message not the same as expected for when a similar Location exists with same key and a different type. Was expecting {message.ToString()}, but was {handle.OperationException.Message}");
            }
            finally // Cleanup
            {
                if (handle.IsValid())
                    handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAsset_AssetFromGUIDFoundWithDifferentType()
        {
            if (string.IsNullOrEmpty(TypeName) || TypeName != "BuildScriptFastMode")
            {
                Assert.Ignore($"Skipping test {nameof(InvalidKeyException_LoadAsset_AssetFromGUIDFoundWithDifferentType)} for {TypeName}, Editor AssetDatabase based test.");
            }

            //Setup
            yield return Init();
#if UNITY_EDITOR
            string keyString = $"test0{BuildSuffix}";

            AsyncOperationHandle<TextAsset> handle = new AsyncOperationHandle<TextAsset>();
            AsyncOperationHandle<GameObject> goLoadHandle = new AsyncOperationHandle<GameObject>();

            try
            {
                //Test
                goLoadHandle = m_Addressables.LoadAssetAsync<GameObject>(keyString);
                yield return goLoadHandle;
                Assert.AreEqual(goLoadHandle.Status, AsyncOperationStatus.Succeeded);
                bool foundGuid =
                    UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(goLoadHandle.Result, out string guid,
                        out long id);
                Assert.IsTrue(foundGuid, "Failed to get the Guid for loaded Object");
                goLoadHandle.Release();
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetAsync<TextAsset>(guid);
                    yield return handle;
                }

                InvalidKeyException expected = new InvalidKeyException(guid, typeof(TextAsset));
                string message = $"Exception of type 'UnityEngine.AddressableAssets.InvalidKeyException' was thrown. No Asset found for Key={guid} with Type={typeof(TextAsset)}. Key exists as Type={typeof(GameObject)}, which is not assignable from the requested Type={typeof(TextAsset)}";
                Assert.AreEqual(message, handle.OperationException.Message,
                    "InvalidKeyException message not the same as expected for when a similar Location exists with same key and a different type");
            }
            finally
            {
                //Cleanup
                if (handle.IsValid())
                    handle.Release();
                if (goLoadHandle.IsValid())
                    goLoadHandle.Release();
            }
#endif
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAsset_AssetFromGUIDFoundInProject()
        {
            if (string.IsNullOrEmpty(TypeName) || TypeName != "BuildScriptFastMode" || TypeName == "BuildScriptVirtualMode")
            {
                Assert.Ignore($"Skipping test {nameof(InvalidKeyException_LoadAsset_AssetFromGUIDFoundWithDifferentType)} for {TypeName}, Editor AssetDatabase based test.");
            }

            //Setup
            yield return Init();
#if UNITY_EDITOR
            var found = AssetDatabase.FindAssets("nonAddressableAsset");
            Assert.GreaterOrEqual(found.Length, 1);

            string keyString = found[0];
            AsyncOperationHandle<GameObject> goLoadHandle = new AsyncOperationHandle<GameObject>();

            try
            {
                //Test
                goLoadHandle = m_Addressables.LoadAssetAsync<GameObject>(keyString);
                yield return goLoadHandle;
                Assert.AreEqual(goLoadHandle.Status, AsyncOperationStatus.Failed);

                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(keyString);
                string message = $"Exception of type 'UnityEngine.AddressableAssets.InvalidKeyException' was thrown. No Location found for Key={keyString}. Asset exists in project at Path={path}, verify the asset is marked as Addressable.";
                Assert.AreEqual(message, goLoadHandle.OperationException.Message,
                    "InvalidKeyException message not the same as expected for when a asset in project is not addressable but attempting to load through guid");
            }
            finally
            {
                //Cleanup
                if (goLoadHandle.IsValid())
                    goLoadHandle.Release();
            }
#endif
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_MixedTypesGivesCorrectError()
        {
            //Setup
            yield return Init();
            object[] keys = new object[] {"noSuchKey", 123};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            //Test
            try
            {
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<GameObject>(keys, null, Addressables.MergeMode.Union, true);
                }

                InvalidKeyException expectedEx = new InvalidKeyException(keys, typeof(GameObject));
                string message = handle.OperationException.Message;
                string expected = expectedEx.FormatMessage(InvalidKeyException.Format.MultipleTypesRequested);

                bool equalOne = message == expected;
                bool equalTwo = message == expected.Replace("Types=System.String, System.Int32", "System.Int32, Types=System.String");
                Assert.IsTrue(equalOne || equalTwo, $"Failed to get correct message for mixedTypes being requested. was {message}. But expected {expected}");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAsset_MultipleKeysNoMergeMode()
        {
            //Setup
            yield return Init();
            string[] keysArray = new string[] {"noSuchKey1", "noSuchKey2"};

            //Test
            AsyncOperationHandle handle = default(AsyncOperationHandle);
            using (new IgnoreFailingLogMessage())
            {
                handle = m_Addressables.LoadAssetAsync<GameObject>(keysArray);
            }

            InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(GameObject));
            string keysErrorString = expected.FormatMergeModeMessage(InvalidKeyException.Format.NoMergeMode);
                //"Exception of type 'UnityEngine.AddressableAssets.InvalidKeyException' was thrown. No MergeMode is set to merge the multiple keys requested. Keys=noSuchKey1, noSuchKey2, Type=UnityEngine.GameObject";
            Assert.AreEqual(keysErrorString, handle.OperationException.Message);
            yield return handle;

            //Cleanup
            handle.Release();
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_Union_NoKeysFound()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] {"noSuchKey1", "noSuchKey2"};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<TextAsset>(keysArray, null, Addressables.MergeMode.Union, true);
                }

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(TextAsset), Addressables.MergeMode.Union);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, null, "noSuchKey1"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, null, "noSuchKey2"));
                string expectedMessage = stringBuilder.ToString();
                Assert.AreEqual(expectedMessage, handle.OperationException.Message, "Incorrect invalidKeyMessage. Expected to inform the two locations have for other type");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_Union_IncorrectType()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] {$"test0{BuildSuffix}", $"test1{BuildSuffix}"};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<TextAsset>(keysArray, null, Addressables.MergeMode.Union, true);
                }

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(TextAsset), Addressables.MergeMode.Union);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.UnionAvailableForKeys, $"Keys=test0{BuildSuffix}, test1{BuildSuffix}", null, typeof(GameObject).FullName));
                Assert.AreEqual(stringBuilder.ToString(), handle.OperationException.Message, "Incorrect invalidKeyMessage. Expected to inform the two locations have for other type");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator WhenLoadingAssetsAsync_WithArrayOfCharsAsKey_NothingLoads()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] { "t", "e", "s", "t", "0", "B", "A", "S", "E" };
            AsyncOperationHandle<IList<TextAsset>> handle = default;

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<TextAsset>(keysArray, null, Addressables.MergeMode.UseFirst, true);
                }
                Debug.Log(handle.OperationException.Message);

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(TextAsset), Addressables.MergeMode.UseFirst);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "t"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "e"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "s"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "t"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "0"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "B"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "A"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "S"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "E"));
                Assert.AreEqual(stringBuilder.ToString(), handle.OperationException.Message, "Incorrect invalidKeyMessage. Expected to inform that all keys have no location");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_Union_MultipleTypesAvailable()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] {$"test0{BuildSuffix}", "assetWithSubObjects"};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<TextAsset>(keysArray, null, Addressables.MergeMode.Union, true);
                }

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(TextAsset), Addressables.MergeMode.Union);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.UnionAvailableForKeysWithoutOther, "Key=test0" + BuildSuffix, "Key=assetWithSubObjects", typeof(GameObject).FullName));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.UnionAvailableForKeysWithoutOther, "Key=assetWithSubObjects", "Key=test0" + BuildSuffix, typeof(TestObject).FullName));
                Assert.AreEqual(stringBuilder.ToString(), handle.OperationException.Message, "Incorrect invalidKeyMessage. Expected to inform that a merge could be made for two different types");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_Intersection_OneMissingKey()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] {$"test0{BuildSuffix}", "noSuchKey"};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<GameObject>(keysArray, null, Addressables.MergeMode.Intersection, true);
                }

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(GameObject), Addressables.MergeMode.Intersection);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, null, "noSuchKey"));
                Assert.AreEqual(stringBuilder.ToString(), handle.OperationException.Message, "Incorrect invalidKeyMessage. Expected to error due to noSuchKey");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_Intersection_PossibleForAnotherType()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] {$"test0{BuildSuffix}", "mixed"};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<TextAsset>(keysArray, null, Addressables.MergeMode.Intersection, true);
                }

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(TextAsset), Addressables.MergeMode.Intersection);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.IntersectionAvailable, typeString:typeof(GameObject).FullName));
                Assert.AreEqual(stringBuilder.ToString(), handle.OperationException.Message, "Incorrect invalidKeyMessage. Expected to inform that an intersection exists with GameObject");
                yield return handle;
            }
            finally
            {
                //Cleanup
                if (handle.IsValid())
                    handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_UseFirst_NoLocations()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] {"noSuchKey1", "noSuchKey2"};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<GameObject>(keysArray, null, Addressables.MergeMode.UseFirst, true);
                }

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(GameObject), Addressables.MergeMode.UseFirst);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "noSuchKey1"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "noSuchKey2"));
                Assert.AreEqual(stringBuilder.ToString(), handle.OperationException.Message, "Incorrect invalidKeyMessage. Expected to inform that all keys have no location");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator InvalidKeyException_LoadAssets_UseFirst_PossibleForAnotherType()
        {
            //Setup
            yield return Init();
            string[] keysArray = new[] {$"test0{BuildSuffix}", "noSuchKey"};
            AsyncOperationHandle handle = default(AsyncOperationHandle);

            try
            {
                //Test
                using (new IgnoreFailingLogMessage())
                {
                    handle = m_Addressables.LoadAssetsAsync<TextAsset>(keysArray, null, Addressables.MergeMode.UseFirst, true);
                }

                InvalidKeyException expected = new InvalidKeyException(keysArray, typeof(TextAsset), Addressables.MergeMode.UseFirst);
                StringBuilder stringBuilder = new StringBuilder(expected.FormatMergeModeMessage(InvalidKeyException.Format.MergeModeBase));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.NoLocation, keysUnavailable: "noSuchKey"));
                stringBuilder.Append(expected.FormatMergeModeMessage(InvalidKeyException.Format.KeyAvailableAsType, $"test0{BuildSuffix}", null, typeof(GameObject).FullName));
                string expectedMessage = stringBuilder.ToString();
                Assert.AreEqual(expectedMessage, handle.OperationException.Message,
                    "Incorrect invalidKeyMessage. Expected to inform that one key has no location and the other can be loaded with GameObject");
                yield return handle;
            }
            finally
            {
                //Cleanup
                handle.Release();
            }
        }

        [UnityTest]
        public IEnumerator CanLoadTextureAsSprite()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Sprite>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite), op.Result.GetType());
            op.Release();
        }

        class AtlasSpriteProviderStub : AtlasSpriteProvider
        {
            public bool provideWasCalled = false;
            public bool releaseWasCalled = false;
            public override string ProviderId => nameof(AtlasSpriteProviderStub);
            public override void Provide(ProvideHandle providerInterface)
            {
                provideWasCalled = true;
                var sprite = Sprite.Create(new Texture2D(32, 32), new Rect(0, 0, 1, 1), new Vector2(0, 0));
                providerInterface.Complete(sprite, true, null);
            }
            public override void Release(IResourceLocation location, object obj)
            {
                if (obj is Sprite sprite)
                    Object.Destroy(sprite);
                releaseWasCalled = true;
            }
        }
        [UnityTest]
        public IEnumerator AtlasSpriteProviderIsCalledForProvideAndRelease()
        {
            //Setup
            yield return Init();
            var rm = m_Addressables.ResourceManager;
            var prov = new AtlasSpriteProviderStub();
            rm.ResourceProviders.Insert(0, prov);
            var handle = rm.ProvideResource<Sprite>(new ResourceLocationBase("", "id", nameof(AtlasSpriteProviderStub), typeof(Sprite)));

            while (!handle.IsDone)
                yield return null;

            Assert.IsTrue(prov.provideWasCalled);
            handle.Release();
            Assert.IsTrue(prov.releaseWasCalled);
            rm.ResourceProviders.RemoveAt(0);
        }

        [UnityTest]
        public IEnumerator CanLoadSpriteByName()
        {
            //Setup
            yield return Init();
            var op = m_Addressables.LoadAssetAsync<Sprite>("sprite[botright]");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite), op.Result.GetType());
            Assert.AreEqual("botright", op.Result.name);
            op.Release();

            var op2 = m_Addressables.LoadAssetAsync<Sprite>("sprite[topleft]");
            yield return op2;
            Assert.IsNotNull(op2.Result);
            Assert.AreEqual(typeof(Sprite), op2.Result.GetType());
            Assert.AreEqual("topleft", op2.Result.name);
            op2.Release();

            yield return null; //< Process deferred callback
        }

        [UnityTest]
        public IEnumerator CanLoadFromFolderEntry_SpriteAtlas()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<SpriteAtlas>("folderEntry/atlas.spriteatlas");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(SpriteAtlas), op.Result.GetType());
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadFromFolderEntry_SpriteFromSpriteAtlas()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Sprite>("folderEntry/atlas.spriteatlas[sprite]");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite), op.Result.GetType());
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadFromFolderEntry_Texture()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Texture2D>("folderEntry/spritesheet.png");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Texture2D), op.Result.GetType());
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadFromFolderEntry_SpriteFromTexture()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Sprite>("folderEntry/spritesheet.png[topleft]");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite), op.Result.GetType());
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadAllSpritesAsArray()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Sprite[]>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite[]), op.Result.GetType());
            Assert.AreEqual(2, op.Result.Length);
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadAllSpritesAsList()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<IList<Sprite>>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.IsTrue(typeof(IList<Sprite>).IsAssignableFrom(op.Result.GetType()));
            Assert.AreEqual(2, op.Result.Count);
            op.Release();
        }
        [UnityTest]
        public IEnumerator CanUseCustomAssetBundleResource_LoadFromCustomProvider()
        {
            if (!UseJsonCatalog)
                Assert.Ignore("Custom bundle provider test requires JSON catalog format.");
            //Setup
            yield return Init();
            if (string.IsNullOrEmpty(TypeName) || TypeName == "BuildScriptFastMode")
            {
                Assert.Ignore($"Skipping test {nameof(CanUseCustomAssetBundleResource_LoadFromCustomProvider)} for {TypeName}, AssetBundle based test.");
            }

            string hash = "123456789";
            string bundleName = $"test_{hash}";
            string key = "lockey_key";

            ResourceLocationBase location = null;
            TestCatalogProviderCustomAssetBundleResource testProvider;
            SetupBundleForProviderTests(bundleName, "bundle", key, out location, out testProvider);

            var op = m_Addressables.LoadAssetAsync<TestCatalogProviderCustomAssetBundleResource.TestAssetBundleResource>(key);
            yield return op;

            Assert.IsTrue(op.Result.WasUsed);

            op.Release();
        }
        string TransFunc(IResourceLocation loc)
        {
            return "transformed";
        }

        [UnityTest]
        public IEnumerator InternalIdTranslationTest()
        {
            //Setup
            yield return Init();
            m_Addressables.InternalIdTransformFunc = TransFunc;
            var loc = new ResourceLocationBase("none", "original", "none", typeof(object));
            var transformedId = m_Addressables.ResourceManager.TransformInternalId(loc);
            Assert.AreEqual("transformed", transformedId);
            m_Addressables.InternalIdTransformFunc = null;
            var originalId = m_Addressables.ResourceManager.TransformInternalId(loc);
            Assert.AreEqual("original", originalId);
        }

        [UnityTest]
        public IEnumerator WebRequestOverrideTest()
        {
            yield return Init();
            var originalUrl = "http://127.0.0.1/original.asset";
            var replacedUrl = "http://127.0.0.1/replaced.asset";
            var uwr = new UnityWebRequest(originalUrl);

            m_Addressables.WebRequestOverride = request => request.url = replacedUrl;
            m_Addressables.ResourceManager.WebRequestOverride(uwr);
            var currentUrl = uwr.url;
            uwr.Dispose();
            m_Addressables.WebRequestOverride = null;
            Assert.AreEqual(replacedUrl, currentUrl);
        }

        [UnityTest]
        public IEnumerator CanLoadTextureAsTexture()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Texture2D>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Texture2D), op.Result.GetType());
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_ValidKeyDoesNotThrow()
        {
            //Setup
            yield return Init();

            //Test
            AsyncOperationHandle handle = default(AsyncOperationHandle);
            Assert.DoesNotThrow(() => { handle = m_Addressables.LoadAssetAsync<GameObject>(AddressablesTestUtility.GetPrefabLabel(BuildSuffix)); });
            yield return handle;

            //Cleanup
            handle.Release();
        }

        [UnityTest]
        public IEnumerator VerifyChainOpPercentCompleteCalculation()
        {
            //Setup
            yield return Init();
            AsyncOperationHandle<GameObject> op = m_Addressables.LoadAssetAsync<GameObject>(AddressablesTestUtility.GetPrefabLabel(BuildSuffix));

            //Test
            while (op.PercentComplete < 1)
            {
                Assert.False(op.IsDone);
                yield return null;
            }

            yield return null;
            Assert.True(op.PercentComplete == 1 && op.IsDone);
            yield return op;

            //Cleanup
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_ReturnsCorrectNumberOfLocationsForStringKey()
        {
            yield return Init();

            var handle = m_Addressables.LoadResourceLocationsAsync("assetWithDifferentTypedSubAssets");
            yield return handle;

            Assert.AreEqual(3, handle.Result.Count);
            HashSet<Type> typesSeen = new HashSet<Type>();
            foreach (var result in handle.Result)
            {
                Assert.IsNotNull(result.ResourceType);
                typesSeen.Add(result.ResourceType);
            }

            Assert.AreEqual(3, typesSeen.Count);
            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_ReturnsCorrectNumberOfLocationsForSubStringKey()
        {
            yield return Init();

            var handle = m_Addressables.LoadResourceLocationsAsync("assetWithDifferentTypedSubAssets[Mesh]");
            yield return handle;

            Assert.AreEqual(3, handle.Result.Count);
            HashSet<Type> typesSeen = new HashSet<Type>();
            foreach (var result in handle.Result)
            {
                Assert.IsNotNull(result.ResourceType);
                typesSeen.Add(result.ResourceType);
            }

            Assert.AreEqual(3, typesSeen.Count);
            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_ReturnsCorrectNumberOfLocationsForSubStringKey_WhenTypeIsPassedIn()
        {
            yield return Init();

            var handle = m_Addressables.LoadResourceLocationsAsync("assetWithDifferentTypedSubAssets[Mesh]", typeof(Mesh));
            yield return handle;

            Assert.AreEqual(1, handle.Result.Count);
            Assert.AreEqual(typeof(Mesh), handle.Result[0].ResourceType);

            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_ReturnsCorrectNumberOfLocationsForAssetReference()
        {
            yield return Init();

            AsyncOperationHandle assetReferenceHandle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return assetReferenceHandle;
            Assert.IsNotNull(assetReferenceHandle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (assetReferenceHandle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            var handle = m_Addressables.LoadResourceLocationsAsync(behavior.ReferenceWithMultiTypedSubObject);
            yield return handle;

            Assert.AreEqual(3, handle.Result.Count);
            HashSet<Type> typesSeen = new HashSet<Type>();
            foreach (var result in handle.Result)
            {
                Assert.IsNotNull(result.ResourceType);
                typesSeen.Add(result.ResourceType);
            }

            Assert.AreEqual(3, typesSeen.Count);

            assetReferenceHandle.Release();
            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadSubAssetFromAssetWithMultipleSubAssetTypes()
        {
            yield return Init();

            AsyncOperationHandle assetReferenceHandle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return assetReferenceHandle;
            Assert.IsNotNull(assetReferenceHandle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (assetReferenceHandle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            var handle = m_Addressables.LoadAssetAsync<Material>(behavior.ReferenceWithMultiTypedSubObjectSubReference);
            yield return handle;

            Assert.NotNull(handle.Result);

            assetReferenceHandle.Release();
            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_ReturnsCorrectNumberOfLocationsForSubAssetReference()
        {
            yield return Init();

            AsyncOperationHandle assetReferenceHandle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return assetReferenceHandle;
            Assert.IsNotNull(assetReferenceHandle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (assetReferenceHandle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            var handle = m_Addressables.LoadResourceLocationsAsync(behavior.ReferenceWithMultiTypedSubObjectSubReference);
            yield return handle;

            Assert.AreEqual(1, handle.Result.Count);
            Assert.AreEqual(typeof(Material), handle.Result[0].ResourceType);

            assetReferenceHandle.Release();
            handle.Release();
        }

        [UnityTest]
        public IEnumerator PercentComplete_NeverHasDecreasedValue_WhenLoadingAsset()
        {
            //Setup
            yield return Init();
            AsyncOperationHandle<GameObject> op = m_Addressables.LoadAssetAsync<GameObject>(AddressablesTestUtility.GetPrefabLabel(BuildSuffix));

            //Test
            float lastPercentComplete = 0f;
            while (!op.IsDone)
            {
                Assert.IsFalse(lastPercentComplete > op.PercentComplete);
                lastPercentComplete = op.PercentComplete;
                yield return null;
            }

            Assert.True(op.PercentComplete == 1 && op.IsDone);
            yield return op;

            //Cleanup
            op.Release();
        }

#if !UNITY_SWITCH && !UNITY_SWITCH2
        [UnityTest]
        public IEnumerator LoadContentCatalogAsync_SetsUpLocalAndRemoteAndCacheLocations()
        {
            yield return Init();
            string catalogPath = "fakeCatalogPath" + kCatalogExt;
            string catalogHashPath = "fakeCatalogPath.hash";

            var loc = m_Addressables.CreateCatalogLocationWithHashDependencies<ContentCatalogProvider>(catalogPath);
            Assert.AreEqual(3, loc.Dependencies.Count);
            var remoteLocation = loc.Dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Remote];
            var cacheLocation = loc.Dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Cache];
            var localLocation = loc.Dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Local];

            Assert.AreEqual(catalogHashPath, remoteLocation.ToString());
            Assert.AreEqual(cacheLocation, localLocation);
            Assert.AreEqual(AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + catalogHashPath.GetHashCode() + catalogHashPath.Substring(catalogHashPath.LastIndexOf("."))),
                cacheLocation.ToString());
        }
#endif
        [UnityTest]
        public IEnumerator IsCatalogCached_ReturnsFalse_WhenCatalogLocationDoesNotHaveDependencies()
        {
            yield return Init();

            ResourceLocationBase fakeCatlaogLoc = new ResourceLocationBase("name", "FakeCatalogID", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider));
            Hash128 hash = Hash128.Parse("1234");
            var result = m_Addressables.IsCatalogCached(fakeCatlaogLoc, hash);
            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator IsCatalogCached_ReturnsFalse_WhenCatalogLocationDoesNotHaveRemoteHashFileDependency()
        {
            yield return Init();

            ResourceLocationBase fakeCatlaogLoc = new ResourceLocationBase("name", "FakeCatalogID", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider),
                new ResourceLocationBase("dep", "fakedep", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider)));
            Hash128 hash = Hash128.Parse("1234");
            var result = m_Addressables.IsCatalogCached(fakeCatlaogLoc, hash);
            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator IsCatalogCached_ReturnsFalse_WhenCatalogLocationCacheFileDoesNotExist()
        {
            yield return Init();

            ResourceLocationBase fakeCatlaogLoc = new ResourceLocationBase("name", "FakeCatalogID", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider),
                new ResourceLocationBase("dep", "notarealfilepath", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider)),
                new ResourceLocationBase("dep", "fakedep", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider)));
            Hash128 hash = Hash128.Parse("1234");
            var result = m_Addressables.IsCatalogCached(fakeCatlaogLoc, hash);
            Assert.IsFalse(result);
        }

#if UNITY_EDITOR //these tests involve writing files, which can have problems on some of the consoles
        [UnityTest]
        public IEnumerator IsCatalogCached_ReturnsFalse_WhenCatalogLocationCacheFileHashDoesNotMatchProvdedRemoteHash()
        {
            yield return Init();
            Hash128 cacheHash = Hash128.Parse("1234");
            Hash128 remoteHash = Hash128.Parse("5678");
            string cacheHashFilePath = Path.Combine(kCatalogFolderPath, "CachedFilePath.hash");

            WriteHashFileForCatalog(cacheHashFilePath, cacheHash.ToString());

            ResourceLocationBase fakeCatlaogLoc = new ResourceLocationBase("name", "FakeCatalogID", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider),
                new ResourceLocationBase("dep", "fakedep", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider)),
                new ResourceLocationBase("dep", cacheHashFilePath, typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider)));
            var result = m_Addressables.IsCatalogCached(fakeCatlaogLoc, remoteHash);
            Assert.IsFalse(result);
            File.Delete(cacheHashFilePath);
        }

        [UnityTest]
        public IEnumerator IsCatalogCached_ReturnsTrue_WhenCatalogLocationCacheFileHashMatchesProvdedRemoteHash()
        {
            yield return Init();
            Hash128 cacheHash = Hash128.Parse("1234");
            Hash128 remoteHash = Hash128.Parse("1234");
            string cacheHashFilePath = Path.Combine(kCatalogFolderPath, "CachedFilePath.hash");

            WriteHashFileForCatalog(cacheHashFilePath, cacheHash.ToString());

            ResourceLocationBase fakeCatlaogLoc = new ResourceLocationBase("name", "FakeCatalogID", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider),
                new ResourceLocationBase("dep", "fakedep", typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider)),
                new ResourceLocationBase("dep", cacheHashFilePath, typeof(ContentCatalogProvider).FullName, typeof(ContentCatalogProvider)));
            var result = m_Addressables.IsCatalogCached(fakeCatlaogLoc, remoteHash);
            Assert.IsTrue(result);
            File.Delete(cacheHashFilePath);
        }
#endif
#if !UNITY_SWITCH && !UNITY_SWITCH2
        [UnityTest]
        public IEnumerator LoadContentCatalogAsync_LocationsHaveTimeout()
        {
            yield return Init();
            string catalogPath = "fakeCatalogPath" + kCatalogExt;

            m_Addressables.CatalogRequestsTimeout = 13;
            var loc = m_Addressables.CreateCatalogLocationWithHashDependencies<ContentCatalogProvider>(catalogPath);
            Assert.AreEqual(3, loc.Dependencies.Count);
            var remoteLocation = loc.Dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Remote];
            var cacheLocation = loc.Dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Cache];

            var data = loc.Data as ProviderLoadRequestOptions;
            Assert.IsNotNull(data);
            Assert.AreEqual(data.WebRequestTimeout, m_Addressables.CatalogRequestsTimeout);
            data = remoteLocation.Data as ProviderLoadRequestOptions;
            Assert.IsNotNull(data);
            Assert.AreEqual(data.WebRequestTimeout, m_Addressables.CatalogRequestsTimeout);
            data = cacheLocation.Data as ProviderLoadRequestOptions;
            Assert.IsNotNull(data);
            Assert.AreEqual(data.WebRequestTimeout, m_Addressables.CatalogRequestsTimeout);
        }
#endif
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator LoadingContentCatalogTwice_DoesNotThrowException_WhenHandleIsntReleased()
        {
            yield return Init();

            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            Directory.CreateDirectory(kCatalogFolderPath);
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                ContentCatalogData data = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
                data.SetData(new List<ContentCatalogDataEntry>
                {
                    new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
                });
                data.SaveToFile(fullRemotePath);
            }
            else
            {
                string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
                if (baseCatalogPath.StartsWith("file://"))
                    baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
                File.Copy(baseCatalogPath, fullRemotePath);
            }

            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;

            var op2 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op2;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, op1.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op2.Status);

            op1.Release();
            op2.Release();
            if (Directory.Exists(kCatalogFolderPath))
                Directory.Delete(kCatalogFolderPath, true);
        }

#endif

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator LoadingContentCatalogWithCacheTwice_DoesNotThrowException_WhenHandleIsntReleased()
        {
            yield return Init();

            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            Directory.CreateDirectory(kCatalogFolderPath);
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                ContentCatalogData data = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
                data.SetData(new List<ContentCatalogDataEntry>
                {
                    new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
                });
                data.SaveToFile(fullRemotePath);
            }
            else
            {
                string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
                if (baseCatalogPath.StartsWith("file://"))
                    baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
                File.Copy(baseCatalogPath, fullRemotePath);
            }

            WriteHashFileForCatalog(fullRemotePath, "123");

            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;

            var op2 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op2;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, op1.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op2.Status);

            op1.Release();
            op2.Release();
            if (Directory.Exists(kCatalogFolderPath))
                Directory.Delete(kCatalogFolderPath, true);
        }

#endif

        [UnityTest]
        public IEnumerator LoadingContentCatalog_WithInvalidCatalogPath_Fails()
        {
            yield return Init();

            bool ignoreValue = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            var op1 = m_Addressables.LoadContentCatalogAsync("notarealpath" + kCatalogExt, false);
            yield return op1;

            Assert.AreEqual(AsyncOperationStatus.Failed, op1.Status);

            op1.Release();
            LogAssert.ignoreFailingMessages = ignoreValue;

            yield return null; //< Process deferred callback
        }

        private string kCatalogRemotePath => "remotecatalog" + kCatalogExt;
        private const string kCatalogFolderPath = "Assets/CatalogTestFolder";

        bool CreateCatalogAtFakeRemotePath(string fakeRemotePath, string catalogFolderPath = kCatalogFolderPath)
        {
            Directory.CreateDirectory(catalogFolderPath);
#if UNITY_EDITOR
            // Always create the catalog in the format matching UseJsonCatalog. The built catalog
            // at CatalogLocation is always binary, so copying it would give JSON tests the wrong format.
            ContentCatalogData data = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
            data.SetData(new List<ContentCatalogDataEntry>
            {
                new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
            });
            data.SaveToFile(fakeRemotePath);
#else
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
                return false;
            string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
            if (baseCatalogPath.StartsWith("file://"))
                baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
            File.Copy(baseCatalogPath, fakeRemotePath, overwrite: true);
#endif
            return true;
        }

        private string WriteHashFileForCatalog(string catalogPath, string hash)
        {
            string hashPath = CatalogUtilities.GetHashFilePath(catalogPath);
            Directory.CreateDirectory(Path.GetDirectoryName(hashPath));
            File.WriteAllText(hashPath, hash);
            return hashPath;
        }

        void StubProviders()
        {
            var textProvider = m_Addressables.ResourceManager.ResourceProviders.FirstOrDefault(rp => rp.GetType() == typeof(TextDataProvider)) as TextDataProvider;
            m_Addressables.ResourceManager.ResourceProviders.Remove(textProvider);
            m_Addressables.ResourceManager.ResourceProviders.Add(new TextDataProviderStub(kCatalogFolderPath, textProvider));

            if (UseJsonCatalog)
            {
                var jsonProvider = m_Addressables.ResourceManager.ResourceProviders.FirstOrDefault(rp => rp.GetType() == typeof(JsonAssetProvider)) as JsonAssetProvider;
                m_Addressables.ResourceManager.ResourceProviders.Remove(jsonProvider);
                m_Addressables.ResourceManager.ResourceProviders.Add(new JsonAssetProviderStub(kCatalogFolderPath, jsonProvider));
            }
            else
            {
                var binaryProvider = m_Addressables.ResourceManager.ResourceProviders.FirstOrDefault(rp => rp is BinaryAssetProvider<BinaryContentCatalogData.Serializer>) as BinaryAssetProvider<BinaryContentCatalogData.Serializer>;
                m_Addressables.ResourceManager.ResourceProviders.Remove(binaryProvider);
                m_Addressables.ResourceManager.ResourceProviders.Add(new BinaryAssetProviderStub(kCatalogFolderPath, binaryProvider));
            }
            m_Addressables.ResourceManager.m_providerMap.Clear();
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator LoadingContentCatalog_CachesCatalogData_IfValidHashFound()
        {
            yield return Init();

            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            Directory.CreateDirectory(kCatalogFolderPath);
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                ContentCatalogData data = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
                data.SetData(new List<ContentCatalogDataEntry>
                {
                    new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
                });
                data.SaveToFile(fullRemotePath);
            }
            else
            {
                string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
                if (baseCatalogPath.StartsWith("file://"))
                    baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
                File.Copy(baseCatalogPath, fullRemotePath);
            }

            WriteHashFileForCatalog(fullRemotePath, "123");

            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;

            string fullRemoteHashPath = CatalogUtilities.GetHashFilePath(fullRemotePath);
            string cachedDataPath = AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + fullRemoteHashPath.GetHashCode() + fullRemotePath.Substring(fullRemotePath.LastIndexOf(".")));
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);
            Assert.IsTrue(File.Exists(cachedDataPath));
            Assert.IsTrue(File.Exists(cachedHashPath));
            Assert.AreEqual("123", File.ReadAllText(cachedHashPath));

            op1.Release();
            Directory.Delete(kCatalogFolderPath, true);
            File.Delete(cachedDataPath);
            File.Delete(cachedHashPath);
        }

#endif

#if UNITY_EDITOR

        // Runs in both formats — uses the fixture's active kCatalogRemotePath.
        [UnityTest]
        public IEnumerator LoadingContentCatalog_CachesCatalogData_IfValidHashFoundAndRemotePathContainsQueryParameters()
        {
            yield return Init();

            string fakeFullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            if (!CreateCatalogAtFakeRemotePath(fakeFullRemotePath))
                Assert.Ignore($"Skipping test {TestContext.CurrentContext.Test.Name} due to missing CatalogLocation.");
            WriteHashFileForCatalog(fakeFullRemotePath, "123");

            StubProviders();

            string catalogRemotePath = "http://127.0.0.1/" + kCatalogRemotePath;
            string catalogRemotePathWithQueryParams = catalogRemotePath + "?param1=value1&param2=value2:date=number";
            var op1 = m_Addressables.LoadContentCatalogAsync(catalogRemotePathWithQueryParams, false);
            while(op1.IsValid() && !op1.IsDone)
                yield return op1;
            Assert.Null(op1.OperationException);

            var expectedHash = CatalogUtilities.GetHashFilePath(catalogRemotePath).GetHashCode();
            string expectedCatalogName = expectedHash + kCatalogExt;
            string cachedDataPath = Addressables.ResolveInternalId(AddressablesImpl.kCacheDataFolder + expectedCatalogName);
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);
            Assert.IsTrue(File.Exists(cachedDataPath));
            Assert.IsTrue(File.Exists(cachedHashPath));
            Assert.AreEqual("123", File.ReadAllText(cachedHashPath));

            op1.Release();
            Directory.Delete(kCatalogFolderPath, true);
            File.Delete(cachedDataPath);
            File.Delete(cachedHashPath);
        }

        // Runs in both formats — loads a catalog whose extension matches the fixture's active format.
        [UnityTest]
        public IEnumerator LoadingContentCatalog_LoadsActiveFormatCatalog_Succeeds()
        {
            yield return Init();

            string fakeCatalogFullPath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);

            if (!CreateCatalogAtFakeRemotePath(fakeCatalogFullPath))
                Assert.Ignore($"Skipping test {TestContext.CurrentContext.Test.Name} due to missing CatalogLocation.");
            WriteHashFileForCatalog(fakeCatalogFullPath, "123");

            var catalogOp = m_Addressables.LoadContentCatalogAsync(fakeCatalogFullPath, false);
            yield return catalogOp;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, catalogOp.Status);

            catalogOp.Release();
        }

        // Runs in both formats — loads a catalog with an unexpected extension (.unknown).
        // This triggers the ContentCatalogProvider extension-mismatch error.
        [UnityTest]
        public IEnumerator LoadingContentCatalog_WithUnexpectedExtension_FailsWithExtensionError()
        {
            yield return Init();

            // .unknown is neither .json nor .bin, so BinaryCatalogProvider is selected and
            // its extension check fires: "Expecting to load catalog with extension '.bin' but
            // catalog path has extension '.unknown'."
            string fakeCatalogFullPath = Path.Combine(kCatalogFolderPath, "remotecatalog.unknown");

            if (!CreateCatalogAtFakeRemotePath(fakeCatalogFullPath))
                Assert.Ignore($"Skipping test {TestContext.CurrentContext.Test.Name} due to missing CatalogLocation.");
            WriteHashFileForCatalog(fakeCatalogFullPath, "123");

            var catalogOp = m_Addressables.LoadContentCatalogAsync(fakeCatalogFullPath, false);
            yield return catalogOp;

            Assert.AreEqual(AsyncOperationStatus.Failed, catalogOp.Status);

            // Walk to the innermost exception where ContentCatalogProvider reports the mismatch.
            Exception inner = catalogOp.OperationException;
            while (inner?.InnerException != null) inner = inner.InnerException;
            StringAssert.Contains(
                "No catalog provider registered for extension '.unknown'.",
                inner?.Message ?? string.Empty);

            catalogOp.Release();
            yield return null; // process deferred callback
        }

        // Runs in both formats — loads opposite-format content (e.g. binary data in a .json file).
        // Provider selection is by extension, so the correct provider is chosen, but parse fails.
        // The parse error message is implementation-specific — assert only that the op fails.
        [UnityTest]
        public IEnumerator LoadingContentCatalog_WithOppositeFormatContent_Fails()
        {
            yield return Init();

            string oppositeExt = UseJsonCatalog ? ".bin" : ".json";
            string fakeCatalogFullPath = Path.Combine(kCatalogFolderPath, "remotecatalog" + oppositeExt);

            if (!CreateCatalogAtFakeRemotePath(fakeCatalogFullPath))
                Assert.Ignore($"Skipping test {TestContext.CurrentContext.Test.Name} due to missing CatalogLocation.");
            WriteHashFileForCatalog(fakeCatalogFullPath, "123");

            var previousFailingMessages = LogAssert.ignoreFailingMessages;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                var catalogOp = m_Addressables.LoadContentCatalogAsync(fakeCatalogFullPath, false);
                yield return catalogOp;

                Assert.AreEqual(AsyncOperationStatus.Failed, catalogOp.Status);

                catalogOp.Release();
            } finally
            {
                LogAssert.ignoreFailingMessages = previousFailingMessages;
            }

            yield return null; // process deferred callback
        }

        [UnityTest]
        public IEnumerator LoadingContentCatalog_CachesCatalogData_ForTwoCatalogsWithSameName()
        {
            yield return Init();

            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            string fullRemotePathTwo = Path.Combine(kCatalogFolderPath, "secondCatalog", kCatalogRemotePath);
            Directory.CreateDirectory(kCatalogFolderPath);
            Directory.CreateDirectory(Path.Combine(kCatalogFolderPath, "secondCatalog"));
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                ContentCatalogData data = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
                data.SetData(new List<ContentCatalogDataEntry>
                {
                    new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
                });
                data.SaveToFile(fullRemotePath);
                data.SaveToFile(fullRemotePathTwo);
            }
            else
            {
                string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
                if (baseCatalogPath.StartsWith("file://"))
                    baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
                File.Copy(baseCatalogPath, fullRemotePath);
            }
            ContentCatalogData catalogData = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
            catalogData.SetData(new List<ContentCatalogDataEntry>
                {
                    new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
                });

            catalogData.SaveToFile(fullRemotePathTwo);

            WriteHashFileForCatalog(fullRemotePath, "123");
            WriteHashFileForCatalog(fullRemotePathTwo, "123");

            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;

            var op2 = m_Addressables.LoadContentCatalogAsync(fullRemotePathTwo, false);
            yield return op2;

            string fullRemoteHashPath = CatalogUtilities.GetHashFilePath(fullRemotePath);
            string fullRemoteHashPathTwo = CatalogUtilities.GetHashFilePath(fullRemotePathTwo);
            string cachedDataPath = AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + fullRemoteHashPath.GetHashCode() + fullRemotePath.Substring(fullRemotePath.LastIndexOf(".")));
            string cachedDataPathTwo =
                AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + fullRemoteHashPathTwo.GetHashCode() + fullRemotePathTwo.Substring(fullRemotePathTwo.LastIndexOf(".")));
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);
            string cachedHashPathTwo = CatalogUtilities.GetHashFilePath(cachedDataPathTwo);
            Assert.IsTrue(File.Exists(cachedDataPath));
            Assert.IsTrue(File.Exists(cachedDataPathTwo));
            Assert.IsTrue(File.Exists(cachedHashPath));
            Assert.IsTrue(File.Exists(cachedHashPathTwo));
            Assert.AreEqual("123", File.ReadAllText(cachedHashPath));
            Assert.AreEqual("123", File.ReadAllText(cachedHashPathTwo));

            op1.Release();
            op2.Release();
            Directory.Delete(kCatalogFolderPath, true);
            File.Delete(cachedDataPath);
            File.Delete(cachedHashPath);
            File.Delete(cachedDataPathTwo);
            File.Delete(cachedHashPathTwo);
        }

#endif

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator LoadingContentCatalog_IfNoCachedHashFound_Succeeds()
        {
            yield return Init();

            ResourceManager.ExceptionHandler = m_PrevHandler;
            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            Directory.CreateDirectory(kCatalogFolderPath);
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                ContentCatalogData data = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
                data.SetData(new List<ContentCatalogDataEntry>
                {
                    new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
                });

                data.SaveToFile(fullRemotePath);
            }
            else
            {
                string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
                if (baseCatalogPath.StartsWith("file://"))
                    baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
                File.Copy(baseCatalogPath, fullRemotePath);
            }

            string hashPath = WriteHashFileForCatalog(fullRemotePath, "123");

            string cachedDataPath = AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + hashPath.GetHashCode() + kCatalogExt);
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);
            if (File.Exists(cachedDataPath))
                File.Delete(cachedDataPath);
            if (File.Exists(cachedHashPath))
                File.Delete(cachedHashPath);
            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;

            Assert.IsTrue(op1.IsValid());
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op1.Status);
            Assert.NotNull(op1.Result);

            // Cleanup
            op1.Release();
            Directory.Delete(kCatalogFolderPath, true);
            File.Delete(cachedDataPath);
            File.Delete(cachedHashPath);
        }

        [UnityTest]
        public IEnumerator LoadingContentCatalog_IfNoHashFileForCatalog_DoesntThrowException()
        {
            yield return Init();
            ResourceManager.ExceptionHandler = m_PrevHandler;
            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            Directory.CreateDirectory(kCatalogFolderPath);
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                ContentCatalogData data = AddressablesTestUtility.CreateCatalogData(UseJsonCatalog, "test_catalog");
                data.SetData(new List<ContentCatalogDataEntry>
                {
                    new ContentCatalogDataEntry(typeof(string), "testString", "test.provider", new[] {"key"})
                });
                data.SaveToFile(fullRemotePath);
            }
            else
            {
                string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
                if (baseCatalogPath.StartsWith("file://"))
                    baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
                File.Copy(baseCatalogPath, fullRemotePath);
            }


            string cachedDataPath = AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + Path.GetFileName(kCatalogRemotePath));
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);
            if (File.Exists(cachedDataPath))
                File.Delete(cachedDataPath);
            if (File.Exists(cachedHashPath))
                File.Delete(cachedHashPath);
            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;

            Assert.IsTrue(op1.IsValid());
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op1.Status);
            Assert.NotNull(op1.Result);
            Assert.IsFalse(File.Exists(cachedHashPath));

            // Cleanup
            op1.Release();
            Directory.Delete(kCatalogFolderPath, true);
            // the cached files are automically cleaned up as part of cleanup
        }

#endif
        [UnityTest]
        public IEnumerator IResourceLocationComparing_SameKeySameTypeDifferentInternalId_ReturnsFalse()
        {
            yield return Init();

            IResourceLocation loc1 = new ResourceLocationBase("address", "internalid1", typeof(BundledAssetProvider).FullName, typeof(GameObject));
            IResourceLocation loc2 = new ResourceLocationBase("address", "internalid2", typeof(BundledAssetProvider).FullName, typeof(GameObject));

            Assert.IsFalse(m_Addressables.Equals(loc1, loc2));
        }

        [UnityTest]
        public IEnumerator IResourceLocationComparing_SameKeyTypeAndInternalId_ReturnsTrue()
        {
            yield return Init();

            IResourceLocation loc1 = new ResourceLocationBase("address", "internalid1", typeof(BundledAssetProvider).FullName, typeof(GameObject));
            IResourceLocation loc2 = new ResourceLocationBase("address", "internalid1", typeof(BundledAssetProvider).FullName, typeof(GameObject));

            Assert.IsTrue(m_Addressables.Equals(loc1, loc2));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator LoadingContentCatalog_UpdatesCachedData_IfHashFileUpdates()
        {
            yield return Init();
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                UnityEngine.Debug.Log($"Skipping test {nameof(LoadingContentCatalog_UpdatesCachedData_IfHashFileUpdates)} due to missing CatalogLocation.");
                yield break;
            }

            Directory.CreateDirectory(kCatalogFolderPath);
            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            string fullRemoteHashPath = CatalogUtilities.GetHashFilePath(fullRemotePath);
            string cachedDataPath = AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + fullRemoteHashPath.GetHashCode() + fullRemotePath.Substring(fullRemotePath.LastIndexOf(".")));
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);
            string remoteHashPath = WriteHashFileForCatalog(fullRemotePath, "123");

            string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
            if (baseCatalogPath.StartsWith("file://"))
                baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
            File.Copy(baseCatalogPath, fullRemotePath);

            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;
            op1.Release();

            Assert.IsTrue(File.Exists(cachedDataPath));
            Assert.IsTrue(File.Exists(cachedHashPath));
            Assert.AreEqual("123", File.ReadAllText(cachedHashPath));

            remoteHashPath = WriteHashFileForCatalog(fullRemotePath, "456");

            var op2 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op2;

            Assert.AreEqual("456", File.ReadAllText(cachedHashPath));

            op2.Release();
            Directory.Delete(kCatalogFolderPath, true);
            File.Delete(cachedDataPath);
            File.Delete(cachedHashPath);
        }

        [UnityTest]
        public IEnumerator UpdateContentCatalog_UpdatesCachedData_IfCacheCorrupted()
        {
            if (!UseJsonCatalog)
                Assert.Ignore("UpdateContentCatalog_IfCacheCorrupted relies on JSON parse error behavior.");
            yield return Init();
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                UnityEngine.Debug.Log($"Skipping test {nameof(LoadingContentCatalog_UpdatesCachedData_IfHashFileUpdates)} due to missing CatalogLocation.");
                yield break;
            }

            Directory.CreateDirectory(kCatalogFolderPath);
            var cacheFolder = Addressables.ResolveInternalId(AddressablesImpl.kCacheDataFolder);
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            string fullRemoteHashPath = CatalogUtilities.GetHashFilePath(fullRemotePath);
            string cachedDataPath = Addressables.ResolveInternalId(AddressablesImpl.kCacheDataFolder + fullRemoteHashPath.GetHashCode() + fullRemotePath.Substring(fullRemotePath.LastIndexOf(".")));
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);
            string remoteHashPath = WriteHashFileForCatalog(fullRemoteHashPath, "123");

            string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
            if (baseCatalogPath.StartsWith("file://"))
                baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
            File.Copy(baseCatalogPath, fullRemotePath);

            File.WriteAllText(cachedHashPath, File.ReadAllText(remoteHashPath));
            File.WriteAllText(cachedDataPath, "corrupted content");

            //load from fullRemotePath will first load cachedDataPath, then load fullRemotePath on error
            var op = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            LogAssert.Expect(LogType.Exception, new Regex(".*JSON parse error.*"));
            yield return op;

            Assert.IsTrue(File.Exists(cachedDataPath));
            Assert.IsTrue(File.Exists(cachedHashPath));
            Assert.AreEqual(File.ReadAllText(cachedDataPath), File.ReadAllText(fullRemotePath));

            op.Release();
            Directory.Delete(kCatalogFolderPath, true);
            File.Delete(cachedDataPath);
            File.Delete(cachedHashPath);
        }

#endif
        [UnityTest]
        public IEnumerator LoadingContentCatalog_NoCacheDataCreated_IfRemoteHashDoesntExist()
        {
            yield return Init();
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                UnityEngine.Debug.Log($"Skipping test {nameof(LoadingContentCatalog_NoCacheDataCreated_IfRemoteHashDoesntExist)} due to missing CatalogLocation.");
                yield break;
            }

            Directory.CreateDirectory(kCatalogFolderPath);
            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);
            string cachedDataPath = AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder + Path.GetFileName(kCatalogRemotePath));
            string cachedHashPath = CatalogUtilities.GetHashFilePath(cachedDataPath);

            string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
            if (baseCatalogPath.StartsWith("file://"))
                baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
            File.Copy(baseCatalogPath, fullRemotePath);

            var op1 = m_Addressables.LoadContentCatalogAsync(fullRemotePath, false);
            yield return op1;
            op1.Release();

            Assert.IsFalse(File.Exists(cachedDataPath));
            Assert.IsFalse(File.Exists(cachedHashPath));

            Directory.Delete(kCatalogFolderPath, true);
        }

        [UnityTest]
        public IEnumerator ContentCatalogData_IsCleared_WhenInitializationOperationLoadContentCatalogOp_IsReleased()
        {
            yield return Init();
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                UnityEngine.Debug.Log($"Skipping test {nameof(ContentCatalogData_IsCleared_WhenInitializationOperationLoadContentCatalogOp_IsReleased)} due to missing CatalogLocation.");
                yield break;
            }

            string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;

            if (baseCatalogPath.StartsWith("file://"))
                baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;

            var ext = Path.GetExtension(baseCatalogPath);
            var catalogProvider = m_Addressables.ResourceManager.ResourceProviders
                .OfType<ContentCatalogProvider>()
                .First(p => string.Equals(p.CatalogExtension, ext, StringComparison.OrdinalIgnoreCase));
            var location = m_Addressables.CreateCatalogLocationWithHashDependencies(baseCatalogPath, catalogProvider.GetType());
            var loadCatalogHandle = InitializationOperation.LoadContentCatalog(m_Addressables, location, string.Empty);

            yield return loadCatalogHandle;

            var ccd = catalogProvider.m_LocationToCatalogLoadOpMap[location].m_ContentCatalogData;
            Assert.IsFalse(CatalogDataWasCleaned(ccd));

            loadCatalogHandle.Release();

            Assert.IsTrue(CatalogDataWasCleaned(ccd));

            PostTearDownEvent = ResetAddressables;
        }

        internal bool CatalogDataWasCleaned(ContentCatalogData data)
        {
            if (data is JsonContentCatalogData jccd)
                return string.IsNullOrEmpty(jccd.m_KeyDataString) &&
                    string.IsNullOrEmpty(jccd.m_BucketDataString) &&
                    string.IsNullOrEmpty(jccd.m_EntryDataString) &&
                    string.IsNullOrEmpty(jccd.m_ExtraDataString) &&
                    jccd.m_InternalIds == null &&
                    string.IsNullOrEmpty(jccd.m_LocatorId) &&
                    jccd.m_ProviderIds == null &&
                    jccd.m_ResourceProviderData == null &&
                    jccd.m_resourceTypes == null;
            return string.IsNullOrEmpty(data.m_LocatorId);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator ContentCatalogData_IsCleared_ForCorrectCatalogLoadOp_WhenOpIsReleased()
        {
            yield return Init();
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                UnityEngine.Debug.Log($"Skipping test {nameof(ContentCatalogData_IsCleared_ForCorrectCatalogLoadOp_WhenOpIsReleased)} due to missing CatalogLocation.");
                yield break;
            }

            Directory.CreateDirectory(kCatalogFolderPath);
            string fullRemotePath = Path.Combine(kCatalogFolderPath, kCatalogRemotePath);

            string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
            if (baseCatalogPath.StartsWith("file://"))
                baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;
            File.Copy(baseCatalogPath, fullRemotePath);

            var ext = Path.GetExtension(baseCatalogPath);
            var catalogProvider = m_Addressables.ResourceManager.ResourceProviders
                .OfType<ContentCatalogProvider>()
                .First(p => string.Equals(p.CatalogExtension, ext, StringComparison.OrdinalIgnoreCase));
            var location = m_Addressables.CreateCatalogLocationWithHashDependencies(baseCatalogPath, catalogProvider.GetType());
            var location2 = m_Addressables.CreateCatalogLocationWithHashDependencies(fullRemotePath, catalogProvider.GetType());
            var loadCatalogHandle = InitializationOperation.LoadContentCatalog(m_Addressables, location, string.Empty);
            yield return loadCatalogHandle;
            var loadCatalogHandle2 = InitializationOperation.LoadContentCatalog(m_Addressables, location2, string.Empty);
            yield return loadCatalogHandle2;

            ContentCatalogProvider ccp = catalogProvider;

            var ccd = ccp.m_LocationToCatalogLoadOpMap[location].m_ContentCatalogData;
            var ccd2 = ccp.m_LocationToCatalogLoadOpMap[location2].m_ContentCatalogData;

            Assert.IsFalse(CatalogDataWasCleaned(ccd));
            Assert.IsFalse(CatalogDataWasCleaned(ccd2));

            loadCatalogHandle.Release();

            Assert.IsTrue(CatalogDataWasCleaned(ccd));
            Assert.IsFalse(CatalogDataWasCleaned(ccd2));

            Directory.Delete(kCatalogFolderPath, true);
            loadCatalogHandle2.Release();

            PostTearDownEvent = ResetAddressables;
        }

#endif

        [UnityTest]
        public IEnumerator ContentCatalogProvider_RemovesEntryFromMap_WhenOperationHandleReleased()
        {
            yield return Init();
            if (m_Addressables.m_ResourceLocators[0].CatalogLocation == null)
            {
                UnityEngine.Debug.Log($"Skipping test {nameof(ContentCatalogProvider_RemovesEntryFromMap_WhenOperationHandleReleased)} due to missing CatalogLocation.");
                yield break;
            }

            string baseCatalogPath = m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId;
            if (baseCatalogPath.StartsWith("file://"))
                baseCatalogPath = new Uri(m_Addressables.m_ResourceLocators[0].CatalogLocation.InternalId).AbsolutePath;

            var handle = m_Addressables.LoadContentCatalogAsync(baseCatalogPath, false);
            yield return handle;

            var ext = Path.GetExtension(baseCatalogPath);
            ContentCatalogProvider ccp = m_Addressables.ResourceManager.ResourceProviders
                .OfType<ContentCatalogProvider>()
                .First(p => string.Equals(p.CatalogExtension, ext, StringComparison.OrdinalIgnoreCase));

            Assert.AreEqual(1, ccp.m_LocationToCatalogLoadOpMap.Count);

            handle.Release();

            Assert.AreEqual(0, ccp.m_LocationToCatalogLoadOpMap.Count);

            PostTearDownEvent = ResetAddressables;
        }

        [UnityTest]
        public IEnumerator VerifyProfileVariableEvaluation()
        {
            yield return Init();
            Assert.AreEqual(string.Format("{0}", AddressablesImpl.RuntimePath), AddressablesRuntimeProperties.EvaluateString("{UnityEngine.AddressableAssets.Addressables.RuntimePath}"));
        }

        [UnityTest]
        public IEnumerator VerifyDownloadSize()
        {
            yield return Init();
            long expectedSize = 0;
            var locMap = new ResourceLocationMap("TestLocator");

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nonExistingUrlForAddressableTests1337.com/mybundle1.bundle", typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 = (bundleLoc1.Data = CreateLocationSizeData("sizeTestBundle1", 1000, 123, "hashstring1")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1, null);

            var bundleLoc2 = new ResourceLocationBase("sizeTestBundle2", "http://nonExistingUrlForAddressableTests1337.com/mybundle2.bundle", typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 = (bundleLoc2.Data = CreateLocationSizeData("sizeTestBundle2", 500, 123, "hashstring2")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2, null);

            var assetLoc = new ResourceLocationBase("sizeTestAsset", "myASset.asset", typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1, bundleLoc2);

            locMap.Add("sizeTestBundle1", bundleLoc1);
            locMap.Add("sizeTestBundle2", bundleLoc2);
            locMap.Add("sizeTestAsset", assetLoc);
            m_Addressables.AddResourceLocator(locMap);

            var dOp = m_Addressables.GetDownloadSizeAsync((object)"sizeTestAsset");
            yield return dOp;
            Assert.AreEqual(expectedSize, dOp.Result);
            dOp.Release();
        }

        public IEnumerator GetDownloadSize_CalculatesCachedBundlesInternal()
        {
#if ENABLE_CACHING
            yield return Init();
            long expectedSize = 0;
            long bundleSize1 = 1000;
            long bundleSize2 = 500;
            var locMap = new ResourceLocationMap("TestLocator");

            Caching.ClearCache();
            //Simulating a cached bundle
            string fakeCachePath = CreateFakeCachedBundle("cachedSizeTestBundle1", "be38e35d2177c282d5d6a2e54a803aab");

            var bundleLoc1 = new ResourceLocationBase("cachedSizeTestBundle1", "http://nonExistingUrlForAddressableTests1337.com/GetDownloadSize_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "be38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1, null);

            var bundleLoc2 = new ResourceLocationBase("sizeTestBundle2", "http://nonExistingUrlForAddressableTests1337.com/GetDownloadSize_CalculatesCachedBundlesBundle2.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 =
                (bundleLoc2.Data = CreateLocationSizeData("sizeTestBundle2", bundleSize2, 123,
                    "d9fe965a6b253fb9dbd3e1cb08b7d66f")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2, null);

            var assetLoc = new ResourceLocationBase("cachedSizeTestAsset", "myASset.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1, bundleLoc2);

            locMap.Add("cachedSizeTestBundle1", bundleLoc1);
            locMap.Add("cachedSizeTestBundle2", bundleLoc2);
            locMap.Add("cachedSizeTestAsset", assetLoc);
            m_Addressables.AddResourceLocator(locMap);

            var dOp = m_Addressables.GetDownloadSizeAsync((object)"cachedSizeTestAsset");
            yield return dOp;
            Assert.IsTrue((bundleSize1 + bundleSize2) > dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);

            dOp.Release();
            m_Addressables.RemoveResourceLocator(locMap);
            Directory.Delete(fakeCachePath, true);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        public IEnumerator GetDownloadSize_WithList_CalculatesCachedBundlesInternal()
        {
#if ENABLE_CACHING
            yield return Init();
            long expectedSize = 0;
            long bundleSize1 = 1000;
            long bundleSize2 = 500;
            var locMap = new ResourceLocationMap("TestLocator");

            Assert.IsTrue(Caching.ClearCache(), "Was unable to clear the cache.  Test results are affected");
            //Simulating a cached bundle
            string fakeCachePath = CreateFakeCachedBundle("cachedSizeTestBundle1", "0e38e35d2177c282d5d6a2e54a803aab");

            var bundleLoc1 = new ResourceLocationBase("cachedSizeTestBundle1", "http://nonExistingUrlForAddressableTests1337.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "0e38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1, null);

            var bundleLoc2 = new ResourceLocationBase("sizeTestBundle2", "http://nonExistingUrlForAddressableTests1337.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle2.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 =
                (bundleLoc2.Data = CreateLocationSizeData("sizeTestBundle2", bundleSize2, 123,
                    "09fe965a6b253fb9dbd3e1cb08b7d66f")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2, null);

            var assetLoc = new ResourceLocationBase("cachedSizeTestAsset", "myASset.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1, bundleLoc2);

            locMap.Add("cachedSizeTestBundle1", bundleLoc1);
            locMap.Add("cachedSizeTestBundle2", bundleLoc2);
            locMap.Add("cachedSizeTestAsset", assetLoc);
            m_Addressables.AddResourceLocator(locMap);

            var dOp = m_Addressables.GetDownloadSizeAsync(new List<object>()
                {
                    "cachedSizeTestAsset",
                    bundleLoc1,
                    bundleLoc2
                }
            );
            yield return dOp;
            Assert.IsTrue((bundleSize1 + bundleSize2) > dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);

            dOp.Release();
            m_Addressables.RemoveResourceLocator(locMap);
            Directory.Delete(fakeCachePath, true);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GetDownloadSizeAsync_OnlyCalculatesDistinctLocations()
        {
            yield return Init();

            long expectedSize = 0;
            var locMap = new ResourceLocationMap("TestLocator");

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nonExistingUrlForAddressableTests1337.com/mybundle1.bundle", typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 = (bundleLoc1.Data = CreateLocationSizeData("sizeTestBundle1", 1000, 123, "hashstring1")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1, null);

            var bundleLocCopy = new ResourceLocationBase("sizeTestBundle1", "http://nonExistingUrlForAddressableTests1337.com/mybundle1.bundle", typeof(AssetBundleProvider).FullName, typeof(object));
            bundleLocCopy.Data = CreateLocationSizeData("sizeTestBundle1", 1000, 123, "hashstring1");

            var bundleLoc2 = new ResourceLocationBase("sizeTestBundle2", "http://nonExistingUrlForAddressableTests1337.com/mybundle2.bundle", typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 = (bundleLoc2.Data = CreateLocationSizeData("sizeTestBundle2", 500, 123, "hashstring2")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2, null);

            var assetLoc = new ResourceLocationBase("sizeTestAsset", "myASset.asset", typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1, bundleLocCopy, bundleLoc2);

            locMap.Add("sizeTestBundle1", bundleLoc1);
            locMap.Add("sizeTestBundle1Copy", bundleLocCopy);
            locMap.Add("sizeTestBundle2", bundleLoc2);
            locMap.Add("sizeTestAsset", assetLoc);
            m_Addressables.AddResourceLocator(locMap);

            var dOp = m_Addressables.GetDownloadSizeAsync((object)"sizeTestAsset");
            yield return dOp;
            Assert.AreEqual(expectedSize, dOp.Result);
            dOp.Release();

        }

        public IEnumerator GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceSameBundleInternal()
        {
#if ENABLE_CACHING
            yield return Init();
            long bundleSize1 = 1000;
            long expectedSize = 0;

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nonExistingUrlForAddressableTests1337.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "0e38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1, null);

            var assetLoc1 = new ResourceLocationBase("cachedSizeTestAsset1", "myAsset1.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1);

            var assetLoc2 = new ResourceLocationBase("cachedSizeTestAsset2", "myAsset2.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1);

            var dOp = m_Addressables.GetDownloadSizeAsync(new List<object>()
                {
                    assetLoc1,
                    assetLoc2
                }
            );
            yield return dOp;
            Assert.IsTrue(bundleSize1 >= dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceDifferentBundle()
        {
#if ENABLE_CACHING
            yield return Init();
            long bundleSize1 = 1000;
            long bundleSize2 = 250;
            long expectedSize = 0;

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nonExistingUrlForAddressableTests1337.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "0e38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1, null);

            var bundleLoc2 = new ResourceLocationBase("cachedSizeTestBundle2", "http://nonExistingUrlForAddressableTests1337.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle2.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 =
                (bundleLoc2.Data = CreateLocationSizeData("cachedSizeTestBundle2", bundleSize2, 123,
                    "09fe965a6b253fb9dbd3e1cb08b7d66f")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2, null);

            var assetLoc1 = new ResourceLocationBase("cachedSizeTestAsset1", "myAsset1.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1);

            var assetLoc2 = new ResourceLocationBase("cachedSizeTestAsset2", "myAsset2.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc2);

            var dOp = m_Addressables.GetDownloadSizeAsync(new List<object>()
                {
                    assetLoc1,
                    assetLoc2
                }
            );
            yield return dOp;
            Assert.IsTrue((bundleSize1 + bundleSize2) >= dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsWithCorrectKeyAndWrongTypeReturnsEmptyResult()
        {
            yield return Init();
            AsyncOperationHandle<IList<IResourceLocation>> op = m_Addressables.LoadResourceLocationsAsync($"prefabs_even{BuildSuffix}", typeof(Texture2D));
            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(op.Result.Count, 0);
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanGetResourceLocationsWithSingleKey()
        {
            yield return Init();
            int loadCount = 0;
            int loadedCount = 0;
            var ops = new List<AsyncOperationHandle<IList<IResourceLocation>>>();
            foreach (var k in m_KeysHashSet)
            {
                loadCount++;
                AsyncOperationHandle<IList<IResourceLocation>> op = m_Addressables.LoadResourceLocationsAsync(k.Key, typeof(object));
                ops.Add(op);
                op.Completed += op2 =>
                {
                    loadedCount++;
                    Assert.IsNotNull(op2.Result);
                    Assert.AreEqual(k.Value, op2.Result.Count);
                };
            }

            foreach (var op in ops)
            {
                yield return op;
                op.Release();
            }
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModesFailsWithNoKeys(
            [Values(Addressables.MergeMode.UseFirst, Addressables.MergeMode.Intersection, Addressables.MergeMode.Union)] Addressables.MergeMode mode)
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] { }, typeof(GameObject), mode, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModesSucceedsWithSingleKey(
            [Values(Addressables.MergeMode.UseFirst, Addressables.MergeMode.Intersection, Addressables.MergeMode.Union)] Addressables.MergeMode mode)
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}"}, typeof(GameObject), mode, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeUnionSucceedsWithValidKeys()
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var evenCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_odd{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var oddCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}", $"prefabs_odd{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Union, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            Assert.AreEqual(oddCount + evenCount, results.Count);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeUnionSucceedsWithInvalidKeys()
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var evenCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_odd{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var oddCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}", $"prefabs_odd{BuildSuffix}", "INVALIDKEY"}, typeof(GameObject), Addressables.MergeMode.Union, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            Assert.AreEqual(oddCount + evenCount, results.Count);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeIntersectionFailsIfNoResultsDueToIntersection()
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_odd{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}", $"prefabs_odd{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeIntersectionFailsIfNoResultsDueToInvalidKey()
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_odd{BuildSuffix}"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}", $"prefabs_odd{BuildSuffix}", "INVALIDKEY"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);

            ret = m_Addressables.GetResourceLocations(new object[] {$"prefabs_even{BuildSuffix}", "INVALIDKEY"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);

            ret = m_Addressables.GetResourceLocations(new object[] {"INVALIDKEY"}, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);
        }

        [UnityTest]
        public IEnumerator WhenLoadWithInvalidKey_ReturnedOpIsFailed()
        {
            yield return Init();
            List<object> keys = new List<object>() {"INVALID1", "INVALID2"};
            AsyncOperationHandle<IList<GameObject>> gop = new AsyncOperationHandle<IList<GameObject>>();
            using (new IgnoreFailingLogMessage())
            {
                gop = m_Addressables.LoadAssetsAsync<GameObject>(keys, null, Addressables.MergeMode.Intersection, true);
            }

            while (!gop.IsDone)
                yield return null;
            Assert.IsTrue(gop.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Failed, gop.Status);
            gop.Release();

            yield return null; //< Process deferred callback
        }

        [UnityTest]
        public IEnumerator CanLoadAssetsWithMultipleKeysMerged()
        {
            yield return Init();
            List<object> keys = new List<object>() {AddressablesTestUtility.GetPrefabLabel(BuildSuffix), AddressablesTestUtility.GetPrefabUniqueLabel(BuildSuffix, 0)};
            AsyncOperationHandle<IList<GameObject>> gop = m_Addressables.LoadAssetsAsync<GameObject>(keys, null, Addressables.MergeMode.Intersection, true);
            while (!gop.IsDone)
                yield return null;
            Assert.IsTrue(gop.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, gop.Status);
            Assert.NotNull(gop.Result);
            Assert.AreEqual(1, gop.Result.Count);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, gop.Status);
            gop.Release();
        }

        [UnityTest]
        public IEnumerator Release_WhenObjectIsUnknown_LogsErrorAndDoesNotDestroy()
        {
            yield return Init();
            GameObject go = Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = "TestCube";

            m_Addressables.Release(go);
            LogAssert.Expect(LogType.Error, new Regex("Addressables.Release was called on.*"));
            yield return null;

            GameObject foundObj = GameObject.Find("TestCube");
            Assert.IsNotNull(foundObj);
            Object.Destroy(foundObj);
        }

        [UnityTest]
        public IEnumerator ReleaseInstance_WhenObjectIsUnknown_LogsErrorAndDestroys()
        {
            yield return Init();
            GameObject go = Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = "TestCube";

            Assert.IsFalse(m_Addressables.ReleaseInstance(go));
        }

        [UnityTest]
        public IEnumerator LoadAsset_WhenEntryExists_ReturnsAsset()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabUniqueLabel(BuildSuffix, 0);
            AsyncOperationHandle<GameObject> op = m_Addressables.LoadAssetAsync<GameObject>(label);
            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
            Assert.IsTrue(op.Result != null);
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_SuccessfulWhenLoadAssetMode_LoadAllAssets()
        {
            yield return Init();
            if (string.IsNullOrEmpty(TypeName) || TypeName == "BuildScriptFastMode")
            {
                Assert.Ignore($"Skipping test {nameof(LoadAsset_SuccessfulWhenLoadAssetMode_LoadAllAssets)} for {TypeName}, AssetBundle based test.");
            }

            string label = AddressablesTestUtility.GetPrefabUniqueLabel(BuildSuffix, 0);

            var locationHandle = m_Addressables.LoadResourceLocationsAsync(label);
            yield return locationHandle;
            Assert.IsTrue(locationHandle.Result != null, "Failed to get Location for " + label);
            Assert.AreEqual(1, locationHandle.Result.Count, "Failed to get Location for " + label);
            IResourceLocation loc = locationHandle.Result[0];
            locationHandle.Release();

            foreach (IResourceLocation dependency in loc.Dependencies)
            {
                var locOptions = dependency.Data as AssetBundleRequestOptions;
                Assert.IsNotNull(locOptions, "Location dependency did not contain expected AssetBundleRequestOptions data");
                locOptions.AssetLoadMode = AssetLoadMode.AllPackedAssetsAndDependencies;
            }

            AsyncOperationHandle<GameObject> op = m_Addressables.LoadAssetAsync<GameObject>(loc);
            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status, "Loading of " + label + " failed.");
            Assert.IsTrue(op.Result != null, "Loading of " + label + " was successful, but result was null.");
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAssetWithWrongType_WhenEntryExists_Fails()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabUniqueLabel(BuildSuffix, 0);
            AsyncOperationHandle<Texture> op = new AsyncOperationHandle<Texture>();
            using (new IgnoreFailingLogMessage())
            {
                op = m_Addressables.LoadAssetAsync<Texture>(label);
                yield return op;
            }

            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsNull(op.Result);
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_WhenEntryDoesNotExist_OperationFails()
        {
            yield return Init();
            AsyncOperationHandle<GameObject> op = new AsyncOperationHandle<GameObject>();
            using (new IgnoreFailingLogMessage())
            {
                op = m_Addressables.LoadAssetAsync<GameObject>("unknownlabel");
            }

            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsTrue(op.Result == null);
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_CanReleaseThroughAddressablesInCallback([Values(true, false)] bool addressableRelease)
        {
            yield return Init();
            var op = m_Addressables.LoadAssetAsync<object>(m_PrefabKeysList[0]);
            op.Completed += x =>
            {
                Assert.IsNotNull(x.Result);
                if (addressableRelease)
                    m_Addressables.Release(x.Result);
                else
                    op.Release();
            };
            yield return op;
        }

        [UnityTest]
        public IEnumerator LoadAsset_WhenPrefabLoadedAsMultipleTypes_ResultIsEqual()
        {
            yield return Init();

            string label = AddressablesTestUtility.GetPrefabUniqueLabel(BuildSuffix, 0);
            AsyncOperationHandle<object> op1 = m_Addressables.LoadAssetAsync<object>(label);
            AsyncOperationHandle<GameObject> op2 = m_Addressables.LoadAssetAsync<GameObject>(label);
            yield return op1;
            yield return op2;
            Assert.AreEqual(op1.Result, op2.Result);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op1.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op2.Status);
            op1.Release();
            op2.Release();
        }

        [UnityTest]
        public IEnumerator LoadAssets_InvokesCallbackPerAsset()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            HashSet<GameObject> ops = new HashSet<GameObject>();
            var gop = m_Addressables.LoadAssetsAsync<GameObject>(label, x => { ops.Add(x); }, true);
            yield return gop;
            Assert.AreEqual(AddressablesTestUtility.kPrefabCount, ops.Count);
            for (int i = 0; i < ops.Count; i++)
                Assert.IsTrue(ops.Contains(gop.Result[i]));
            gop.Release();
        }

        [UnityTest]
        public IEnumerator LoadAssets_InvokesCallbackPerAssetBeforeCompletedCallback()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            HashSet<GameObject> ops = new HashSet<GameObject>();
            int opsCompletedOnCompleted = 0;
            var gop = m_Addressables.LoadAssetsAsync<GameObject>(label, x => { ops.Add(x); }, true);
            gop.Completed += handle => { opsCompletedOnCompleted = ops.Count; };
            yield return gop;
            Assert.AreEqual(AddressablesTestUtility.kPrefabCount, ops.Count);
            Assert.AreEqual(AddressablesTestUtility.kPrefabCount, opsCompletedOnCompleted);
            for (int i = 0; i < ops.Count; i++)
                Assert.IsTrue(ops.Contains(gop.Result[i]));
            gop.Release();
        }

        // TODO: this doesn't actually check that something was downloaded. It is more: can load dependencies.
        // We really need to address the downloading feature
        [UnityTest]
        public IEnumerator DownloadDependencies_CanDownloadDependencies()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label);
            yield return op;
            AssertDownloadDependencyBundlesAreValid(op);
            op.Release();
        }

        [UnityTest]
        public IEnumerator DownloadDependencies_AutoReleaseHandle_ReleasesOnCompletion()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label, true);
            yield return op;
            Assert.IsFalse(op.IsValid());
        }

        [UnityTest]
        public IEnumerator DownloadDependenciesWithAddress_AutoReleaseHandle_ReleasesOnCompletion()
        {
            yield return Init();
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(m_PrefabKeysList[0], true);
            yield return op;
            Assert.IsFalse(op.IsValid());
        }

        [UnityTest]
        public IEnumerator DownloadDependencies_DoesNotRetainLoadedBundles_WithAutoRelease()
        {
            yield return Init();
            int bundleCountBefore = AssetBundle.GetAllLoadedAssetBundles().Count();
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label, true);
            yield return op;
            AssetBundleProvider.WaitForAllUnloadingBundlesToComplete();
            Assert.AreEqual(bundleCountBefore, AssetBundleProvider.AssetBundleCount);
        }

        [UnityTest]
        public IEnumerator DownloadDependencies_CanLoadAssetWhenHandleNotReleased()
        {
#if ENABLE_CACHING
            if (string.IsNullOrEmpty(TypeName) || TypeName == "BuildScriptFastMode")
            {
                Assert.Ignore($"Skipping test {nameof(DownloadDependencies_CanLoadAssetWhenHandleNotReleased)} for {TypeName}, AssetBundle based test.");
            }
            Caching.ClearCache();

            yield return Init();
            int bundleCountBefore = AssetBundle.GetAllLoadedAssetBundles().Count();
            Assert.AreEqual(0, bundleCountBefore);
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label);
            yield return op;
            Assert.IsTrue(op.IsValid());

            var handle = m_Addressables.LoadAssetAsync<IList<Object>>($"test0{BuildSuffix}");
            yield return handle;
            Assert.IsNotNull(handle.Result);

            handle.Release();
            op.Release();
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator DownloadDependencies_CanLoadAssetWhenHandleIsReleased()
        {
#if ENABLE_CACHING
            Caching.ClearCache();

            yield return Init();
            int bundleCountBefore = AssetBundle.GetAllLoadedAssetBundles().Count();
            Assert.AreEqual(0, bundleCountBefore);
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label);
            yield return op;
            Assert.IsTrue(op.IsValid());
            op.Release();

            var handle = m_Addressables.LoadAssetAsync<IList<Object>>($"test0{BuildSuffix}");
            yield return handle;
            Assert.IsTrue(handle.IsValid());
            Assert.IsNotNull(handle.Result);
            handle.Release();
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator AssetBundleProvider_CanDownloadAndLoadAsset_Simultaneously()
        {
            if (string.IsNullOrEmpty(TypeName) || TypeName == "BuildScriptFastMode")
            {
                Assert.Ignore($"Skipping test {nameof(AssetBundleProvider_CanDownloadAndLoadAsset_Simultaneously)} for {TypeName}, AssetBundle based test.");
            }

#if ENABLE_CACHING
            Caching.ClearCache();
            yield return Init();

            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);

            AsyncOperationHandle downloadOp = m_Addressables.DownloadDependenciesAsync(label);
            var loadHandle = m_Addressables.LoadAssetAsync<IList<Object>>("test0" + BuildSuffix);

            yield return downloadOp;
            yield return loadHandle;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, downloadOp.Status, "DownloadDependenciesAsync should succeed");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadHandle.Status, "LoadAssetAsync should succeed");

            downloadOp.Release();
            loadHandle.Release();
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator AssetBundleProvider_TrackedOperation_CleanedUpAfterCompletion()
        {
            if (string.IsNullOrEmpty(TypeName) || TypeName == "BuildScriptFastMode")
            {
                Assert.Ignore($"Skipping test {nameof(AssetBundleProvider_TrackedOperation_CleanedUpAfterCompletion)} for {TypeName}");
                yield break;
            }

#if ENABLE_CACHING
            yield return Init();
            Caching.ClearCache();

            int initialCount = AssetBundleProvider.LoadingRemoteBundles.Count;
            Assert.AreEqual(0, initialCount, "Should start with no tracked operations");

            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            AsyncOperationHandle downloadOp = m_Addressables.DownloadDependenciesAsync(label);
            yield return downloadOp;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, downloadOp.Status);

            int countAfterCompletion = AssetBundleProvider.LoadingRemoteBundles.Count;
            Assert.AreEqual(0, countAfterCompletion, "Tracking dictionary should be cleaned up after operation completes");

            downloadOp.Release();
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator AssetBundleProvider_CanDownloadAndLoadManyAssets_Simultaneously()
        {
            yield return Init();
            if (string.IsNullOrEmpty(TypeName) || TypeName == "BuildScriptFastMode")
            {
                Assert.Ignore($"Skipping test {nameof(AssetBundleProvider_CanDownloadAndLoadManyAssets_Simultaneously)} for {TypeName}");
                yield break;
            }

#if ENABLE_CACHING
            yield return Init();
            Caching.ClearCache();

            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);

            var load1 = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKeysList[0]);
            AsyncOperationHandle downloadOp = m_Addressables.DownloadDependenciesAsync(label);
            var load2 = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKeysList[1]);
            var load3 = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKeysList[2]);

            yield return load1;
            yield return downloadOp;
            yield return load2;
            yield return load3;

            // All should succeed
            Assert.AreEqual(AsyncOperationStatus.Succeeded, load1.Status, "First load should succeed");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, downloadOp.Status, "Download should succeed");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, load2.Status, "Second load should succeed");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, load3.Status, "Third load should succeed");

            Assert.IsNotNull(load1.Result, "First asset should be loaded");
            Assert.IsNotNull(load2.Result, "Second asset should be loaded");
            Assert.IsNotNull(load3.Result, "Third asset should be loaded");

            // Cleanup
            load1.Release();
            downloadOp.Release();
            load2.Release();
            load3.Release();
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [Test]
        public void AssetBundleProvider_CanSet_UnloadingBundles()
        {
            var unloadingBundles = AssetBundleProvider.UnloadingBundles;

            string key = "op1";
            var newBundles = new Dictionary<string, AssetBundleUnloadOperation>() {{ key, new AssetBundleUnloadOperation() }};
            AssetBundleProvider.UnloadingBundles = newBundles;
            Assert.IsTrue(AssetBundleProvider.UnloadingBundles.ContainsKey(key));

            AssetBundleProvider.UnloadingBundles = unloadingBundles;
        }

        [UnityTest]
        [Ignore("Test is unstable until task refactor is finished.")]
        public IEnumerator DownloadDependencies_ReturnsValidTask()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label);

            Assert.IsNotNull(op.Task);
            yield return op;
            Assert.IsNotNull(op.Task);

            op.Release();
        }

        [UnityTest]
        public IEnumerator StressInstantiation()
        {
            yield return Init();

            // TODO: move this safety check to test fixture base
            var objs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in objs)
                Assert.False(r.name.EndsWith("(Clone)"), "All instances from previous test were not cleaned up");

            var ops = new List<AsyncOperationHandle<GameObject>>();
            for (int i = 0; i < 50; i++)
            {
                var key = m_PrefabKeysList[i % m_PrefabKeysList.Count];
                ops.Add(m_Addressables.InstantiateAsync(key));
            }

            foreach (AsyncOperationHandle<GameObject> op in ops)
                yield return op;

            foreach (AsyncOperationHandle<GameObject> op in ops)
                m_Addressables.ReleaseInstance(op.Result);

            yield return null;

            objs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in objs)
                Assert.False(r.name.EndsWith("(Clone)"), "All instances from this test were not cleaned up");
        }

        [UnityTest]
        public IEnumerator AssetReference_HandleIsInvalidated_WhenReleasingLoadOperation()
        {
            yield return Init();
            AsyncOperationHandle handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (handle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            using (new IgnoreFailingLogMessage())
                yield return behavior.Reference.LoadAssetAsync<GameObject>();

            AsyncOperationHandle referenceHandle = behavior.Reference.OperationHandle;
            Assert.IsTrue(behavior.Reference.IsValid());
            referenceHandle.Release();
            yield return referenceHandle;
            Assert.IsFalse(behavior.Reference.IsValid());

            handle.Release();
        }

        [UnityTest]
        public IEnumerator CanUnloadAssetReference_WithAddressables()
        {
            yield return Init();

            AsyncOperationHandle handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (handle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();
            AsyncOperationHandle<GameObject> assetRefHandle = m_Addressables.InstantiateAsync(behavior.Reference);
            yield return assetRefHandle;
            Assert.IsNotNull(assetRefHandle.Result);

            string name = assetRefHandle.Result.name;
            Assert.IsNotNull(GameObject.Find(name));

            m_Addressables.ReleaseInstance(assetRefHandle.Result);
            yield return null;
            Assert.IsNull(GameObject.Find(name));

            handle.Release();
        }

        [UnityTest]
        public IEnumerator CanloadAssetReferenceSubObject()
        {
            yield return Init();

            var handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result);
            AssetReferenceTestBehavior behavior = handle.Result.GetComponent<AssetReferenceTestBehavior>();

            AsyncOperationHandle<Object> assetRefHandle = m_Addressables.LoadAssetAsync<Object>(behavior.ReferenceWithSubObject);
            yield return assetRefHandle;
            Assert.IsNotNull(assetRefHandle.Result);
            assetRefHandle.Release();
            handle.Release();
        }

        [UnityTest]
        public IEnumerator AddressablesIntegration_LoadAssetAsync_CanLoadAssetReferenceObjectList()
        {
            yield return Init();

            var assetRefHandle = m_Addressables.LoadAssetAsync<Object[]>("assetWithSubObjects");
            yield return assetRefHandle;
            Assert.IsNotNull(assetRefHandle.Result);
            Assert.AreEqual(assetRefHandle.Result.Length, 2);
            Assert.AreEqual(assetRefHandle.Result[0].name, "assetWithSubObjects");
            Assert.AreEqual(assetRefHandle.Result[1].name, "sub-shown");
            assetRefHandle.Release();
        }

        [UnityTest]
        public IEnumerator LoadAssets_WithHiddenSubObjects_OnlyReturnsNonHidden_WithMainAssetFirst()
        {
            yield return Init();

            var handle = m_Addressables.LoadAssetAsync<IList<Object>>("assetWithSubObjects");
            yield return handle;
            Assert.IsNotNull(handle.Result);
            Assert.AreEqual(handle.Result.Count, 2);
            Assert.AreEqual(handle.Result[0].name, "assetWithSubObjects");
            Assert.AreEqual(handle.Result[1].name, "sub-shown");
            handle.Release();
        }

        [UnityTest]
        public IEnumerator RuntimeKeyIsValid_ReturnsTrueForSubObjects()
        {
            yield return Init();

            var handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result);
            AssetReferenceTestBehavior behavior = handle.Result.GetComponent<AssetReferenceTestBehavior>();

            Assert.IsTrue(behavior.ReferenceWithSubObject.RuntimeKeyIsValid());

            handle.Release();
        }

        [UnityTest]
        public IEnumerator RuntimeKeyIsValid_ReturnsTrueForValidKeys()
        {
            yield return Init();

            AsyncOperationHandle handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (handle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            Assert.IsTrue((behavior.Reference as IKeyEvaluator).RuntimeKeyIsValid());
            Assert.IsTrue((behavior.LabelReference as IKeyEvaluator).RuntimeKeyIsValid());

            handle.Release();
        }

        [UnityTest]
        public IEnumerator PercentComplete_CalculationIsCorrect_WhenInAGroupOperation()
        {
            yield return Init();
            GroupOperation groupOp = new GroupOperation();

            float handle1PercentComplete = 0.22f;
            float handle2PercentComplete = 0.78f;
            float handle3PercentComplete = 1.0f;
            float handle4PercentComplete = 0.35f;

            List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>()
            {
                new ManualPercentCompleteOperation(handle1PercentComplete).Handle,
                new ManualPercentCompleteOperation(handle2PercentComplete).Handle,
                new ManualPercentCompleteOperation(handle3PercentComplete).Handle,
                new ManualPercentCompleteOperation(handle4PercentComplete).Handle
            };

            groupOp.Init(handles);

            Assert.AreEqual((handle1PercentComplete + handle2PercentComplete + handle3PercentComplete + handle4PercentComplete) / 4, groupOp.PercentComplete);
        }

        private class DebugNameTestOperation : AsyncOperationBase<string>
        {
            string m_DebugName;
            List<AsyncOperationHandle> m_Dependencies;

            protected override void Execute()
            {
            }

            internal DebugNameTestOperation(string debugName)
            {
                m_DebugName = debugName;
                m_Dependencies = new List<AsyncOperationHandle>();
            }

            internal DebugNameTestOperation(string debugName, List<AsyncOperationHandle> deps)
            {
                m_DebugName = debugName;
                m_Dependencies = deps;
            }

            /// <inheritdoc />
            public override void GetDependencies(List<AsyncOperationHandle> dependencies)
            {
                foreach (var handle in m_Dependencies)
                    dependencies.Add(handle);
            }

            protected override string DebugName
            {
                get { return m_DebugName; }
            }
        }

        [UnityTest]
        public IEnumerator PercentComplete_CalculationIsCorrect_WhenInAChainOperation()
        {
            yield return Init();

            float handle1PercentComplete = 0.6f;
            float handle2PercentComplete = 0.98f;

            AsyncOperationHandle<GameObject> slowHandle1 = new ManualPercentCompleteOperation(handle1PercentComplete, new DownloadStatus() {DownloadedBytes = 1, IsDone = false, TotalBytes = 2})
                .Handle;
            AsyncOperationHandle<GameObject> slowHandle2 = new ManualPercentCompleteOperation(handle2PercentComplete, new DownloadStatus() {DownloadedBytes = 1, IsDone = false, TotalBytes = 2})
                .Handle;

            slowHandle1.m_InternalOp.m_RM = m_Addressables.ResourceManager;
            slowHandle2.m_InternalOp.m_RM = m_Addressables.ResourceManager;

            var chainOperation = m_Addressables.ResourceManager.CreateChainOperation(slowHandle1, (op) => { return slowHandle2; });

            chainOperation.m_InternalOp.Start(m_Addressables.ResourceManager, default, null);

            Assert.AreEqual((handle1PercentComplete + handle2PercentComplete) / 2, chainOperation.PercentComplete);
        }

        [UnityTest]
        public IEnumerator RuntimeKeyIsValid_ReturnsFalseForInValidKeys()
        {
            yield return Init();

            AsyncOperationHandle handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (handle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            Assert.IsFalse((behavior.InValidAssetReference as IKeyEvaluator).RuntimeKeyIsValid());
            Assert.IsFalse((behavior.InvalidLabelReference as IKeyEvaluator).RuntimeKeyIsValid());

            handle.Release();
        }
        static ResourceLocationMap GetRLM(AddressablesImpl addr)
        {
            foreach (var rl in addr.m_ResourceLocators)
            {
                if (rl.Locator is ResourceLocationMap)
                    return rl.Locator as ResourceLocationMap;
            }

            return null;
        }

        private void SetupBundleForCacheDependencyClearTests(string bundleName, string depName, string hash, string key, out ResourceLocationBase location)
        {
            CreateFakeCachedBundle(bundleName, hash);
            location = new ResourceLocationBase(key, bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource),
                new ResourceLocationBase(depName, bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource)));
            (location.Dependencies[0] as ResourceLocationBase).Data = location.Data = new AssetBundleRequestOptions()
            {
                BundleName = bundleName
            };
        }

        class TestCatalogProviderCustomAssetBundleResource : BundledAssetProvider
        {

            public TestAssetBundleResourceInternalOp TestInternalOp;

            internal class TestAssetBundleResourceInternalOp : AsyncOperationBase<TestAssetBundleResource>
            {
                internal TestAssetBundleResource m_Resource;

                public TestAssetBundleResourceInternalOp(TestAssetBundleResource resource)
                {
                    m_Resource = resource;
                }

                protected override void Execute()
                {
                    Complete(m_Resource, m_Resource != null, m_Resource != null ? "" : "TestAssetBundleResource was null");
                }
            }

            /// <inheritdoc/>
            public override string ProviderId
            {
                get
                {
                    if (string.IsNullOrEmpty(m_ProviderId))
                        m_ProviderId = typeof(TestCatalogProviderCustomAssetBundleResource).FullName;

                    return m_ProviderId;
                }
            }

            public override void Provide(ProvideHandle provideHandle)
            {
                ProviderOperation<Object> op = new ProviderOperation<Object>();
                GroupOperation group = new GroupOperation();
                TestInternalOp = new TestAssetBundleResourceInternalOp(new TestAssetBundleResource());
                TestInternalOp.m_RM = Addressables.Instance.ResourceManager;
                var opRef1 = provideHandle.ResourceManager.Acquire(TestInternalOp.Handle);
                var opRef2 = provideHandle.ResourceManager.Acquire(TestInternalOp.Handle);
                group.Init(new List<AsyncOperationHandle>() {new AsyncOperationHandle(TestInternalOp)});
                op.Init(provideHandle.ResourceManager, null, provideHandle.Location, group.Handle);
                op.m_RM = Addressables.Instance.ResourceManager;
                ProvideHandle handle = new ProvideHandle(provideHandle.ResourceManager, op);
                TestInternalOp.InvokeExecute();
                base.Provide(handle);
                provideHandle.Complete(TestInternalOp.Result, TestInternalOp.Status == AsyncOperationStatus.Succeeded, TestInternalOp.OperationException);
                opRef1.Release();
                opRef2.Release();
            }

            internal class TestAssetBundleResource : IAssetBundleResource
            {
                public bool WasUsed = false;

                public AssetBundle GetAssetBundle()
                {
                    WasUsed = true;
                    return null;
                }
            }
        }
        private void SetupBundleForProviderTests(string bundleName, string depName, string key, out ResourceLocationBase location, out TestCatalogProviderCustomAssetBundleResource testProvider)
        {
            testProvider = new TestCatalogProviderCustomAssetBundleResource();
            m_Addressables.ResourceManager.ResourceProviders.Add(testProvider);
            location = new ResourceLocationBase(key, bundleName, typeof(TestCatalogProviderCustomAssetBundleResource).FullName,
                typeof(TestCatalogProviderCustomAssetBundleResource.TestAssetBundleResource),
                new ResourceLocationBase(depName, bundleName, typeof(TestCatalogProviderCustomAssetBundleResource).FullName,
                    typeof(TestCatalogProviderCustomAssetBundleResource.TestAssetBundleResource)));
            (location.Dependencies[0] as ResourceLocationBase).Data = location.Data = new AssetBundleRequestOptions()
            {
                BundleName = bundleName
            };

            GetRLM(m_Addressables).Add(key, new List<IResourceLocation>() {location});
        }
        [UnityTest]
        [Platform(Exclude = "PS5")]
        public IEnumerator ClearDependencyCache_ClearsAllCachedFilesForKey()
        {
            if (!UseJsonCatalog)
                Assert.Ignore("ClearDependencyCache test requires JSON catalog format.");
            yield return Init();
            var rlm = GetRLM(m_Addressables);
            if (rlm == null)
                yield break;
#if ENABLE_CACHING

            string hash = "123456789";
            string bundleName = $"test_{hash}";
            string key = "lockey_key";

            List<Hash128> versions = new List<Hash128>();
            ResourceLocationBase location = null;
            SetupBundleForCacheDependencyClearTests(bundleName, "bundle", hash, key, out location);

            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(1, versions.Count);
            versions.Clear();
            rlm.Add(location.PrimaryKey, new List<IResourceLocation>() {location});

            yield return m_Addressables.ClearDependencyCacheAsync((object)key, true);
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(0, versions.Count);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator ClearDependencyCache_ClearsAllCachedFilesForKeyWithDependencies()
        {
            yield return Init();
            var rlm = GetRLM(m_Addressables);
            if (rlm == null)
                yield break;
#if ENABLE_CACHING

            string hash = "123456789";
            string bundleName = $"test_{hash}";

            string depHash = "97564231";
            string depBundleName = $"test_{depHash}";
            ResourceLocationBase depLocation = null;

            string key = "lockey_deps_key";

            SetupBundleForCacheDependencyClearTests(depBundleName, "test", depHash, "depKey", out depLocation);
            CreateFakeCachedBundle(bundleName, hash);

            ResourceLocationBase location = new ResourceLocationBase(key, bundleName,
                typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource),
                new ResourceLocationBase("bundle", bundleName, typeof(AssetBundleProvider).FullName,
                    typeof(IAssetBundleResource)),
                depLocation);
            (location.Dependencies[0] as ResourceLocationBase).Data = location.Data = new AssetBundleRequestOptions()
            {
                BundleName = bundleName
            };

            rlm.Add(location.PrimaryKey, new List<IResourceLocation>() {location});

            yield return m_Addressables.ClearDependencyCacheAsync((object)key, true);

            List<Hash128> versions = new List<Hash128>();
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(0, versions.Count);
            versions.Clear();
            Caching.GetCachedVersions(depBundleName, versions);
            Assert.AreEqual(0, versions.Count);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator ClearDependencyCache_ClearsAllCachedFilesForLocationWithDependencies()
        {
            yield return Init();
            var rlm = GetRLM(m_Addressables);
            if (rlm == null)
                yield break;
#if ENABLE_CACHING
            string hash = "123456789";
            string bundleName = $"test_{hash}";
            string key = "lockey_deps_location";

            string depHash = "97564231";
            string depBundleName = $"test_{depHash}";
            ResourceLocationBase depLocation = null;

            SetupBundleForCacheDependencyClearTests(depBundleName, "test", depHash, "depKey", out depLocation);
            CreateFakeCachedBundle(bundleName, hash);

            ResourceLocationBase location = new ResourceLocationBase(key, bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource),
                new ResourceLocationBase("bundle", bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource)),
                depLocation);
            (location.Dependencies[0] as ResourceLocationBase).Data = location.Data = new AssetBundleRequestOptions()
            {
                BundleName = bundleName
            };

            rlm.Add(location.PrimaryKey, new List<IResourceLocation>() {location});

            yield return m_Addressables.ClearDependencyCacheAsync(location, true);

            List<Hash128> versions = new List<Hash128>();
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(0, versions.Count);
            versions.Clear();
            Caching.GetCachedVersions(depBundleName, versions);
            Assert.AreEqual(0, versions.Count);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }

#if !UNITY_PS5
        [UnityTest]
        public IEnumerator ClearDependencyCache_ClearsAllCachedFilesForLocationList()
        {
            yield return Init();
            var rlm = GetRLM(m_Addressables);
            if (rlm == null)
                yield break;
#if ENABLE_CACHING
            string hash = "123456789";
            string bundleName = $"test_{hash}";
            string key = "lockey_location_list";
            ResourceLocationBase location = null;
            SetupBundleForCacheDependencyClearTests(bundleName, "bundle", hash, key, out location);

            List<Hash128> versions = new List<Hash128>();
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(1, versions.Count);
            versions.Clear();

            rlm.Add(location.PrimaryKey, new List<IResourceLocation>() {location});

            yield return m_Addressables.ClearDependencyCacheAsync(new List<IResourceLocation>() {location}, true);
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(0, versions.Count);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }
#endif

#if !UNITY_PS5
        [UnityTest]
        public IEnumerator ClearDependencyCache_ClearsAllCachedFilesForKeyList()
        {
            yield return Init();
            var rlm = GetRLM(m_Addressables);
            if (rlm == null)
                yield break;
#if ENABLE_CACHING

            string hash = "123456789";
            string bundleName = $"test_{hash}";
            string key = "lockey_key_list";
            ResourceLocationBase location = null;

            SetupBundleForCacheDependencyClearTests(bundleName, "bundle", hash, key, out location);

            List<Hash128> versions = new List<Hash128>();
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(1, versions.Count);
            versions.Clear();

            rlm.Add(location.PrimaryKey, new List<IResourceLocation>() {location});

            yield return m_Addressables.ClearDependencyCacheAsync(new List<object>() {(object)key}, true);
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(0, versions.Count);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }
#endif

        [UnityTest]
        public IEnumerator ClearDependencyCache_ClearsAllCachedFilesForLocationListWithDependencies()
        {
            yield return Init();
            var rlm = GetRLM(m_Addressables);
            if (rlm == null)
                yield break;
#if ENABLE_CACHING

            string hash = "123456789";
            string bundleName = $"test_{hash}";
            string key = "lockey_deps_location_list";

            string depHash = "97564231";
            string depBundleName = $"test_{depHash}";
            ResourceLocationBase depLocation = null;

            SetupBundleForCacheDependencyClearTests(depBundleName, "test", depHash, "depKey", out depLocation);

            CreateFakeCachedBundle(bundleName, hash);
            ResourceLocationBase location = new ResourceLocationBase(key, bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource),
                new ResourceLocationBase("bundle", bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource)),
                depLocation);
            (location.Dependencies[0] as ResourceLocationBase).Data = location.Data = new AssetBundleRequestOptions()
            {
                BundleName = bundleName
            };

            rlm.Add(location.PrimaryKey, new List<IResourceLocation>() {location});

            yield return m_Addressables.ClearDependencyCacheAsync(new List<IResourceLocation>() {location}, true);

            List<Hash128> versions = new List<Hash128>();
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(0, versions.Count);
            versions.Clear();
            Caching.GetCachedVersions(depBundleName, versions);
            Assert.AreEqual(0, versions.Count);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator ClearDependencyCache_ClearsAllCachedFilesForKeyListWithDependencies()
        {
            yield return Init();
            var rlm = GetRLM(m_Addressables);
            if (rlm == null)
                yield break;
#if ENABLE_CACHING

            string hash = "123456789";
            string bundleName = $"test_{hash}";
            string key = "lockey_deps_key_list";

            string depHash = "97564231";
            string depBundleName = $"test_{depHash}";
            ResourceLocationBase depLocation = null;

            SetupBundleForCacheDependencyClearTests(depBundleName, "test", depHash, "depKey", out depLocation);

            CreateFakeCachedBundle(bundleName, hash);
            ResourceLocationBase location = new ResourceLocationBase(key, bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource),
                new ResourceLocationBase("bundle", bundleName, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource)),
                depLocation);
            (location.Dependencies[0] as ResourceLocationBase).Data = location.Data = new AssetBundleRequestOptions()
            {
                BundleName = bundleName
            };

            rlm.Add(location.PrimaryKey, new List<IResourceLocation>() {location});


            yield return m_Addressables.ClearDependencyCacheAsync(new List<object>() {key}, true);

            List<Hash128> versions = new List<Hash128>();
            Caching.GetCachedVersions(bundleName, versions);
            Assert.AreEqual(0, versions.Count);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator AssetBundleRequestOptions_ComputesCorrectSize_WhenLocationDoesNotMatchBundleName_WithoutHash()
        {
#if ENABLE_CACHING
            yield return Init();

            //Setup
            string bundleName = "bundle";
            string bundleLocation = "http://fake.com/default-bundle";
            long size = 123;
            IResourceLocation location = new ResourceLocationBase("test", bundleLocation,
                typeof(AssetBundleProvider).FullName, typeof(GameObject));

            Hash128 hash = Hash128.Compute("213412341242134");
            AssetBundleRequestOptions abro = new AssetBundleRequestOptions()
            {
                BundleName = bundleName,
                BundleSize = size,
            };

            //Test
            Assert.AreEqual(size, abro.ComputeSize(location, m_Addressables.ResourceManager));
            CreateFakeCachedBundle(bundleName, hash.ToString());

            // No hash, so the bundle should be downloaded
            Assert.AreEqual(size, abro.ComputeSize(location, m_Addressables.ResourceManager));

            //Cleanup
            Caching.ClearAllCachedVersions(bundleName);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator Autorelease_False_ClearDependencyCache_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            var clearCache = m_Addressables.ClearDependencyCacheAsync("NotARealKey", false);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsTrue(clearCache.IsValid());
            clearCache.Release();
            Assert.IsFalse(clearCache.IsValid());
        }

        [UnityTest]
        public IEnumerator Autorelease_True_ClearDependencyCache_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            var clearCache = m_Addressables.ClearDependencyCacheAsync("NotARealKey", true);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsFalse(clearCache.IsValid());
        }

        [UnityTest]
        public IEnumerator Autorelease_False_ClearDependencyCacheList_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            var clearCache = m_Addressables.ClearDependencyCacheAsync(new List<object> {"NotARealKey"}, false);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsTrue(clearCache.IsValid());
            clearCache.Release();
            Assert.IsFalse(clearCache.IsValid());
        }

        [UnityTest]
        public IEnumerator Autorelease_True_ClearDependencyCacheList_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            var clearCache = m_Addressables.ClearDependencyCacheAsync(new List<object> {"NotARealKey"}, true);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsFalse(clearCache.IsValid());
        }

        public IEnumerator Autorelease_False_ClearDependencyCacheObject_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            var clearCache = m_Addressables.ClearDependencyCacheAsync((object)"NotARealKey", false);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsTrue(clearCache.IsValid());
            clearCache.Release();
            Assert.IsFalse(clearCache.IsValid());
        }

        [UnityTest]
        public IEnumerator Autorelease_True_ClearDependencyCacheObject_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            var clearCache = m_Addressables.ClearDependencyCacheAsync((object)"NotARealKey", true);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsFalse(clearCache.IsValid());
        }

        [UnityTest]
        public IEnumerator Autorelease_False_ClearDependencyCacheListIResourceLocs_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            List<IResourceLocation> locations = new List<IResourceLocation>()
            {
                new ResourceLocationBase("NotARealKey", "NotARealKey", typeof(BundledAssetProvider).FullName,
                    typeof(ResourceLocationBase))
            };
            var clearCache = m_Addressables.ClearDependencyCacheAsync(locations, false);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsTrue(clearCache.IsValid());
            clearCache.Release();
            Assert.IsFalse(clearCache.IsValid());
        }

        [UnityTest]
        public IEnumerator Autorelease_True_ClearDependencyCacheListIResourceLocs_WithChainOperation_DoesNotThrowInvalidHandleError()
        {
            yield return Init();
            //This is to make sure we use the ShouldChainRequest
            var dumbUpdate = new DumbUpdateOperation();
            m_Addressables.m_ActiveUpdateOperation = new AsyncOperationHandle<List<IResourceLocator>>(dumbUpdate);

            List<IResourceLocation> locations = new List<IResourceLocation>()
            {
                new ResourceLocationBase("NotARealKey", "NotARealKey", typeof(BundledAssetProvider).FullName,
                    typeof(ResourceLocationBase))
            };
            var clearCache = m_Addressables.ClearDependencyCacheAsync(locations, true);
            dumbUpdate.CallComplete();
            yield return clearCache;
            Assert.IsFalse(clearCache.IsValid());
        }

#if !UNITY_PS5
        [UnityTest]
        public IEnumerator AssetBundleRequestOptions_ComputesCorrectSize_WhenLocationDoesNotMatchBundleName_WithHash()
        {
#if ENABLE_CACHING
            yield return Init();

            //Setup
            string bundleName = "bundle";
            string bundleLocation = "http://fake.com/default-bundle";
            long size = 123;
            IResourceLocation location = new ResourceLocationBase("test", bundleLocation,
                typeof(AssetBundleProvider).FullName, typeof(GameObject));

            Hash128 hash = Hash128.Compute("213412341242134");
            AssetBundleRequestOptions abro = new AssetBundleRequestOptions()
            {
                BundleName = bundleName,
                BundleSize = size,
                Hash = hash.ToString()
            };

            //Test
            Assert.AreEqual(size, abro.ComputeSize(location, m_Addressables.ResourceManager));
            CreateFakeCachedBundle(bundleName, hash.ToString());
            Assert.AreEqual(0, abro.ComputeSize(location, m_Addressables.ResourceManager));

            //Cleanup
            Caching.ClearAllCachedVersions(bundleName);
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }
#endif

#if !UNITY_PS5
        [UnityTest]
        public IEnumerator AssetBundleResource_RemovesCachedBundle_OnLoadFailure()
        {
#if ENABLE_CACHING
            yield return Init();
            string bundleName = "bundleName";
            Hash128 hash = Hash128.Parse("123456");
            uint crc = 1;
            AssetBundleResource abr = new AssetBundleResource();
            abr.m_ProvideHandle = new ProvideHandle(m_Addressables.ResourceManager, new ProviderOperation<AssetBundleResource>());
            abr.m_Options = new AssetBundleRequestOptions()
            {
                BundleName = bundleName,
                Hash = hash.ToString(),
                Crc = crc,
                RetryCount = 3
            };
            CreateFakeCachedBundle(bundleName, hash.ToString());
            CachedAssetBundle cab = new CachedAssetBundle(bundleName, hash);
            var request = abr.CreateWebRequest(new ResourceLocationBase("testName", $"http://127.0.01/{bundleName}", typeof(AssetBundleProvider).FullName,
                typeof(IAssetBundleResource)));

            Assert.IsTrue(Caching.IsVersionCached(cab));
            yield return request.SendWebRequest();
            Assert.IsFalse(Caching.IsVersionCached(cab));
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }
#endif

#if !UNITY_PS5
        [UnityTest]
        public IEnumerator AssetBundleResource_RemovesCachedBundle_OnLoadFailure_WhenRetryCountIsZero()
        {
#if ENABLE_CACHING
            yield return Init();
            string bundleName = "bundleName";
            Hash128 hash = Hash128.Parse("123456");
            uint crc = 1;
            AssetBundleResource abr = new AssetBundleResource();
            abr.m_ProvideHandle = new ProvideHandle(m_Addressables.ResourceManager, new ProviderOperation<AssetBundleResource>());
            abr.m_Options = new AssetBundleRequestOptions()
            {
                BundleName = bundleName,
                Hash = hash.ToString(),
                Crc = crc,
                RetryCount = 0
            };
            CreateFakeCachedBundle(bundleName, hash.ToString());
            CachedAssetBundle cab = new CachedAssetBundle(bundleName, hash);
            var request = abr.CreateWebRequest(new ResourceLocationBase("testName", $"http://127.0.01/{bundleName}", typeof(AssetBundleProvider).FullName,
                typeof(IAssetBundleResource)));

            Assert.IsTrue(Caching.IsVersionCached(cab));
            yield return request.SendWebRequest();
            Assert.IsFalse(Caching.IsVersionCached(cab));
#else
            Assert.Ignore("Caching not enabled.");
            yield return null;
#endif
        }
#endif

#if !UNITY_PS5
        [Test]
        public void AssetBundleResource_WhenNotLoaded_GetAssetPreloadRequest_ReturnsNull()
        {
            AssetBundleResource abr = new AssetBundleResource();
            Assert.AreEqual(null, abr.GetAssetPreloadRequest());
        }
#endif

        [Test]
        public void WebRequestQueueOperation_CanSetWebRequest()
        {
            string url = "https://www.mynewsite.com/";
            var op = new WebRequestQueueOperation(new UnityWebRequest("https://www.myoldsite.com/"));
            op.WebRequest = new UnityWebRequest(url);
            Assert.AreEqual(url, op.WebRequest.url);
        }

        [UnityTest]
        public IEnumerator WebRequestQueue_GetsSetOnInitialization_FromRuntimeData()
        {
            yield return Init();
            Assert.AreEqual(AddressablesTestUtility.kMaxWebRequestCount, WebRequestQueue.s_MaxRequest);
        }

        [Test]
        public void WebRequestQueue_WhenMaxConcurrentRequests_SetToZero_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => WebRequestQueue.SetMaxConcurrentRequests(0), "MaxRequests must be 1 or greater.");
        }

        internal static string CreateFakeCachedBundle(string bundleName, string hash)
        {
#if ENABLE_CACHING
            string fakeCachePath = string.Format("{0}/{1}/{2}", Caching.currentCacheForWriting.path, bundleName, hash);
            Directory.CreateDirectory(fakeCachePath);
            var dataFile = File.Create(Path.Combine(fakeCachePath, "__data"));
            var infoFile = File.Create(Path.Combine(fakeCachePath, "__info"));

            byte[] info = new UTF8Encoding(true).GetBytes(
                @"-1
1554740658
1
__data");
            infoFile.Write(info, 0, info.Length);

            dataFile.Dispose();
            infoFile.Dispose();

            return fakeCachePath;
#else
            return null;
#endif
        }

        class AsyncWaitForCompletion : MonoBehaviour
        {
            public string key1;
            public string key2;
            public bool done = false;
            [System.NonSerialized] public AsyncOperationHandle<IList<IResourceLocation>> op;
            [System.NonSerialized] public AsyncOperationHandle<GameObject> op2;
            [System.NonSerialized] public AddressablesImpl addressables;
            public string errorMsg;

            async void Start()
            {
                try
                {
                    op = addressables.LoadResourceLocationsAsync(key1, typeof(Texture2D));
                    await op.Task;
                    op2 = addressables.LoadAssetAsync<GameObject>(key2);
                    op2.WaitForCompletion();
                }
                catch (Exception e)
                {
                    errorMsg = e.Message;
                }
                finally
                {
                    done = true;
                }
            }
        }

        [UnityTest]
        public IEnumerator WhenCallingWaitForCompletion_InAsyncMethod_NoExceptionIsThrown()
        {
            yield return Init();
            var go = new GameObject("test", typeof(AsyncWaitForCompletion));
            var comp = go.GetComponent<AsyncWaitForCompletion>();
            comp.addressables = m_Addressables;
            comp.key1 = $"prefabs_even{BuildSuffix}";
            comp.key2 = AddressablesTestUtility.GetPrefabLabel(BuildSuffix);

            while (!comp.done)
                yield return null;
            Assert.IsTrue(string.IsNullOrEmpty(comp.errorMsg), comp.errorMsg);

            comp.op.Release();
            comp.op2.Release();
            GameObject.Destroy(go);
        }
    }
}

