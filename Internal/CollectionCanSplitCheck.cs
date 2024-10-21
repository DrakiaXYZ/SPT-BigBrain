using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class CollectionCanSplitCheck<T>
    {
        public IEnumerable<T> SameItems { get; private set; }
        public IEnumerable<T> RemainingItemsOld { get; private set; }
        public IEnumerable<T> RemainingItemsNew { get; private set; }
        public bool CanSplit { get; private set; } = false;

        public CollectionCanSplitCheck(IEnumerable<T> existingItems, IEnumerable<T> newItems)
        {
            performCheck(existingItems, newItems);
        }

        private void performCheck(IEnumerable<T> existingItems, IEnumerable<T> newItems)
        {
            SameItems = existingItems.Intersect(newItems);
            RemainingItemsOld = existingItems;
            RemainingItemsNew = newItems;
            
            if (SameItems.Any())
            {
                RemainingItemsOld = existingItems.Except(SameItems);
                RemainingItemsNew = newItems.Except(SameItems);

                CanSplit = RemainingItemsOld.Any() || RemainingItemsNew.Any();
            }
        }

        public IEnumerable<IEnumerable<T>> GetRemainingItemsCollections()
        {
            if (RemainingItemsOld.Any())
            {
                yield return RemainingItemsOld;
            }

            if (RemainingItemsNew.Any())
            {
                yield return RemainingItemsNew;
            }
        }
    }
}
