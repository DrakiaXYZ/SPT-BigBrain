using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

using AICoreLogicAgentClass = AICoreAgentClass<BotLogicDecision>;
using AILogicActionResultStruct = AICoreActionResultStruct<BotLogicDecision>;
using AICoreNodeClass = GClass103;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class CustomLayerWrapper : BaseLogicLayerSimpleClass
    {
        private static FieldInfo _logicInstanceDictField = null;

        private static int _currentLogicId = BrainManager.START_LOGIC_ID;

        protected ManualLogSource Logger;
        private readonly CustomLayer customLayer;
        private AICoreActionEndStruct endAction;
        private AICoreActionEndStruct continueAction;

        public CustomLayerWrapper(Type customLayerType, BotOwner bot, int priority) : base(bot, priority)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            customLayer = (CustomLayer)Activator.CreateInstance(customLayerType, new object[] { bot, priority });
            endAction = gstruct7_0;
            continueAction = gstruct7_1;

            if (_logicInstanceDictField == null)
            {
                Type botAgentType = typeof(AICoreLogicAgentClass);
                _logicInstanceDictField = AccessTools.Field(botAgentType, "dictionary_0");
            }
        }

        public override AILogicActionResultStruct GetDecision()
        {
            CustomLayer.Action action = customLayer.GetNextAction();

            if (!typeof(CustomLogic).IsAssignableFrom(action.Type))
            {
                throw new ArgumentException($"Custom logic type {action.Type.FullName} must inherit CustomLogic");
            }
//#if DEBUG
//            Logger.LogDebug($"{botOwner_0.name} NextAction: {action.Type.FullName}");
//            Logger.LogDebug($"    Reason: {action.Reason}");
//#endif

            customLayer.CurrentAction = action;

            if (!BrainManager.Instance.CustomLogics.TryGetValue(action.Type, out int logicId))
            {
                logicId = _currentLogicId++;
                BrainManager.Instance.CustomLogics.Add(action.Type, logicId);
                BrainManager.Instance.CustomLogicList.Add(action.Type);
            }

            return new AILogicActionResultStruct((BotLogicDecision)logicId, action.Reason);
        }

        public override string Name()
        {
            return customLayer.GetName();
        }

        public override bool ShallUseNow()
        {
            return customLayer.IsActive();
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AILogicActionResultStruct curDecision)
        {
            // If this isn't a custom action, we want to end it (So we can take control)
            if ((int)curDecision.Action < BrainManager.START_LOGIC_ID)
            {
                customLayer.CurrentAction = null;
                return endAction;
            }

            // If the custom layer has a null action, we've switched between custom layers, so return that we're ending
            if (customLayer.CurrentAction == null)
            {
                return endAction;
            }

            if (customLayer.IsCurrentActionEnding())
            {
                StopCurrentLogic();
                return endAction;
            }

            return continueAction;
        }

        public void Start()
        {
            customLayer.Start();
        }

        public void Stop()
        {
            // We want to tell the current Logic to stop before we stop the layer
            StopCurrentLogic();
            customLayer.Stop();
        }

        private void StopCurrentLogic()
        {
            customLayer.CurrentAction = null;
            CustomLogicWrapper logicInstance = GetLogicInstance(botOwner_0) as CustomLogicWrapper;
            if (logicInstance != null)
            {
                logicInstance.Stop();
            }
        }

        static internal BaseNodeClass GetLogicInstance(BotOwner botOwner)
        {
            // Sanity check
            if (botOwner?.Brain?.Agent == null)
            {
                return null;
            }

            BotLogicDecision logicDecision = botOwner.Brain.Agent.LastResult().Action;

            Dictionary<BotLogicDecision, AICoreNodeClass> aiCoreNodeDict = _logicInstanceDictField.GetValue(botOwner.Brain.Agent) as Dictionary<BotLogicDecision, AICoreNodeClass>;
            if (aiCoreNodeDict.TryGetValue(logicDecision, out AICoreNodeClass nodeInstance))
            {
                return nodeInstance as BaseNodeClass;
            }

            return null;
        }

        internal CustomLayer CustomLayer()
        {
            return customLayer;
        }

    }
}
