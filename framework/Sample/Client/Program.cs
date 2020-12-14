using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Sample;

namespace F1.Sample.Client
{
    public static class Ext 
    {
        private static ArraySegment<byte> Combine(params ArraySegment<byte>[] arrays)
        {
            var length = arrays.Sum(a => a.Count);
            var rv = new byte[length];
            int offset = 0;
            foreach (var array in arrays)
            {
                System.Buffer.BlockCopy(array.Array, array.Offset, rv, offset, array.Count);
                offset += array.Count;
            }
            return new ArraySegment<byte>(rv);
        }

        static ThreadLocal<byte[]> numberByteArray = new ThreadLocal<byte[]>(() => new byte[4]);
        public unsafe static  ArraySegment<byte> ToByteArray(this int number) 
        {
            var array = numberByteArray.Value;
            fixed (byte* pointer = array) 
            {
                var p = (byte*)&number;
                *pointer = *p;
            }
            return new ArraySegment<byte>(array);
        }

        static ThreadLocal<byte[]> nameByteArray = new ThreadLocal<byte[]>(() => new byte[1024]);
        public static ArraySegment<byte> ToNameByteArray(this IMessage message) 
        {
            var name = message.Descriptor.FullName;
            var bytes = nameByteArray.Value;
            var nameLength = Encoding.UTF8.GetBytes(name, 0, name.Length, bytes, 1);
            bytes[0] = (byte)nameLength;
            return new ArraySegment<byte>(bytes, 0, nameLength + 1);
        }

        public static Func<byte[], int, int, CodedOutputStream> GetNewStream = GetNewCodecOutputStream();

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

        static ThreadLocal<byte[]> bodyByteArray = new ThreadLocal<byte[]>(() => new byte[1024 * 10]);
        public static ArraySegment<byte> ToBodyByteArray(this IMessage message) 
        {
            var bodyLength = message.CalculateSize();

            var bytes = bodyByteArray.Value;
            using var stream = GetNewStream(bytes, 0, bodyLength);
            message.WriteTo(stream);
            stream.Flush();

            return new ArraySegment<byte>(bytes, 0, bodyLength);
        }

        public static ArraySegment<byte> Encode(this IMessage message) 
        {
            var name = message.ToNameByteArray();
            var body = message.ToBodyByteArray();
            var header = (name.Count + body.Count).ToByteArray();
            return Combine(header, name, body);
        }

        public static ArraySegment<byte> Encode(this string hello) 
        {
            var bytes = nameByteArray.Value;
            var count = Encoding.UTF8.GetBytes(hello, 0, hello.Length, bytes, 0);
            var helloArray = new ArraySegment<byte>(bytes, 0, count);

            return Combine(count.ToByteArray(), helloArray);
        }

        public static unsafe IMessage Decode(this ArraySegment<byte> buffer) 
        {
            var bytes = buffer.Array;
            var offset = buffer.Offset;
            fixed (byte* pointer = bytes)
            {
                var length = *(int*)(pointer + offset);
                offset += 4;
                var nameLength = pointer[offset];
                offset += 1;
                var name = Encoding.UTF8.GetString(bytes, offset, nameLength);
                offset += nameLength;

                IMessage msg = null;
                if (name == ResponseLogin.Descriptor.FullName)
                {
                    msg = new ResponseLogin();
                }
                else if (name == ResponseEcho.Descriptor.FullName)
                {
                    msg = new ResponseEcho();
                }

                using var stream = new CodedInputStream(bytes, offset, length - 1 - name.Length);
                msg.MergeFrom(stream);
                return msg;
            }
        }
    }

    class Program
    {
        private static string Content = "01234567890QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm";
        private static ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());

        public static int NextInt => random.Value.Next();

        static async Task Connect(string ip, int port, string playerID)
        {
            var client = new TcpClient();
            await client.ConnectAsync(ip, port).ConfigureAwait(false);
            var stream = client.GetStream();

            var loginMessage = playerID.Encode();
            await stream.WriteAsync(loginMessage);

            var recvBuffer = new byte[1024];

            var echoMessage = new RequestEcho();

            try
            {
                while (true)
                {
                    var length = await stream.ReadAsync(recvBuffer);
                    var resp = new ArraySegment<byte>(recvBuffer, 0, length);
                    var msg = resp.Decode();
                    Console.WriteLine("Response:{0}", msg);

                    echoMessage.Content = Content.Substring(0, (NextInt % Content.Length) + 1);
                    await stream.WriteAsync(echoMessage.Encode());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e);
            }
        }

        static void Main(string[] args)
        {
            _ = Connect("127.0.0.1", 18888, "30001");

            while (true) 
            {
                Thread.Sleep(100);
            }
        }
    }
}
