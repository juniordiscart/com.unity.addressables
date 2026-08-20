using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{

    public class TextDataProviderStub : TextDataProvider
    {
        TextDataProvider m_TextDataProvider;
        string m_FakeRemoteFolder;

        public TextDataProviderStub(string fakeRemoteFolder, TextDataProvider textDataProvider)
        {
            m_TextDataProvider = textDataProvider;
            m_FakeRemoteFolder = fakeRemoteFolder;
        }

        public override string ProviderId => m_TextDataProvider.ProviderId;

        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOpStub(m_FakeRemoteFolder).Start(provideHandle, m_TextDataProvider);
        }

        internal class InternalOpStub : TextDataProvider.InternalOp
        {
            string m_FakeRemoteFolder;
            static readonly Regex k_Pattern = new Regex(@"http://[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}");

            public InternalOpStub(string fakeRemoteFolder)
            {
                m_FakeRemoteFolder = fakeRemoteFolder;
            }

            protected override void SendWebRequest(string path)
            {
                string pathWithFakeRemoteFolder = k_Pattern.Replace(ResourceManagerConfig.StripQueryParameters(path), m_FakeRemoteFolder);

                string fileText = null;
                Exception ex = null;
                if (File.Exists(pathWithFakeRemoteFolder))
                    fileText = File.ReadAllText(pathWithFakeRemoteFolder);
                else
                    ex = new Exception($"{nameof(TextDataProvider)} unable to load from url {path}");

                CompleteOperation(fileText, ex);
            }
        }
    }

    public class JsonAssetProviderStub : TextDataProviderStub
    {
        public JsonAssetProviderStub(string fakeRemoteFolder, JsonAssetProvider jsonAssetProvider)
            : base(fakeRemoteFolder, jsonAssetProvider)
        {
        }
    }

    public class JsonCatalogProviderStub : JsonCatalogProvider
    {
        public JsonCatalogProviderStub(ResourceManager resourceManagerInstance) : base(resourceManagerInstance)
        {
        }
    }

    internal class BinaryAssetProviderStub : BinaryAssetProvider<BinaryContentCatalogData.Serializer>
    {
        BinaryDataProvider m_BinaryDataProvider;
        string m_FakeRemoteFolder;

        public BinaryAssetProviderStub(string fakeRemoteFolder, BinaryDataProvider binaryDataProvider)
        {
            m_BinaryDataProvider = binaryDataProvider;
            m_FakeRemoteFolder = fakeRemoteFolder;
        }

        public override string ProviderId => m_BinaryDataProvider.ProviderId;

        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOpStub(m_FakeRemoteFolder).Start(provideHandle, m_BinaryDataProvider);
        }

        internal class InternalOpStub : BinaryDataProvider.InternalOp
        {
            string m_FakeRemoteFolder;
            static readonly Regex k_Pattern = new Regex(@"http://[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}");

            public InternalOpStub(string fakeRemoteFolder)
            {
                m_FakeRemoteFolder = fakeRemoteFolder;
            }

            protected override void SendWebRequest(string path)
            {
                string localPath = k_Pattern.Replace(ResourceManagerConfig.StripQueryParameters(path), m_FakeRemoteFolder);
                byte[] data = null;
                Exception ex = null;
                if (File.Exists(localPath))
                    data = File.ReadAllBytes(localPath);
                else
                    ex = new Exception($"{nameof(BinaryDataProvider)} unable to load from url {path}");
                CompleteOperation(data, ex);
            }
        }
    }
}
