using BepInEx;
using DrakiaXYZ.BigBrain.Patches;
using DrakiaXYZ.BigBrain.VersionChecker;
using System;

namespace DrakiaXYZ.BigBrain
{
    [BepInPlugin("xyz.drakia.bigbrain", "DrakiaXYZ-BigBrain", "0.3.1.0")]
    [BepInDependency("com.spt-aki.core", "3.7.1")]
    internal class BigBrainPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Loading: DrakiaXYZ-BigBrain");

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
                new BotBaseBrainAddLayerPatch().Enable();
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
