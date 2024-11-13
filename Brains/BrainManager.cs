using DrakiaXYZ.BigBrain.Internal;
using EFT;
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

        private static List<WildSpawnType> _allWildSpawnTypes;
        internal static List<WildSpawnType> AllWildSpawnTypes
        {
            get
            {
                if (_allWildSpawnTypes == null)
                {
                    _allWildSpawnTypes = Enum.GetValues(typeof(WildSpawnType))
                        .Cast<WildSpawnType>()
                        .ToList();
                }

                return _allWildSpawnTypes;
            }
        }

        private static int _currentLayerId = START_LAYER_ID;
        
        internal Dictionary<int, LayerInfo> CustomLayers = new Dictionary<int, LayerInfo>();
        internal Dictionary<Type, int> CustomLogics = new Dictionary<Type, int>();
        internal List<Type> CustomLogicList = new List<Type>();
        internal List<ExcludeLayerInfo> ExcludeLayers = new List<ExcludeLayerInfo>();
        internal Dictionary<IPlayer, BotOwner> ActivatedBots = new Dictionary<IPlayer, BotOwner>();
        internal List<ExcludedLayerInfo> ExcludedLayers = new List<ExcludedLayerInfo>();

        // Allow modders to access read-only collections of the brain layers added/removed and custom logics used by bots
        public static IReadOnlyDictionary<int, LayerInfo> CustomLayersReadOnly => Instance.CustomLayers.ToDictionary(i => i.Key, i => i.Value);
        public static IReadOnlyDictionary<Type, int> CustomLogicsReadOnly => Instance.CustomLogics.ToDictionary(i => i.Key, i => i.Value);
        public static IReadOnlyList<ExcludeLayerInfo> ExcludeLayersReadOnly => Instance.ExcludeLayers.AsReadOnly();
        public static int ExcludedLayerCount => Instance.ExcludeLayers.Count;

        private static FieldInfo _strategyField = Utils.GetFieldByType(typeof(AICoreLogicAgentClass), typeof(AICoreStrategyAbstractClass<>));

        // Hide the constructor so we can have this as a guaranteed singleton
        private BrainManager() { }

        public class LayerInfo : AbstractLayerInfo
        {
            public Type customLayerType { get; private set; }
            public int customLayerPriority { get; private set; }
            public int customLayerId { get; private set; }

            public IReadOnlyList<string> CustomLayerBrains => affectedBrainNames.AsReadOnly();
            public IReadOnlyList<WildSpawnType> CustomLayerRoles => affectedRoles.AsReadOnly();

            public LayerInfo(Type layerType, List<string> layerBrains, int layerPriority, int layerId)
            {
                customLayerType = layerType;
                affectedBrainNames = layerBrains;
                customLayerPriority = layerPriority;
                customLayerId = layerId;

                affectedRoles = AllWildSpawnTypes;
            }

            public LayerInfo(Type layerType, List<string> layerBrains, int layerPriority, int layerId, List<WildSpawnType> roles)
                : this(layerType, layerBrains, layerPriority, layerId)
            {
                affectedRoles = roles;
            }
        }

        public class ExcludeLayerInfo : AbstractLayerInfo
        {
            public string excludeLayerName { get; private set; }

            public IReadOnlyList<string> ExcludeLayerBrains => affectedBrainNames.AsReadOnly();
            public IReadOnlyList<WildSpawnType> ExcludeLayerRoles => affectedRoles.AsReadOnly();

            public ExcludeLayerInfo(string layerName, List<string> brains)
            {
                affectedBrainNames = brains;
                excludeLayerName = layerName;

                affectedRoles = AllWildSpawnTypes;
            }

            public ExcludeLayerInfo(string layerName, List<string> brains, List<WildSpawnType> roles)
                : this(layerName, brains)
            {
                affectedRoles = roles;
            }
        }

        internal class ExcludedLayerInfo
        {
            public BotOwner BotOwner { get; private set; }
            public AICoreLogicLayerClass Layer { get; private set; }
            public string BrainName { get; private set; }
            public string LayerName { get; private set; }
            public int Index { get; private set; }

            public WildSpawnType Role => BotOwner.Profile.Info.Settings.Role;

            public ExcludedLayerInfo(BotOwner botOwner, AICoreLogicLayerClass layer, string brainName, int index)
            {
                BotOwner = botOwner;
                Layer = layer;
                BrainName = brainName;
                Index = index;

                LayerName = layer.Name();
            }
        }

        public static int AddCustomLayer(Type customLayerType, List<string> brainNames, int customLayerPriority, List<WildSpawnType> roles)
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
            Instance.CustomLayers.Add(customLayerId, new LayerInfo(customLayerType, brainNames, customLayerPriority, customLayerId, roles));
            return customLayerId;
        }

        public static int AddCustomLayer(Type customLayerType, List<string> brainNames, int customLayerPriority)
        {
            return AddCustomLayer(customLayerType, brainNames, customLayerPriority, AllWildSpawnTypes);
        }

        public static void AddCustomLayers(List<Type> customLayerTypes, List<string> brainNames, int customLayerPriority, List<WildSpawnType> roles)
        {
            customLayerTypes.ForEach(customLayerType => AddCustomLayer(customLayerType, brainNames, customLayerPriority, roles));
        }

        public static void AddCustomLayers(List<Type> customLayerTypes, List<string> brainNames, int customLayerPriority)
        {
            customLayerTypes.ForEach(customLayerType => AddCustomLayer(customLayerType, brainNames, customLayerPriority, AllWildSpawnTypes));
        }

        public static void RemoveLayer(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            // Dynamically create ExcludeLayers as needed to match the provided settings
            ExcludeLayerSplitter.SplitOrAddLayersForSettings(layerName, brainNames, roles);

            // Remove the layer for all bots that have already spawned
            foreach (ExcludeLayerInfo excludeLayer in Instance.ExcludeLayers)
            {
                if (!excludeLayer.HasOneOrMoreOfEachSetting(layerName, brainNames, roles))
                {
                    continue;
                }

                foreach (BotOwner botOwner in Instance.ActivatedBots.Values)
                {
                    if ((botOwner == null) || botOwner.IsDead)
                    {
                        continue;
                    }

                    botOwner.RemoveLayerForBot(excludeLayer);
                }
            }
        }

        public static void RemoveLayer(string layerName, List<string> brainNames)
        {
            RemoveLayer(layerName, brainNames, AllWildSpawnTypes);
        }

        public static void RemoveLayers(List<string> layerNames, List<string> brainNames, List<WildSpawnType> roles)
        {
            layerNames.ForEach(layerName => RemoveLayer(layerName, brainNames, roles));
        }

        public static void RemoveLayers(List<string> layerNames, List<string> brainNames)
        {
            layerNames.ForEach(layerName => RemoveLayer(layerName, brainNames, AllWildSpawnTypes));
        }

        public static void RestoreLayer(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            // Dynamically create ExcludeLayers as needed to match the provided settings
            ExcludeLayerSplitter.SplitLayersForSettings(layerName, brainNames, roles);

            Instance.ExcludeLayers.RemoveAllWithOneOrMoreOfEachSetting(layerName, brainNames, roles);

            // Restore the layer for all applicable bots that have already spawned
            foreach (BotOwner botOwner in Instance.ActivatedBots.Values)
            {
                if ((botOwner == null) || botOwner.IsDead)
                {
                    continue;
                }

                if (!AbstractLayerInfo.DoSettingsAffectBot(botOwner, brainNames, roles))
                {
                    continue;
                }

                botOwner.RestoreLayerForBot(layerName);
            }
        }

        public static void RestoreLayer(string layerName, List<string> brainNames)
        {
            RestoreLayer(layerName, brainNames, AllWildSpawnTypes);
        }

        public static void RestoreLayers(List<string> layerNames, List<string> brainNames, List<WildSpawnType> roles)
        {
            layerNames.ForEach(layerName => RestoreLayer(layerName, brainNames, roles));
        }

        public static void RestoreLayers(List<string> layerNames, List<string> brainNames)
        {
            layerNames.ForEach(layerName => RestoreLayer(layerName, brainNames, AllWildSpawnTypes));
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
