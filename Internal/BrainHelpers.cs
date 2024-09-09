using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AICoreLogicLayerClass = AICoreLayerClass<BotLogicDecision>;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal static class BrainHelpers
    {
        private static FieldInfo _layerDictionary = null;

        internal static Dictionary<int, AICoreLogicLayerClass> GetBrainLayerDictionary(this object brain)
        {
            if (_layerDictionary == null)
            {
                Type baseBrainType = typeof(BaseBrain);
                Type aiCoreStrategyType = baseBrainType.BaseType;

                _layerDictionary = AccessTools.Field(aiCoreStrategyType, "dictionary_0");
            }

            return _layerDictionary.GetValue(brain) as Dictionary<int, AICoreLogicLayerClass>;
        }

        internal static void RemoveAllExcludedLayers(this BotOwner botOwner)
        {
            foreach (BrainManager.ExcludeLayerInfo excludeLayer in BrainManager.Instance.ExcludeLayers)
            {
                if (!excludeLayer.ExcludeLayerBrains.Contains(botOwner.Brain.BaseBrain.ShortName()))
                {
                    continue;
                }

                botOwner.RemoveLayer(excludeLayer.excludeLayerName);
            }
        }

        internal static void RemoveLayer(this BotOwner botOwner, string layerName)
        {
            Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();
            int layerIndexToRemove = -1;

            foreach (int index in botBrainLayerDictionary.Keys)
            {
                if (botBrainLayerDictionary[index].Name() != layerName)
                {
                    continue;
                }

                Logger.CreateLogSource("BIGBRAIN").LogInfo("Removing " + layerName + " from " + botOwner.name + " (" + botOwner.Brain.BaseBrain.ShortName() + ", " + botOwner.Profile.Info.Settings.Role + ")");

                botOwner.Brain.BaseBrain.method_3(index);

                if (botOwner.Brain.BaseBrain.method_2(botBrainLayerDictionary[index]))
                {
                    throw new InvalidOperationException($"Could not remove brain layer '{layerName}' from {botOwner.name}");
                }

                BrainManager.Instance.ExcludedLayers.Add(new BrainManager.ExcludedLayerInfo(botOwner, botBrainLayerDictionary[index], botOwner.Brain.BaseBrain.ShortName(), index));
                layerIndexToRemove = index;
                break;
            }

            if (layerIndexToRemove > -1)
            {
                botBrainLayerDictionary.Remove(layerIndexToRemove);
            }
        }

        internal static void RestoreLayer(this BotOwner botOwner, string layerName)
        {
            Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();
            List<BrainManager.ExcludedLayerInfo> restoredLayers = new List<BrainManager.ExcludedLayerInfo>();

            foreach (BrainManager.ExcludedLayerInfo excludedLayer in BrainManager.Instance.ExcludedLayers)
            {
                if ((excludedLayer.BotOwner != botOwner) || (excludedLayer.LayerName != layerName))
                {
                    continue;
                }

                Logger.CreateLogSource("BIGBRAIN").LogInfo("Restoring " + layerName + " to " + botOwner.name + " (" + botOwner.Brain.BaseBrain.ShortName() + ", " + botOwner.Profile.Info.Settings.Role + ")");

                if (botBrainLayerDictionary.ContainsKey(excludedLayer.Index))
                {
                    throw new InvalidOperationException($"Cannot restore '{excludedLayer.LayerName}' for {botOwner.name}. Index already exists in bot brain.");
                }

                if (!botOwner.Brain.BaseBrain.method_0(excludedLayer.Index, excludedLayer.Layer, true))
                {
                    throw new InvalidOperationException($"Cannot restore '{excludedLayer.LayerName}' for {botOwner.name}. Failed to add layer.");
                }

                restoredLayers.Add(excludedLayer);
            }

            foreach (BrainManager.ExcludedLayerInfo restoredLayer in restoredLayers)
            {
                BrainManager.Instance.ExcludedLayers.Remove(restoredLayer);
            }
        }
    }
}
