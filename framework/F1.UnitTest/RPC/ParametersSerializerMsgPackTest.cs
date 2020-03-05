using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using F1.Core.RPC;
using MessagePack;

namespace F1.UnitTest.RPC
{
    public class ParametersSerializerMsgPackTest
    {
        private readonly ParametersSerializerMsgPack serializer = new ParametersSerializerMsgPack();

        public enum XXXX1
        {
            XXXXXXXX = 10000,
        }

        [Fact]
        public void Response() 
        {
            var o1 = 1;
            var bytes = this.serializer.Serialize(o1, o1.GetType());

            var o2 = this.serializer.Deserialize(bytes, o1.GetType());
            Assert.Equal(o1, o2);
        }

        [Fact]
        public void SimpleType()
        {
            var o1 = new object[] { 1, 1.0, "1.1", XXXX1.XXXXXXXX };

            var types = new Type[] { o1[0].GetType(), o1[1].GetType(), o1[2].GetType(), o1[3].GetType() };
            var bytes = this.serializer.Serialize(o1, types);

            var o2 = this.serializer.Deserialize(bytes, types);
            Assert.Equal(o1, o2);
        }

        [Fact]
        public void NullArgs()
        {
            var o1 = new object[] { 1, 1.0, "1.1", null};
            var types = new Type[] { o1[0].GetType(), o1[1].GetType(), o1[2].GetType(), typeof(string) };
            var bytes = this.serializer.Serialize(o1, types);
            var o2 = this.serializer.Deserialize(bytes, types);
            Assert.Equal(o1, o2);
        }

        [Fact]
        public void ComplexType()
        {
            var o1 = new object[]
            {
                new int[2] { 3, 5 },
                new List<string>() { "121212", "3232423" },
                new Dictionary<string, float>()
                {
                    {"1111", 1111},
                    {"2211", 121212},
                },
            };

            var types = new Type[] { typeof(int[]), typeof(List<string>), typeof(Dictionary<string, float>) };
            var bytes = this.serializer.Serialize(o1, types);

            var o2 = this.serializer.Deserialize(bytes, types);
            Assert.Equal(o1, o2);
        }

        [MessagePackObject]
        public class UserDefined1 
        {
            [Key(0)]
            public string s1;
            [Key(1)]
            public int i2;
            [Key(2)]
            public List<int> l3;
            [Key(3)]
            public Dictionary<int, int> d4;
        }

        [Fact]
        public void UserDefinedType() 
        {
            var o1 = new object[]
            {
                new UserDefined1()
                {
                    s1 = "12121",
                    i2 = 333,
                    l3 = new List<int>() { 11, 2, 4, 7, 8, 0, },
                    d4 = new Dictionary<int, int>()
                    {
                        { 1, 1},
                        { 2, 3},
                        { 10, 13},
                    },
                },
            };
            var types = new Type[] { typeof(UserDefined1) };
            var bytes = this.serializer.Serialize(o1, types);

            var o2 = this.serializer.Deserialize(bytes, types);
            Assert.Equal(((UserDefined1)o1[0]).s1, ((UserDefined1)o2[0]).s1);
            Assert.Equal(((UserDefined1)o1[0]).i2, ((UserDefined1)o2[0]).i2);
            Assert.Equal(((UserDefined1)o1[0]).l3, ((UserDefined1)o2[0]).l3);
            Assert.Equal(((UserDefined1)o1[0]).d4, ((UserDefined1)o2[0]).d4);
        }
    }
}
