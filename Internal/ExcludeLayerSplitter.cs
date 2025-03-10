﻿using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static DrakiaXYZ.BigBrain.Brains.BrainManager;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class ExcludeLayerSplitter
    {
        private string _layerName;
        private List<string> _brainNames;
        private List<WildSpawnType> _roles;

        // The constructor should be private because the object should only be used for one split operation. This could be a static class, but the layer
        // settings are used so frequently that it becomes tedious to pass them to each method. 
        private ExcludeLayerSplitter(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            _layerName = layerName;
            _brainNames = brainNames;
            _roles = roles;
        }

        internal static int SplitOrAddLayersForSettings(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            return new ExcludeLayerSplitter(layerName, brainNames, roles).splitOrAddLayersForSettings();
        }

        internal static int SplitLayersForSettings(string layerName, List<string> brainNames, List<WildSpawnType> roles)
        {
            return new ExcludeLayerSplitter(layerName, brainNames, roles).splitLayersForSettings();
        }

        private int splitOrAddLayersForSettings()
        {
            // If an Exclude Layer already exists for the settings, there's no need to do anything else
            if (Instance.ExcludeLayers.DoesLayerExist(_layerName, _brainNames, _roles))
            {
                return 0;
            }

            int excludeLayersAdded = splitLayersForSettings();
            if (excludeLayersAdded > 0)
            {
                return excludeLayersAdded;
            }

            // If no layers were created via splitLayersForSettings(), check if layers already exist for the settings
            if (Instance.ExcludeLayers.GetFirstWithOneOrMoreOfEachSetting(_layerName, _brainNames, _roles) != null)
            {
                return 0;
            }

            Instance.ExcludeLayers.Add(new ExcludeLayerInfo(_layerName, _brainNames, _roles));

#if DEBUG
            BigBrainPlugin.BigBrainLogger.LogDebug($"Added exclusion for {_layerName} for brains {Utils.CreateCollectionText(_brainNames)} and roles {Utils.CreateCollectionText(_roles)}");
#endif

            return 1;
        }

        private int splitLayersForSettings()
        {
            // Split layers that have the same name and roles for the new brains
            int splitCount = splitForSettingsGeneric
            (
                hasMatchingSettings: (excludeLayer) => Utils.HasSameContents(excludeLayer.roles, _roles),
                excludeLayerExists: (brainNames) => Instance.ExcludeLayers.DoesLayerExist(_layerName, brainNames, _roles),
                collectionIntersectionGenerator: (excludeLayer) => new CollectionIntersection<string>(excludeLayer.brainNames, _brainNames),
                excludeLayerGenerator: (brainNames) => new ExcludeLayerInfo(_layerName, brainNames, _roles)
            );

            // Split layers that have the same name and brains for the new roles
            splitCount += splitForSettingsGeneric
            (
                hasMatchingSettings: (excludeLayer) => Utils.HasSameContents(excludeLayer.brainNames, _brainNames),
                excludeLayerExists: (roles) => Instance.ExcludeLayers.DoesLayerExist(_layerName, _brainNames, roles),
                collectionIntersectionGenerator: (excludeLayer) => new CollectionIntersection<WildSpawnType>(excludeLayer.roles, _roles),
                excludeLayerGenerator: (roles) => new ExcludeLayerInfo(_layerName, _brainNames, roles)
            );

            // Duplicates are sometimes created when layers with the same settings are split from two different sets of settings. For example:
            //
            // - Existing brains 1: BossGluhar, BossKojaniy, BossSanitar, Tagilla, BossTest, Gifter, Killa, SectantPriest
            // - Existing brains 2: BossBully, FollowerBully, FollowerGluharAssault, FollowerGluharProtect, FollowerGluharScout, FollowerKojaniy, FollowerSanitar, TagillaFollower
            // - New brains: BossBully, BossGluhar, BossKojaniy, BossSanitar, Tagilla, BossTest, Gifter, Killa, SectantPriest
            //
            // - Differences between brains 1 and new brains: BossBully
            // - Common elements between brains 2 and new brains: BossBully
            int duplicates = Instance.ExcludeLayers.RemoveDuplicates();

            return splitCount - duplicates;
        }

        private int splitForSettingsGeneric<T>
        (
            Predicate<ExcludeLayerInfo> hasMatchingSettings,
            Predicate<IEnumerable<T>> excludeLayerExists,
            Func<ExcludeLayerInfo, CollectionIntersection<T>> collectionIntersectionGenerator,
            Func<List<T>, ExcludeLayerInfo> excludeLayerGenerator
        )
        {
            if (hasMatchingSettings == null)
            {
                throw new ArgumentNullException(nameof(hasMatchingSettings));
            }

            if (collectionIntersectionGenerator == null)
            {
                throw new ArgumentNullException(nameof(collectionIntersectionGenerator));
            }

            // Only initialize these if they're needed
            List<ExcludeLayerInfo> allNewExcludeLayers = null;
            List<ExcludeLayerInfo> obsoleteExcludeLayers = null;

            foreach (ExcludeLayerInfo excludeLayer in Instance.ExcludeLayers)
            {
                if (excludeLayer.excludeLayerName != _layerName)
                {
                    continue;
                }

                if (!hasMatchingSettings(excludeLayer))
                {
                    continue;
                }

                CollectionIntersection<T> collectionIntersection = collectionIntersectionGenerator(excludeLayer);
                if (collectionIntersection == null)
                {
                    throw new InvalidOperationException("Cannot generate new Exclude Layers from a null CollectionIntersection instance");
                }

                IEnumerable<ExcludeLayerInfo> newExcludeLayers = generateNewExcludeLayers(collectionIntersection, excludeLayerExists, excludeLayerGenerator);
                if (!newExcludeLayers.Any())
                {
                    continue;
                }

                // If these are the first new layers to add, the lists need to be initialized
                if (allNewExcludeLayers == null)
                {
                    allNewExcludeLayers = new List<ExcludeLayerInfo>();
                    obsoleteExcludeLayers = new List<ExcludeLayerInfo>();
                }

                allNewExcludeLayers.AddRange(newExcludeLayers);

                // Remove the original layer unless it matches the intersection or difference collections that were generated. In that case, the layer
                // settings are already reduced as much as possible. 
                var newElementCollections = collectionIntersection.GetAllElementCollections();
                if (newElementCollections.All(collection => !Utils.HasSameContents(collection, collectionIntersection.Collection1)))
                {
                    obsoleteExcludeLayers.Add(excludeLayer);

#if DEBUG
                    BigBrainPlugin.BigBrainLogger.LogDebug($"Removing original elements: {Utils.CreateCollectionText(collectionIntersection.Collection1)}");
#endif
                }
            }

            // If the lists are null, there are no layers to add or remove, so there's nothing else to do
            if (allNewExcludeLayers == null)
            {
                return 0;
            }

            foreach (ExcludeLayerInfo obsoleteExcludeLayer in obsoleteExcludeLayers)
            {
                Instance.ExcludeLayers.Remove(obsoleteExcludeLayer);
            }

            foreach (ExcludeLayerInfo newExcludeLayer in allNewExcludeLayers)
            {
                Instance.ExcludeLayers.Add(newExcludeLayer);
            }

            return allNewExcludeLayers.Count;
        }

        private IEnumerable<ExcludeLayerInfo> generateNewExcludeLayers<T>
        (
            CollectionIntersection<T> collectionIntersection,
            Predicate<IEnumerable<T>> excludeLayerExists,
            Func<List<T>, ExcludeLayerInfo> excludeLayerGenerator
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

            // The layer only needs to be split if it has some overlapping and unique content. If it has the same content, it's redundant. If it only
            // has unique content, it should be added as a separate layer. 
            if (!collectionIntersection.HasCommonAndUniqueElements)
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }
            
            // Only add layers that have new settings
            IEnumerable<IEnumerable<T>> newCollections = collectionIntersection
                .GetAllElementCollections()
                .Where(collection => !excludeLayerExists(collection));

            if (!newCollections.Any())
            {
                return Enumerable.Empty<ExcludeLayerInfo>();
            }

#if DEBUG
            BigBrainPlugin.BigBrainLogger.LogDebug($"Splitting exclusion for {_layerName} for brains {Utils.CreateCollectionText(_brainNames)} and roles {Utils.CreateCollectionText(_roles)} into groups:");
#endif

            List<ExcludeLayerInfo> newExcludeLayers = new List<ExcludeLayerInfo>();
            foreach (IEnumerable<T> collection in newCollections)
            {
                ExcludeLayerInfo newExcludeLayer = excludeLayerGenerator(collection.ToList());
                newExcludeLayers.Add(newExcludeLayer);

#if DEBUG
                BigBrainPlugin.BigBrainLogger.LogDebug($"-> {Utils.CreateCollectionText(collection)}");
#endif
            }

            return newExcludeLayers;
        }
    }
}
