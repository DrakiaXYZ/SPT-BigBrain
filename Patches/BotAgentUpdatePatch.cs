using Aki.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

using AICoreLogicAgentClass = GClass26<BotLogicDecision>; // GClass26 = AICoreAgentClass
using AICoreNode = GClass103;
using AILogicActionResult = GStruct8<BotLogicDecision>; // GStruct8 = AICoreActionResult

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the bot agent update method so we can trigger a Start() method on custom logic actions
     **/
    internal class BotAgentUpdatePatch : ModulePatch
    {
        private static FieldInfo _brainFieldInfo;
        private static FieldInfo _lastResultField;
        private static FieldInfo _logicInstanceDictField;
        private static FieldInfo _lazyGetterField;

        protected override MethodBase GetTargetMethod()
        {
            Type botAgentType = typeof(AICoreLogicAgentClass);

            _brainFieldInfo = AccessTools.Field(botAgentType, "gclass216_0");
            _lastResultField = AccessTools.Field(botAgentType, "gstruct8_0");
            _logicInstanceDictField = AccessTools.Field(botAgentType, "dictionary_0");
            _lazyGetterField = AccessTools.Field(botAgentType, "func_0");

            return AccessTools.Method(botAgentType, "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance)
        {
#if DEBUG
            try {
#endif

                // Get values we'll use later
                BotBaseBrainClass brain = _brainFieldInfo.GetValue(__instance) as BotBaseBrainClass;
                Dictionary<BotLogicDecision, AICoreNode> aiCoreNodeDict = _logicInstanceDictField.GetValue(__instance) as Dictionary<BotLogicDecision, AICoreNode>;

                // Update the brain, this is instead of method_10 in the original code
                brain.ManualUpdate();

                // Call the brain update
                AILogicActionResult lastResult = (AILogicActionResult)_lastResultField.GetValue(__instance);
                AILogicActionResult? result = brain.Update(lastResult);
                if (result != null)
                {
                    // If an instance of our action doesn't exist in our dict, add it
                    int action = (int)result.Value.Action;
                    if (!aiCoreNodeDict.TryGetValue((BotLogicDecision)action, out AICoreNode nodeInstance))
                    {
                        Func<BotLogicDecision, AICoreNode> lazyGetter = _lazyGetterField.GetValue(__instance) as Func<BotLogicDecision, AICoreNode>;
                        nodeInstance = lazyGetter((BotLogicDecision)action);

                        if (nodeInstance != null)
                        {
                            aiCoreNodeDict.Add((BotLogicDecision)action, nodeInstance);
                        }
                    }

                    if (nodeInstance != null)
                    {
                        // If we're switching to a new action, call Start() on the new logic
                        if (lastResult.Action != result.Value.Action && nodeInstance is CustomLogicWrapper customLogic)
                        {
                            customLogic.Start();
                        }

                        nodeInstance.Update();
                    }

                    _lastResultField.SetValue(__instance, result);
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
