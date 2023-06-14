using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

using BaseLogicLayerSimpleClass = GClass30;
using AICoreLogicAgentClass = GClass26<BotLogicDecision>; // GClass26 = AICoreAgentClass
using AICoreNode = GClass103;
using AICoreActionEnd = GStruct7;
using AILogicActionResult = GStruct8<BotLogicDecision>; // GStruct8 = AICoreActionResult

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class CustomLayerWrapper : BaseLogicLayerSimpleClass
    {
        private static FieldInfo _logicInstanceDictField = null;

        private static int _currentLogicId = BrainManager.START_LOGIC_ID;

        protected ManualLogSource Logger;
        private readonly CustomLayer customLayer;
        private AICoreActionEnd endAction;
        private AICoreActionEnd continueAction;

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

        public override AILogicActionResult GetDecision()
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

            return new AILogicActionResult((BotLogicDecision)logicId, action.Reason);
        }

        public override string Name()
        {
            return customLayer.GetName();
        }

        public override bool ShallUseNow()
        {
            return customLayer.IsActive();
        }

        public override AICoreActionEnd ShallEndCurrentDecision(AILogicActionResult curDecision)
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

            BotLogicDecision logicId = botOwner_0.Brain.Agent.LastResult().Action;
            CustomLogicWrapper logicInstance = GetLogicInstance(logicId);
            if (logicInstance != null)
            {
                logicInstance.Stop();
            }
        }

        private CustomLogicWrapper GetLogicInstance(BotLogicDecision logicDecision)
        {
            // Sanity check
            if (botOwner_0?.Brain?.Agent == null)
            {
                return null;
            }

            Dictionary<BotLogicDecision, AICoreNode> aiCoreNodeDict = _logicInstanceDictField.GetValue(botOwner_0.Brain.Agent) as Dictionary<BotLogicDecision, AICoreNode>;
            if (aiCoreNodeDict.TryGetValue(logicDecision, out AICoreNode nodeInstance))
            {
                return nodeInstance as CustomLogicWrapper;
            }

            return null;
        }

        internal CustomLayer CustomLayer()
        {
            return customLayer;
        }

    }
}
