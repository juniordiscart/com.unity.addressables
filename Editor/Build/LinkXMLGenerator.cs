using System;
using System.Collections.Generic;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// This can be used to create a LinkXml for your build.  This will ensure that the desired runtime types are packed into the build.
    /// </summary>
    [Obsolete("UnityEditor.AddressableAssets.Build.LinkXmlGenerator is obsolete. Use UnityEditor.Build.Pipeline.Utilities.LinkXmlGenerator instead.")]
    public class LinkXmlGenerator : UnityEditor.Build.Pipeline.Utilities.LinkXmlGenerator
    {
#pragma warning disable CA1061 // Do not hide base class methods
        /// <summary>
        /// Add types to the LinkXML
        /// </summary>
        /// <param name="types">Types to add</param>
        public new void AddTypes(params Type[] types)
        {
            base.AddTypes(types);
        }

        /// <summary>
        /// Add types to the LinkXML
        /// </summary>
        /// <param name="types">Types to add</param>
        public new void AddTypes(IEnumerable<Type> types)
        {
            base.AddTypes(types);
        }

        /// <summary>
        /// Set Editor -> Runtime type mapping for player build
        /// </summary>
        /// <param name="a">Type to convert from</param>
        /// <param name="b">Type to convert to</param>
        public new void SetTypeConversion(Type a, Type b)
        {
            base.SetTypeConversion(a, b);
        }

        /// <summary>
        /// Save LinkXML
        /// </summary>
        /// <param name="path">Path to save the LinkXML</param>
        public new void Save(string path)
        {
            base.Save(path);
        }
#pragma warning restore CA1061 // Do not hide base class methods
    }
}
