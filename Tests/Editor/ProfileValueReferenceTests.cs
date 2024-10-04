using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public class ProfileValueReferenceTests : AddressableAssetTestBase
    {
        [Test]
        public void IsValueValid()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            var pid = Settings.profileSettings.GetProfileDataById(schema.BuildPath.Id);
            Assert.IsNotNull(pid);
            var varVal = Settings.profileSettings.GetValueById(Settings.activeProfileId, pid.Id);
            Assert.IsNotNull(varVal);
            var evalVal = Settings.profileSettings.EvaluateString(Settings.activeProfileId, varVal);
            var val = schema.BuildPath.GetValue(Settings);
            Assert.AreEqual(evalVal, val);
        }

        [Test]
        public void CanSetValueByName()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(schema.BuildPath.Id, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
        }

        [Test]
        public void CanSetValueById()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            schema.BuildPath.SetVariableById(Settings, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
            Assert.AreEqual(schema.BuildPath.Id, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
        }

        [Test]
        public void CallbackInvokedWhenValueChanged()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public void CallbackNotInvokedWhenValueNotChanged()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableById(Settings, "invalid id");
            Assert.IsFalse(callbackInvoked);
        }

        [Test]
        public void CanGetValue()
        {
            var value = "[UnityEngine.AddressableAssets.Addressables.LibraryPath]";
            var evalValue = UnityEngine.AddressableAssets.Addressables.LibraryPath;
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, value);
            Assert.AreEqual(schema.BuildPath.Id, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);

            // check we get our expected values for all overloads
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings));
            Assert.AreEqual(value, schema.BuildPath.GetValue(Settings, false));
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings, true));
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings.profileSettings, Settings.activeProfileId));
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings.profileSettings, Settings.activeProfileId, true));
            Assert.AreEqual(value, schema.BuildPath.GetValue(Settings.profileSettings, Settings.activeProfileId, false));
        }

        [Test]
        public void CanGetCustomValue()
        {
            var value = "[UnityEngine.AddressableAssets.Addressables.LibraryPath]";
            var evalValue = UnityEngine.AddressableAssets.Addressables.LibraryPath;
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);

            schema.BuildPath.Id = value;

            // check we get our expected values for all overloads
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings));
            Assert.AreEqual(value, schema.BuildPath.GetValue(Settings, false));
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings, true));
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings.profileSettings, Settings.activeProfileId));
            Assert.AreEqual(evalValue, schema.BuildPath.GetValue(Settings.profileSettings, Settings.activeProfileId, true));
            Assert.AreEqual(value, schema.BuildPath.GetValue(Settings.profileSettings, Settings.activeProfileId, false));
        }

        [Test]
        public void CanGetValueNoSettings()
        {
            var value = "[UnityEngine.AddressableAssets.Addressables.LibraryPath]";
            var evalValue = UnityEngine.AddressableAssets.Addressables.LibraryPath;
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, value);
            Assert.AreEqual(schema.BuildPath.Id, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);

            // check we get null and not exceptions for all overloads
            Assert.AreEqual(null, schema.BuildPath.GetValue(null));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: AddressableAssetSettings object is null.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(null, true));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: AddressableAssetSettings object is null.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(null, false));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: AddressableAssetSettings object is null.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(null, null));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: AddressableAssetProfileSettings object is null.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(Settings.profileSettings, null));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: GetValue called with invalid profileId.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(null, null, true));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: AddressableAssetProfileSettings object is null.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(Settings.profileSettings, null, true));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: GetValue called with invalid profileId.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(null, null, false));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: AddressableAssetProfileSettings object is null.");

            Assert.AreEqual(null, schema.BuildPath.GetValue(Settings.profileSettings, null, false));
            LogAssert.Expect(LogType.Warning, "ProfileValueReference: GetValue called with invalid profileId.");
        }
    }
}
