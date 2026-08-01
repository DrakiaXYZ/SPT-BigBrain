using SPT.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the bot agent update method so we can trigger a Start() method on custom logic actions
     **/
    internal class BotAgentUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AICoreAgent<BotLogicDecision>), "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(AICoreAgent<BotLogicDecision> __instance)
        {
#if DEBUG
            try {
#endif
                // Update the brain, this is instead of method_10 in the original code
                __instance._strategy.ManualUpdate();

                // Call the brain update
                AICoreActionResult<BotLogicDecision, CoreActionResultParams>? result = __instance._strategy.Update(__instance._lastResult);
                if (result != null)
                {
                    // If an instance of our action doesn't exist in our dict, add it
                    BotLogicDecision action = result.Value.Action;
                    AICoreNode nodeInstance;
                    if (!__instance._nodesDictionary.TryGetValue(action, out nodeInstance))
                    {
                        nodeInstance = __instance._lazyGetter(action);

                        if (nodeInstance != null)
                        {
                            __instance._nodesDictionary.Add(action, nodeInstance);
                        }
                    }

                    if (nodeInstance != null)
                    {
                        // If we're switching to a new action, call Start() on the new logic
                        if (__instance._lastResult.Action != result.Value.Action && nodeInstance is CustomLogicWrapper customLogic)
                        {
                            customLogic.Start();
                        }

                        nodeInstance.UpdateNodeByMain(__instance._lastResult.Data);
                    }

                    __instance._lastResult = result.Value;
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
