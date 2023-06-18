﻿using Aki.Common.Http;
using Aki.Common.Utils;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DrakiaXYZ.BigBrain.VersionChecker
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class TarkovVersion : Attribute
    {
        private int version;
        public TarkovVersion() : this(0) { }
        public TarkovVersion(int version)
        {
            this.version = version;
        }

        public static int BuildVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(TarkovVersion), false)
                    ?.Cast<TarkovVersion>()?.FirstOrDefault()?.version ?? 0;
            }
        }

        // Make sure the version of EFT being run is the correct version, throw an exception and output log message if it isn't
        /// <summary>
        /// Check the currently running program's version against the plugin assembly TarkovVersion attribute, and
        /// return false if they do not match. 
        /// Optionally add a fake setting to the F12 menu if Config is passed in
        /// </summary>
        /// <param name="Logger">The ManualLogSource to output an error to</param>
        /// <param name="Info">The PluginInfo object for the plugin, used to get the plugin name and version</param>
        /// <param name="Config">A BepinEx ConfigFile object, if provided, a custom message will be added to the F12 menu</param>
        /// <returns></returns>
        public static bool CheckEftVersion(ManualLogSource Logger, PluginInfo Info, ConfigFile Config = null)
        {
            int currentVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
            int buildVersion = BuildVersion;
            if (currentVersion != buildVersion)
            {
                string errorMessage = $"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.";
                Logger.LogError(errorMessage);

                if (Config != null)
                {
                    // This results in a bogus config entry in the BepInEx config file for the plugin, but it shouldn't hurt anything
                    // We leave the "section" parameter empty so there's no section header drawn
                    Config.Bind("", "TarkovVersion", "", new ConfigDescription(
                        errorMessage, null, new ConfigurationManagerAttributes
                        {
                            CustomDrawer = ErrorLabelDrawer,
                            ReadOnly = true,
                            HideDefaultButton = true,
                            HideSettingName = true,
                            Category = null
                        }
                    ));
                }

                return false;
            }

            // Because 3.5.7 and 3.5.8 are both 23399, but have different remappings, we need to do an extra check
            // here that the actual SPT version is what we expect. 
            // TODO: Once a new EFT version comes out, we can drop this
            if (currentVersion == 23399)
            {
                Version expectedSptVersion = new Version(3, 5, 8);
                Version sptVersion = GetSptVersion();
                if (sptVersion != expectedSptVersion)
                {
                    string errorMessage = $"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for SPT {expectedSptVersion}, but you are running {sptVersion}. Please download the correct plugin version.";
                    Logger.LogError(errorMessage);
                    return false;
                }
            }
            // TODO: Delete above when we have a new assembly version

            return true;
        }

        public static Version GetSptVersion()
        {
            var json = RequestHandler.GetJson("/singleplayer/settings/version");
            string version = Json.Deserialize<VersionResponse>(json).Version;
            version = Regex.Match(version, @"(\d+\.?)+").Value;

            Console.WriteLine($"SPT Version: {version}");
            return Version.Parse(version);
        }

        static void ErrorLabelDrawer(ConfigEntryBase entry)
        {
            GUIStyle styleNormal = new GUIStyle(GUI.skin.label);
            styleNormal.wordWrap = true;
            styleNormal.stretchWidth = true;

            GUIStyle styleError = new GUIStyle(GUI.skin.label);
            styleError.stretchWidth = true;
            styleError.alignment = TextAnchor.MiddleCenter;
            styleError.normal.textColor = Color.red;
            styleError.fontStyle = FontStyle.Bold;

            // General notice that we're the wrong version
            GUILayout.BeginVertical();
            GUILayout.Label(entry.Description.Description, styleNormal, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });

            // Centered red disabled text
            GUILayout.Label("Plugin has been disabled!", styleError, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            GUILayout.EndVertical();
        }

        public struct VersionResponse
        {
            public string Version { get; set; }
        }

#pragma warning disable 0169, 0414, 0649
        internal sealed class ConfigurationManagerAttributes
        {
            public bool? ShowRangeAsPercent;
            public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;
            public CustomHotkeyDrawerFunc CustomHotkeyDrawer;
            public delegate void CustomHotkeyDrawerFunc(BepInEx.Configuration.ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
            public bool? Browsable;
            public string Category;
            public object DefaultValue;
            public bool? HideDefaultButton;
            public bool? HideSettingName;
            public string Description;
            public string DispName;
            public int? Order;
            public bool? ReadOnly;
            public bool? IsAdvanced;
            public System.Func<object, string> ObjToStr;
            public System.Func<string, object> StrToObj;
        }
    }
}
