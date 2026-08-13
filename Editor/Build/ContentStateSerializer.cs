#if UNITY_6000_0_OR_NEWER
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Handles serialization and deserialization of AddressablesContentState using DataContractSerializer.
    /// Uses Binary XML format for compact storage with JSON extraction for debugging.
    /// </summary>
    internal static class ContentStateSerializer
    {
        private static DataContractSerializer s_Serializer;

        private static DataContractSerializer Serializer
        {
            get
            {
                if (s_Serializer == null)
                {
                    var settings = new DataContractSerializerSettings
                    {
                        KnownTypes = new[]
                        {
                            typeof(AssetBundleRequestOptions),
                            typeof(SerializableGUID),
                            typeof(SerializableHash128)
                        },
                        PreserveObjectReferences = true,
                        DataContractSurrogate = new ContentStateSurrogate()
                    };
                    s_Serializer = new DataContractSerializer(typeof(AddressablesContentState), settings);
                }
                return s_Serializer;
            }
        }

        /// <summary>
        /// Checks if the file at the given path is in the legacy BinaryFormatter format.
        /// </summary>
        /// <param name="path">Path to the content state file.</param>
        /// <returns>True if the file is in legacy format, false if it's the new format.</returns>
        public static bool IsLegacyFormat(string path)
        {
            return LegacyFormatConverter.IsLegacyFormat(path, LegacyFormatConverter.ContentStateVersionMarker);
        }

        /// <summary>
        /// Checks if the stream contains legacy BinaryFormatter format data.
        /// </summary>
        /// <param name="stream">The stream to check. Position will be reset after checking.</param>
        /// <returns>True if the stream contains legacy format data.</returns>
        public static bool IsLegacyFormat(Stream stream)
        {
            return LegacyFormatConverter.IsLegacyFormat(stream, LegacyFormatConverter.ContentStateVersionMarker);
        }

        /// <summary>
        /// Serializes the content state to a file using Binary XML format.
        /// </summary>
        /// <param name="contentState">The content state to serialize.</param>
        /// <param name="path">The file path to write to.</param>
        public static void Serialize(AddressablesContentState contentState, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(path))
                File.Delete(path);

            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                Serialize(contentState, stream);
            }
        }

        /// <summary>
        /// Serializes the content state to a stream using Binary XML format.
        /// </summary>
        /// <param name="contentState">The content state to serialize.</param>
        /// <param name="stream">The stream to write to.</param>
        public static void Serialize(AddressablesContentState contentState, Stream stream)
        {
            // Write version marker first
            var marker = LegacyFormatConverter.ContentStateVersionMarker;
            stream.Write(marker, 0, marker.Length);

            // Write using Binary XML format
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false))
            {
                Serializer.WriteObject(writer, contentState);
                writer.Flush();
            }
        }

        /// <summary>
        /// Deserializes the content state from a file.
        /// </summary>
        /// <param name="path">The file path to read from.</param>
        /// <returns>The deserialized content state, or null if deserialization fails.</returns>
        public static AddressablesContentState Deserialize(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return Deserialize(stream);
            }
        }

        /// <summary>
        /// Deserializes the content state from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The deserialized content state, or null if deserialization fails.</returns>
        public static AddressablesContentState Deserialize(Stream stream)
        {
            // Skip version marker
            stream.Position = LegacyFormatConverter.ContentStateVersionMarker.Length;

            // Read using Binary XML format
            using (var reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                return Serializer.ReadObject(reader) as AddressablesContentState;
            }
        }

        /// <summary>
        /// Extracts the content state to a human-readable JSON file for debugging.
        /// </summary>
        /// <param name="inputPath">Path to the binary content state file.</param>
        /// <param name="outputPath">Path for the output JSON file.</param>
        public static void ExtractToJson(string inputPath, string outputPath)
        {
            var contentState = Deserialize(inputPath);
            if (contentState == null)
            {
                Debug.LogError($"Failed to deserialize content state from {inputPath}");
                return;
            }

            var jsonSettings = new DataContractJsonSerializerSettings
            {
                KnownTypes = new[]
                {
                    typeof(AssetBundleRequestOptions),
                    typeof(SerializableGUID),
                    typeof(SerializableHash128)
                },
                UseSimpleDictionaryFormat = true
            };
            var jsonSerializer = new DataContractJsonSerializer(typeof(AddressablesContentState), jsonSettings);

            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, System.Text.Encoding.UTF8, true, true, "  "))
            {
                jsonSerializer.WriteObject(writer, contentState);
            }
        }

        [MenuItem("Window/Asset Management/Addressables/Extract Binary Content State", priority = 2053)]
        private static void ExtractBinaryContentStateMenuCommand()
        {
            var contentStatePath = EditorUtility.OpenFilePanelWithFilters(
                "Select Binary Content State",
                Path.GetDirectoryName(Application.dataPath),
                new string[] { "Content State", "bin" });

            if (string.IsNullOrEmpty(contentStatePath))
                return;

            if (IsLegacyFormat(contentStatePath))
            {
                EditorUtility.DisplayDialog(
                    "Legacy Format Detected",
                    "The selected file is in the legacy BinaryFormatter format. Please convert it first using the migration tool.",
                    "OK");
                return;
            }

            var outputPath = contentStatePath.Replace(".bin", ".extracted.json");
            ExtractToJson(contentStatePath, outputPath);

            Debug.Log($"Content state extracted to: {outputPath}");
            EditorUtility.RevealInFinder(outputPath);
        }
    }

    /// <summary>
    /// Serializable wrapper for Unity's GUID struct.
    /// </summary>
    [DataContract]
    internal struct SerializableGUID
    {
        [DataMember]
        public string Value;

        public SerializableGUID(GUID guid)
        {
            Value = guid.ToString();
        }

        public GUID ToGUID()
        {
            return string.IsNullOrEmpty(Value) ? new GUID() : new GUID(Value);
        }

        public static implicit operator SerializableGUID(GUID guid) => new SerializableGUID(guid);
        public static implicit operator GUID(SerializableGUID serializable) => serializable.ToGUID();
    }

    /// <summary>
    /// Serializable wrapper for Unity's Hash128 struct.
    /// </summary>
    [DataContract]
    internal struct SerializableHash128
    {
        [DataMember]
        public string Value;

        public SerializableHash128(Hash128 hash)
        {
            Value = hash.ToString();
        }

        public Hash128 ToHash128()
        {
            return string.IsNullOrEmpty(Value) ? new Hash128() : Hash128.Parse(Value);
        }

        public static implicit operator SerializableHash128(Hash128 hash) => new SerializableHash128(hash);
        public static implicit operator Hash128(SerializableHash128 serializable) => serializable.ToHash128();
    }

    /// <summary>
    /// Data contract surrogate to handle Unity types during serialization.
    /// </summary>
    internal class ContentStateSurrogate : IDataContractSurrogate
    {
        public Type GetDataContractType(Type type)
        {
            if (type == typeof(GUID))
                return typeof(SerializableGUID);
            if (type == typeof(Hash128))
                return typeof(SerializableHash128);
            return type;
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (targetType == typeof(GUID) && obj is SerializableGUID serializableGuid)
                return serializableGuid.ToGUID();
            if (targetType == typeof(Hash128) && obj is SerializableHash128 serializableHash)
                return serializableHash.ToHash128();
            return obj;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            if (obj is GUID guid)
                return new SerializableGUID(guid);
            if (obj is Hash128 hash)
                return new SerializableHash128(hash);
            return obj;
        }

        public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
        {
            return null;
        }

        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            return null;
        }

        public void GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<Type> customDataTypes)
        {
        }

        public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            return null;
        }

        public System.CodeDom.CodeTypeDeclaration ProcessImportedType(
            System.CodeDom.CodeTypeDeclaration typeDeclaration,
            System.CodeDom.CodeCompileUnit compileUnit)
        {
            return typeDeclaration;
        }
    }
}
#endif
