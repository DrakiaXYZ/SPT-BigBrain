using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DrakiaXYZ.BigBrain
{
    internal class Utils
    {
        public static FieldInfo GetFieldByType(Type classType, Type fieldType)
        {
            return AccessTools.GetDeclaredFields(classType).FirstOrDefault(
                x => fieldType.IsAssignableFrom(x.FieldType) || (x.FieldType.IsGenericType && fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().IsAssignableFrom(x.FieldType.GetGenericTypeDefinition())));
        }

        public static string GetPropertyNameByType(Type classType, Type propertyType)
        {
            return AccessTools.GetDeclaredProperties(classType).FirstOrDefault(
                x => propertyType.IsAssignableFrom(x.PropertyType) || (x.PropertyType.IsGenericType && propertyType.IsGenericType && propertyType.GetGenericTypeDefinition().IsAssignableFrom(x.PropertyType.GetGenericTypeDefinition())))?.Name;
        }

        public static bool HasSameContents<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
        {
            if (collection1.Count() != collection2.Count())
            {
                return false;
            }

            if (collection1.Any(item => !collection2.Contains(item)))
            {
                return false;
            }

            return true;
        }

        internal static string CreateCollectionText<T>(IEnumerable<T> items, int maxItemsToList = 10)
        {
            int itemCount = items.Count();

            if (itemCount > maxItemsToList)
            {
                return $"{itemCount} {typeof(T).Name}s";
            }

            return string.Join(", ", items);
        }
    }
}
