using SPT.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the base brain Update method so we can trigger Stop/Start methods on custom layers
     **/
    internal class BotBaseBrainUpdatePatch : ModulePatch
    {
        private static MethodInfo _activeLayerGetter;
        private static MethodInfo _activeLayerSetter;
        private static FieldInfo _activeLayerListField;
        private static FieldInfo _onLayerChangedToField;
        private static FieldInfo _ownerField;

        protected override MethodBase GetTargetMethod()
        {
            Type baseBrainType = typeof(BaseBrain);
            Type aiCoreStrategyType = baseBrainType.BaseType;

            _ownerField = AccessTools.Field(baseBrainType, "_owner");

            string activeLayerPropertyName = Utils.GetPropertyNameByType(aiCoreStrategyType, typeof(AICoreLayer<BotLogicDecision>));
            _activeLayerGetter = AccessTools.PropertyGetter(aiCoreStrategyType, activeLayerPropertyName);
            _activeLayerSetter = AccessTools.PropertySetter(aiCoreStrategyType, activeLayerPropertyName);

            _activeLayerListField = Utils.GetFieldByType(aiCoreStrategyType, typeof(List<AICoreLayer<BotLogicDecision>>));
            _onLayerChangedToField = Utils.GetFieldByType(aiCoreStrategyType, typeof(Action<AICoreLayer<BotLogicDecision>>));

            return AccessTools.Method(aiCoreStrategyType, "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance, AICoreActionResult<BotLogicDecision, CoreActionResultParams> prevResult, ref AICoreActionResult<BotLogicDecision, CoreActionResultParams>? __result)
        {
#if DEBUG
            try
            {
#endif

                // Get values we'll use later
                List<AICoreLayer<BotLogicDecision>> activeLayerList = _activeLayerListField.GetValue(__instance) as List<AICoreLayer<BotLogicDecision>>;
                AICoreLayer<BotLogicDecision> activeLayer = _activeLayerGetter.Invoke(__instance, null) as AICoreLayer<BotLogicDecision>;

                if (activeLayerList == null)
                {
                    __result = null;
                    return false;
                }

                foreach (AICoreLayer<BotLogicDecision> layer in activeLayerList)
                {
                    if (layer.ShallUseNow())
                    {
                        if (layer != activeLayer)
                        {
                            // Allow telling custom layers they're stopping
                            if (activeLayer is CustomLayerWrapper customActiveLayer)
                            {
                                customActiveLayer.Stop();
                            }

                            activeLayer = layer;
                            _activeLayerSetter.Invoke(__instance, new object[] { layer });
                            Action<AICoreLayer<BotLogicDecision>> action = _onLayerChangedToField.GetValue(__instance) as Action<AICoreLayer<BotLogicDecision>>;
                            if (action != null)
                            {
                                action(activeLayer);
                            }

                            // Allow telling custom layers they're starting
                            if (activeLayer is CustomLayerWrapper customNewLayer)
                            {
                                customNewLayer.Start();
                            }
                        }

                        // Call the active layer's Update() method
                        __result = activeLayer.Update(new AICoreActionResult<BotLogicDecision, CoreActionResultParams>?(prevResult));
                        return false;
                    }
                }

                // No layers are active, return null
                __result = null;
                return false;

#if DEBUG
            }
            catch (Exception ex)
            {
                BotOwner owner = _ownerField.GetValue(__instance) as BotOwner;
                Logger.LogError($"Exception in ShallUseNow for {owner.Profile.Nickname} ({owner.name})");

                Logger.LogError(ex);
                throw ex;
            }
#endif
        }
    }
}
