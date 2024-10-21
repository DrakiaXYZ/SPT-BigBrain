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

        internal static bool DoLayerInfoSettingsAffectBot(BotOwner botOwner, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
        {
            if (!roles.Contains(botOwner.Profile.Info.Settings.Role))
            {
                return false;
            }

            if (!brainNames.Contains(botOwner.Brain.BaseBrain.ShortName()))
            {
                return false;
            }

            return true;
        }

        internal bool AffectsBot(BotOwner botOwner)
        {
            return DoLayerInfoSettingsAffectBot(botOwner, affectedBrainNames, affectedRoles);
        }

        internal bool ContainsAll(IEnumerable<string> brainNames)
        {
            IEnumerable<string> matchingBrainNames = brainNames.Intersect(affectedBrainNames);
            return Utils.HasSameContents(affectedBrainNames, matchingBrainNames);
        }

        internal bool ContainsAll(IEnumerable<WildSpawnType> roles)
        {
            IEnumerable<WildSpawnType> matchingRoles = roles.Intersect(affectedRoles);
            return Utils.HasSameContents(affectedRoles, matchingRoles);
        }
    }
}
