using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;


namespace Sample.Client
{
    class Program
    {
        static async Task DispatchMessage(Account account, IMessage message, NetworkStream stream) 
        {
            await Task.Yield();
            switch (message) 
            {
                case ResponseLogin login:
                    {
                        account.Logined = true;
                        Console.WriteLine("Account:{0} Login success", account.AccountID);
                        var requestPlayerList = new RequestPlayerList();
                        await stream.WriteAsync(requestPlayerList.Encode());
                    }
                    break;
                case ResponsePlayerList playerList:
                    {
                        account.AddPlayers(playerList.Player);
                        Console.WriteLine("Acount:{0} PlayersCount:{1}, PlayerList:{2}", account.AccountID, account.PlayerCount, playerList);

                        var randomPlayer = account.RandomPlayer();
                        var changePlayer = new RequestChangePlayer();
                        changePlayer.Player = randomPlayer;
                        await stream.WriteAsync(changePlayer.Encode());
                        Console.WriteLine("Account:{0} RandomSelectPlayer:{1}", account.AccountID, randomPlayer);
                    }
                    break;
                case ResponseChangePlayer changePlayer:
                    {
                        Console.WriteLine("Account:{0} ChangePlayer:{1}, Error:{2}", 
                                    account.AccountID, changePlayer.Player, changePlayer.Error);
                        var requestGetID = new RequestGetID();
                        await stream.WriteAsync(requestGetID.Encode());
                    }
                    break;
                case ResponseGetID getId:
                    {
                        Console.WriteLine("Account:{0} Actor:{1}/{2}", account.AccountID, getId.ActorType, getId.ActorId);
                        account.EchoCount = 0;
                        var echoMessage = new RequestEcho();
                        echoMessage.Content = account.RandomEchoString();
                        await stream.WriteAsync(echoMessage.Encode());
                    }
                    break;
                case ResponseEcho echo:
                    {
                        Console.WriteLine("Account:{0} EchoResponse:{1}", account.AccountID, echo.Content);
                        if (account.EchoCount++ >= 100) 
                        {
                            var requestGoBack = new RequestGoBack();
                            await stream.WriteAsync(requestGoBack.Encode());
                            return;
                        }
                        var echoMessage = new RequestEcho();
                        echoMessage.Content = account.RandomEchoString();
                        await stream.WriteAsync(echoMessage.Encode());
                    }
                    break;
                case ResponseGoBack goback:
                    {
                        Console.WriteLine("Account:{0} go back", account.AccountID);
                    }
                    break;
                default:
                    Console.WriteLine("Account:{0} Message:{1} not processed", account.AccountID, message.GetType().Name);
                    break;
            }
        }

        static async Task Connect(string ip, int port, string accountID)
        {
            var account = new Account(accountID);

            var client = new TcpClient();
            await client.ConnectAsync(ip, port).ConfigureAwait(false);
            Console.WriteLine("Connect Server: {0}:{1}", ip, port);
            var stream = client.GetStream();

            var loginMessage = account.AccountID.Encode();
            await stream.WriteAsync(loginMessage);

            var recvBuffer = new byte[1024];

            try
            {
                while (true)
                {
                    var length = await stream.ReadAsync(recvBuffer);
                    var resp = new ArraySegment<byte>(recvBuffer, 0, length);
                    var msg = resp.Decode();
                    await DispatchMessage(account, msg, stream);
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
