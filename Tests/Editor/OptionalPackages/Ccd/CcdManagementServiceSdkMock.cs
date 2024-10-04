#if ENABLE_MOQ
using Moq;
using Moq.Language.Flow;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
#if ENABLE_CCD
using Unity.Services.Ccd.Management;
#endif
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.OptionalPackages.Ccd
{
    public class CcdManagementServiceSdkMock
    {
#if (ENABLE_CCD && ENABLE_MOQ)
        private Mock<ICcdManagementServiceSdk> mock;

        public CcdManagementServiceSdkMock()
        {
            mock = new Mock<ICcdManagementServiceSdk>(MockBehavior.Strict);
        }

        public void Init()
        {
#endif
#if (ENABLE_CCD && !CCD_2_2_0_OR_NEWER && ENABLE_MOQ)
            var serviceField = typeof(CcdManagement).GetField("service", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            // save off our http mocked service
            ICcdManagementServiceSdk httpClientMockedService = (ICcdManagementServiceSdk)serviceField.GetValue(null);
            serviceField.SetValue(null, mock.Object);
#elif (ENABLE_CCD && ENABLE_MOQ)
        var instanceProperty = typeof(CcdManagement).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var ccdManagementInstance = instanceProperty.GetValue(null);

        var serviceField = typeof(CcdManagement).GetField("service", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        serviceField.SetValue(ccdManagementInstance, mock.Object);
#endif
#if (ENABLE_CCD && ENABLE_MOQ)
        }

        public void VerifyAll()
        {
            mock.VerifyAll();
        }

        public ISetup<ICcdManagementServiceSdk, TResult> Setup<TResult>(Expression<Func<ICcdManagementServiceSdk, TResult>> expression)
        {
            return mock.Setup(expression);
        }

        public ISetup<ICcdManagementServiceSdk> Setup(Expression<Action<ICcdManagementServiceSdk>> expression)
        {
            return mock.Setup(expression);
        }
#endif
    }
}
