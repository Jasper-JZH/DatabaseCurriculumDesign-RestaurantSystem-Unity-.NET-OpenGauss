using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>
/// 实现注册、登录等逻辑
/// </summary>
public class Authentication : MonoSingleton<Authentication>
{
    //public Client client;
    public enum Result
    {
        idNull,             //id不存在
        passwordMis,        //密码错误
        loginAsAdmin,       //管理员身份验证成功
        loginAsCustomer,    //顾客身份验证成功
        signAsCustomer,      //顾客注册成功
        signFailAsIdUsed     //顾客注册失败，ID已被使用
    }

    //声明事件的委托
    public delegate void LoginHandler(Result _authenticateResult);
    //声明事件（事件event可以看作一个限制，限制委托的实例只能在本类中调用（invoke等））
    public static event LoginHandler AdminLoginEvent;
    public static event LoginHandler CusLoginEvent;
    public static event LoginHandler CusSignEvent;

    public Tadmin curAdmin;
    public Tcustomer curCustomer;

    public int GetCurCusID()
    {
        return curCustomer.id;
    }

    private void Awake()
    {
        //ReceiveCbDic索引添加
        if(!Client.ReceiveCbDic.ContainsKey(DataDisposer.Ins.select_Login_Admin))
            Client.ReceiveCbDic.Add(DataDisposer.Ins.select_Login_Admin, Authentication.Instance.AuthenticateAdmin);
        
        if (!Client.ReceiveCbDic.ContainsKey(DataDisposer.Ins.select_Login_Cus))
            Client.ReceiveCbDic.Add(DataDisposer.Ins.select_Login_Cus, Authentication.Instance.AuthenticateCus);
        
        if (!Client.ReceiveCbDic.ContainsKey(DataDisposer.Ins.insert_Sign_Cus))
            Client.ReceiveCbDic.Add(DataDisposer.Ins.insert_Sign_Cus, Authentication.Instance.AuthenticateCusSign);
    }

    private void Start()
    {
        curAdmin = new();
        curCustomer = new();
       
    }

    /// <summary>
    /// 管理员登录
    /// </summary>
    /// <returns></returns>
    public void AdminLogin(string _id, string _phone)
    {
        //缓存id和ps
        curAdmin.id = Convert.ToInt16(_id);
        curAdmin.phone = _phone;

        //封装id并打包成Ins指令
        JArray Jproperty = 
        DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("a_id", _id)});

        //JArray Jtable = DataDisposer.CombineTables2Json(new string[] { "admin" });

        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.select_Login_Admin,Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.select_Login_Admin);
    }

    /// <summary>
    /// 顾客登录
    /// </summary>
    /// <param name="_id"></param>
    /// <param name="_phone"></param>
    public void CusLogin(string _id, string _phone)
    {
        //缓存id和ps
        curCustomer.id = Convert.ToInt16(_id);
        curCustomer.phone = _phone;

        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("cus_id", _id)});

        //JArray Jtable = DataDisposer.CombineTables2Json(new string[] { "customer" });

        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.select_Login_Cus, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.select_Login_Cus);
    }

    /// <summary>
    /// 顾客注册
    /// </summary>
    /// <param name="_id"></param>
    /// <param name="_name"></param>
    /// <param name="_phone"></param>
    public void CusSign(string _id, string _name, string _phone)
    {
        //缓存id和ps
        curCustomer.id = Convert.ToInt16(_id);
        curCustomer.name = _name;
        curCustomer.phone = _phone;

        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("cus_id", _id),
            DataDisposer.ConvertStrProperty2Json("cus_name", _name),
            DataDisposer.ConvertStrProperty2Json("cus_phone", _phone)
         });

        //JArray Jtable = DataDisposer.CombineTables2Json(new string[] { "customer" });

        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.insert_Sign_Cus, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.insert_Sign_Cus);
    }

    /// <summary>
    /// 验证Admin的身份（密码）
    /// </summary>
    public void AuthenticateAdmin(string _adminStr)
    {
        //调用DataDisposer的方法解析
        Tadmin newAdmin = DataDisposer.DisposeTadminFromJson2Objs(_adminStr);
        
        //判断是否为空
        if(newAdmin.id == 0)    //a_id不存在
        {
            AdminLoginEvent?.Invoke(Result.idNull);
        }
        else
        {
            //比较newAdmin和缓存的curAdmin的phone是否相同,并将比较的结果通知给AdminLoginHandlerEvent
            //的订阅者（例如Menu，实现UI的显示）
            Result result = curAdmin.phone.Equals(newAdmin.phone) == true ? Result.loginAsAdmin : Result.passwordMis; 
            AdminLoginEvent?.Invoke(result);
        }
    }

    /// <summary>
    /// 验证Customer的身份（密码）
    /// </summary>
    public void AuthenticateCus(string _cusStr)
    {
        //调用DataDisposer的方法解析
        Tcustomer newCus = DataDisposer.DisposeTcustomerFromJson2Objs(_cusStr);

        //判断是否为空
        if (newCus.id == 0)    //cus_id不存在
        {
            Debug.Log("cus_id不存在");
            CusLoginEvent?.Invoke(Result.idNull);
        }
        else
        {
            Result result = curCustomer.phone.Equals(newCus.phone) == true ? Result.loginAsCustomer : Result.passwordMis;
            CusLoginEvent?.Invoke(result);
        }
    }

    /// <summary>
    /// Customer注册验证
    /// </summary>
    /// <param name="_cusStr"></param>
    public void AuthenticateCusSign(string _result)
    {
        Result result = _result == "1" ? Result.signAsCustomer : Result.signFailAsIdUsed;
        CusSignEvent?.Invoke(result);
    }
}
