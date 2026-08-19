using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.Build.Pipeline;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Build scripts used for player builds and running with bundles in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPacked.asset", menuName = "Addressables/Content Builders/Default Build Script")]
    public class BuildScriptPackedMode : BuildScriptBase
    {
        /// <summary>
        /// The extension to use for type tree data files when type tree data extraction is enabled.
        /// </summary>
        public const string kTypeTreeDataExtension = ".typetreedata";
        /// <summary>
        /// The file name to use for type tree data when type tree data extraction is enabled.
        /// This file will be moved to the catalog build path with a hash as the file name during the build.
        /// </summary>
        public const string kTypeTreeDataFileName = "AssetBundle" + kTypeTreeDataExtension;

        /// <inheritdoc />
        public override string Name
        {
            get { return "Default Build Script"; }
        }

        /// <summary>
        /// Schema-driven build script used by <see cref="BuildScriptPackedMode"/>. Forwards selected hooks to the
        /// outer packed script so subclasses can override behavior while the build pipeline keeps using this instance.
        /// </summary>
        public class PackedModeSchemaDriven : BuildScriptSchemaDriven
        {
            /// <summary>
            /// The packed-mode script that owns this instance. Set when <see cref="BuildScriptPackedMode.SchemaDrivenBuildScriptInstance"/> is first accessed.
            /// </summary>
            internal BuildScriptPackedMode m_OuterBuildScript;

            /// <inheritdoc />
            public override string Name
            {
                get { return "Default Build Script"; }
            }

            /// <inheritdoc />
            public override ISchemaBuilder[] CreateSchemaBuilders()
            {
                return new ISchemaBuilder[] {
                    new BundledAssetSchemaBuilder(),
                };
            }

            /// <inheritdoc />
            protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
            {
                if (m_OuterBuildScript != null)
                    return m_OuterBuildScript.BuildDataImplementation<TResult>(builderInput);
                return base.BuildDataImplementation<TResult>(builderInput);
            }

            /// <inheritdoc />
            protected override string ProcessAllGroups(AddressableAssetsBuildContext aaContext)
            {
                if (m_OuterBuildScript != null)
                    return m_OuterBuildScript.ProcessAllGroups(aaContext);
                return base.ProcessAllGroups(aaContext);
            }

            /// <inheritdoc />
            protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
            {
                if (m_OuterBuildScript != null)
                    return m_OuterBuildScript.ProcessGroup(assetGroup, aaContext);
                return base.ProcessGroup(assetGroup, aaContext);
            }

            /// <inheritdoc />
            protected override string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
            {
                if (m_OuterBuildScript != null)
                    return m_OuterBuildScript.ProcessGroupSchema(schema, assetGroup, aaContext);
                return base.ProcessGroupSchema(schema, assetGroup, aaContext);
            }

            /// <inheritdoc />
            protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
            {
                if (m_OuterBuildScript != null)
                    return m_OuterBuildScript.DoBuild<TResult>(builderInput, aaContext);
                return base.DoBuild<TResult>(builderInput, aaContext);
            }

            /// <inheritdoc />
            protected override void NotifyUserAboutBuildReport()
            {
                if (m_OuterBuildScript != null)
                    m_OuterBuildScript.NotifyUserAboutBuildReport();
                else
                    base.NotifyUserAboutBuildReport();
            }

            /// <inheritdoc />
            protected override void DisplayBuildReport()
            {
                if (m_OuterBuildScript != null)
                    m_OuterBuildScript.DisplayBuildReport();
                else
                    base.DisplayBuildReport();
            }

            /// <inheritdoc />
            protected override void ClearContentUpdateNotifications(List<AddressableAssetGroup> groups)
            {
                if (m_OuterBuildScript != null)
                    m_OuterBuildScript.ClearContentUpdateNotifications(groups);
                else
                    base.ClearContentUpdateNotifications(groups);
            }

            /// <inheritdoc />
            public override void CopyAndRegisterContentState(string tempPath, string contentStatePath, AddressablesDataBuilderInput builderInput, AddressablesPlayerBuildResult addrResult)
            {
                if (m_OuterBuildScript != null)
                    m_OuterBuildScript.CopyAndRegisterContentState(tempPath, contentStatePath, builderInput, addrResult);
                else
                    base.CopyAndRegisterContentState(tempPath, contentStatePath, builderInput, addrResult);
            }

            /// <inheritdoc />
            protected override string ProcessBundledAssetSchema(
                BundledAssetGroupSchema schema,
                AddressableAssetGroup assetGroup,
                AddressableAssetsBuildContext aaContext)
            {
                if (m_OuterBuildScript != null)
                    return m_OuterBuildScript.ProcessBundledAssetSchema(schema, assetGroup, aaContext);
                return base.ProcessBundledAssetSchema(schema, assetGroup, aaContext);
            }

            /// <summary>
            /// Routes bundle naming to <see cref="BuildScriptPackedMode.ConstructAssetBundleName"/> so overrides on
            /// the packed script run from <see cref="BuildScriptSchemaDriven.ConstructOutputName"/> during the build.
            /// </summary>
            /// <param name="assetGroup">Group being built, if any.</param>
            /// <param name="schema">Bundled asset schema controlling naming.</param>
            /// <param name="info">Bundle details including hash.</param>
            /// <param name="assetBundleName">Base bundle name before group prefix and hashing.</param>
            /// <returns>Final bundle name for the build output.</returns>
            protected override string ConstructAssetBundleName(
                AddressableAssetGroup assetGroup,
                BundledAssetGroupSchema schema,
                BundleDetails info,
                string assetBundleName)
            {
                if (m_OuterBuildScript != null)
                    return m_OuterBuildScript.ConstructAssetBundleName(assetGroup, schema, info, assetBundleName);
                return base.ConstructAssetBundleName(assetGroup, schema, info, assetBundleName);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptSchemaDriven.BuildDataImplementation{TResult}"/> without re-entering the nested
            /// <see cref="BuildDataImplementation{TResult}"/> override. Used by the outer default to avoid infinite recursion.
            /// </summary>
            internal TResult InvokeBaseBuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput) where TResult : IDataBuilderResult
            {
                return base.BuildDataImplementation<TResult>(builderInput);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptBase.ProcessAllGroups"/> for this nested instance without forwarding to the outer script again.
            /// </summary>
            internal string InvokeBaseProcessAllGroups(AddressableAssetsBuildContext aaContext)
            {
                return base.ProcessAllGroups(aaContext);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptSchemaDriven.ProcessGroup"/> without forwarding to the outer script again.
            /// </summary>
            internal string InvokeBaseProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
            {
                return base.ProcessGroup(assetGroup, aaContext);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptSchemaDriven.ProcessGroupSchema"/> without forwarding to the outer script again.
            /// </summary>
            internal string InvokeBaseProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
            {
                return base.ProcessGroupSchema(schema, assetGroup, aaContext);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptSchemaDriven.DoBuild{TResult}"/> without forwarding to the outer script again.
            /// </summary>
            internal TResult InvokeBaseDoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) where TResult : IDataBuilderResult
            {
                return base.DoBuild<TResult>(builderInput, aaContext);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptBase.NotifyUserAboutBuildReport"/> without forwarding to the outer script again.
            /// </summary>
            internal void InvokeBaseNotifyUserAboutBuildReport()
            {
                base.NotifyUserAboutBuildReport();
            }

            /// <summary>
            /// Runs <see cref="BuildScriptBase.DisplayBuildReport"/> without forwarding to the outer script again.
            /// </summary>
            internal void InvokeBaseDisplayBuildReport()
            {
                base.DisplayBuildReport();
            }

            /// <summary>
            /// Runs <see cref="BuildScriptBase.ClearContentUpdateNotifications"/> without forwarding to the outer script again.
            /// </summary>
            internal void InvokeBaseClearContentUpdateNotifications(List<AddressableAssetGroup> groups)
            {
                base.ClearContentUpdateNotifications(groups);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptBase.CopyAndRegisterContentState"/> without forwarding to the outer script again.
            /// </summary>
            internal void InvokeBaseCopyAndRegisterContentState(string tempPath, string contentStatePath, AddressablesDataBuilderInput builderInput, AddressablesPlayerBuildResult addrResult)
            {
                base.CopyAndRegisterContentState(tempPath, contentStatePath, builderInput, addrResult);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptSchemaDriven.ProcessBundledAssetSchema"/> directly on the base class,
            /// bypassing this type's override. Used by the outer
            /// <see cref="BuildScriptPackedMode.ProcessBundledAssetSchema"/> to avoid infinite recursion.
            /// </summary>
            internal string InvokeBaseProcessBundledAssetSchema(
                BundledAssetGroupSchema schema,
                AddressableAssetGroup assetGroup,
                AddressableAssetsBuildContext aaContext)
            {
                return base.ProcessBundledAssetSchema(schema, assetGroup, aaContext);
            }

            /// <summary>
            /// Runs <see cref="BuildScriptSchemaDriven.ConstructAssetBundleName"/> without calling this type's
            /// <see cref="ConstructAssetBundleName"/> override again. Used by the outer default
            /// <see cref="BuildScriptPackedMode.ConstructAssetBundleName"/> to avoid infinite recursion.
            /// </summary>
            internal string InvokeBaseConstructAssetBundleName(
                AddressableAssetGroup assetGroup,
                BundledAssetGroupSchema schema,
                BundleDetails info,
                string assetBundleName)
            {
                return base.ConstructAssetBundleName(assetGroup, schema, info, assetBundleName);
            }
        }

        [NonSerialized]
        private BuildScriptSchemaDriven m_SchemaDrivenBuildScriptInstance;

        /// <summary>
        /// Lazily created schema-driven script that performs the build. For default packed mode this is a
        /// <see cref="PackedModeSchemaDriven"/> with <see cref="PackedModeSchemaDriven.m_OuterBuildScript"/> set to this instance.
        /// </summary>
        internal BuildScriptSchemaDriven SchemaDrivenBuildScriptInstance
        {
            get
            {
                if (m_SchemaDrivenBuildScriptInstance == null)
                {
                    m_SchemaDrivenBuildScriptInstance = CreateSchemaDrivenBuildScript();
                    if (m_SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                        packed.m_OuterBuildScript = this;
                }

                return m_SchemaDrivenBuildScriptInstance;
            }
        }

        /// <summary>
        /// Creates the <see cref="BuildScriptSchemaDriven"/> used by <see cref="SchemaDrivenBuildScriptInstance"/>.
        /// Override to plug a custom schema-driven implementation while keeping packed-mode forwarding behavior.
        /// </summary>
        /// <returns>A new schema-driven build script instance (typically <see cref="PackedModeSchemaDriven"/>).</returns>
        public virtual BuildScriptSchemaDriven CreateSchemaDrivenBuildScript()
        {
            return CreateInstance<PackedModeSchemaDriven>();
        }

        /// <summary>
        /// Destroys the lazily created <see cref="SchemaDrivenBuildScriptInstance"/> when this
        /// <see cref="ScriptableObject"/> is disabled or unloaded.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (m_SchemaDrivenBuildScriptInstance != null)
            {
                DestroyImmediate(m_SchemaDrivenBuildScriptInstance);
                m_SchemaDrivenBuildScriptInstance = null;
            }
        }

        /// <inheritdoc />
        public override bool CanBuildData<T>()
        {
            return SchemaDrivenBuildScriptInstance.CanBuildData<T>();
        }

        /// <summary>
        /// Performs the build after groups have been processed. The default runs the schema-driven implementation via
        /// <see cref="PackedModeSchemaDriven.InvokeBaseDoBuild{TResult}"/> when using <see cref="PackedModeSchemaDriven"/>.
        /// </summary>
        /// <remarks>
        /// Do not call <see cref="BuildScriptSchemaDriven.GetDoBuildCallback{TResult}"/> from overrides; use
        /// <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseDoBuild{TResult}"/> to run stock logic without re-entrancy.
        /// </remarks>
        /// <typeparam name="TResult">The type of <see cref="IDataBuilderResult"/> to produce.</typeparam>
        /// <param name="builderInput">Input describing how to run the build.</param>
        /// <param name="aaContext">Addressables build context populated during group processing.</param>
        /// <returns>The build result instance.</returns>
        protected virtual TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) where TResult : IDataBuilderResult
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                return packed.InvokeBaseDoBuild<TResult>(builderInput, aaContext);
            return SchemaDrivenBuildScriptInstance.GetDoBuildCallback<TResult>()(builderInput, aaContext);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Do not call <see cref="BuildScriptSchemaDriven.GetBuildDataImplementationCallback{TResult}"/> from overrides; use
        /// <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseBuildDataImplementation{TResult}"/> to avoid re-entrancy.
        /// </remarks>
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                return packed.InvokeBaseBuildDataImplementation<TResult>(builderInput);
            return SchemaDrivenBuildScriptInstance.GetBuildDataImplementationCallback<TResult>()(builderInput);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Do not call <see cref="BuildScriptSchemaDriven.GetProcessAllGroupsCallback"/> from overrides; use
        /// <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseProcessAllGroups"/> to avoid re-entrancy.
        /// </remarks>
        protected override string ProcessAllGroups(AddressableAssetsBuildContext aaContext)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                return packed.InvokeBaseProcessAllGroups(aaContext);
            return SchemaDrivenBuildScriptInstance.GetProcessAllGroupsCallback()(aaContext);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Do not call <see cref="BuildScriptSchemaDriven.GetProcessGroupCallback"/> from overrides; use
        /// <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseProcessGroup"/> to avoid re-entrancy.
        /// </remarks>
        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                return packed.InvokeBaseProcessGroup(assetGroup, aaContext);
            return SchemaDrivenBuildScriptInstance.GetProcessGroupCallback()(assetGroup, aaContext);
        }

        /// <summary>
        /// Called once per enabled schema on a group. For behavior details see <see cref="BuildScriptSchemaDriven.ProcessGroupSchema"/>.
        /// </summary>
        /// <remarks>
        /// Do not call <see cref="BuildScriptSchemaDriven.GetProcessGroupSchemaCallback"/> from overrides; use
        /// <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseProcessGroupSchema"/> to avoid re-entrancy.
        /// </remarks>
        /// <param name="schema">The schema to evaluate.</param>
        /// <param name="assetGroup">The group that owns the schema.</param>
        /// <param name="aaContext">The Addressables build context.</param>
        /// <returns>An error message if validation or processing failed; otherwise an empty string.</returns>
        protected virtual string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                return packed.InvokeBaseProcessGroupSchema(schema, assetGroup, aaContext);
            return SchemaDrivenBuildScriptInstance.GetProcessGroupSchemaCallback()(schema, assetGroup, aaContext);
        }

        /// <summary>
        /// Compatibility wrapper for <see cref="BuildScriptSchemaDriven.PrepGroupBundlePacking"/>. See that method for parameter and return semantics.
        /// </summary>
        /// <param name="assetGroup">The group to pack.</param>
        /// <param name="bundleInputDefs">Bundle definitions to append to for the build pipeline.</param>
        /// <param name="schema">Bundled asset schema controlling packing mode.</param>
        /// <param name="entryFilter">Optional filter excluding entries from packing.</param>
        /// <returns>All entries that were gathered for packing.</returns>
        public static List<AddressableAssetEntry> PrepGroupBundlePacking(AddressableAssetGroup assetGroup, List<AssetBundleBuild> bundleInputDefs, BundledAssetGroupSchema schema,
            Func<AddressableAssetEntry, bool> entryFilter = null)
        {
            return BuildScriptSchemaDriven.PrepGroupBundlePacking(assetGroup, bundleInputDefs, schema, entryFilter);
        }

        /// <inheritdoc />
        public override void ClearCachedData()
        {
            SchemaDrivenBuildScriptInstance.ClearCachedData();
        }

        /// <inheritdoc />
        public override bool IsDataBuilt()
        {
            return SchemaDrivenBuildScriptInstance.IsDataBuilt();
        }

        /// <inheritdoc />
        /// <remarks>
        /// Do not dispatch through <see cref="SchemaDrivenBuildScriptInstance"/> from overrides in a way that re-enters this method;
        /// use <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseNotifyUserAboutBuildReport"/>.
        /// </remarks>
        protected override void NotifyUserAboutBuildReport()
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                packed.InvokeBaseNotifyUserAboutBuildReport();
            else
                base.NotifyUserAboutBuildReport();
        }

        /// <inheritdoc />
        /// <remarks>
        /// Do not dispatch through <see cref="SchemaDrivenBuildScriptInstance"/> from overrides in a way that re-enters this method;
        /// use <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseDisplayBuildReport"/>.
        /// </remarks>
        protected override void DisplayBuildReport()
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                packed.InvokeBaseDisplayBuildReport();
            else
                base.DisplayBuildReport();
        }

        /// <inheritdoc />
        /// <remarks>
        /// Do not dispatch through <see cref="SchemaDrivenBuildScriptInstance"/> from overrides in a way that re-enters this method;
        /// use <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseClearContentUpdateNotifications"/>.
        /// </remarks>
        protected override void ClearContentUpdateNotifications(List<AddressableAssetGroup> groups)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                packed.InvokeBaseClearContentUpdateNotifications(groups);
            else
                base.ClearContentUpdateNotifications(groups);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Do not dispatch through <see cref="SchemaDrivenBuildScriptInstance"/> from overrides in a way that re-enters this method;
        /// use <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseCopyAndRegisterContentState"/>.
        /// </remarks>
        public override void CopyAndRegisterContentState(string tempPath, string contentStatePath, AddressablesDataBuilderInput builderInput, AddressablesPlayerBuildResult addrResult)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                packed.InvokeBaseCopyAndRegisterContentState(tempPath, contentStatePath, builderInput, addrResult);
            else
                SchemaDrivenBuildScriptInstance.CopyAndRegisterContentState(tempPath, contentStatePath, builderInput, addrResult);
        }

        /// <summary>
        /// Extension point for bundled-asset processing. Called by <see cref="PackedModeSchemaDriven.ProcessBundledAssetSchema"/>
        /// when the build pipeline runs on a <see cref="PackedModeSchemaDriven"/> instance. The default implementation
        /// runs the standard <see cref="BuildScriptSchemaDriven.ProcessBundledAssetSchema"/> logic without recursion.
        /// </summary>
        /// <remarks>
        /// Use <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseProcessBundledAssetSchema"/> from
        /// overrides to invoke the standard logic without re-entrancy.
        /// </remarks>
        /// <param name="schema">Bundled asset schema for the group.</param>
        /// <param name="assetGroup">Group being processed.</param>
        /// <param name="aaContext">Build context.</param>
        /// <returns>Error message if processing failed; otherwise empty.</returns>
        protected virtual string ProcessBundledAssetSchema(
            BundledAssetGroupSchema schema,
            AddressableAssetGroup assetGroup,
            AddressableAssetsBuildContext aaContext)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                return packed.InvokeBaseProcessBundledAssetSchema(schema, assetGroup, aaContext);
            return SchemaDrivenBuildScriptInstance.GetProcessBundledAssetSchemaCallback()(schema, assetGroup, aaContext);
        }

        /// <summary>
        /// Extension point for asset bundle file naming. Invoked from <see cref="PackedModeSchemaDriven.ConstructAssetBundleName"/>
        /// when the build pipeline resolves bundle names. The default implementation runs the standard
        /// <see cref="BuildScriptSchemaDriven.ConstructAssetBundleName"/> logic without recursion.
        /// </summary>
        /// <remarks>
        /// Do not call <see cref="BuildScriptSchemaDriven.GetConstructAssetBundleNameCallback"/> from overrides; use
        /// <see langword="base"/> or <see cref="PackedModeSchemaDriven.InvokeBaseConstructAssetBundleName"/> to avoid re-entrancy.
        /// </remarks>
        /// <param name="assetGroup">Group being built, if any.</param>
        /// <param name="schema">Bundled asset schema controlling naming.</param>
        /// <param name="info">Bundle details including hash.</param>
        /// <param name="assetBundleName">Base bundle name before group prefix and hashing.</param>
        /// <returns>Final bundle name for the build output.</returns>
        protected virtual string ConstructAssetBundleName(AddressableAssetGroup assetGroup, BundledAssetGroupSchema schema, BundleDetails info, string assetBundleName)
        {
            if (SchemaDrivenBuildScriptInstance is PackedModeSchemaDriven packed)
                return packed.InvokeBaseConstructAssetBundleName(assetGroup, schema, info, assetBundleName);
            return SchemaDrivenBuildScriptInstance.GetConstructAssetBundleNameCallback()(assetGroup, schema, info, assetBundleName);
        }
    }
}