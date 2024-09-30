namespace AddressableAssets.DocExampleCode
{
	using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    internal class UsingLogException
    {

        #region SAMPLE
        public void LogExceptionDebugLogging(bool isErrored)
		{            
            try
            {
                if (isErrored)
                {
                    throw new Exception("Unable to complete task");
                }

            } catch(Exception e)
            {
                Addressables.LogException(e);
            }            
        }
        #endregion

        private Material k_PlaceholderMaterial;
        #region SAMPLE_ASYNC_OP
        public async Task<Material> LogExceptionSuccessfulTask(bool isErrored)
        {
            var loadHandle = Addressables.LoadAssetAsync<Material>("green.material");
            var material = await loadHandle.Task;
            if (loadHandle.Status == AsyncOperationStatus.Failed)
            {
                // something went wrong, log it and return the placeholder material
                Addressables.LogException(loadHandle, loadHandle.OperationException);
                return k_PlaceholderMaterial;
            }
            return material;
        }
        #endregion

    }
}
