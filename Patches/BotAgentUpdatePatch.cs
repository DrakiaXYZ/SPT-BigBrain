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
    internal class BotAgentUpdatePatch : ModulePatch
    {
        private static FieldInfo _brainFieldInfo;
        private static FieldInfo _lastResultField;
        private static FieldInfo _logicInstanceDictField;
        private static FieldInfo _lazyGetterField;

        protected override MethodBase GetTargetMethod()
        {
            Type botAgentType = typeof(GClass26<BotLogicDecision>);

            _brainFieldInfo = AccessTools.Field(botAgentType, "gclass216_0");
            _lastResultField = AccessTools.Field(botAgentType, "gstruct8_0");
            _logicInstanceDictField = AccessTools.Field(botAgentType, "dictionary_0");
            _lazyGetterField = AccessTools.Field(botAgentType, "func_0");

            return AccessTools.Method(botAgentType, "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance)
        {
            // Get values we'll use later
            BotBaseBrainClass brain = _brainFieldInfo.GetValue(__instance) as BotBaseBrainClass;
            Dictionary<BotLogicDecision, GClass103> logicInstanceDict = _logicInstanceDictField.GetValue(__instance) as Dictionary<BotLogicDecision, GClass103>;

            // Update the brain, this is instead of method_10 in the original code
            brain.ManualUpdate();

            // Call the brain update
            GStruct8<BotLogicDecision> lastResult = (GStruct8<BotLogicDecision>)_lastResultField.GetValue(__instance);
            GStruct8<BotLogicDecision>? result = brain.Update(lastResult);
            if (result != null)
            {
                // If an instance of our action doesn't exist in our dict, add it
                int action = (int)result.Value.Action;
                if (!logicInstanceDict.TryGetValue((BotLogicDecision)action, out GClass103 logicInstance))
                {
                    Func<BotLogicDecision, GClass103> lazyGetter = _lazyGetterField.GetValue(__instance) as Func<BotLogicDecision, GClass103>;
                    logicInstance = lazyGetter((BotLogicDecision)action);

                    if (logicInstance != null)
                    {
                        logicInstanceDict.Add((BotLogicDecision)action, logicInstance);
                    }
                }

                if (logicInstance != null)
                {
                    // If we're switching to a new action, call Start() on the new logic
                    if (lastResult.Action != result.Value.Action && logicInstance is CustomLogicWrapper customLogic)
                    {
                        customLogic.Start();
                    }

                    logicInstance.Update();
                }

                _lastResultField.SetValue(__instance, result);
            }

            return false;
        }
    }
}
