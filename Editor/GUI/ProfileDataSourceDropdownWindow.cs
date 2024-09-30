using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.ResourceLocators;

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
using Unity.Services.Ccd.Management.Models;
#endif

namespace UnityEditor.AddressableAssets.GUI
{
    internal class ProfileDataSourceDropdownWindow : PopupWindowContent
    {
        internal AddressableAssetProfileSettings.BuildProfile m_Profile;
        internal Rect m_WindowRect;

        internal enum DropdownState
        {
            None,
            BuiltIn,
            EditorHosted,
            CCD,
            Custom
        };

        internal DropdownState state;
        internal ProfileGroupType m_GroupType;
        internal const float k_Margin = 4;
        internal const float k_MaxHeight = 286;
        internal const float k_MinHeight = 80;
        internal Vector2 scrollPos;

        internal delegate void ValueChangedEventHandler(object sender, DropdownWindowEventArgs e);

        internal event ValueChangedEventHandler ValueChanged;

        internal enum CCDDropdownState
        {
            None,
            General,
            Bucket,
            Badge,
            Environment,
            AutomaticEnvironment
        };

        internal CCDDropdownState CCDState = CCDDropdownState.General;

        //temp variables
        internal List<ProfileGroupType> m_ProfileGroupTypes = new List<ProfileGroupType>();
        internal string m_EnvironmentId;
        internal string m_EnvironmentName;
        internal string m_BucketId;
        internal string m_BucketName;
        internal ProfileGroupType m_Bucket;
        internal bool m_isRefreshingCCDDataSources;

        private ProfileDataSourceSettings m_ProfileDataSource;

        internal ProfileDataSourceSettings dataSourceSettings
        {
            get
            {
                if (m_ProfileDataSource == null)
                    m_ProfileDataSource = ProfileDataSourceSettings.GetSettings();
                return m_ProfileDataSource;
            }
        }

        static GUIStyle dropdownTitleStyle;
        static GUIStyle menuOptionStyle;
        static GUIStyle horizontalBarStyle;

        internal static string externalLinkIcon = EditorGUIUtility.isProSkin ? "d_ScaleTool" : "ScaleTool";
        internal static string nextIcon = EditorGUIUtility.isProSkin ? "d_tab_next" : "tab_next";
        internal static string backIcon = EditorGUIUtility.isProSkin ? "d_tab_prev" : "tab_prev";
        internal static string refreshIcon = EditorGUIUtility.isProSkin ? "d_refresh" : "refresh";
        internal static string infoIcon = EditorGUIUtility.isProSkin ? "d_UnityEditor.InspectorWindow" : "UnityEditor.InspectorWindow";
        internal static OrgData m_Organization;

        List<BaseOption> options = new List<BaseOption>();

        private GUIContent m_BundleLocationsGUIContent = new GUIContent("Bundle Locations", "Where AssetBundles are stored");
        private GUIContent m_CCDBucketsGUIContent = new GUIContent("Cloud Content Delivery Buckets", "Storage buckets for Unity Cloud Content Delivery");

        public ProfileDataSourceDropdownWindow(AddressableAssetProfileSettings.BuildProfile profile, Rect fieldRect, ProfileGroupType groupType)
        {
            m_Profile = profile;
            m_GroupType = groupType;
            m_WindowRect = fieldRect;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(m_WindowRect.width, m_WindowRect.height);
        }

        public async override void OnOpen()
        {
            options.Add(new BuiltInOption());
            options.Add(new EditorHostedOption());
            options.Add(new CCDOption());
            options.Add(new CustomOption());

            var blackTexture = new Texture2D(2, 2);
            blackTexture.SetPixels(new Color[4] {Color.black, Color.black, Color.black, Color.black});
            blackTexture.Apply();

            dropdownTitleStyle = new GUIStyle()
            {
                name = "datasource-dropdown-title",
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 26,
                border = new RectOffset(1, 1, 1, 1),
                normal = new GUIStyleState()
                {
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                }
            };

            menuOptionStyle = new GUIStyle()
            {
                name = "datasource-dropdown-option",
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                fixedHeight = 20,
                padding = new RectOffset(20, 2, 0, 0),
                normal = new GUIStyleState()
                {
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                }
            };

            horizontalBarStyle = new GUIStyle()
            {
                normal =
                {
                    background = blackTexture,
                    scaledBackgrounds = new Texture2D[1] {blackTexture}
                },
                fixedHeight = 1,
                stretchHeight = false
            };

            if (!string.IsNullOrEmpty(CloudProjectSettings.projectId))
            {
                m_Organization = await GetOrgData();
            }

            SyncProfileGroupTypes();
        }

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
        public override async void OnGUI(Rect window)
#else
        public override void OnGUI(Rect window)
#endif
        {
            EditorGUI.BeginDisabledGroup(m_isRefreshingCCDDataSources);
            Event evt = Event.current;
            Rect horizontalBarRect = new Rect(0, 30, 0, 0);
            Rect backButtonRect = new Rect(5, 0, 30, 30);
            Rect refreshButtonRect = new Rect(window.width - 30 + k_Margin, 0, 30, 30);
            switch (state)
            {
                case DropdownState.None:
                    EditorGUILayout.LabelField(m_BundleLocationsGUIContent, dropdownTitleStyle);
                    EditorGUILayout.Space(10);
                    EditorGUI.LabelField(horizontalBarRect, "", new GUIStyle(horizontalBarStyle) {fixedWidth = window.width});
                    //List all options
                    foreach (var option in options)
                    {
                        option.Draw(() =>
                        {
                            state = option.state;
                            switch (option.state)
                            {
                                case DropdownState.BuiltIn:
                                case DropdownState.EditorHosted:
                                    var args = new DropdownWindowEventArgs();
                                    args.GroupType = m_GroupType;
                                    args.Option = option;
                                    args.IsCustom = false;
                                    OnValueChanged(args);
                                    return;
                                case DropdownState.Custom:
                                    var custom = new DropdownWindowEventArgs();
                                    custom.GroupType = m_GroupType;
                                    custom.Option = option;
                                    custom.IsCustom = true;
                                    OnValueChanged(custom);
                                    return;
                                default:
                                    return;
                            }
                        });
                    }

                    return;
                case DropdownState.CCD:
                    var isRefreshing = false;

                    switch (CCDState)
                    {
                        case CCDDropdownState.None:
                            state = DropdownState.None;
                            break;
                        case CCDDropdownState.General:
#if !ENABLE_CCD
                            isRefreshing = DrawCcdDisabledDropdownHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, "Cloud Content Delivery", DropdownState.None,
                                CCDDropdownState.General);
#else
                            isRefreshing =
 await DrawCcdEnabledDropdownHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, "Cloud Content Delivery", DropdownState.None, CCDDropdownState.General);
#endif
                            if (isRefreshing) return;
                            BaseOption.DrawMenuItem("Automatic (set using CcdManager)", nextIcon, () => { CCDState = CCDDropdownState.AutomaticEnvironment; });
                            BaseOption.DrawMenuItem("Specify the Environment, Bucket, and Badge", nextIcon, () => { CCDState = CCDDropdownState.Environment; });
                            break;
#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
                        case CCDDropdownState.AutomaticEnvironment:
                            isRefreshing =
 await DrawCcdEnabledDropdownHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, "Select Environment", DropdownState.CCD, CCDDropdownState.General);
                            if (isRefreshing) return;

                            m_WindowRect.height = dataSourceSettings.environments.Count > 0 ? k_MaxHeight : k_MinHeight;

                            if (dataSourceSettings.environments.Count == 1 && dataSourceSettings.environments[0].name == "production")
                            {
                                string environmentHelpInfo = $"It is recommended to have at least 1 <b>non-production</b> environment when using the Automatic setting.";
                                EditorStyles.helpBox.fontSize = 11;
                                EditorStyles.helpBox.margin = new RectOffset(20, 20, 5, 5);
                                EditorStyles.helpBox.richText = true;
                                EditorGUILayout.HelpBox(environmentHelpInfo, MessageType.Info);
                            }

                            foreach (var env in dataSourceSettings.environments)
                            {
                                BaseOption.DrawMenuItem(env.name, null, () =>
                                {
                                    m_EnvironmentId = env.id;
                                    m_EnvironmentName = env.name;

                                    dataSourceSettings.SetEnvironmentById(AddressableAssetSettingsDefaultObject.Settings.profileSettings, m_Profile.id, env.id);
                                    SetCcdManagedDataState(CcdManagedData.ConfigState.Default);

                                    var args = new DropdownWindowEventArgs();
                                    var groupType = dataSourceSettings.GetGroupTypesByPrefix("Automatic").First();
                                    args.GroupType = m_GroupType;

                                    args.Option = new CCDOption();
                                    args.Option.BuildPath = groupType.GetVariableBySuffix("BuildPath").Value;
                                    args.Option.LoadPath = groupType.GetVariableBySuffix("LoadPath").Value;
                                    args.IsCustom = false;
                                    OnValueChanged(args);
                                    editorWindow.Close();
                                });
                            }
                            CCDOption.DrawCreateEnv();

                            break;
                        case CCDDropdownState.Environment:
                            isRefreshing =
 await DrawCcdEnabledDropdownHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, "Select Environment", DropdownState.CCD, CCDDropdownState.General);
                            if (isRefreshing) return;

                            m_WindowRect.height = dataSourceSettings.environments.Count > 0 ? k_MaxHeight : k_MinHeight;

                            foreach (var env in dataSourceSettings.environments)
                            {
                                BaseOption.DrawMenuItem(env.name, nextIcon, () =>
                                {
                                    m_EnvironmentId = env.id;
                                    m_EnvironmentName = env.name;
                                    CCDState = CCDDropdownState.Bucket;
                                });
                            }
                            CCDOption.DrawCreateEnv();

                            break;
                        case CCDDropdownState.Bucket:
                            isRefreshing =
 await DrawCcdEnabledDropdownHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, $"{m_EnvironmentName} Buckets", DropdownState.CCD, CCDDropdownState.Environment);
                            if (isRefreshing) return;

                            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

                            m_WindowRect.height = m_ProfileGroupTypes.Count > 0 ? k_MaxHeight : k_MinHeight;


                            Dictionary<string, ProfileGroupType> buckets = new Dictionary<string, ProfileGroupType>();
                            m_ProfileGroupTypes.ForEach((groupType) =>
                            {
                                if (groupType.GetVariableBySuffix($"{nameof(ProfileDataSourceSettings.Environment)}{nameof(ProfileDataSourceSettings.Environment.name)}").Value == m_EnvironmentName)
                                {
                                    var parts = groupType.GroupTypePrefix.Split(ProfileGroupType.k_PrefixSeparator);
                                    var bucketId = parts[3];
                                    var bucketName = groupType.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}");
                                    if (!buckets.ContainsKey(bucketId))
                                        buckets.Add(bucketId, groupType);
                                }
                            });

                            CCDOption.DrawBuckets(buckets, m_EnvironmentId,
                                (KeyValuePair<string, ProfileGroupType> bucket) =>
                                {
                                    CCDState = CCDDropdownState.Badge;
                                    m_BucketName = bucket.Value.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}").Value;
                                    m_BucketId = bucket.Key;
                                    m_Bucket = bucket.Value;
                                });
                            EditorGUILayout.EndScrollView();
                            break;
                        case CCDDropdownState.Badge:
                            isRefreshing =
 await DrawCcdEnabledDropdownHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, $"{m_BucketName} Badges", DropdownState.CCD, CCDDropdownState.Bucket);
                            if (isRefreshing) return;

                            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
                            if (bool.Parse(m_Bucket.GetVariableBySuffix("PromoteOnly").Value))
                            {
                                const string promotionOnlyBucketInfo = "Using Build & Release directly to this bucket is not supported, but you can load content.";
                                EditorStyles.helpBox.fontSize = 11;
                                EditorStyles.helpBox.margin = new RectOffset(20, 20, 5, 5);
                                EditorGUILayout.HelpBox(promotionOnlyBucketInfo, MessageType.Info);
                            }
                            var selectedProfileGroupTypes = m_ProfileGroupTypes.Where(groupType =>
                                groupType.GroupTypePrefix.StartsWith(
                                    String.Join(
                                        ProfileGroupType.k_PrefixSeparator.ToString(), new string[] { "CCD", CloudProjectSettings.projectId, m_EnvironmentId, m_BucketId }
                                    ), StringComparison.Ordinal
                                )
                                ).ToList();

                            m_WindowRect.height = m_ProfileGroupTypes.Count > 0 ? k_MaxHeight : k_MinHeight;

                            HashSet<ProfileGroupType> groupTypes = new HashSet<ProfileGroupType>();
                            selectedProfileGroupTypes.ForEach((groupType) =>
                            {
                                var parts = groupType.GroupTypePrefix.Split(ProfileGroupType.k_PrefixSeparator);
                                var badgeName = String.Join(ProfileGroupType.k_PrefixSeparator.ToString(), parts, 4, parts.Length - 4);
                                if (!groupTypes.Contains(groupType))
                                    groupTypes.Add(groupType);
                            });


                            CCDOption.DrawBadges(groupTypes, m_BucketId, m_EnvironmentId, (ProfileGroupType groupType) =>
                            {

                                dataSourceSettings.SetEnvironmentById(AddressableAssetSettingsDefaultObject.Settings.profileSettings, m_Profile.id, m_EnvironmentId);
                                var args = new DropdownWindowEventArgs();
                                args.GroupType = m_GroupType;
                                args.Option = new CCDOption();
                                args.Option.BuildPath = groupType.GetVariableBySuffix("BuildPath").Value;
                                args.Option.LoadPath = groupType.GetVariableBySuffix("LoadPath").Value;
                                args.IsCustom = false;
                                SetCcdManagedDataState(CcdManagedData.ConfigState.None);
                                OnValueChanged(args);
                                editorWindow.Close();
                            });
                            EditorGUILayout.EndScrollView();
                            break;
                        default:
                            CCDState = CCDDropdownState.General;
                            break;

#endif
                    }

                    break;
                case DropdownState.BuiltIn:
                case DropdownState.EditorHosted:
                default:
#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
                    SetCcdManagedDataState(CcdManagedData.ConfigState.None);
#endif
                    editorWindow.Close();
                    break;
            }

            EditorGUI.EndDisabledGroup();
        }

        private bool DrawCcdDisabledDropdownHeader(Rect window, Rect horizontalBarRect, Rect backButtonRect, Rect refreshButtonRect, Event evt, string title, DropdownState dropdownState,
            CCDDropdownState prevState)
        {
            DrawHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, title, dropdownState, prevState);
#if !ENABLE_CCD
            //Used to Display whether or not a user has the CCD Package
            EditorStyles.helpBox.fontSize = 12;
            EditorGUILayout.HelpBox("Connecting to Cloud Content Delivery requires the CCD Management SDK Package", MessageType.Warning);
            var installPackageButton = GUILayout.Button("Install CCD Management SDK Package");
            if (installPackageButton)
            {
                editorWindow.Close();
                AddressableAssetUtility.InstallCCDPackage();
            }
#endif
            return true;
        }

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
        private async Task<bool> DrawCcdEnabledDropdownHeader(Rect window, Rect horizontalBarRect, Rect backButtonRect, Rect refreshButtonRect, Event evt, string title, DropdownState dropdownState, CCDDropdownState prevState)
        {
            DrawHeader(window, horizontalBarRect, backButtonRect, refreshButtonRect, evt, title, dropdownState, prevState);

            if (CloudProjectSettings.projectId != String.Empty)
            {
                EditorGUI.LabelField(refreshButtonRect, EditorGUIUtility.IconContent(refreshIcon));
                if (evt.type == EventType.MouseDown && refreshButtonRect.Contains(evt.mousePosition) && !m_isRefreshingCCDDataSources)
                {
                    m_isRefreshingCCDDataSources = true;
                    await ProfileDataSourceSettings.UpdateCCDDataSourcesAsync(CloudProjectSettings.projectId, true);
                    SyncProfileGroupTypes();
                    m_isRefreshingCCDDataSources = false;
                    return true;
                }
            }

            if (CloudProjectSettings.projectId == String.Empty)
            {
                EditorStyles.helpBox.fontSize = 12;
                EditorGUILayout.LabelField("Connecting to Cloud Content Delivery requires enabling Cloud Project Settings in the Services Window.", EditorStyles.helpBox);
                return true;
            }
            return false;
        }
#endif

        private void DrawHeader(Rect window, Rect horizontalBarRect, Rect backButtonRect, Rect refreshButtonRect, Event evt, string title, DropdownState dropdownState, CCDDropdownState prevState)
        {
            EditorGUI.LabelField(backButtonRect, EditorGUIUtility.IconContent(backIcon));
            if (evt.type == EventType.MouseDown && backButtonRect.Contains(evt.mousePosition))
            {
                state = dropdownState;
                CCDState = prevState;
                m_WindowRect.height = 120;
            }

            EditorGUILayout.LabelField(title, dropdownTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUI.LabelField(horizontalBarRect, "", new GUIStyle(horizontalBarStyle) {fixedWidth = window.width});
        }

        private void SyncProfileGroupTypes()
        {
            m_ProfileGroupTypes = dataSourceSettings.GetGroupTypesByPrefix("CCD" + ProfileGroupType.k_PrefixSeparator + CloudProjectSettings.projectId);
        }


#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
        private void SetCcdManagedDataState(CcdManagedData.ConfigState state)
        {
            AddressableAssetSettingsDefaultObject.Settings.m_CcdManagedData.State = state;
        }
#endif
        private async Task<OrgData> GetOrgData()
        {
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + CloudProjectSettings.accessToken);
                var response = await client.GetAsync(String.Format("{0}/v1/core/api/orgs/{1}", ProfileDataSourceSettings.m_GenesisBasePath, CloudProjectSettings.organizationId));
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to retrieve org data.");
                }

                var data = await response.Content.ReadAsStringAsync();
                return OrgData.ParseOrgData(data);
            }
        }

        protected virtual void OnValueChanged(DropdownWindowEventArgs e)
        {
            ValueChangedEventHandler handler = ValueChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public class DropdownWindowEventArgs : EventArgs
        {
            public ProfileGroupType GroupType { get; set; }
            public BaseOption Option { get; set; }
            public bool IsCustom { get; set; }
        }

        internal abstract class BaseOption
        {
            internal string OptionName;
            internal DropdownState state;
            internal string BuildPath;
            internal string LoadPath;
            internal abstract void Draw(Action action);

            internal static void DrawMenuItem(string title, string displayIcon, Action action)
            {
                Rect labelRect = EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(title, menuOptionStyle);
                EditorGUILayout.EndHorizontal();
                if (displayIcon != null)
                {
                    Rect linkRect = new Rect(labelRect.x + labelRect.width - menuOptionStyle.fixedHeight - 10, labelRect.y, menuOptionStyle.fixedHeight, menuOptionStyle.fixedHeight);
                    EditorGUI.LabelField(linkRect, EditorGUIUtility.IconContent(displayIcon));
                }

                if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                {
                    action.Invoke();
                }
            }

            internal static void DrawMenuItemWithArg<T>(string title, Action<T> action, T arg, string infoIcon = null, string displayIcon = null)
            {
                Rect labelRect = EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(title, menuOptionStyle);
                EditorGUILayout.EndHorizontal();

                if (infoIcon != null)
                {
                    Rect infoRect = new Rect(labelRect.x, labelRect.y + 2, menuOptionStyle.fixedHeight, menuOptionStyle.fixedHeight);
                    EditorGUI.LabelField(infoRect, EditorGUIUtility.IconContent(infoIcon));
                }

                if (displayIcon != null)
                {
                    Rect linkRect = new Rect(labelRect.x + labelRect.width - menuOptionStyle.fixedHeight, labelRect.y, menuOptionStyle.fixedHeight, menuOptionStyle.fixedHeight);
                    EditorGUI.LabelField(linkRect, EditorGUIUtility.IconContent(displayIcon));
                }

                if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                {
                    action.Invoke(arg);
                }
            }
        }

        internal class BuiltInOption : BaseOption
        {
            internal BuiltInOption()
            {
                OptionName = "Built-In";
                state = DropdownState.BuiltIn;
                BuildPath = AddressableAssetSettings.kLocalBuildPathValue;
                LoadPath = AddressableAssetSettings.kLocalLoadPathValue;
            }

            internal override void Draw(Action action)
            {
                DrawMenuItem(OptionName, null, action);
            }
        }

        internal class EditorHostedOption : BaseOption
        {
            internal EditorHostedOption()
            {
                OptionName = "Editor Hosted";
                state = DropdownState.EditorHosted;
                BuildPath = AddressableAssetSettings.kRemoteBuildPathValue;
                LoadPath = AddressableAssetSettings.RemoteLoadPathValue;
            }

            internal override void Draw(Action action)
            {
                DrawMenuItem(OptionName, null, action);
            }
        }

        internal class CustomOption : BaseOption
        {
            internal CustomOption()
            {
                OptionName = "Custom";
                state = DropdownState.Custom;
                BuildPath = null;
                LoadPath = null;
            }

            internal override void Draw(Action action)
            {
                DrawMenuItem(OptionName, null, action);
            }
        }


        internal class CCDOption : BaseOption
        {
            internal CCDOption()
            {
                OptionName = "Cloud Content Delivery";
                state = DropdownState.CCD;
                BuildPath = null;
                LoadPath = null;
            }

            internal override void Draw(Action action)
            {
                DrawMenuItem(OptionName, nextIcon, action);
            }

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
            internal static void DrawBuckets(Dictionary<string, ProfileGroupType> buckets, string environmentId,
                Action<KeyValuePair<string, ProfileGroupType>> action)
            {
                if (buckets.Count > 0)
                {
                    foreach (var bucket in buckets)
                    {
                        bool showInfo = bool.Parse(bucket.Value.GetVariableBySuffix(nameof(CcdBucket.Attributes.PromoteOnly)).Value);
                        DrawMenuItemWithArg(
                            bucket.Value.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}").Value,
                            action,
                            new KeyValuePair<string, ProfileGroupType>(bucket.Key, bucket.Value),
                            showInfo ? infoIcon : null,
                            nextIcon);
                    }
                }
                else
                {
                    DrawCompleteCCDOnBoarding();
                }
                DrawCreateBucket(environmentId);
            }

            internal static void DrawBadges(HashSet<ProfileGroupType> groupTypes, string bucketId, string environmentId,
                Action<ProfileGroupType> action)
            {
                if (groupTypes.Count > 0)
                {
                    foreach (var groupType in groupTypes)
                    {
                        DrawMenuItemWithArg(groupType.GetVariableBySuffix($"{nameof(CcdBadge)}{nameof(CcdBadge.Name)}").Value, action, groupType);
                    }
                }

                DrawCreateBadge(bucketId, environmentId);
            }

#endif
            internal static void DrawCreateEnv()
            {
                DrawMenuItem("<a>Create new environment</a>", null, () =>
                {
                    Application.OpenURL(
                        String.Format("https://dashboard.unity3d.com/organizations/{0}/projects/{1}/settings/environments",
                            m_Organization.foreign_key,
                            CloudProjectSettings.projectId));
                });
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.y += 2;
                EditorGUI.LabelField(lastRect, "<a>___________________________</a>", menuOptionStyle);
            }

            internal static void DrawCreateBucket(string environmentId)
            {
                DrawMenuItem("<a>Create new bucket</a>", null, () =>
                {
                    Application.OpenURL(
                        String.Format("{0}/organizations/{1}/projects/{2}/environments/{3}/cloud-content-delivery",
                            ProfileDataSourceSettings.m_DashboardBasePath,
                            m_Organization.foreign_key,
                            CloudProjectSettings.projectId,
                            environmentId));
                });
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.y += 2;
                EditorGUI.LabelField(lastRect, "<a>_____________________</a>", menuOptionStyle);
            }

            internal static void DrawCreateBadge(string bucketId, string environmentId)
            {
                DrawMenuItem("<a>Create new badge</a>", null, () =>
                {
                    Application.OpenURL(
                        String.Format("{0}/organizations/{1}/projects/{2}/environments/{3}/cloud-content-delivery/buckets/{4}/badges",
                            ProfileDataSourceSettings.m_DashboardBasePath,
                            m_Organization.foreign_key,
                            CloudProjectSettings.projectId,
                            environmentId,
                            bucketId));
                });
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.y += 2;
                EditorGUI.LabelField(lastRect, "<a>____________________</a>", menuOptionStyle);
            }

            internal static void DrawCompleteCCDOnBoarding()
            {
                DrawMenuItem("<a>Complete CCD Onboarding</a>", null, () =>
                {
                    Application.OpenURL(
                        String.Format("{0}/organizations/{1}/projects/{2}/cloud-content-delivery/onboarding",
                            ProfileDataSourceSettings.m_DashboardBasePath,
                            m_Organization.foreign_key,
                            CloudProjectSettings.projectId));
                });
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.y += 2;
                EditorGUI.LabelField(lastRect, "<a>______________________________</a>", menuOptionStyle);
            }
        }
    }
}
