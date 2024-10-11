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
            if (_layerDictionary == null)
            {
                Type baseBrainType = typeof(BaseBrain);
                Type aiCoreStrategyType = baseBrainType.BaseType;

                _layerDictionary = AccessTools.Field(aiCoreStrategyType, "dictionary_0");
            }

            return _layerDictionary.GetValue(brain) as Dictionary<int, AICoreLogicLayerClass>;
        }

        internal static BrainManager.ExcludeLayerInfo FindExcludeLayerInfo(string layerName, IEnumerable<string> exactBrainNames, IEnumerable<WildSpawnType> exactRoles)
        {
            foreach (BrainManager.ExcludeLayerInfo excludeLayerInfo in BrainManager.Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.excludeLayerName != layerName)
                {
                    continue;
                }

                if (!Utils.HasSameContents(excludeLayerInfo.affectedRoles, exactRoles))
                {
                    continue;
                }

                if (!Utils.HasSameContents(excludeLayerInfo.affectedBrainNames, exactBrainNames))
                {
                    continue;
                }

                return excludeLayerInfo;
            }

            return null;
        }

        internal static bool CheckIfExcludeLayerInfosMustSplit(string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            bool excludeLayerInfosWereSplit = false;

            List<BrainManager.ExcludeLayerInfo> newExcludeLayerInfos = new List<BrainManager.ExcludeLayerInfo>();

            foreach (BrainManager.ExcludeLayerInfo excludeLayerInfo in BrainManager.Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.excludeLayerName != layerName)
                {
                    continue;
                }

                if (!Utils.HasSameContents(excludeLayerInfo.affectedRoles, newRoles))
                {
                    continue;
                }

                IEnumerable<string> sameBrainNames = excludeLayerInfo.affectedBrainNames.Intersect(newBrainNames);
                if (!sameBrainNames.Any())
                {
                    continue;
                }

                IEnumerable<string> remainingBrainNamesOld = excludeLayerInfo.affectedBrainNames.Except(sameBrainNames);
                IEnumerable<string> remainingBrainNamesNew = newBrainNames.Except(sameBrainNames);
                if (!remainingBrainNamesOld.Any() && !remainingBrainNamesNew.Any())
                {
                    continue;
                }

                if (FindExcludeLayerInfo(layerName, sameBrainNames, newRoles) != null)
                {
                    continue;
                }

                Logger.CreateLogSource("BIGBRAIN").LogInfo($"Splitting {excludeLayerInfo.excludeLayerName} for {string.Join(", ", newRoles)} into two brain groups:");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"1) {string.Join(", ", sameBrainNames)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"2) {string.Join(", ", remainingBrainNamesOld)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"3) {string.Join(", ", remainingBrainNamesNew)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"New) {string.Join(", ", newBrainNames)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"Old) {string.Join(", ", excludeLayerInfo.affectedBrainNames)}");

                excludeLayerInfo.affectedBrainNames = sameBrainNames.ToList();
                excludeLayerInfosWereSplit = true;

                if (remainingBrainNamesOld.Any())
                {
                    BrainManager.ExcludeLayerInfo splitLayerInfo = new BrainManager.ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, remainingBrainNamesOld.ToList(), newRoles);
                    newExcludeLayerInfos.Add(splitLayerInfo);
                }

                if (remainingBrainNamesNew.Any())
                {
                    BrainManager.ExcludeLayerInfo splitLayerInfo = new BrainManager.ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, remainingBrainNamesNew.ToList(), newRoles);
                    newExcludeLayerInfos.Add(splitLayerInfo);
                }

                break;
            }

            foreach (BrainManager.ExcludeLayerInfo newExcludeLayerInfo in newExcludeLayerInfos)
            {
                BrainManager.Instance.ExcludeLayers.Add(newExcludeLayerInfo);
            }

            newExcludeLayerInfos.Clear();

            foreach (BrainManager.ExcludeLayerInfo excludeLayerInfo in BrainManager.Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.excludeLayerName != layerName)
                {
                    continue;
                }

                if (!Utils.HasSameContents(excludeLayerInfo.affectedBrainNames, newBrainNames))
                {
                    continue;
                }

                IEnumerable<WildSpawnType> sameRoles = excludeLayerInfo.affectedRoles.Intersect(newRoles);
                if (!sameRoles.Any())
                {
                    continue;
                }

                IEnumerable<WildSpawnType> remainingRolesOld = excludeLayerInfo.affectedRoles.Except(sameRoles);
                IEnumerable<WildSpawnType> remainingRolesNew = newRoles.Except(sameRoles);
                if (!remainingRolesOld.Any() && !remainingRolesNew.Any())
                {
                    continue;
                }

                if (FindExcludeLayerInfo(layerName, newBrainNames, sameRoles) != null)
                {
                    continue;
                }

                Logger.CreateLogSource("BIGBRAIN").LogInfo($"Splitting {excludeLayerInfo.excludeLayerName} for {string.Join(", ", newBrainNames)} into two role groups:");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"1) {string.Join(", ", sameRoles)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"2) {string.Join(", ", remainingRolesOld)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"3) {string.Join(", ", remainingRolesNew)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"New) {string.Join(", ", newRoles)}");
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"Old) {string.Join(", ", excludeLayerInfo.affectedRoles)}");

                excludeLayerInfo.affectedRoles = sameRoles.ToList();
                excludeLayerInfosWereSplit = true;

                if (remainingRolesOld.Any())
                {
                    BrainManager.ExcludeLayerInfo splitLayerInfo = new BrainManager.ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, newBrainNames, remainingRolesOld.ToList());
                    newExcludeLayerInfos.Add(splitLayerInfo);
                }

                if (remainingRolesNew.Any())
                {
                    BrainManager.ExcludeLayerInfo splitLayerInfo = new BrainManager.ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, newBrainNames, remainingRolesNew.ToList());
                    newExcludeLayerInfos.Add(splitLayerInfo);
                }

                break;
            }

            foreach (BrainManager.ExcludeLayerInfo newExcludeLayerInfo in newExcludeLayerInfos)
            {
                BrainManager.Instance.ExcludeLayers.Add(newExcludeLayerInfo);
            }

            return excludeLayerInfosWereSplit;
        }

        internal static int RemoveDuplicateExcludeLayerInfos()
        {
            List<BrainManager.ExcludeLayerInfo> duplicateLayerInfos = new List<BrainManager.ExcludeLayerInfo>();

            foreach (BrainManager.ExcludeLayerInfo excludeLayerInfo1 in BrainManager.Instance.ExcludeLayers)
            {
                if (duplicateLayerInfos.Contains(excludeLayerInfo1))
                {
                    continue;
                }

                foreach (BrainManager.ExcludeLayerInfo excludeLayerInfo2 in BrainManager.Instance.ExcludeLayers)
                {
                    if (excludeLayerInfo1 == excludeLayerInfo2)
                    {
                        continue;
                    }

                    if (duplicateLayerInfos.Contains(excludeLayerInfo2))
                    {
                        continue;
                    }

                    if (excludeLayerInfo1.excludeLayerName != excludeLayerInfo2.excludeLayerName)
                    {
                        continue;
                    }

                    if (!Utils.HasSameContents(excludeLayerInfo1.affectedRoles, excludeLayerInfo2.affectedRoles))
                    {
                        continue;
                    }

                    if (!Utils.HasSameContents(excludeLayerInfo1.affectedBrainNames, excludeLayerInfo2.affectedBrainNames))
                    {
                        continue;
                    }

                    duplicateLayerInfos.Add(excludeLayerInfo2);
                }
            }

            foreach (BrainManager.ExcludeLayerInfo duplicateLayerInfo in duplicateLayerInfos)
            {
                BrainManager.Instance.ExcludeLayers.Remove(duplicateLayerInfo);

                Logger.CreateLogSource("BIGBRAIN").LogWarning($"Removed duplicate layer for {duplicateLayerInfo.excludeLayerName} for brains {string.Join(", ", duplicateLayerInfo.affectedBrainNames)} and roles {string.Join(", ", duplicateLayerInfo.affectedRoles)}");
            }

            return duplicateLayerInfos.Count;
        }

        internal static void RemoveAllExcludedLayers(this BotOwner botOwner)
        {
            foreach (BrainManager.ExcludeLayerInfo excludeLayer in BrainManager.Instance.ExcludeLayers)
            {
                botOwner.RemoveLayerForBot(excludeLayer);
            }
        }

        internal static void RemoveLayerForBot(this BotOwner botOwner, BrainManager.ExcludeLayerInfo excludeLayer)
        {
            if (!excludeLayer.AffectsBot(botOwner))
            {
                return;
            }    

            botOwner.RemoveLayerForBot(excludeLayer.excludeLayerName);
        }

        internal static void RemoveLayerForBot(this BotOwner botOwner, string layerName)
        {
            // Get all brain layers the bot currently has
            Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();

            int layerIndexToRemove = -1;

            foreach (int index in botBrainLayerDictionary.Keys)
            {
                if (botBrainLayerDictionary[index].Name() != layerName)
                {
                    continue;
                }

                Logger.CreateLogSource("BIGBRAIN").LogInfo("Removing " + layerName + " from " + botOwner.name + " (" + botOwner.Brain.BaseBrain.ShortName() + ", " + botOwner.Profile.Info.Settings.Role + ")");

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
            // Get all brain layers the bot currently has
            Dictionary<int, AICoreLogicLayerClass> botBrainLayerDictionary = botOwner.Brain.BaseBrain.GetBrainLayerDictionary();

            List<BrainManager.ExcludedLayerInfo> restoredLayers = new List<BrainManager.ExcludedLayerInfo>();

            foreach (BrainManager.ExcludedLayerInfo excludedLayer in BrainManager.Instance.ExcludedLayers)
            {
                if ((excludedLayer.BotOwner != botOwner) || (excludedLayer.LayerName != layerName))
                {
                    continue;
                }

                Logger.CreateLogSource("BIGBRAIN").LogInfo("Restoring " + layerName + " to " + botOwner.name + " (" + botOwner.Brain.BaseBrain.ShortName() + ", " + botOwner.Profile.Info.Settings.Role + ")");

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
