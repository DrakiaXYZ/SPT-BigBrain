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
        internal static bool DoesLayerExist(this IEnumerable<ExcludeLayerInfo> excludeLayers, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            return excludeLayers.GetFirstMatch(layerName, brainNames, roles) != null;
        }

        internal static ExcludeLayerInfo GetFirstMatch(this IEnumerable<ExcludeLayerInfo> excludeLayers, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            foreach (ExcludeLayerInfo excludeLayerInfo in excludeLayers)
            {
                if (excludeLayerInfo.HasSameSettings(layerName, brainNames, roles))
                {
                    return excludeLayerInfo;
                }
            }

            return null;
        }

        internal static ExcludeLayerInfo GetFirstWithOneOrMoreOfEachSetting(this IEnumerable<ExcludeLayerInfo> excludeLayers, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            foreach (ExcludeLayerInfo excludeLayerInfo in excludeLayers)
            {
                if (excludeLayerInfo.HasOneOrMoreOfEachSetting(layerName, brainNames, roles))
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

        internal static bool HasSameSettings(this ExcludeLayerInfo excludeLayer, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            if (excludeLayer == null)
            {
                throw new ArgumentNullException(nameof(excludeLayer));
            }

            if (excludeLayer.excludeLayerName != layerName)
            {
                return false;
            }

            if (!Utils.HasSameContents(excludeLayer.affectedRoles, roles))
            {
                return false;
            }

            if (!Utils.HasSameContents(excludeLayer.affectedBrainNames, brainNames))
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

        internal static void RemoveAllWithOneOrMoreOfEachSetting(this IList<ExcludeLayerInfo> excludeLayers, string layerName, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            // Only initialize the list if it's needed
            List<ExcludeLayerInfo> excludeLayersToRemove = null;

            foreach (ExcludeLayerInfo excludeLayer in excludeLayers)
            {
                if (!excludeLayer.HasOneOrMoreOfEachSetting(layerName, brainNames, roles))
                {
                    continue;
                }

                // Initialize the list if there are layers to remove
                if (excludeLayersToRemove == null)
                {
                    excludeLayersToRemove = new List<ExcludeLayerInfo>();
                }

                excludeLayersToRemove.Add(excludeLayer);
            }

            // If the list is null, there are no layers to remove, so there's no need to do anything else
            if (excludeLayersToRemove == null)
            {
                return;
            }

            foreach (ExcludeLayerInfo excludeLayer in excludeLayersToRemove)
            {
                excludeLayers.Remove(excludeLayer);

#if DEBUG
                BigBrainPlugin.BigBrainLogger.LogInfo($"Removed exclusion for {layerName} for brains {Utils.CreateCollectionText(excludeLayer.affectedBrainNames)} and roles {Utils.CreateCollectionText(excludeLayer.affectedRoles)}");
#endif
            }
        }

        internal static int RemoveDuplicates(this IList<ExcludeLayerInfo> excludeLayers)
        {
            // Only initialize the list if it's needed
            List<ExcludeLayerInfo> duplicateLayers = null;

            foreach (ExcludeLayerInfo excludeLayer1 in excludeLayers)
            {
                if (duplicateLayers?.Contains(excludeLayer1) == true)
                {
                    continue;
                }

                foreach (ExcludeLayerInfo excludeLayer2 in excludeLayers)
                {
                    if (excludeLayer1 == excludeLayer2)
                    {
                        continue;
                    }

                    if (duplicateLayers?.Contains(excludeLayer2) == true)
                    {
                        continue;
                    }

                    if (!excludeLayer1.HasSameSettings(excludeLayer2))
                    {
                        continue;
                    }

                    // Initialize the list if there are layers to remove
                    if (duplicateLayers == null)
                    {
                        duplicateLayers = new List<ExcludeLayerInfo>();
                    }

                    duplicateLayers.Add(excludeLayer2);
                }
            }

            // If the list is null, there are no layers to remove, so there's no need to do anything else
            if (duplicateLayers == null)
            {
                return 0;
            }

            foreach (ExcludeLayerInfo duplicateLayer in duplicateLayers)
            {
                excludeLayers.Remove(duplicateLayer);

#if DEBUG
                BigBrainPlugin.BigBrainLogger.LogInfo($"Removed duplicate layer for {duplicateLayer.excludeLayerName} for brains {Utils.CreateCollectionText(duplicateLayer.affectedBrainNames)} and roles {Utils.CreateCollectionText(duplicateLayer.affectedRoles)}");
#endif
            }

            return duplicateLayers.Count;
        }
    }
}
