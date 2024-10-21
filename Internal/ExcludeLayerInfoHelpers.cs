using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static DrakiaXYZ.BigBrain.Brains.BrainManager;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal static class ExcludeLayerInfoHelpers
    {
        internal static ExcludeLayerInfo FindExcludeLayerInfo(string layerName, IEnumerable<string> exactBrainNames, IEnumerable<WildSpawnType> exactRoles)
        {
            foreach (ExcludeLayerInfo excludeLayerInfo in Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.IsExactMatch(layerName, exactBrainNames, exactRoles))
                {
                    return excludeLayerInfo;
                }
            }

            return null;
        }

        internal static IEnumerable<ExcludeLayerInfo> FindAllExcludeLayerInfosWithSettings(string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            foreach (ExcludeLayerInfo excludeLayerInfo in Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.HasAllSettings(layerName, brainNames, roles))
                {
                    yield return excludeLayerInfo;
                }
            }
        }

        internal static bool IsExactMatch(this ExcludeLayerInfo excludeLayerInfo, string layerName, IEnumerable<string> exactBrainNames, IEnumerable<WildSpawnType> exactRoles)
        {
            if (excludeLayerInfo.excludeLayerName != layerName)
            {
                return false;
            }

            if (!Utils.HasSameContents(excludeLayerInfo.affectedRoles, exactRoles))
            {
                return false;
            }

            if (!Utils.HasSameContents(excludeLayerInfo.affectedBrainNames, exactBrainNames))
            {
                return false;
            }

            return true;
        }

        internal static bool HasSameSettings(this ExcludeLayerInfo excludeLayerInfo1, ExcludeLayerInfo excludeLayerInfo2)
        {
            return excludeLayerInfo1.IsExactMatch(excludeLayerInfo2.excludeLayerName, excludeLayerInfo2.affectedBrainNames, excludeLayerInfo2.affectedRoles);
        }

        internal static bool HasAllSettings(this ExcludeLayerInfo excludeLayerInfo, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            if (excludeLayerInfo.excludeLayerName != layerName)
            {
                return false;
            }

            if (!excludeLayerInfo.ContainsAll(brainNames))
            {
                return false;
            }

            if (!excludeLayerInfo.ContainsAll(roles))
            {
                return false;
            }

            return true;
        }

        internal static void SplitOrAddForSettings(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            bool layerExists = FindExcludeLayerInfo(layerName, brainNames, roles) != null;
            if (layerExists)
            {
                return;
            }

            int excludeLayersAdded = SplitForSettings(layerName, brainNames, roles);
            if (excludeLayersAdded == 0)
            {
                Instance.ExcludeLayers.Add(new ExcludeLayerInfo(layerName, brainNames, roles));
            }

            RemoveDuplicates();
        }

        internal static int SplitForSettings(string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            int splitCount = 0;

            List<ExcludeLayerInfo> newExcludeLayerInfos = new List<ExcludeLayerInfo>();

            foreach (ExcludeLayerInfo excludeLayerInfo in Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.excludeLayerName != layerName)
                {
                    continue;
                }

                if (!Utils.HasSameContents(excludeLayerInfo.affectedRoles, newRoles))
                {
                    continue;
                }

                IEnumerable<ExcludeLayerInfo> newExcludeLayerInfosForBrains = excludeLayerInfo.splitForBrains(layerName, newBrainNames, newRoles);
                if (!newExcludeLayerInfosForBrains.Any())
                {
                    continue;
                }

                newExcludeLayerInfos.AddRange(newExcludeLayerInfosForBrains);
                splitCount += newExcludeLayerInfosForBrains.Count();

                break;
            }

            foreach (ExcludeLayerInfo newExcludeLayerInfo in newExcludeLayerInfos)
            {
                Instance.ExcludeLayers.Add(newExcludeLayerInfo);
            }

            newExcludeLayerInfos.Clear();

            foreach (ExcludeLayerInfo excludeLayerInfo in Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.excludeLayerName != layerName)
                {
                    continue;
                }

                if (!Utils.HasSameContents(excludeLayerInfo.affectedBrainNames, newBrainNames))
                {
                    continue;
                }

                IEnumerable<ExcludeLayerInfo> newExcludeLayerInfosForRoles = excludeLayerInfo.splitForRoles(layerName, newBrainNames, newRoles);
                if (!newExcludeLayerInfosForRoles.Any())
                {
                    continue;
                }

                newExcludeLayerInfos.AddRange(newExcludeLayerInfosForRoles);
                splitCount += newExcludeLayerInfosForRoles.Count();

                break;
            }

            foreach (ExcludeLayerInfo newExcludeLayerInfo in newExcludeLayerInfos)
            {
                Instance.ExcludeLayers.Add(newExcludeLayerInfo);
            }

            return splitCount;
        }

        private static IEnumerable<ExcludeLayerInfo> splitForBrains(this ExcludeLayerInfo excludeLayerInfo, string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            var canSplitCheck = new CollectionCanSplitCheck<string>(excludeLayerInfo.affectedBrainNames, newBrainNames);
            if (!canSplitCheck.CanSplit)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            if (FindExcludeLayerInfo(layerName, canSplitCheck.SameItems, newRoles) != null)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Splitting {excludeLayerInfo.excludeLayerName} for {string.Join(", ", newRoles)} into two brain groups:");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"1) {string.Join(", ", canSplitCheck.SameItems)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"2) {string.Join(", ", canSplitCheck.RemainingItemsOld)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"3) {string.Join(", ", canSplitCheck.RemainingItemsNew)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"New) {string.Join(", ", newBrainNames)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Old) {string.Join(", ", excludeLayerInfo.affectedBrainNames)}");

            excludeLayerInfo.affectedBrainNames = canSplitCheck.SameItems.ToList();

            List<ExcludeLayerInfo> newExcludeLayerInfos = new List<ExcludeLayerInfo>();

            foreach (IEnumerable<string> remainingBrainNames in canSplitCheck.GetRemainingItemsCollections())
            {
                var splitLayerInfo = new ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, remainingBrainNames.ToList(), newRoles);
                newExcludeLayerInfos.Add(splitLayerInfo);
            }

            return newExcludeLayerInfos;
        }

        private static IEnumerable<ExcludeLayerInfo> splitForRoles(this ExcludeLayerInfo excludeLayerInfo, string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            var canSplitCheck = new CollectionCanSplitCheck<WildSpawnType>(excludeLayerInfo.affectedRoles, newRoles);
            if (!canSplitCheck.CanSplit)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            if (FindExcludeLayerInfo(layerName, newBrainNames, canSplitCheck.SameItems) != null)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Splitting {excludeLayerInfo.excludeLayerName} for {string.Join(", ", newBrainNames)} into two role groups:");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"1) {string.Join(", ", canSplitCheck.SameItems)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"2) {string.Join(", ", canSplitCheck.RemainingItemsOld)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"3) {string.Join(", ", canSplitCheck.RemainingItemsNew)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"New) {string.Join(", ", newRoles)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Old) {string.Join(", ", excludeLayerInfo.affectedRoles)}");

            excludeLayerInfo.affectedRoles = canSplitCheck.SameItems.ToList();

            List<ExcludeLayerInfo> newExcludeLayerInfos = new List<ExcludeLayerInfo>();

            foreach (IEnumerable<WildSpawnType> remainingRoles in canSplitCheck.GetRemainingItemsCollections())
            {
                var splitLayerInfo = new ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, newBrainNames, remainingRoles.ToList());
                newExcludeLayerInfos.Add(splitLayerInfo);
            }

            return newExcludeLayerInfos;
        }

        internal static int RemoveDuplicates()
        {
            List<ExcludeLayerInfo> duplicateLayerInfos = new List<ExcludeLayerInfo>();

            foreach (ExcludeLayerInfo excludeLayerInfo1 in Instance.ExcludeLayers)
            {
                if (duplicateLayerInfos.Contains(excludeLayerInfo1))
                {
                    continue;
                }

                foreach (ExcludeLayerInfo excludeLayerInfo2 in Instance.ExcludeLayers)
                {
                    if (excludeLayerInfo1 == excludeLayerInfo2)
                    {
                        continue;
                    }

                    if (duplicateLayerInfos.Contains(excludeLayerInfo2))
                    {
                        continue;
                    }

                    if (excludeLayerInfo1.HasSameSettings(excludeLayerInfo2))
                    {
                        duplicateLayerInfos.Add(excludeLayerInfo2);
                    }
                }
            }

            foreach (ExcludeLayerInfo duplicateLayerInfo in duplicateLayerInfos)
            {
                Instance.ExcludeLayers.Remove(duplicateLayerInfo);

                Logger.CreateLogSource("BIGBRAIN").LogWarning($"Removed duplicate layer for {duplicateLayerInfo.excludeLayerName} for brains {string.Join(", ", duplicateLayerInfo.affectedBrainNames)} and roles {string.Join(", ", duplicateLayerInfo.affectedRoles)}");
            }

            return duplicateLayerInfos.Count;
        }
    }
}
