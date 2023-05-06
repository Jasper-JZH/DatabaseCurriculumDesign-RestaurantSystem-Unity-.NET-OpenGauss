namespace restaurantSystemServer
{
    //主程序
    class Program
    {
        static void Main(string[] args)
        {
            //初始化生成SQL的字典
            JsonDisposer.InitInstructionDic();
            //初始化DB中reader的字典
            DataBase.InitDic();
            //开启服务器
            Server.Instance.Start();
            while (true)
            {
                string str = Console.ReadLine();
                switch (str)
                {
                    case "quit":
                        return;
                    case "msg":
                        {
                            Server.Instance.SendMsg(Server.Instance.client, "你好我是服务端\n你能看到我们unity客户端？");
                        }
                        break;
                }
            }
        }
    }
}