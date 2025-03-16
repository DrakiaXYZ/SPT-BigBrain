using SPT.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;

using AICoreLogicAgentClass = AICoreAgentClass<BotLogicDecision>;
using BaseNodeAbstractClass = GClass168;
using AILogicActionResultStruct = AICoreActionResultStruct<BotLogicDecision, GClass26>;

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
                __instance.aICoreStrategyAbstractClass.ManualUpdate();

                // Call the brain update
                AILogicActionResultStruct lastResult = __instance.aICoreActionResultStruct;
                AILogicActionResultStruct? result = __instance.aICoreStrategyAbstractClass.Update(lastResult);
                if (result != null)
                {
                    // If an instance of our action doesn't exist in our dict, add it
                    var aiCoreNodeDict = __instance.dictionary_0;
                    BotLogicDecision action = result.Value.Action;
                    BaseNodeAbstractClass nodeInstance;
                    if (!aiCoreNodeDict.TryGetValue(action, out nodeInstance))
                    {
                        nodeInstance = __instance.func_0(action);

                        if (nodeInstance != null)
                        {
                            aiCoreNodeDict.Add(action, nodeInstance);
                        }
                    }

                    if (nodeInstance != null)
                    {
                        // If we're switching to a new action, call Start() on the new logic
                        if (lastResult.Action != result.Value.Action && nodeInstance is CustomLogicWrapper customLogic)
                        {
                            customLogic.Start();
                        }

                        nodeInstance.UpdateNodeByMain(lastResult.Data);
                    }

                    __instance.aICoreActionResultStruct = result.Value;
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
