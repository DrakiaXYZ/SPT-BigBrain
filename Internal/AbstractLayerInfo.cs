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

        internal static bool DoSettingsAffectBot(BotOwner botOwner, IEnumerable<string> brainNames, IEnumerable<WildSpawnType> roles)
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

        internal bool AffectsBot(BotOwner botOwner) => DoSettingsAffectBot(botOwner, affectedBrainNames, affectedRoles);

        internal bool ContainsAll(IEnumerable<string> brainNames) => brainNames.All(name => affectedBrainNames.Contains(name));
        internal bool ContainsAll(IEnumerable<WildSpawnType> roles) => roles.All(role => affectedRoles.Contains(role));

        internal bool ContainsAny(IEnumerable<string> brainNames) => brainNames.Any(name => affectedBrainNames.Contains(name));
        internal bool ContainsAny(IEnumerable<WildSpawnType> roles) => roles.Any(role => affectedRoles.Contains(role));
    }
}
