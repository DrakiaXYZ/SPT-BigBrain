using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class CustomLayerWrapper : BaseLogicLayer
    {
        private static int _currentLogicId = BrainManager.START_LOGIC_ID;

        protected ManualLogSource Logger;
        private readonly CustomLayer customLayer;
        private AICoreActionEnd endAction = new AICoreActionEnd("Base logic", true);
        private AICoreActionEnd continueAction = new AICoreActionEnd(null, false);

        public CustomLayerWrapper(Type customLayerType, BotOwner bot, int priority) : base(bot, priority)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            customLayer = (CustomLayer)Activator.CreateInstance(customLayerType, new object[] { bot, priority });
        }

        public override AICoreActionResult<BotLogicDecision, CoreActionResultParams> GetDecision()
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

            return new AICoreActionResult<BotLogicDecision, CoreActionResultParams>((BotLogicDecision)logicId, action.Reason, action.Data);
        }

        public override string Name()
        {
            return customLayer.GetName();
        }

        public override bool ShallUseNow()
        {
            return customLayer.IsActive();
        }

        public override AICoreActionEnd ShallEndCurrentDecision(AICoreActionResult<BotLogicDecision, CoreActionResultParams> curDecision)
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
            CustomLogicWrapper logicInstance = GetLogicInstance(_owner) as CustomLogicWrapper;
            if (logicInstance != null)
            {
                logicInstance.Stop();
            }
        }

        static internal AICoreNode GetLogicInstance(BotOwner botOwner)
        {
            // Sanity check
            if (botOwner == null || botOwner.Brain?.Agent == null)
            {
                return null;
            }

            BotLogicDecision logicDecision = botOwner.Brain.Agent.LastResult().Action;
            if (botOwner.Brain.Agent._nodesDictionary.TryGetValue(logicDecision, out var logicInstance))
            {
                return logicInstance;
            }

            return null;
        }

        internal CustomLayer CustomLayer()
        {
            return customLayer;
        }

    }
}
