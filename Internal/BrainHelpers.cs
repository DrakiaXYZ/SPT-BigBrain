using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using AICoreLogicLayerClass = AICoreLayerClass<BotLogicDecision>;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal static class BrainHelpers
    {
        private static FieldInfo _layerDictionary = null;

        public static Dictionary<int, AICoreLogicLayerClass> GetBrainLayerDictionary(this BaseBrain brain)
        {
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain));
            }

            if (_layerDictionary == null)
            {
                Type baseBrainType = typeof(BaseBrain);
                Type aiCoreStrategyType = baseBrainType.BaseType;

                _layerDictionary = AccessTools.Field(aiCoreStrategyType, "dictionary_0");
            }

            var brainLayerDictionayer = _layerDictionary.GetValue(brain) as Dictionary<int, AICoreLogicLayerClass>;
            if (brainLayerDictionayer == null)
            {
                throw new InvalidOperationException($"Brain dictionary not found for provided base brain (Brain type = {brain?.ShortName() ?? "null"})");
            }

            return brainLayerDictionayer;
        }

        internal static void RemoveAllExcludedLayers(this BotOwner botOwner)
        {
            if (botOwner == null)
            {
                throw new ArgumentNullException(nameof(botOwner));
            }

            foreach (BrainManager.ExcludeLayerInfo excludeLayer in BrainManager.Instance.ExcludeLayers)
            {
                botOwner.RemoveLayerForBot(excludeLayer);
            }
        }

        internal static void RemoveLayerForBot(this BotOwner botOwner, BrainManager.ExcludeLayerInfo excludeLayer)
        {
            if (botOwner == null)
            {
                throw new ArgumentNullException(nameof(botOwner));
            }

            if (excludeLayer == null)
            {
                throw new ArgumentNullException(nameof(excludeLayer));
            }

            if (!excludeLayer.AffectsBot(botOwner))
            {
                return;
            }    

            botOwner.RemoveLayerForBot(excludeLayer.excludeLayerName);
        }

        internal static void RemoveLayerForBot(this BotOwner botOwner, string layerName)
        {
            if (botOwner == null)
            {
                throw new ArgumentNullException(nameof(botOwner));
            }

            if (layerName == null)
            {
                throw new ArgumentNullException(nameof(layerName));
            }

            // Get all brain layers the bot currently has
            Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();

            int layerIndexToRemove = -1;

            foreach (int index in botBrainLayerDictionary.Keys)
            {
                if (botBrainLayerDictionary[index].Name() != layerName)
                {
                    continue;
                }

                BigBrainPlugin.BigBrainLogger.LogInfo($"Removing {layerName} from {botOwner.name} ({botOwner.Brain.BaseBrain.ShortName()}, {botOwner.Profile.Info.Settings.Role})");

                // Remove the brain layer from the bot's brain
                layerIndexToRemove = index;
                botOwner.Brain.BaseBrain.method_3(index);
                
                // Ensure there is no longer a brain layer of the same type in the bot's brain
                if (botOwner.Brain.BaseBrain.method_2(botBrainLayerDictionary[index]))
                {
                    throw new InvalidOperationException($"Could not remove brain layer '{layerName}' from {botOwner.name}");
                }

                // Cache the removed layer so it can be restored later if needed
                BrainManager.Instance.ExcludedLayers.Add(new BrainManager.ExcludedLayerInfo(botOwner, botBrainLayerDictionary[index], botOwner.Brain.BaseBrain.ShortName(), index));
                
                break;
            }

            // If a matching layer was found, remove it from the bot's brain-layer dictionary. This is not done in method_3.
            if (layerIndexToRemove > -1)
            {
                botBrainLayerDictionary.Remove(layerIndexToRemove);
            }
        }

        internal static void RestoreLayerForBot(this BotOwner botOwner, string layerName)
        {
            if (botOwner == null)
            {
                throw new ArgumentNullException(nameof(botOwner));
            }

            if (layerName == null)
            {
                throw new ArgumentNullException(nameof(layerName));
            }

            // Get all brain layers the bot currently has
            Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();

            List<BrainManager.ExcludedLayerInfo> restoredLayers = new List<BrainManager.ExcludedLayerInfo>();

            foreach (BrainManager.ExcludedLayerInfo excludedLayer in BrainManager.Instance.ExcludedLayers)
            {
                if ((excludedLayer.BotOwner != botOwner) || (excludedLayer.LayerName != layerName))
                {
                    continue;
                }

                BigBrainPlugin.BigBrainLogger.LogInfo($"Restoring {layerName} to {botOwner.name} ({botOwner.Brain.BaseBrain.ShortName()}, {botOwner.Profile.Info.Settings.Role})");

                // Ensure the brain-layer index doesn't already exist in the bot's brain
                if (botBrainLayerDictionary.ContainsKey(excludedLayer.Index))
                {
                    throw new InvalidOperationException($"Cannot restore '{excludedLayer.LayerName}' for {botOwner.name}. Index already exists in bot brain.");
                }

                // Add the brain layer back to the bot's brain
                if (!botOwner.Brain.BaseBrain.method_0(excludedLayer.Index, excludedLayer.Layer, true))
                {
                    throw new InvalidOperationException($"Cannot restore '{excludedLayer.LayerName}' for {botOwner.name}. Failed to add layer.");
                }

                restoredLayers.Add(excludedLayer);
            }

            // After a brain layer has been restored, the cached version of it can be removed
            foreach (BrainManager.ExcludedLayerInfo restoredLayer in restoredLayers)
            {
                BrainManager.Instance.ExcludedLayers.Remove(restoredLayer);
            }
        }
    }
}
