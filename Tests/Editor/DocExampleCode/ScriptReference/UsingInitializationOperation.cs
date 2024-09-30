namespace AddressableAssets.DocExampleCode
{
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.AsyncOperations;

    internal class UsingInitializationOperation
    {
        #region DECLARATION
        public static AsyncOperationHandle<IResourceLocator> InitializationOperation { get; }
        #endregion
    }
}
