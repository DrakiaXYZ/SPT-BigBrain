﻿using DrakiaXYZ.BigBrain.Internal;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using AICoreLogicAgentClass = AICoreAgentClass<BotLogicDecision>;
using AICoreLogicLayerClass = AICoreLayerClass<BotLogicDecision>;

namespace DrakiaXYZ.BigBrain.Brains
{
    public class BrainManager
    {
        public const int START_LAYER_ID = 9000;
        public const int START_LOGIC_ID = 9000;

        private static BrainManager _instance;
        internal static BrainManager Instance
        {
            get 
            { 
                if (_instance == null)
                {
                    _instance = new BrainManager();
                }

                return _instance; 
            }
        }

        private static int _currentLayerId = START_LAYER_ID;

        internal Dictionary<int, LayerInfo> CustomLayers = new Dictionary<int, LayerInfo>();
        internal Dictionary<Type, int> CustomLogics = new Dictionary<Type, int>();
        internal List<Type> CustomLogicList = new List<Type>();
        internal List<ExcludeLayerInfo> ExcludeLayers = new List<ExcludeLayerInfo>();

        // Allow modders to access read-only collections of the brain layers added/removed and custom logics used by bots
        public static IReadOnlyDictionary<int, LayerInfo> CustomLayersReadOnly => Instance.CustomLayers.ToDictionary(i => i.Key, i => i.Value);
        public static IReadOnlyDictionary<Type, int> CustomLogicsReadOnly => Instance.CustomLogics.ToDictionary(i => i.Key, i => i.Value);
        public static IReadOnlyList<ExcludeLayerInfo> ExcludeLayersReadOnly => Instance.ExcludeLayers.AsReadOnly();

        private static FieldInfo _strategyField = Utils.GetFieldByType(typeof(AICoreLogicAgentClass), typeof(AICoreStrategyAbstractClass<>));

        // Hide the constructor so we can have this as a guaranteed singleton
        private BrainManager() { }

        public class LayerInfo
        {
            public Type customLayerType { get; private set; }
            public int customLayerPriority { get; private set; }
            public int customLayerId { get; private set; }

            private List<string> _customLayerBrains;

            public IReadOnlyList<string> CustomLayerBrains => _customLayerBrains.AsReadOnly();

            public LayerInfo(Type layerType, List<string> layerBrains, int layerPriority, int layerId)
            {
                customLayerType = layerType;
                _customLayerBrains = layerBrains;
                customLayerPriority = layerPriority;
                customLayerId = layerId;
            }
        }

        public class ExcludeLayerInfo
        {
            public string excludeLayerName { get; private set; }

            private List<string> _excludeLayerBrains;

            public IReadOnlyList<string> ExcludeLayerBrains => _excludeLayerBrains.AsReadOnly();

            public ExcludeLayerInfo(string layerName, List<string> brains)
            {
                _excludeLayerBrains = brains;
                excludeLayerName = layerName;
            }
        }

        public static int AddCustomLayer(Type customLayerType, List<string> brainNames, int customLayerPriority)
        {
            if (!typeof(CustomLayer).IsAssignableFrom(customLayerType))
            {
                throw new ArgumentException($"Custom layer type {customLayerType.FullName} must inherit CustomLayer");
            }

            if (brainNames.Count == 0)
            {
                throw new ArgumentException($"Custom layer type {customLayerType.FullName} must specify at least 1 brain to be added to");
            }

            int customLayerId = _currentLayerId++;
            Instance.CustomLayers.Add(customLayerId, new LayerInfo(customLayerType, brainNames, customLayerPriority, customLayerId));
            return customLayerId;
        }

        public static void AddCustomLayers(List<Type> customLayerTypes, List<string> brainNames, int customLayerPriority)
        {
            customLayerTypes.ForEach(customLayerType => AddCustomLayer(customLayerType, brainNames, customLayerPriority));
        }

        public static void RemoveLayer(string layerName, List<string> brainNames)
        {
            Instance.ExcludeLayers.Add(new ExcludeLayerInfo(layerName, brainNames));
        }

        public static void RemoveLayers(List<string> layerNames, List<string> brainNames)
        {
            layerNames.ForEach(layerName => RemoveLayer(layerName, brainNames));
        }

        public static bool IsCustomLayerActive(BotOwner botOwner)
        {
            object activeLayer = GetActiveLayer(botOwner);
            if (activeLayer is CustomLayer)
            {
                return true;
            }

            return false;
        }

        public static string GetActiveLayerName(BotOwner botOwner)
        {
            return botOwner.Brain.ActiveLayerName();
        }

        /**
         * Return the currently active base layer, which will extend "AICoreLayerClass", or the active
         * CustomLayer if a custom layer is enabled
         **/
        public static object GetActiveLayer(BotOwner botOwner)
        {
            if (botOwner?.Brain?.Agent == null)
            {
                return null;
            }

            BaseBrain botBrainStrategy = _strategyField.GetValue(botOwner.Brain.Agent) as BaseBrain;
            if (botBrainStrategy == null)
            {
                return null;
            }

            AICoreLogicLayerClass activeLayer = botBrainStrategy.CurLayerInfo;
            if (activeLayer is CustomLayerWrapper customLayerWrapper)
            {
                return customLayerWrapper.CustomLayer();
            }

            return activeLayer;
        }

        /**
         * Return the current active logic instance, which will extend "BaseNodeClass", or the active
         * CustomLogic if a custom logic is enabled
         * Note: This is mostly here for BotDebug, please don't use this in plugins
         **/
        public static object GetActiveLogic(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return null;
            }

            BaseNodeAbstractClass activeLogic = CustomLayerWrapper.GetLogicInstance(botOwner);
            if (activeLogic is CustomLogicWrapper customLogicWrapper)
            {
                return customLogicWrapper.CustomLogic();
            }

            return activeLogic;
        }
    }
}
