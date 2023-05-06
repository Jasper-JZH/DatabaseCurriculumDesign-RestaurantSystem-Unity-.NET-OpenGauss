using System.Net;
using System.Net.Sockets;

namespace restaurantSystemServer
{

    //Socket服务器类，包含一些和Unity客户端进行Socket通信的方法
    class Server : Singleton<Server>
    {
        //Socket通信相关参数
        public Socket listenSocket;
        public Client client;

        private static string ip = "127.0.0.1";
        private static int port = 1234;

        public Server()
        {
            client = new Client();
        }

        //开启服务器
        public void Start()
        {
            try
            {
                //初始化服务端socket
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //配置ip和port并绑定到socket
                IPAddress ipAdr = IPAddress.Parse(ip);
                IPEndPoint ipEp = new IPEndPoint(ipAdr, port);
                listenSocket.Bind(ipEp);
                //开始监听
                listenSocket.Listen(3); // 随意设置，这里设置最多监听三台
                Console.WriteLine("开启监听成功！");

                //开始异步接收客户端的数据
                listenSocket.BeginAccept(AcceptCb, null);
                Console.WriteLine("[服务器]启动完成！");
            }
            catch (Exception e)
            {
                Console.WriteLine($"【异常】Start:{e.Message}");
            }

        }


        //BeginAccept回调函数，负责异步接收客户端数据，并再次调用BeginAccept实现循环
        private void AcceptCb(IAsyncResult ar)
        {
            try
            {
                Socket socket = listenSocket.EndAccept(ar);
                client.Init(socket);

                string clientAdr = client.GetAddress();    //获取客户端的ipAddress
                Console.WriteLine($"[客户端:{clientAdr}]连接成功，正在接收数据...");

                //异步接收客户端数据
                client.socket.BeginReceive(client.readBuff, client.buffCount, client.BufferRemain(), SocketFlags.None, ReceiveCb, client);
                //再次调用BeginAccept接收客户端的消息，实现循环
                listenSocket.BeginAccept(AcceptCb, null);

            }
            catch (Exception e)
            {
                Console.WriteLine($"【异常】AcceptCb:{e.Message}");
            }
        }

        //BeginReceive回调函数，负责接收客户端发来的消息并处理，并再次调用BeginReceive接收下一个数据
        private void ReceiveCb(IAsyncResult ar)
        {
            try
            {
                //获取BeginReceive传入的客户端对象
                Client client = (Client)ar.AsyncState;
                int count = client.socket.EndReceive(ar);   //获取接收的字节数
                Console.WriteLine($"收到[{client.GetAddress()}]{count}个字节的数据");
                //处理关闭信号
                if (count <= 0)
                {
                    Console.WriteLine($"收到[{client.GetAddress()}]断开连接");
                    client.Close();
                    return;
                }
                //处理正常数据信号
                string msgStr = System.Text.Encoding.UTF8.GetString(client.readBuff, 0, count);
                Console.WriteLine($"收到[{client.GetAddress()}]的数据：{msgStr}");

                //处理msg，生成SQL语句
                string sql;
                JsonDisposer.Ins ins = JsonDisposer.DisposeReceive(msgStr, out sql);

                //确定SQL语句的类型
                DataBase.SQLType sqlType = DataBase.GetSQLType(ins);

                //根据SQL语句类型调用不同的SQL执行,只有select用reader，其它都一样
                if (sqlType == DataBase.SQLType.SELECT)
                {
                    DataBase.ExecuteSelectSQL(sql, ins);
                }
                else DataBase.ExecuteAlterSQL(sql, ins);

                //再次调用BeginReceive接收下一个数据
                client.socket.BeginReceive(client.readBuff, client.buffCount, client.BufferRemain(), SocketFlags.None, ReceiveCb, client);
            }
            catch (Exception e)
            {
                Console.WriteLine($"【异常】ReceiveCb:{e}");
            }
        }

        public void SendMsg(Client _tarClient, string _msg)
        {
            try
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(_msg);    //将字符串转换为字节流
                _tarClient.socket.Send(bytes);
            }
            catch(Exception e)
            {
                Console.WriteLine($"【异常】SendMsg:{e.Message}");
            }
        }
    }
}
