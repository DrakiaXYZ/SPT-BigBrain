using BepInEx;
using DrakiaXYZ.BigBrain.Patches;
using DrakiaXYZ.BigBrain.VersionChecker;
using System;

namespace DrakiaXYZ.BigBrain
{
    [BepInPlugin("xyz.drakia.bigbrain", "DrakiaXYZ-BigBrain", "1.3.1")]
#if !DEBUG
    [BepInDependency("com.SPT.core", "3.11.0")]
#endif
    internal class BigBrainPlugin : BaseUnityPlugin
    {
        // This needs to be initialized because other mods could interact with BrainManager before this plugin has loaded
        internal static BepInEx.Logging.ManualLogSource BigBrainLogger { get; private set; } = new BepInEx.Logging.ManualLogSource("DrakiaXYZ-BigBrain");

        public void Awake()
        {
            Logger.LogInfo("Loading: DrakiaXYZ-BigBrain");
            BigBrainLogger = Logger;

            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }

            try
            {
                new BotBaseBrainActivatePatch().Enable();
                new BotBrainCreateLogicNodePatch().Enable();

                new BotBaseBrainUpdatePatch().Enable();
                new BotAgentUpdatePatch().Enable();

                new BotBaseBrainActivateLayerPatch().Enable();

                new BotStandartBotBrainActivatePatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw;
            }

            Logger.LogInfo("Completed: DrakiaXYZ-BigBrain");
        }
    }
}
