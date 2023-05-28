using Aki.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the layer activate method (method_4) of GClass216, so we can prioritize custom layers above default layers
     **/
    internal class BotBaseBrainActivateLayerPatch : ModulePatch
    {
        private static FieldInfo _activeLayerListField;

        protected override MethodBase GetTargetMethod()
        {
            Type botLogicBrainType = typeof(BotBaseBrainClass);
            Type botBaseBrainType = botLogicBrainType.BaseType;

            _activeLayerListField = AccessTools.Field(botBaseBrainType, "list_0");

            return AccessTools.Method(botBaseBrainType, "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance, GClass28<BotLogicDecision> layer)
        {
            // For base layers, we can fall back to the original code, as it will add to the end 
            // of the same-priority layers, which will already prioritize custom layers
            if (!(layer is CustomLayerWrapper))
            {
                return true;
            }

            List<GClass28<BotLogicDecision>> activeLayerList = _activeLayerListField.GetValue(__instance) as List<GClass28<BotLogicDecision>>;

            layer.Activate();

            // Look for the first layer with an equal or lower priority, and add out layer before it
            for (int i = 0; i < activeLayerList.Count; i++)
            {
                GClass28<BotLogicDecision> gclass = activeLayerList[i];
                if (layer.Priority >= gclass.Priority)
                {
                    activeLayerList.Insert(i, layer);
                    return false;
                }
            }
            activeLayerList.Add(layer);

            return false;
        }
    }
}
