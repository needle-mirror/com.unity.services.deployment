using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Unity.Services.Deployment.Editor.JsonUtils
{
    /// <summary>
    /// Extension methods for converting Newtonsoft.Json JToken types to native .NET objects.
    /// </summary>
    static class JTokenExtensions
    {
        public static object ToValue(this JToken token)
        {
            return token.Type switch
            {
                JTokenType.Null => null!,
                JTokenType.String => ((JValue)token).Value!,
                JTokenType.Integer => ((JValue)token).Value!,
                JTokenType.Float => ((JValue)token).Value!,
                JTokenType.Boolean => ((JValue)token).Value!,
                JTokenType.Date => ((JValue)token).Value!,
                JTokenType.Guid => ((JValue)token).Value!,
                JTokenType.Uri => ((JValue)token).Value!,
                JTokenType.TimeSpan => ((JValue)token).Value!,
                JTokenType.Array => ToList((JArray)token),
                JTokenType.Object => ToDictionary((JObject)token),
                _ => token
            };
        }

        static object ToList(JArray jArray)
        {
            var items = jArray.Select(ToValue).ToList();
            return TryCoerceToTypedList(items);
        }

        /// <summary>
        /// If every non-null element in <paramref name="items"/> shares the same runtime type,
        /// returns a strongly-typed <c>List&lt;T&gt;</c>; otherwise returns the original
        /// <c>List&lt;object&gt;</c> unchanged.
        /// Because <see cref="ToValue"/> is called recursively, nested arrays are also coerced
        /// (e.g. <c>[[1,2],[3,4]]</c> becomes <c>List&lt;List&lt;long&gt;&gt;</c>).
        /// </summary>
        static object TryCoerceToTypedList(List<object> items)
        {
            if (items.Count == 0)
                return items;

            var nonNull = items.Where(x => x != null).ToList();
            if (nonNull.Count == 0)
                return items;

            var elementType = nonNull[0].GetType();
            if (!nonNull.All(x => x.GetType() == elementType))
                return items;

            // Value types cannot hold null – fall back to List<object> if any element is null
            if (elementType.IsValueType && items.Count != nonNull.Count)
                return items;

            var typedListType = typeof(List<>).MakeGenericType(elementType);
            var typedList = (IList)Activator.CreateInstance(typedListType, items.Count);
            foreach (var item in items)
                typedList.Add(item);

            return typedList;
        }

        static Dictionary<string, object> ToDictionary(JObject jObject)
        {
            var dictionary = new Dictionary<string, object>(jObject.Count);
            foreach (var property in jObject.Properties())
            {
                dictionary[property.Name] = ToValue(property.Value);
            }

            return dictionary;
        }
    }
}
