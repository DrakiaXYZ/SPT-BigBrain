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
        internal List<string> brainNames;
        internal List<WildSpawnType> roles;

        internal bool AffectsBot(BotOwner botOwner) => botOwner.IsAffectedByLayer(this);

        internal bool ContainsAll(IEnumerable<string> brainNames) => brainNames.All(name => this.brainNames.Contains(name));
        internal bool ContainsAll(IEnumerable<WildSpawnType> roles) => roles.All(role => this.roles.Contains(role));

        internal bool ContainsAny(IEnumerable<string> brainNames) => brainNames.Any(name => this.brainNames.Contains(name));
        internal bool ContainsAny(IEnumerable<WildSpawnType> roles) => roles.Any(role => this.roles.Contains(role));
    }
}
