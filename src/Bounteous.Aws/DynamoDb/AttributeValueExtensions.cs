using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using Bounteous.Core.Extensions;
using Bounteous.Core.Validations;

namespace Bounteous.Aws.DynamoDb
{
    public static class AttributeValueExtensions
    {
        public static T Convert<T>(this AttributeValue value, Func<AttributeValue, T> converter)
        {
            return converter(value);
        }
        
        public static string Str(this IReadOnlyDictionary<string, AttributeValue> value, string key, bool enforce = true)
        {
            if (!enforce) 
                return value.TryGetValue(key, out var value1) ? value1.S ?? string.Empty : string.Empty;
            
            Validate.Begin().IsTrue(value.ContainsKey(key), "key not found").Check()
                    .IsNotEmpty(value[key].S, $"value at {key} is empty")
                    .Check();
            return value[key].S;
        }

        public static bool Bool(this IReadOnlyDictionary<string, AttributeValue> value, string key, bool withDefault = false)
        {
            return value.ContainsKey(key) ? value[key].BOOL : withDefault;
        }

        public static T EnumStr<T>(this IReadOnlyDictionary<string, AttributeValue> value, string key) where T : struct, IConvertible
        {
            return value[key].S.ToEnum<T>();
        }

        public static double Integer(IReadOnlyDictionary<string, AttributeValue> value, string key)
        {
            return int.Parse(value[key].N);
        }

        public static double Double(this IReadOnlyDictionary<string, AttributeValue> value, string key)
        {
            return double.Parse(value[key].N);
        }

        public static T Enum<T>(this IReadOnlyDictionary<string, AttributeValue> value, string key) where T : struct, IConvertible
        {
            return value[key].N.ToEnum<T>();
        }

        public static DateTime DateTime(this IReadOnlyDictionary<string, AttributeValue> value, string key)
        {
            return value[key].ToDateTime();
        }

        public static IEnumerable<T> FromList<T>(this Dictionary<string, AttributeValue> value, string key,
            Func<IReadOnlyDictionary<string, AttributeValue>, T> converter)
        {
            return !value.ContainsKey(key) ? Enumerable.Empty<T>() : 
                    value[key].L.Select(x => converter(x.M));
        }

        public static T Map<T>(this IReadOnlyDictionary<string, AttributeValue> value, string key, 
                        Func<IReadOnlyDictionary<string, AttributeValue>, T> converter, bool enforce = false)
        {
            var item = value.ContainsKey(key) ? value[key] : null;
            if (!enforce && item == null) return default;
            
            Validate.Begin()
                    .IsNotNull(item.M, "item is null").Check()
                    .IsNotEmpty(item.M, $"value at {key} is empty")
                .Check();
            return converter(item.M);
        }

        public static DateTime ToDateTime(this AttributeValue value)
        {
            return System.DateTime.Parse(value.S);
        }

        public static T ToEnum<T>(this AttributeValue value) where T : struct, IConvertible
        {
            return value.Convert(x => x.N.ToString().ToEnum<T>());
        }
    }
}