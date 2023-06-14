using Aki.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

using AICoreLogicLayerClass = GClass28<BotLogicDecision>; // GClass28 = AICoreLayerClass

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

        protected override MethodBase GetTargetMethod()
        {
            Type botLogicBrainType = typeof(BotBaseBrainClass);
            Type botBaseBrainType = botLogicBrainType.BaseType;

            _activeLayerGetter = AccessTools.PropertyGetter(botBaseBrainType, "GClass28_0");
            _activeLayerSetter = AccessTools.PropertySetter(botBaseBrainType, "GClass28_0");
            _activeLayerListField = AccessTools.Field(botBaseBrainType, "list_0");
            _onLayerChangedToField = AccessTools.Field(botBaseBrainType, "action_0");

            return AccessTools.Method(botBaseBrainType, "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance, GStruct8<BotLogicDecision> prevResult, ref GStruct8<BotLogicDecision>? __result)
        {
#if DEBUG
            try
            {
#endif

                // Get values we'll use later
                List<AICoreLogicLayerClass> activeLayerList = _activeLayerListField.GetValue(__instance) as List<AICoreLogicLayerClass>;
                AICoreLogicLayerClass activeLayer = _activeLayerGetter.Invoke(__instance, null) as AICoreLogicLayerClass;

                foreach (AICoreLogicLayerClass layer in activeLayerList)
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
                            Action<AICoreLogicLayerClass> action = _onLayerChangedToField.GetValue(__instance) as Action<AICoreLogicLayerClass>;
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
                        __result = activeLayer.Update(new GStruct8<BotLogicDecision>?(prevResult));
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
                Logger.LogError(ex);
                throw ex;
            }
#endif
        }
    }
}
