#if UNITY_EDITOR
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Build.Pipeline;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Test-only <see cref="BuildScriptPackedMode"/> that overrides every overridable outer hook (15 instance methods
    /// declared on <see cref="BuildScriptPackedMode"/>) and delegates to <see langword="base"/> using the same
    /// non-reentrant patterns as the stock implementation.
    /// </summary>
    /// <remarks>
    /// <para><b>Hooks recorded in <see cref="InvokedHooks"/>:</b>
    /// <see cref="CreateSchemaDrivenBuildScript"/>, <see cref="CanBuildData{TResult}"/>, <see cref="DoBuild{TResult}"/>,
    /// <see cref="BuildScriptBase.BuildDataImplementation{TResult}"/> (override), <see cref="BuildScriptBase.ProcessAllGroups"/>,
    /// <see cref="BuildScriptBase.ProcessGroup"/>, <see cref="ProcessGroupSchema"/>, <see cref="ClearCachedData"/>,
    /// <see cref="IsDataBuilt"/>, <see cref="BuildScriptBase.NotifyUserAboutBuildReport"/>, <see cref="BuildScriptBase.DisplayBuildReport"/>,
    /// <see cref="BuildScriptBase.ClearContentUpdateNotifications"/>, <see cref="BuildScriptBase.CopyAndRegisterContentState"/>,
    /// <see cref="ProcessBundledAssetSchema"/>, <see cref="ConstructAssetBundleName"/>.</para>
    /// <para><see cref="BuildScriptBase.CopyAndRegisterContentState"/> is typically not invoked for plain editor
    /// <c>AddressableAssetBuildResult</c> builds; do not require it in hook coverage unless testing player/content-update flows.</para>
    /// </remarks>
    public sealed class AllHooksLoggingPackedMode : BuildScriptPackedMode
    {
        public static readonly HashSet<string> InvokedHooks = new HashSet<string>();

        public static void ClearInvocationRecord() => InvokedHooks.Clear();

        static void Record([CallerMemberName] string name = null)
        {
            if (!string.IsNullOrEmpty(name))
                InvokedHooks.Add(name);
        }

        public override BuildScriptSchemaDriven CreateSchemaDrivenBuildScript()
        {
            Record();
            return base.CreateSchemaDrivenBuildScript();
        }

        public override bool CanBuildData<T>()
        {
            Record();
            return base.CanBuildData<T>();
        }

        protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            Record();
            return base.DoBuild<TResult>(builderInput, aaContext);
        }

        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            Record();
            return base.BuildDataImplementation<TResult>(builderInput);
        }

        protected override string ProcessAllGroups(AddressableAssetsBuildContext aaContext)
        {
            Record();
            return base.ProcessAllGroups(aaContext);
        }

        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            Record();
            return base.ProcessGroup(assetGroup, aaContext);
        }

        protected override string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            Record();
            return base.ProcessGroupSchema(schema, assetGroup, aaContext);
        }

        public override void ClearCachedData()
        {
            Record();
            base.ClearCachedData();
        }

        public override bool IsDataBuilt()
        {
            Record();
            return base.IsDataBuilt();
        }

        protected override void NotifyUserAboutBuildReport()
        {
            Record();
            base.NotifyUserAboutBuildReport();
        }

        protected override void DisplayBuildReport()
        {
            Record();
            base.DisplayBuildReport();
        }

        protected override void ClearContentUpdateNotifications(List<AddressableAssetGroup> groups)
        {
            Record();
            base.ClearContentUpdateNotifications(groups);
        }

        public override void CopyAndRegisterContentState(string tempPath, string contentStatePath, AddressablesDataBuilderInput builderInput, AddressablesPlayerBuildResult addrResult)
        {
            Record();
            base.CopyAndRegisterContentState(tempPath, contentStatePath, builderInput, addrResult);
        }

        protected override string ProcessBundledAssetSchema(BundledAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            Record();
            return base.ProcessBundledAssetSchema(schema, assetGroup, aaContext);
        }

        protected override string ConstructAssetBundleName(AddressableAssetGroup assetGroup, BundledAssetGroupSchema schema, BundleDetails info, string assetBundleName)
        {
            Record();
            return base.ConstructAssetBundleName(assetGroup, schema, info, assetBundleName);
        }
    }
}
#endif
