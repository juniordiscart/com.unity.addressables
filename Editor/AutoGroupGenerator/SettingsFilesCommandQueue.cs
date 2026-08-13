using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that ensures required settings assets exist and are configured.
    /// </summary>
    internal class SettingsFilesCommandQueue : CommandQueue
    {
        #region Constants
        private const string DefaultSettingsPath = "Assets/AddressableAssetsData";

        private const string DefaultSettingsName = "AddressableAssetSettings";

        private const string DefaultAutoGroupGeneratorSettingsFolder = "Assets/AddressableAssetsData/AutoGroupGenerator/";
        #endregion

        #region Static Methods
        public static void CreateAddressableSettingsIfRequired()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                CreateDefaultAddressableSettings();
            }
        }

        private static void CreateDefaultAddressableSettings()
        {
            EnsureDirectoryExists(DefaultSettingsPath);

            AddressableAssetSettings settings = AddressableAssetSettings.Create(DefaultSettingsPath, DefaultSettingsName, true, true);

            AddressableAssetSettingsDefaultObject.Settings = settings;

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
        }

        public static AutoGroupGeneratorSettings CreateDefaultToolSettingsAtPath(string settingsFilePath)
        {
            string directory = Path.GetDirectoryName(settingsFilePath);

            AutoGroupGeneratorSettings settings = null;

            try
            {
                AssetDatabase.StartAssetEditing();

                settings = ScriptableObject.CreateInstance<AutoGroupGeneratorSettings>();

                settings.InputRules = new List<InputRule>
                {
                    CreateDefaultInputRule(directory)
                };

                settings.OutputRules = new List<OutputRule>
                {
                    CreateDefaultOutputRule(directory)
                };

                AssetDatabase.CreateAsset(settings, settingsFilePath);

                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();

                AssetDatabase.Refresh();
            }

            return settings;
        }

        private static InputRule CreateDefaultInputRule(string directoryPath)
        {
            var inputRulePath = Path.Combine(directoryPath, $"Default{nameof(InputRule)}.asset");

            var inputRule = ScriptableObject.CreateInstance<AssetSelectionInputRule>();

            inputRule.m_IncludeCurrentAddressables = true;

            AssetDatabase.CreateAsset(inputRule, inputRulePath);

            AssetDatabase.SaveAssets();

            return inputRule;
        }

        private static OutputRule CreateDefaultOutputRule(string directoryPath)
        {
            var outputRulePath = Path.Combine(directoryPath, $"{nameof(DefaultOutputRule)}.asset");

            var outputRule = ScriptableObject.CreateInstance<DefaultOutputRule>();

            AssetDatabase.CreateAsset(outputRule, outputRulePath);

            AssetDatabase.SaveAssets();

            return outputRule;
        }

        #region Utils
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static bool ToolSettingsExists()
        {
            return AssetDatabaseUtil.FindAssetGuidsForType<AutoGroupGeneratorSettings>().Length > 0;
        }
        #endregion
        #endregion

        #region Fields
        private readonly DataContainer m_DataContainer;
        #endregion

        #region Methods
        public SettingsFilesCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(SettingsFilesCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();

            AddCommand(CreateAddressableSettingsIfRequired, nameof(CreateAddressableSettingsIfRequired));

            AddCommand(FindOrCreateDefaultToolSettings, nameof(FindOrCreateDefaultToolSettings));

            AddCommand(() => m_DataContainer.Settings.Validate(), "Validate Settings");
        }

        private void FindOrCreateDefaultToolSettings()
        {
            if (m_DataContainer.Settings != null)
            {
                return;
            }


            if (ToolSettingsExists())
            {
                throw new Exception($"Cannot find AutoGroupGenerator settings file");
            }


            CreateDefaultToolSettings();
        }

        private void CreateDefaultToolSettings()
        {
            EnsureDirectoryExists(DefaultAutoGroupGeneratorSettingsFolder);

            var settingsFilePath = Path.Combine(DefaultAutoGroupGeneratorSettingsFolder, $"Default{nameof(AutoGroupGeneratorSettings)}.asset");

            var settings = CreateDefaultToolSettingsAtPath(settingsFilePath);

            m_DataContainer.SettingsFilePath = settingsFilePath;
            m_DataContainer.Settings = settings;
        }
        #endregion
    }
}
