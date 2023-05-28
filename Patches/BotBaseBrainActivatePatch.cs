using Aki.Reflection.Patching;
using DrakiaXYZ.BigBrain.Brains;
using DrakiaXYZ.BigBrain.Internal;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the base brain activate method so we can inject our custom brain layers
     **/
    internal class BotBaseBrainActivatePatch : ModulePatch
    {
        private static FieldInfo _botOwnerField;
        private static MethodInfo _addLayer;
        protected override MethodBase GetTargetMethod()
        {
            Type botLogicBrainType = typeof(BotBaseBrainClass);
            Type botBaseBrainType = botLogicBrainType.BaseType;

            _botOwnerField = AccessTools.Field(botLogicBrainType, "botOwner_0");
            _addLayer = AccessTools.Method(botBaseBrainType, "method_0");

            return AccessTools.Method(botBaseBrainType, "Activate");
        }

        [PatchPrefix]
        public static void PatchPrefix(object __instance)
        {
            try
            {
                BotBaseBrainClass botBrain = __instance as BotBaseBrainClass;
                BotOwner botOwner = (BotOwner)_botOwnerField.GetValue(botBrain);

                foreach (BrainManager.LayerInfo layerInfo in BrainManager.Instance.CustomLayers.Values)
                {
                    if (layerInfo.customLayerBrains.Contains(botBrain.ShortName()))
                    {
                        CustomLayerWrapper customLayerWrapper = new CustomLayerWrapper(layerInfo.customLayerType, botOwner, layerInfo.customLayerPriority);
                        Logger.LogDebug($"  Injecting {customLayerWrapper.Name()}({layerInfo.customLayerId}) with priority {layerInfo.customLayerPriority}");
                        _addLayer.Invoke(botBrain, new object[] { layerInfo.customLayerId, customLayerWrapper, true });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}
