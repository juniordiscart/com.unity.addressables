namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    internal class UsingLog
    {
	
        public void UsingLogSample()
        {
            #region SAMPLE
            Addressables.Log("Unloading bundle");

            Addressables.Log("<color=red>Unloading bundle</color> ");
            #endregion
        }

    }
}
