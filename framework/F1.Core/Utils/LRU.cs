using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Core.Utils
{
    public class LRU<K, V> where V : class
    {
        private readonly object mutex = new object();
        private readonly LinkedList<ValueTuple<K, V>> list = new LinkedList<ValueTuple<K, V>>();
        private readonly Dictionary<K, LinkedListNode<ValueTuple<K, V>>> dict = new Dictionary<K, LinkedListNode<ValueTuple<K, V>>>();
        private readonly int capacity;

        public LRU(int size) 
        {
            this.capacity = size;
        }

        public bool TryAdd(K k, V v) 
        {
            lock (mutex)
            {
                if (this.dict.TryGetValue(k, out var vv))
                {
                    return false;
                }
                this.list.AddLast((k, v));
                vv = this.list.Last;
                this.dict.Add(k, vv);

                if (this.dict.Count > this.capacity) 
                {
                    var delete = this.list.First;
                    this.dict.Remove(delete.Value.Item1);
                    this.list.Remove(delete);
                }
                return true;
            }
        }

        public void Add(K k, V v) 
        {
            lock (mutex) 
            {
                if (this.dict.TryGetValue(k, out var vv))
                {
                    this.list.Remove(vv);
                    this.list.AddLast(vv);
                    vv.Value = (k, v);
                    return;
                }
                this.list.AddLast((k, v));
                vv = this.list.Last;
                this.dict.Add(k, vv);

                if (this.dict.Count > this.capacity) 
                {
                    var delete = this.list.First;
                    this.dict.Remove(delete.Value.Item1);
                    this.list.Remove(delete);
                }
            }
        }

        public V Get(K k) 
        {
            lock (mutex)
            {
                if (this.dict.TryGetValue(k, out var v))
                {
                    this.list.Remove(v);
                    this.list.AddLast(v);
                    return v.Value.Item2;
                }
                return null;
            }
        }

        public void Remove(K k) 
        {
            lock (mutex) 
            {
                if (this.dict.Remove(k, out var v))
                {
                    this.list.Remove(v);
                }
            }
        }
    }
}
