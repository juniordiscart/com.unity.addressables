namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    internal class UsingLogError
    {
        #region SAMPLE
        public void UsingLogErrorSample()
		{            
            Addressables.LogError("Unable to load asset bundle");            
        }
        #endregion

    }
}
