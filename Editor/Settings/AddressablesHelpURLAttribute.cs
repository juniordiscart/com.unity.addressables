using System;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// A <see cref="HelpURLAttribute"/> that links to a page in the Addressables package manual,
    /// resolving the documentation URL for the currently installed package version.
    /// Apply this to a type so the Inspector header help (?) button opens the correct manual page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AddressablesHelpURLAttribute : HelpURLAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddressablesHelpURLAttribute"/> class.
        /// </summary>
        /// <param name="page">The manual page file name (for example, "AddressableAssetSettings.html").</param>
        public AddressablesHelpURLAttribute(string page)
            : base(AddressableAssetUtility.GenerateDocsURL(page))
        {
        }
    }
}
