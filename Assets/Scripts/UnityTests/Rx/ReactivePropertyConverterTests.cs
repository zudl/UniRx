#if UNIRX_NEWTONSOFT_SUPPORT && !UNIRX_NEWTONSOFT_SUPPORT_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using NUnit.Framework;
using UniRx.Json;

namespace UniRx.Tests.Converters
{
    public static class NewtonsoftTest
    {
        [Test]
        public static void ReactivePropertyConverterTest()
        {
            var instance = new TestClass();
            instance.IntProp1.Value = 1;
            instance.IntProp2.Value = 1;
            instance.VectorProp.Value = new Vector2Struct(1, 1);
            instance.CustomProp.Value = new CustomRecord() { A = 100, B = "str" };
            instance.SimilarProp.Value.Value = 1;
            instance.ArrayProp.Value = new[] { "1", "2", "3" };
            instance.NestedRxProp.Value.Value = 1;
            instance.ListOfNestedRxProp.Value =
                new List<ReactiveProperty<int>>() { new ReactiveProperty<int>(1), new ReactiveProperty<int>(2) };

            var oldFormatJson = JsonConvert.SerializeObject(instance, new JsonSerializerSettings()
            {
                ContractResolver = new IgnoreContractResolver(typeof(ReactivePropertyConverter))
            });
            Debug.Log("Old Json: \n" + oldFormatJson);
            var deserializedOld = JsonConvert.DeserializeObject<TestClass>(oldFormatJson, new ReactivePropertyConverter());
            Debug.Log("Deserialized old json: \n" + deserializedOld);
            Assert.That(deserializedOld.Equals(instance));

            var newFormatJson = JsonConvert.SerializeObject(instance, new ReactivePropertyConverter());
            Debug.Log("\nNew Json: \n" + newFormatJson);
            var deserializedNew = JsonConvert.DeserializeObject<TestClass>(newFormatJson, new ReactivePropertyConverter());
            Debug.Log("Deserialized new json: \n" + deserializedNew);
            Assert.That(deserializedNew.Equals(instance));
        }

        [Serializable]
        private class TestClass : IEquatable<TestClass>
        {
            public ReactiveProperty<int> IntProp1 = new ReactiveProperty<int>(0);

            public IntReactiveProperty IntProp2 = new IntReactiveProperty(0);

            public ReactiveProperty<Vector2Struct> VectorProp = new ReactiveProperty<Vector2Struct>();

            public ReactiveProperty<CustomRecord> CustomProp =
                new ReactiveProperty<CustomRecord>(new CustomRecord());

            public ReactiveProperty<SimilarRecord> SimilarProp =
                new ReactiveProperty<SimilarRecord>(new SimilarRecord());

            public ReactiveProperty<string[]> ArrayProp = new ReactiveProperty<string[]>(new string[0]);

            public ReactiveProperty<ReactiveProperty<int>> NestedRxProp =
                new ReactiveProperty<ReactiveProperty<int>>(new ReactiveProperty<int>());

            public ReactiveProperty<List<ReactiveProperty<int>>> ListOfNestedRxProp =
                new ReactiveProperty<List<ReactiveProperty<int>>>(new List<ReactiveProperty<int>>());

            public override string ToString()
            {
                return string.Format("TestClass({0}, ", IntProp1.Value) +
                       string.Format("{0}, ", IntProp2.Value) +
                       string.Format("{0}, ", VectorProp.Value) +
                       string.Format("{0}, ", CustomProp.Value) +
                       string.Format("{0}, ", SimilarProp.Value) +
                       string.Format("[{0}], ", ArrayProp.Value.Aggregate("", (acc, j) => acc + j + ", ")) +
                       string.Format("Nested({0}), ", NestedRxProp.Value.Value) +
                       string.Format("[{0}])",
                           ListOfNestedRxProp.Value.Aggregate("", (acc, j) => acc + j.Value + ", "));
            }

            public bool Equals(TestClass other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return IntProp1.Value == other.IntProp1.Value &&
                       IntProp2.Value == other.IntProp2.Value &&
                       VectorProp.Value.Equals(other.VectorProp.Value) &&
                       CustomProp.Value.Equals(other.CustomProp.Value) &&
                       SimilarProp.Value.Value == other.SimilarProp.Value.Value &&
                       ArrayProp.Value.Length == other.ArrayProp.Value.Length &&
                       NestedRxProp.Value.Value == other.NestedRxProp.Value.Value &&
                       ListOfNestedRxProp.Value.Count == other.ListOfNestedRxProp.Value.Count;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TestClass)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = (IntProp1.Value.GetHashCode());
                    hashCode = (hashCode * 397) ^ (IntProp2.Value.GetHashCode());
                    hashCode = (hashCode * 397) ^ (VectorProp.Value.GetHashCode());
                    hashCode = (hashCode * 397) ^ (CustomProp.Value != null ? CustomProp.Value.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (SimilarProp.Value != null ? SimilarProp.Value.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (ArrayProp.Value.Length.GetHashCode());
                    hashCode = (hashCode * 397) ^ (NestedRxProp.Value.Value.GetHashCode());
                    hashCode = (hashCode * 397) ^ (ListOfNestedRxProp.Value.Count.GetHashCode());
                    return hashCode;
                }
            }
        }

        [Serializable]
        private class SimilarRecord
        {
            public int Value = 0;

            public override string ToString()
            {
                return string.Format("SimilarRecord({0})", Value);
            }
        }

        [Serializable]
        private class CustomRecord : IEquatable<CustomRecord>
        {
            public int A = 0;
            public string B = "";

            public override string ToString()
            {
                return string.Format("CustomRecord({0}, {1})", A, B);
            }


            public bool Equals(CustomRecord other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return A == other.A && B == other.B;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((CustomRecord)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (A * 397) ^ (B != null ? B.GetHashCode() : 0);
                }
            }
        }

        private struct Vector2Struct : IEquatable<Vector2Struct>
        {
            public float x;
            public float y;

            public Vector2Struct(float x, float y)
            {
                this.x = x;
                this.y = y;
            }

            public override string ToString()
            {
                return string.Format("Vector2Struct({0}, {1})", x, y);
            }

            public bool Equals(Vector2Struct other) {
                return x.Equals(other.x) && y.Equals(other.y);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Vector2Struct && Equals((Vector2Struct)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (x.GetHashCode() * 397) ^ y.GetHashCode();
                }
            }
        }

        private class IgnoreContractResolver : DefaultContractResolver
        {
            private readonly Type[] _typesToIgnore;

            public IgnoreContractResolver(params Type[] typesToIgnore)
            {
                _typesToIgnore = typesToIgnore;
            }

            protected override JsonConverter ResolveContractConverter(Type objectType)
            {
                var converter = base.ResolveContractConverter(objectType);
                if ((converter != null) && _typesToIgnore.Contains(converter.GetType()))
                {
                    converter = null;
                }

                return converter;
            }
        }
    }
}
#endif
