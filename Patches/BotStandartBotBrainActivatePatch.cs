using DrakiaXYZ.BigBrain.Brains;
using DrakiaXYZ.BigBrain.Internal;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace DrakiaXYZ.BigBrain.Patches
{
    internal class BotStandartBotBrainActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StandartBotBrain), "Activate");
        }

        [PatchPostfix]
        public static void PatchPostfix(StandartBotBrain __instance)
        {
            // Everything in this method should be in a try block because an exception will "break" the bot's brain
            try
            {
                // This should only happen if a mod calls this method more than once for some reason
                if (BrainManager.Instance.ActivatedBots.ContainsKey(__instance._owner.GetPlayer))
                {
                    throw new InvalidOperationException($"{__instance._owner.Profile.Nickname} ({__instance._owner.name}) has already been activated");
                }

                BrainManager.Instance.ActivatedBots.Add(__instance._owner.GetPlayer, __instance._owner);
                __instance._owner.GetPlayer.OnPlayerDeadOrUnspawn += (player) => { BrainManager.Instance.ActivatedBots.Remove(player); };

                __instance._owner.RemoveAllExcludedLayers();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Could not remove initially excluded layers for {__instance._owner.Profile.Nickname} ({__instance._owner.name})");

                Logger.LogError(ex);
                throw ex;
            }
        }
    }
}
