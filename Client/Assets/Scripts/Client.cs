using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoSingleton<Client>
{
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 1234;
    [SerializeField] private Socket socket;

    const int BUFFER_SIZE = 1024 * 6;
    private byte[] readBuff = new byte[BUFFER_SIZE];

    [SerializeField] private string recvStr = "";

    //private bool isConnect = false;

    private DataDisposer.Ins curIns;    //表示当前正在处理的Ins

    /// <summary>
    /// 存放不同类型的指令对应的回调函数,在各个模块初始化后把要用的方法添加进来
    /// </summary>
    public static Dictionary<DataDisposer.Ins, Action<string>> ReceiveCbDic = new();


    private void Awake()
    {
        //StartCoroutine(TryConnect(3f));       //TODO :待完善：使用BeginConnect循环尝试连接
        Connect();
    }


    public void DisConnect()
    {
        socket?.Disconnect(true);
    }

  /*  IEnumerator TryConnect(float _reTryTimeLag)
    {
        //初始化socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        do
        {
            //Connect();
            //连接服务器
            socket.BeginConnect()
            socket.Connect(host, port);
            Debug.Log($"客户端地址：{socket.LocalEndPoint}");
            yield return new WaitForSeconds(_reTryTimeLag);
            Debug.Log("尝试连接！");
        } while (!socket.Connected);

        //开始接收
        socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
    }*/

    public void Connect()
    {
        //初始化socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //连接服务器
        socket.Connect(host, port);
        Debug.Log($"客户端地址：{socket.LocalEndPoint.ToString()}");
    }

    private string GetReceiveStr(int _count)
    {
        string recvStr = System.Text.Encoding.UTF8.GetString(readBuff, 0, _count);
        Debug.Log($"【接收到服务器{_count}个字节的数据！】{recvStr}");
        return recvStr;
    }
    /*
        private void ReceiveCb(IAsyncResult ar)
        {
            try
            {
                GetReceiveStr(socket.EndReceive(ar));
                //循环调用继续接收
                socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
            }
            catch(Exception e)
            {
                Debug.Log($"ReceiveCb异常:{e.Message}");
                socket.Close();
            }
        }
    */

    public void SendMsg(string _msg)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(_msg);
        try
        {
            socket.Send(bytes);
        }
        catch(Exception e)
        {
            Debug.Log($"SendMsg异常:{e.Message}");
        }
    }

    public void ReceiveAndDispose(DataDisposer.Ins _ins)
    {
        curIns = _ins;
        //根据_ins从dic中选择使用不同的RecieveCb
        socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
    }

    /// <summary>
    /// 通用的接收回调函数，通过Ins确定要放在主线程中执行的方法
    /// </summary>
    /// <param name="ar"></param>
    private void ReceiveCb(IAsyncResult ar)
    {
        //RCB只负责接收str并调用对应其它模块的处理方法
        try
        {
            string str = GetReceiveStr(socket.EndReceive(ar));
            //从字典中选取对应的方法，放入主线程中调用
            Loom.QueueOnMainThread((param) => { ReceiveCbDic[curIns]?.Invoke(str); }, null);
        }
        catch (Exception e)
        {
            Debug.Log($"ReceiveCb异常:{e.Message}");
            socket.Close();
        }
    }
}
