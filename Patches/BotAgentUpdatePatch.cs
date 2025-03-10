﻿using SPT.Reflection.Patching;
using DrakiaXYZ.BigBrain.Internal;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;

using AICoreLogicAgentClass = AICoreAgentClass<BotLogicDecision>;
using BaseNodeAbstractClass = GClass170<GClass26>;
using AILogicActionResultStruct = AICoreActionResultStruct<BotLogicDecision, GClass26>;

namespace DrakiaXYZ.BigBrain.Patches
{
    /**
     * Patch the bot agent update method so we can trigger a Start() method on custom logic actions
     **/
    internal class BotAgentUpdatePatch : ModulePatch
    {
        private static FieldInfo _strategyField;
        private static FieldInfo _lastResultField;
        private static FieldInfo _logicInstanceDictField;
        private static FieldInfo _lazyGetterField;

        protected override MethodBase GetTargetMethod()
        {
            Type botAgentType = typeof(AICoreLogicAgentClass);

            _strategyField = Utils.GetFieldByType(botAgentType, typeof(AICoreStrategyAbstractClass<>));
            _lastResultField = Utils.GetFieldByType(botAgentType, typeof(AILogicActionResultStruct));
            _logicInstanceDictField = Utils.GetFieldByType(botAgentType, typeof(IDictionary));
            _lazyGetterField = Utils.GetFieldByType(botAgentType, typeof(Delegate));

            return AccessTools.Method(botAgentType, "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object __instance)
        {
#if DEBUG
            try {
#endif

                // Get values we'll use later
                BaseBrain strategy = _strategyField.GetValue(__instance) as BaseBrain;
                var aiCoreNodeDict = _logicInstanceDictField.GetValue(__instance) as IDictionary;

                // Update the brain, this is instead of method_10 in the original code
                strategy.ManualUpdate();

                // Call the brain update
                AILogicActionResultStruct lastResult = (AILogicActionResultStruct)_lastResultField.GetValue(__instance);
                AILogicActionResultStruct? result = strategy.Update(lastResult);
                if (result != null)
                {
                    // If an instance of our action doesn't exist in our dict, add it
                    int action = (int)result.Value.Action;
                    BaseNodeAbstractClass nodeInstance = aiCoreNodeDict[(BotLogicDecision)action] as BaseNodeAbstractClass;
                    if (nodeInstance == null)
                    {
                        Delegate lazyGetter = _lazyGetterField.GetValue(__instance) as Delegate;
                        nodeInstance = lazyGetter.DynamicInvoke(new object[] { (BotLogicDecision)action }) as BaseNodeAbstractClass;

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

                        nodeInstance.UpdateNodeByBrain(lastResult.Data);
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
