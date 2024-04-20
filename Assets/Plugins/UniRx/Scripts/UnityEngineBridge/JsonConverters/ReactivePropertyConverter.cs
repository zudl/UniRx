#if UNIRX_NEWTONSOFT_SUPPORT && !UNIRX_NEWTONSOFT_SUPPORT_DISABLED
using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace UniRx.Json {
    public class ReactivePropertyConverter : JsonConverter {
        [Preserve]
        public ReactivePropertyConverter() : base() { }

        public override void WriteJson(JsonWriter writer, object reactiveProperty, JsonSerializer serializer) {
            var type = reactiveProperty.GetType();
            var propertyInfo = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            var value = propertyInfo.GetValue(reactiveProperty);
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var reactivePropertyGenericType = GetReactivePropertyType(objectType);
            if (reactivePropertyGenericType == null) {
                return null;
            }

            var propertyInfo = reactivePropertyGenericType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            var innerType = reactivePropertyGenericType.GetGenericArguments()[0];
            object innerValue;

            var jToken = JToken.Load(reader);
            switch (jToken.Type) {
                case JTokenType.Object:
                    innerValue = ReadPropertyObject(jToken, serializer, innerType);
                    break;
                case JTokenType.Null:
                    innerValue = null;
                    break;
                default:
                    innerValue = jToken.ToObject(innerType, serializer);
                    break;
            }

            if (existingValue == null) {
                existingValue = Activator.CreateInstance(objectType);
            }
            propertyInfo.SetValue(existingValue, innerValue);
            return existingValue;
        }

        private object ReadPropertyObject(JToken jToken, JsonSerializer serializer, Type innerType) {
            if (!jToken.HasValues) {
                return jToken.ToObject(innerType, serializer);
            }

            var jProperty = jToken.First as JProperty;
            if (jProperty != null) {
                var nextProperty = jProperty.Next as JProperty;
                var nextName = nextProperty != null ? nextProperty.Name : null;
                if (jProperty.Name == "Value" && nextName == "HasValue") {
                    return jProperty.Value.ToObject(innerType, serializer);
                }

                return jToken.ToObject(innerType, serializer);
            }

            return null;
        }

        public override bool CanConvert(Type objectType) {
            return GetReactivePropertyType(objectType) != null;
        }

        private static Type GetReactivePropertyType(Type objectType) {
            while (objectType != null) {
                if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ReactiveProperty<>))
                    return objectType;

                objectType = objectType.BaseType;
            }

            return null;
        }
    }
}
#endif
