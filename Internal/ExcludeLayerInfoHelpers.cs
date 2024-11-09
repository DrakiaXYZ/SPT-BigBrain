using BepInEx.Logging;
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

        internal static bool CanAllSettingsBeApplied(string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            foreach (string brainName in brainNames)
            {
                foreach (WildSpawnType role in roles)
                {
                    IEnumerable<string> brainNameToCheck = brainNames.Where(n => n == brainName);
                    IEnumerable<WildSpawnType> roleToCheck = roles.Where(n => n == role);

                    bool matchingExcludeLayerInfoFound = false;

                    foreach (ExcludeLayerInfo excludeLayerInfo in Instance.ExcludeLayers)
                    {
                        if (excludeLayerInfo.HasAllSettings(layerName, brainNameToCheck, roleToCheck))
                        {
                            matchingExcludeLayerInfoFound = true;
                            break;
                        }
                    }

                    if (!matchingExcludeLayerInfoFound)
                    {
                        return false;
                    }
                }
            }

            return true;
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

        internal static bool HasOneOrMoreOfEachSetting(this ExcludeLayerInfo excludeLayerInfo, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            if (excludeLayerInfo.excludeLayerName != layerName)
            {
                return false;
            }

            if (!excludeLayerInfo.ContainsAny(brainNames))
            {
                return false;
            }

            if (!excludeLayerInfo.ContainsAny(roles))
            {
                return false;
            }

            return true;
        }

        internal static void SplitOrAddForSettings(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            if (FindExcludeLayerInfo(layerName, brainNames, roles) != null)
            {
                return;
            }

            /*if (CanAllSettingsBeApplied(layerName, brainNames, roles))
            {
                Logger.CreateLogSource("BIGBRAIN").LogInfo($"Found all ExcludeLayerInfos for {layerName} for brains {CreateItemsText(brainNames)} and roles {CreateItemsText(roles)}");
                return;
            }*/

            int excludeLayersAdded = SplitForSettings(layerName, brainNames, roles);
            if (excludeLayersAdded == 0)
            {
                Instance.ExcludeLayers.Add(new ExcludeLayerInfo(layerName, brainNames, roles));

                Logger.CreateLogSource("BIGBRAIN").LogWarning($"Added exclusion for {layerName} for brains {CreateItemsText(brainNames)} and roles {CreateItemsText(roles)}");
            }
        }

        internal static int SplitForSettings(string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            int splitCount = 0;

            List<ExcludeLayerInfo> newExcludeLayerInfos = new List<ExcludeLayerInfo>();
            List<ExcludeLayerInfo> obsoleteExcludeLayerInfos = new List<ExcludeLayerInfo>();

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

                obsoleteExcludeLayerInfos.Add(excludeLayerInfo);
            }

            foreach (ExcludeLayerInfo obsoleteExcludeLayerInfo in obsoleteExcludeLayerInfos)
            {
                Instance.ExcludeLayers.Remove(obsoleteExcludeLayerInfo);
            }

            foreach (ExcludeLayerInfo newExcludeLayerInfo in newExcludeLayerInfos)
            {
                Instance.ExcludeLayers.Add(newExcludeLayerInfo);
            }

            newExcludeLayerInfos.Clear();
            obsoleteExcludeLayerInfos.Clear();

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

                obsoleteExcludeLayerInfos.Add(excludeLayerInfo);
            }

            foreach (ExcludeLayerInfo obsoleteExcludeLayerInfo in obsoleteExcludeLayerInfos)
            {
                Instance.ExcludeLayers.Remove(obsoleteExcludeLayerInfo);
            }

            foreach (ExcludeLayerInfo newExcludeLayerInfo in newExcludeLayerInfos)
            {
                Instance.ExcludeLayers.Add(newExcludeLayerInfo);
            }

            return splitCount;
        }

        private static IEnumerable<ExcludeLayerInfo> splitForBrains(this ExcludeLayerInfo excludeLayerInfo, string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            var brainNamesSplitCheck = new CollectionCanSplitCheck<string>(excludeLayerInfo.affectedBrainNames, newBrainNames);
            if (!brainNamesSplitCheck.CanSplit)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            IEnumerable<IEnumerable<string>> newBrainNameGroups = brainNamesSplitCheck
                .GetAllItemsCollections()
                .Where(collection => FindExcludeLayerInfo(layerName, collection, newRoles) == null);

            if (!newBrainNameGroups.Any())
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Splitting exclusion for {excludeLayerInfo.excludeLayerName} for {CreateItemsText(newRoles)} into brain groups:");

            List<ExcludeLayerInfo> newExcludeLayerInfos = new List<ExcludeLayerInfo>();

            foreach (IEnumerable<string> brainNames in newBrainNameGroups)
            {
                var splitLayerInfo = new ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, brainNames.ToList(), newRoles);
                newExcludeLayerInfos.Add(splitLayerInfo);

                Logger.CreateLogSource("BIGBRAIN").LogInfo($"-> {CreateItemsText(brainNames)}");
            }

            Logger.CreateLogSource("BIGBRAIN").LogInfo($"New) {CreateItemsText(newBrainNames)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Old) {CreateItemsText(excludeLayerInfo.affectedBrainNames)}");

            return newExcludeLayerInfos;
        }

        private static IEnumerable<ExcludeLayerInfo> splitForRoles(this ExcludeLayerInfo excludeLayerInfo, string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            var rolesSplitCheck = new CollectionCanSplitCheck<WildSpawnType>(excludeLayerInfo.affectedRoles, newRoles);
            if (!rolesSplitCheck.CanSplit)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            IEnumerable<IEnumerable<WildSpawnType>> newRoleGroups = rolesSplitCheck
                .GetAllItemsCollections()
                .Where(collection => FindExcludeLayerInfo(layerName, newBrainNames, collection) == null);

            if (!newRoleGroups.Any())
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Splitting exclusion for {excludeLayerInfo.excludeLayerName} for {CreateItemsText(newBrainNames)} into role groups:");
            
            List<ExcludeLayerInfo> newExcludeLayerInfos = new List<ExcludeLayerInfo>();

            foreach (IEnumerable<WildSpawnType> roles in newRoleGroups)
            {
                var splitLayerInfo = new ExcludeLayerInfo(excludeLayerInfo.excludeLayerName, newBrainNames, roles.ToList());
                newExcludeLayerInfos.Add(splitLayerInfo);

                Logger.CreateLogSource("BIGBRAIN").LogInfo($"-> {CreateItemsText(roles)}");
            }

            Logger.CreateLogSource("BIGBRAIN").LogInfo($"New) {CreateItemsText(newRoles)}");
            Logger.CreateLogSource("BIGBRAIN").LogInfo($"Old) {CreateItemsText(excludeLayerInfo.affectedRoles)}");

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

                        Logger.CreateLogSource("BIGBRAIN").LogWarning($"Found duplicate layer for {excludeLayerInfo2.excludeLayerName} for brains {CreateItemsText(excludeLayerInfo2.affectedBrainNames)} and roles {CreateItemsText(excludeLayerInfo2.affectedRoles)}");
                    }
                }
            }

            foreach (ExcludeLayerInfo duplicateLayerInfo in duplicateLayerInfos)
            {
                Instance.ExcludeLayers.Remove(duplicateLayerInfo);
            }

            return duplicateLayerInfos.Count;
        }

        internal static string CreateItemsText<T>(IEnumerable<T> items, int maxItemsToList = 10)
        {
            int itemCount = items.Count();

            if (itemCount > maxItemsToList)
            {
                return $"{itemCount} {typeof(T).Name}s";
            }

            return string.Join(", ", items);
        }
    }
}
