using EFT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static DrakiaXYZ.BigBrain.Brains.BrainManager;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal static class ExcludeLayerHelpers
    {
        internal static bool ExcludeLayerExists(string layerName, IEnumerable<string> exactBrainNames, IEnumerable<WildSpawnType> exactRoles)
        {
            return FindExcludeLayer(layerName, exactBrainNames, exactRoles) != null;
        }

        internal static ExcludeLayerInfo FindExcludeLayer(string layerName, IEnumerable<string> exactBrainNames, IEnumerable<WildSpawnType> exactRoles)
        {
            foreach (ExcludeLayerInfo excludeLayerInfo in Instance.ExcludeLayers)
            {
                if (excludeLayerInfo.HasSameSettings(layerName, exactBrainNames, exactRoles))
                {
                    return excludeLayerInfo;
                }
            }

            return null;
        }

        internal static bool HasSameSettings(this ExcludeLayerInfo excludeLayer1, ExcludeLayerInfo excludeLayer2)
        {
            if (excludeLayer1 == null)
            {
                throw new ArgumentNullException(nameof(excludeLayer1));
            }

            if (excludeLayer2 == null)
            {
                throw new ArgumentNullException(nameof(excludeLayer2));
            }

            return excludeLayer1.HasSameSettings(excludeLayer2.excludeLayerName, excludeLayer2.affectedBrainNames, excludeLayer2.affectedRoles);
        }

        internal static bool HasSameSettings(this ExcludeLayerInfo excludeLayer, string layerName, IEnumerable<string> exactBrainNames, IEnumerable<WildSpawnType> exactRoles)
        {
            if (excludeLayer == null)
            {
                throw new ArgumentNullException(nameof(excludeLayer));
            }

            if (excludeLayer.excludeLayerName != layerName)
            {
                return false;
            }

            if (!Utils.HasSameContents(excludeLayer.affectedRoles, exactRoles))
            {
                return false;
            }

            if (!Utils.HasSameContents(excludeLayer.affectedBrainNames, exactBrainNames))
            {
                return false;
            }

            return true;
        }

        internal static bool HasOneOrMoreOfEachSetting(this ExcludeLayerInfo excludeLayer, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            if (excludeLayer == null)
            {
                throw new ArgumentNullException(nameof(excludeLayer));
            }

            if (excludeLayer.excludeLayerName != layerName)
            {
                return false;
            }

            if (!excludeLayer.ContainsAny(brainNames))
            {
                return false;
            }

            if (!excludeLayer.ContainsAny(roles))
            {
                return false;
            }

            return true;
        }

        internal static int RemoveDuplicates()
        {
            List<ExcludeLayerInfo> duplicateLayers = new List<ExcludeLayerInfo>();

            foreach (ExcludeLayerInfo excludeLayer1 in Instance.ExcludeLayers)
            {
                if (duplicateLayers.Contains(excludeLayer1))
                {
                    continue;
                }

                foreach (ExcludeLayerInfo excludeLayer2 in Instance.ExcludeLayers)
                {
                    if (excludeLayer1 == excludeLayer2)
                    {
                        continue;
                    }

                    if (duplicateLayers.Contains(excludeLayer2))
                    {
                        continue;
                    }

                    if (excludeLayer1.HasSameSettings(excludeLayer2))
                    {
                        duplicateLayers.Add(excludeLayer2);

                        // Duplicate layers should not exist, but this message only needs to be a warning because this method removes them. 
                        BigBrainPlugin.BigBrainLogger.LogWarning($"Found duplicate layer for {excludeLayer2.excludeLayerName} for brains {Utils.CreateCollectionText(excludeLayer2.affectedBrainNames)} and roles {Utils.CreateCollectionText(excludeLayer2.affectedRoles)}");
                    }
                }
            }

            foreach (ExcludeLayerInfo duplicateLayer in duplicateLayers)
            {
                Instance.ExcludeLayers.Remove(duplicateLayer);
            }

            return duplicateLayers.Count;
        }

        internal static void SplitOrAddForSettings(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            // If an Exclude Layer already exists for the settings, there's no need to do anything else
            if (ExcludeLayerExists(layerName, brainNames, roles))
            {
                return;
            }

            int excludeLayersAdded = SplitForSettings(layerName, brainNames, roles);

            // If no ExcludeLayers were split, a new ExcludeLayer needs to be created
            if (excludeLayersAdded == 0)
            {
                Instance.ExcludeLayers.Add(new ExcludeLayerInfo(layerName, brainNames, roles));

#if DEBUG
                BigBrainPlugin.BigBrainLogger.LogDebug($"Added exclusion for {layerName} for brains {Utils.CreateCollectionText(brainNames)} and roles {Utils.CreateCollectionText(roles)}");
#endif
            }
        }

        internal static int SplitForSettings(string layerName, List<string> newBrainNames, List<WildSpawnType> newRoles)
        {
            // [For DEBUG builds] This message is only written to the console if any new Exclude Layers are created
            string loggingMessage = null;
#if DEBUG
            loggingMessage = $"Splitting exclusion for {layerName} for brains {Utils.CreateCollectionText(newBrainNames)} and roles {Utils.CreateCollectionText(newRoles)} into groups:";
#endif

            // Split layers that have the same name and roles for the new brains
            int splitCount = splitForSettingsGeneric
            (
                hasMatchingSettings: (excludeLayer) => (excludeLayer.excludeLayerName == layerName) && Utils.HasSameContents(excludeLayer.affectedRoles, newRoles),
                excludeLayerExists: (brainNames) => ExcludeLayerExists(layerName, brainNames, newRoles),
                splitCheckGenerator: (excludeLayer) => new CollectionCanSplitCheck<string>(excludeLayer.affectedBrainNames, newBrainNames),
                excludeLayerGenerator: (brainNames) => new ExcludeLayerInfo(layerName, brainNames, newRoles),
                firstLoggingMessage: loggingMessage
            );

            // Split layers that have the same name and brains for the new roles
            splitCount += splitForSettingsGeneric
            (
                hasMatchingSettings: (excludeLayer) => (excludeLayer.excludeLayerName == layerName) && Utils.HasSameContents(excludeLayer.affectedBrainNames, newBrainNames),
                excludeLayerExists: (roles) => ExcludeLayerExists(layerName, newBrainNames, roles),
                splitCheckGenerator: (excludeLayer) => new CollectionCanSplitCheck<WildSpawnType>(excludeLayer.affectedRoles, newRoles),
                excludeLayerGenerator: (roles) => new ExcludeLayerInfo(layerName, newBrainNames, roles),
                firstLoggingMessage: loggingMessage
            );

            return splitCount;
        }

        private static int splitForSettingsGeneric<T>
        (
            Predicate<ExcludeLayerInfo> hasMatchingSettings,
            Predicate<IEnumerable<T>> excludeLayerExists,
            Func<ExcludeLayerInfo, CollectionCanSplitCheck<T>> splitCheckGenerator,
            Func<List<T>, ExcludeLayerInfo> excludeLayerGenerator,
            string firstLoggingMessage
        )
        {
            if (hasMatchingSettings == null)
            {
                throw new ArgumentNullException(nameof(hasMatchingSettings));
            }

            if (splitCheckGenerator == null)
            {
                throw new ArgumentNullException(nameof(splitCheckGenerator));
            }

            int splitCount = 0;

            // Only initialize these if they're needed
            List<ExcludeLayerInfo> allNewExcludeLayers = null;
            List<ExcludeLayerInfo> obsoleteExcludeLayers = null;

            foreach (ExcludeLayerInfo excludeLayer in Instance.ExcludeLayers)
            {
                if (!hasMatchingSettings(excludeLayer))
                {
                    continue;
                }

                CollectionCanSplitCheck<T> splitCheck = splitCheckGenerator(excludeLayer);
                if (splitCheck == null)
                {
                    throw new InvalidOperationException("Cannot generate new Exclude Layers from a null CollectionCanSplitCheck instance");
                }

                IEnumerable<ExcludeLayerInfo> newExcludeLayers = splitCheck.generateNewExcludeLayers(excludeLayerExists, excludeLayerGenerator, firstLoggingMessage);
                if (!newExcludeLayers.Any())
                {
                    continue;
                }

                // If these are the first new layers to add, the lists need to be initialized
                if (splitCount == 0)
                {
                    allNewExcludeLayers = new List<ExcludeLayerInfo>();
                    obsoleteExcludeLayers = new List<ExcludeLayerInfo>();
                }

                allNewExcludeLayers.AddRange(newExcludeLayers);
                splitCount += newExcludeLayers.Count();

                // If an ExcludeLayer was split, the original one needs to be removed
                obsoleteExcludeLayers.Add(excludeLayer);
            }

            if (splitCount > 0)
            {
                foreach (ExcludeLayerInfo obsoleteExcludeLayer in obsoleteExcludeLayers)
                {
                    Instance.ExcludeLayers.Remove(obsoleteExcludeLayer);
                }

                foreach (ExcludeLayerInfo newExcludeLayer in allNewExcludeLayers)
                {
                    Instance.ExcludeLayers.Add(newExcludeLayer);
                }
            }

            return splitCount;
        }

        private static IEnumerable<ExcludeLayerInfo> generateNewExcludeLayers<T>
        (
            this CollectionCanSplitCheck<T> splitCheck,
            Predicate<IEnumerable<T>> excludeLayerExists,
            Func<List<T>, ExcludeLayerInfo> excludeLayerGenerator,
            string firstLoggingMessage
        )
        {
            if (excludeLayerExists == null)
            {
                throw new ArgumentNullException(nameof(excludeLayerExists));
            }

            if (excludeLayerGenerator == null)
            {
                throw new ArgumentNullException(nameof(excludeLayerGenerator));
            }

            if (!splitCheck.CanSplit)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

            IEnumerable<IEnumerable<T>> newCollections = splitCheck
                .GetAllItemsCollections()
                .Where(collection => !excludeLayerExists(collection));

            if (!newCollections.Any())
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

#if DEBUG
            BigBrainPlugin.BigBrainLogger.LogInfo(firstLoggingMessage);
#endif

            List<ExcludeLayerInfo> newExcludeLayers = new List<ExcludeLayerInfo>();
            foreach (IEnumerable<T> collection in newCollections)
            {
                ExcludeLayerInfo newExcludeLayer = excludeLayerGenerator(collection.ToList());
                newExcludeLayers.Add(newExcludeLayer);

#if DEBUG
                BigBrainPlugin.BigBrainLogger.LogInfo($"-> {Utils.CreateCollectionText(collection)}");
#endif
            }

            return newExcludeLayers;
        }
    }
}
