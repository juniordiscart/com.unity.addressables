namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    internal class UsingLogFormat
    {
        #region SAMPLE
        public void UsingLogFormatSample()
		{
            
            Addressables.LogFormat("{0:o}[{1}]Downloaded bundle", DateTime.Now, this.GetType().Name);
            
        }
        #endregion
    }

}
