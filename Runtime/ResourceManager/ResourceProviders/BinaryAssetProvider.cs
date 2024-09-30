using System;
using System.ComponentModel;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Converts ray binary data into the requested object.
    /// </summary>
    [DisplayName("Binary Asset Provider")]
    internal class BinaryAssetProvider<TAdapter> : BinaryDataProvider where TAdapter : BinaryStorageBuffer.ISerializationAdapter, new()
    {
        /// <summary>
        /// Converts raw bytes into requested object type via BinaryStorageBuffer.ISerializationAdapter
        /// </summary>
        /// <param name="type">The object type the text is converted to.</param>
        /// <param name="data">The data to convert.</param>
        /// <returns>Returns the converted object.</returns>
        public override object Convert(Type type, byte[] data)
        {
            return new BinaryStorageBuffer.Reader(data, 512, new TAdapter()).ReadObject(type, 0, false);
        }
    }
}
