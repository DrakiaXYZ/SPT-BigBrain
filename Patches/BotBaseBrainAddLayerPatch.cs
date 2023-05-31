﻿using Aki.Reflection.Patching;
using DrakiaXYZ.BigBrain.Brains;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the layer add method (method_0) of GClass216, so we can disable layers, and insert custom layers
     * as higher priority than default layers
     **/
    internal class BotBaseBrainAddLayerPatch : ModulePatch
    {
        private static FieldInfo _layerDictionary;
        private static MethodInfo _activateLayerMethod;

        protected override MethodBase GetTargetMethod()
        {
            Type botLogicBrainType = typeof(BotBaseBrainClass);
            Type botBaseBrainType = botLogicBrainType.BaseType;

            _layerDictionary = AccessTools.Field(botBaseBrainType, "dictionary_0");
            _activateLayerMethod = AccessTools.Method(botBaseBrainType, "method_4");

            return AccessTools.Method(botBaseBrainType, "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance, int index, GClass28<BotLogicDecision> layer, bool activeOnStart, ref bool __result)
        {
            // Make sure we're not excluding this layer from this brain
            BotBaseBrainClass botBrain = __instance as BotBaseBrainClass;
            foreach (BrainManager.ExcludeLayerInfo excludeInfo in BrainManager.Instance.ExcludeLayers)
            {
                if (layer.Name() == excludeInfo.excludeLayerName && excludeInfo.excludeLayerBrains.Contains(botBrain.ShortName()))
                {
                    Logger.LogDebug($"Skipping adding {layer.Name()} to {botBrain.ShortName()} as it was removed");
                    __result = false;
                    return false;
                }
            }

            Dictionary<int, GClass28<BotLogicDecision>> layerDictionary = _layerDictionary.GetValue(__instance) as Dictionary<int, GClass28<BotLogicDecision>>;

            // Make sure the layer index doesn't already exist
            if (layerDictionary.ContainsKey(index))
            {
                Logger.LogError($"Trying add layer with existing index: {index}");
                __result = false;
                return false;
            }

            // Add to the dictionary, and activate if required
            layerDictionary.Add(index, layer);
            if (activeOnStart)
            {
                _activateLayerMethod.Invoke(__instance, new object[] { layer });
            }
            __result = true;

            return false;
        }
    }
}