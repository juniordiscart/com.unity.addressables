namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    internal class UsingLogWarning
    {
		public void UsingLogWarningSample()
		{
            #region SAMPLE
            Addressables.LogWarning("Operation took longer than 1 minute");
            #endregion
        }
    }
}
