using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace restaurantSystemServer
{
    class Client
    {
        public Socket socket;

        //缓冲区相关
        public const int BUFFER_SIZE = 1024 * 6;
        public byte[] readBuff = new byte[BUFFER_SIZE];
        public int buffCount = 0;

        public void Init(Socket _socket)
        {
            this.socket = _socket;
            buffCount = 0;
        }

        //缓冲区剩余的字节数
        public int BufferRemain()
        {
            return BUFFER_SIZE - buffCount;
        }

        public string GetAddress()
        {
            return socket.RemoteEndPoint.ToString();
        }

        public void Close()
        {
            Console.WriteLine($"【断开连接】{GetAddress()}");
            socket.Close();
        }
    }
}
