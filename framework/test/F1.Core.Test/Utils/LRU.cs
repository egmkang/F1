using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using F1.Core.Utils;

namespace F1.Core.Test.Utils
{
    public class LRU
    {

        [Fact]
        public void LruFull()
        {
            var lru = new LRU<int, string>(3);
            lru.Add(1, "1");
            lru.Add(2, "2");
            lru.Add(3, "3");

            var v = lru.Get(1);
            Assert.Equal(v, "1");

            v = lru.Get(2);
            Assert.Equal(v, "2");

            v = lru.Get(3);
            Assert.Equal(v, "3");

            lru.Add(4, "4");
            v = lru.Get(1);
            Assert.Equal(v, null);
        }

        [Fact]
        public void LruGet()
        {
            var lru = new LRU<int, string>(3);
            lru.Add(1, "1");
            lru.Add(2, "2");
            lru.Add(3, "3");

            var v = lru.Get(11);
            Assert.Equal(v, null);

            v = lru.Get(1);
            Assert.Equal(v, "1");

            v = lru.Get(1);
            Assert.Equal(v, "1");

            v = lru.Get(2);
            Assert.Equal(v, "2");
        }

        [Fact]
        public void LruRemove()
        {
            var lru = new LRU<int, string>(3);
            lru.Add(1, "1");
            lru.Add(2, "2");
            lru.Add(3, "3");

            var v = lru.Get(1);
            Assert.Equal(v, "1");


            lru.Remove(1);

            v = lru.Get(1);
            Assert.Equal(v, null);
        }


        [Fact]
        public void LruTryAdd() 
        {
            var lru = new LRU<int, string>(3);
            lru.Add(1, "1");
            lru.Add(2, "2");
            lru.Add(3, "3");

            var r1 = lru.TryAdd(1, "1");
            Assert.False(r1);

            var r2 = lru.TryAdd(4, "4");
            Assert.True(r2);
        }
    }
}
