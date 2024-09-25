using DrakiaXYZ.BigBrain.Brains;
using DrakiaXYZ.BigBrain.Internal;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BigBrain.Patches
{
    internal class BotStandartBotBrainActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StandartBotBrain), "Activate");
        }

        [PatchPostfix]
        public static void PatchPostfix(BotOwner ___botOwner_0)
        {
            // Everything in this method should be in a try block because an exception will "break" the bot's brain
            try
            {
                // This should only happen if a mod calls this method more than once for some reason
                if (BrainManager.Instance.ActivatedBots.ContainsKey(___botOwner_0.GetPlayer))
                {
                    throw new InvalidOperationException($"{___botOwner_0.Profile.Nickname} ({___botOwner_0.name}) has already been activated");
                }

                BrainManager.Instance.ActivatedBots.Add(___botOwner_0.GetPlayer, ___botOwner_0);
                ___botOwner_0.GetPlayer.OnPlayerDeadOrUnspawn += (player) => { BrainManager.Instance.ActivatedBots.Remove(player); };

                ___botOwner_0.RemoveAllExcludedLayers();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Could not remove initially excluded layers for {___botOwner_0.Profile.Nickname} ({___botOwner_0.name})");

                Logger.LogError(ex);
                throw ex;
            }
        }
    }
}
