using SPT.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the layer activate method (method_4) of AICoreStrategyClass, so we can prioritize custom layers above default layers
     **/
    internal class BotBaseBrainActivateLayerPatch : ModulePatch
    {
        private static FieldInfo _activeLayerListField;

        protected override MethodBase GetTargetMethod()
        {
            Type baseBrainType = typeof(BaseBrain);
            Type aiCoreStrategyClassType = baseBrainType.BaseType;

            _activeLayerListField = AccessTools.Field(aiCoreStrategyClassType, "_activeLayers");

            return AccessTools.GetDeclaredMethods(aiCoreStrategyClassType).Single(x =>
            {
                var parms = x.GetParameters();
                return (parms.Length == 1 && parms[0].ParameterType == typeof(AICoreLayer<BotLogicDecision>) && parms[0].Name == "layer");
            });
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance, AICoreLayer<BotLogicDecision> layer)
        {
#if DEBUG
            try
            {
#endif
                // For base layers, we can fall back to the original code, as it will add to the end 
                // of the same-priority layers, which will already prioritize custom layers
                if (!(layer is CustomLayerWrapper))
                {
                    return true;
                }

                List<AICoreLayer<BotLogicDecision>> activeLayerList = _activeLayerListField.GetValue(__instance) as List<AICoreLayer<BotLogicDecision>>;

                layer.Activate();

                // Look for the first layer with an equal or lower priority, and add out layer before it
                for (int i = 0; i < activeLayerList.Count; i++)
                {
                    AICoreLayer<BotLogicDecision> activeLayer = activeLayerList[i];
                    if (layer.Priority >= activeLayer.Priority)
                    {
                        activeLayerList.Insert(i, layer);
                        return false;
                    }
                }
                activeLayerList.Add(layer);

                return false;

#if DEBUG
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw ex;
            }
#endif
        }
    }
}
