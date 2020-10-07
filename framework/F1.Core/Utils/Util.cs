using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace F1.Core.Utils
{
    public static class Util
    {
        public static Func<byte[], int, int, CodedOutputStream> GetNewCodecOutputStream()
        {
            var paramBuffer = Expression.Parameter(typeof(byte[]), "buffer");
            var paramOffset = Expression.Parameter(typeof(int), "offset");
            var paramLength = Expression.Parameter(typeof(int), "length");
            var ctor = typeof(CodedOutputStream).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(byte[]), typeof(int), typeof(int) }, null);
            var lambda = Expression.Lambda<Func<byte[], int, int, CodedOutputStream>>(
                Expression.New(ctor, paramBuffer, paramOffset, paramLength), paramBuffer, paramOffset, paramLength);
            return lambda.Compile();
        }

        public static async Task RunTaskTimer(Func<Task> f, int interval) 
        {
            long beginMilliSeconds = Platform.GetMilliSeconds();
            long count = 0;
            while (true) 
            {
                count++;

                try
                {
                    await f().ConfigureAwait(false);
                }
                catch (Exception e) 
                {
                }

                var sleep = beginMilliSeconds + count * interval - Platform.GetMilliSeconds();
                if (sleep > 0) 
                {
                    await Task.Delay((int)sleep).ConfigureAwait(false);
                }
            }
        }
        public static async Task RunTaskTimer(Action f, int interval) 
        {
            long beginMilliSeconds = Platform.GetMilliSeconds();
            long count = 0;
            while (true) 
            {
                count++;

                try
                {
                    f();
                }
                catch (Exception e) 
                {
                }

                var sleep = beginMilliSeconds + count * interval - Platform.GetMilliSeconds();
                if (sleep > 0) 
                {
                    await Task.Delay((int)sleep).ConfigureAwait(false);
                }
            }
        }
    }
}
