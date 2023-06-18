using BepInEx;
using BepInEx.Bootstrap;
using DrakiaXYZ.BigBrain.Patches;
using DrakiaXYZ.BigBrain.VersionChecker;
using System;

namespace DrakiaXYZ.BigBrain
{
    [BepInPlugin("xyz.drakia.bigbrain", "DrakiaXYZ-BigBrain", "0.1.3")]
    internal class BigBrainPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Loading: DrakiaXYZ-BigBrain");

            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                // TODO: Remove when a new EFT version comes out. For now, force remove
                //       ourselves from the list of plugins, so other plugins can know
                //       that we are not loaded
                Chainloader.PluginInfos.Remove(Info.Metadata.GUID);
                // End TODO

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
