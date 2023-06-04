using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class CustomLayerWrapper : GClass30
    {
        private static FieldInfo _logicInstanceDictField = null;

        private static int _currentLogicId = BrainManager.START_LOGIC_ID;

        protected ManualLogSource Logger;
        private readonly CustomLayer customLayer;

        public CustomLayerWrapper(Type customLayerType, BotOwner bot, int priority) : base(bot, priority)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            customLayer = (CustomLayer)Activator.CreateInstance(customLayerType, new object[] { bot, priority });

            if (_logicInstanceDictField == null)
            {
                Type botAgentType = typeof(GClass26<BotLogicDecision>);
                _logicInstanceDictField = AccessTools.Field(botAgentType, "dictionary_0");
            }
        }

        public override GStruct8<BotLogicDecision> GetDecision()
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

            return new GStruct8<BotLogicDecision>((BotLogicDecision)logicId, action.Reason);
        }

        public override string Name()
        {
            return customLayer.GetName();
        }

        public override bool ShallUseNow()
        {
            return customLayer.IsActive();
        }

        public override GStruct7 ShallEndCurrentDecision(GStruct8<BotLogicDecision> curDecision)
        {
            // If this isn't a custom action, we want to end it (So we can take control)
            if ((int)curDecision.Action < BrainManager.START_LOGIC_ID)
            {
                customLayer.CurrentAction = null;
                return gstruct7_0;
            }

            // If the custom layer has a null action, we've switched between custom layers, so return that we're ending
            if (customLayer.CurrentAction == null)
            {
                return gstruct7_0;
            }

            if (customLayer.IsCurrentActionEnding())
            {
                StopCurrentLogic();
                return gstruct7_0;
            }

            return gstruct7_1;
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

        private CustomLogicWrapper GetLogicInstance(BotLogicDecision logicId)
        {
            // Sanity check
            if (botOwner_0?.Brain?.Agent == null)
            {
                return null;
            }

            Dictionary<BotLogicDecision, GClass103> logicInstanceDict = _logicInstanceDictField.GetValue(botOwner_0.Brain.Agent) as Dictionary<BotLogicDecision, GClass103>;
            if (logicInstanceDict.TryGetValue(logicId, out GClass103 logicInstance))
            {
                return logicInstance as CustomLogicWrapper;
            }

            return null;
        }
    }
}
