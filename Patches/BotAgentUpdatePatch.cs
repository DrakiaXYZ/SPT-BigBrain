using SPT.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;

using AICoreLogicAgentClass = AICoreAgentClass<BotLogicDecision>;
using AILogicActionResultStruct = AICoreActionResultStruct<BotLogicDecision, GClass26>;
using BaseNodeAbstractClass = BotNodeAbstractClass;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the bot agent update method so we can trigger a Start() method on custom logic actions
     **/
    internal class BotAgentUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AICoreLogicAgentClass), "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(AICoreLogicAgentClass __instance)
        {
#if DEBUG
            try {
#endif
                // Update the brain, this is instead of method_10 in the original code
                __instance.Gclass309_0.ManualUpdate();

                // Call the brain update
                AILogicActionResultStruct? result = __instance.Gclass309_0.Update(__instance.Gstruct8_0);
                if (result != null)
                {
                    // If an instance of our action doesn't exist in our dict, add it
                    BotLogicDecision action = result.Value.Action;
                    BaseNodeAbstractClass nodeInstance;
                    if (!__instance.Dictionary_0.TryGetValue(action, out nodeInstance))
                    {
                        nodeInstance = __instance.Func_0(action);

                        if (nodeInstance != null)
                        {
                            __instance.Dictionary_0.Add(action, nodeInstance);
                        }
                    }

                    if (nodeInstance != null)
                    {
                        // If we're switching to a new action, call Start() on the new logic
                        if (__instance.Gstruct8_0.Action != result.Value.Action && nodeInstance is CustomLogicWrapper customLogic)
                        {
                            customLogic.Start();
                        }

                        nodeInstance.UpdateNodeByMain(__instance.Gstruct8_0.Data);
                    }

                    __instance.Gstruct8_0 = result.Value;
                }

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
