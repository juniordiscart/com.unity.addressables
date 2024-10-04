namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    internal class UsingLogErrorFormat
    {

        #region SAMPLE
        public void UsingLogErrorFormatSample()
        {            
            Addressables.LogErrorFormat("{0:o}[{1}]Unloading bundle", DateTime.Now, this.GetType().Name);            
        }
        #endregion

    }
}
