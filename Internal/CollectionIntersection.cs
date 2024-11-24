using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BigBrain.Internal
{
    internal class CollectionIntersection<T>
    {
        public IEnumerable<T> Collection1 { get; private set; }
        public IEnumerable<T> Collection2 { get; private set; }
        public IEnumerable<T> CommonElements { get; private set; }
        public IEnumerable<T> UniqueCollection1Elements { get; private set; }
        public IEnumerable<T> UniqueCollection2Elements { get; private set; }

        public bool HasCommonAndUniqueElements => CommonElements.Any() && (UniqueCollection1Elements.Any() || UniqueCollection2Elements.Any());

        public CollectionIntersection(IEnumerable<T> collection1, IEnumerable<T> collection2)
        {
            Collection1 = collection1;
            Collection2 = collection2;

            CommonElements = Collection1.Intersect(Collection2);
            UniqueCollection1Elements = Collection1.Except(CommonElements);
            UniqueCollection2Elements = Collection2.Except(CommonElements);
        }

        public IEnumerable<IEnumerable<T>> GetUniqueElementCollections()
        {
            if (UniqueCollection1Elements.Any())
            {
                yield return UniqueCollection1Elements;
            }

            if (UniqueCollection2Elements.Any())
            {
                yield return UniqueCollection2Elements;
            }
        }

        public IEnumerable<IEnumerable<T>> GetAllElementCollections()
        {
            if (CommonElements.Any())
            {
                yield return CommonElements;
            }

            foreach (IEnumerable<T> collection in GetUniqueElementCollections())
            {
                yield return collection;
            }
        }
    }
}
