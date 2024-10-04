namespace AddressableAssets.DocExampleCode
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.AsyncOperations;

    internal class UsingGetLocatorInfo
    {
        #region DECLARATION_1
        public static ResourceLocatorInfo GetLocatorInfo(string locatorId)
        #endregion
        {
            return default;
        }

        #region DECLARATION_2
        public static ResourceLocatorInfo GetLocatorInfo(IResourceLocator locator)
        #endregion
        {
            return default;
        }

        #region SAMPLE_1
        IEnumerator UsingGetLocatorInfoSampleId()
        {
            yield return Addressables.InitializeAsync();
            IEnumerable<IResourceLocator> resourceLocators = Addressables.ResourceLocators;
            foreach (IResourceLocator locator in resourceLocators)
            {
                // Call GetLocatorInfo using the locator id
                ResourceLocatorInfo locatorInfo = Addressables.GetLocatorInfo(locator.LocatorId);
                if (locatorInfo != null && locatorInfo.CatalogLocation != null)
                {
                    if (locatorInfo.CanUpdateContent)
                        Debug.Log($"Locator {locator.LocatorId} was loaded from an UPDATABLE catalog with internal id : {locatorInfo.CatalogLocation.InternalId}");
                    else
                        Debug.Log($"Locator {locator.LocatorId} was loaded from an NON-UPDATABLE catalog with internal id : {locatorInfo.CatalogLocation.InternalId}");
                }
                else
                {
                    Debug.Log($"Locator {locator.LocatorId} is not associated with a catalog");
                }
            }
        }

        #endregion

        #region SAMPLE_2
        IEnumerator UsingGetLocatorInfoSampleLocator()
        {
            yield return Addressables.InitializeAsync();
            IEnumerable<IResourceLocator> resourceLocators = Addressables.ResourceLocators;
            foreach (IResourceLocator locator in resourceLocators)
            {
                // Call GetLocatorInfo using the locator object
                ResourceLocatorInfo locatorInfo = Addressables.GetLocatorInfo(locator);
                if (locatorInfo != null && locatorInfo.CatalogLocation != null)
                {
                    if (locatorInfo.CanUpdateContent)
                        Debug.Log($"Locator {locator.LocatorId} was loaded from an UPDATABLE catalog with internal id : {locatorInfo.CatalogLocation.InternalId}");
                    else
                        Debug.Log($"Locator {locator.LocatorId} was loaded from an NON-UPDATABLE catalog with internal id : {locatorInfo.CatalogLocation.InternalId}");
                }
                else
                {
                    Debug.Log($"Locator {locator.LocatorId} is not associated with a catalog");
                }
            }
        }

        #endregion
    }
}
