namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    internal class UsingLogWarningFormat
    {
        public void UsingLogWarningFormatSample()
		{
            #region SAMPLE
            Addressables.LogWarningFormat("{0:o}[{1}]Operation took longer than 1 minute", DateTime.Now, this.GetType().Name);
            #endregion

        }
        
    }
}
