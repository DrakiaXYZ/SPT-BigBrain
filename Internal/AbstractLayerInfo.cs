using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BigBrain.Internal
{
    public abstract class AbstractLayerInfo
    {
        internal List<string> affectedBrainNames;
        internal List<WildSpawnType> affectedRoles;

        internal bool AffectsBot(BotOwner botOwner)
        {
            return DoLayerInfoSettingsAffectBot(botOwner, affectedBrainNames, affectedRoles);
        }

        internal static bool DoLayerInfoSettingsAffectBot(BotOwner botOwner, List<string> brainNames, List<WildSpawnType> roles)
        {
            if (!brainNames.Contains(botOwner.Brain.BaseBrain.ShortName()))
            {
                return false;
            }

            if (!roles.Contains(botOwner.Profile.Info.Settings.Role))
            {
                return false;
            }

            return true;
        }
    }
}
