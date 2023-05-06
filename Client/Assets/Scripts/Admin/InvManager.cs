using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using TMPro;

public class InvManager : MonoSingleton<InvManager>
{
    private Transform transform;    //整个_inv区域
    private Transform dataParentTransform; //实例化后id和remain的父

    private TMP_InputField id;      //id输入区域
    private TMP_InputField remain;  //remain输入区域

    private TextMeshProUGUI output;    //输出
    private Button updateButton;   //更新库存按钮
    //[SerializeField] private Button quit;   //退出按钮
    private string idStr;       //输入的编号
    private string remainStr;   //输入的库存

    //用于显示的id和remain的prefab
    private GameObject invDataCellPrefab;

    //本地存放一份所有inventory的副本invArray，这样修改数据后就不用全部重新获取了
    [SerializeField] private Dictionary<int,int> invDic;

    //管理所有实例的remain的对象，通过id来索引从而快速修改remain的显示
    private Dictionary<int, GameObject> remainCellDic;

    //当前在输入框输入的Inv
    private Tinventory curInv;


    private void Awake()
    {
        transform = GetComponent<Transform>();
        dataParentTransform = GameObject.FindGameObjectWithTag("invData").transform;
        //UI绑定
        TMP_InputField[] inputFields = transform.Find("_inputField").GetComponentsInChildren<TMP_InputField>();
        id = inputFields[0];
        remain = inputFields[1];

        updateButton = transform.Find("_button").GetComponentInChildren<Button>();

        //加载Prefab
        invDataCellPrefab = Resources.Load<GameObject>("Prefabs/invDataCell");

        //初始化
        invDic = new();
        remainCellDic = new();
    }

    private void Start()
    {

        //初始化
        idStr = remainStr = "";
        curInv = new();
        //字典索引添加
        Client.ReceiveCbDic.Add(DataDisposer.Ins.select_All_Inventory, InvManager.Instance.ShowAllInvData);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.update_Inventory, InvManager.Instance.ShowOneInvData);

        //按钮绑定
        updateButton.onClick.AddListener(() => { UpdateOneInvData(); });
    }

    private void Update()
    {
        //实时同步输入框的内容
        idStr = id.text;
        remainStr = remain.text;
    }

    /// <summary>
    /// 修改某个食品的库存
    /// </summary>
    public void UpdateOneInvData()
    {
        if (!CheckInputField(true, true)) return;
        //缓存id和ps
        curInv.id = Convert.ToInt16(idStr);
        curInv.remain = Convert.ToInt16(remainStr);

        //封装id并打包成Ins指令
        JArray Jproperty =
        DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("i_mu_id", idStr),
            DataDisposer.ConvertStrProperty2Json("i_remain", remainStr)});

        //JArray Jtable = DataDisposer.CombineTables2Json(new string[] { "inventory" });

        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.update_Inventory, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.update_Inventory);

    }

    /// <summary>
    /// 获取所有Inv信息，并通过UI显示到界面上
    /// </summary>
    public void GetAllInvData()
    {
        //JArray Jtable = DataDisposer.CombineTables2Json(new string[] { "inventory" });

        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("i_mu_id", "0")
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.select_All_Inventory, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.select_All_Inventory);
    }

    public void ShowAllInvData(string _dataStr)
    {
//确保Inv已经更新好了
MenuManager.Instance.GetMenuData();

        //AdminController.Print("接下来调用DataDisposer的方法解析.....");
        //调用DataDisposer的方法解析
        List<Tinventory>invList = DataDisposer.DisposeTinventoryListFromJson2Objs(_dataStr);
        //转成Dic本地存储
        foreach(var item in invList)
        {
            invDic.Add(item.id, item.remain);
        }

        //检查是否已加载资源
        if(invDataCellPrefab == null)
        {
            //AdminController.Print("预制体加载失败！");
            return;
        }

        //实例化预制体来装载Inventory数据
        foreach (var item in invList)
        {
            string id, remain;
            //实例化cell来显示id
            GameObject newId = Instantiate(invDataCellPrefab);
            newId.transform.parent = dataParentTransform;
            id = item.id.ToString();
            newId.name = $"[id]{id}";
            newId.GetComponentInChildren<TextMeshProUGUI>().text = id;

            GameObject newRemain = Instantiate(invDataCellPrefab);
            newRemain.transform.parent = dataParentTransform;
            remain = item.remain.ToString();
            newRemain.name = $"[remain]{remain}";
            newRemain.GetComponentInChildren<TextMeshProUGUI>().text = remain;
            remainCellDic.Add(item.id, newRemain);  //remain要添加到Dic中方便后续修改
        }


    }

    public void ShowOneInvData(string _str)
    {
        //str为1表示更改成功，为0表示失败
        if(_str == "1") //成功
        {
            //有两种情况，判断字典中是否有来分情况处理
            if(remainCellDic.ContainsKey(curInv.id))
            {
                //1. 在初始化获取时就有的id，则找到旧的直接修改
                GameObject tarCell = remainCellDic[curInv.id];
                string newRemainStr = curInv.remain.ToString();
                tarCell.GetComponentInChildren<TextMeshProUGUI>().text = newRemainStr;
                tarCell.name = $"[remain]{newRemainStr}";
                //修改本地invDic中的数据
                invDic[curInv.id] = curInv.remain;
            }
            else
            {
                //2. 通过界面新添加的id，则新增

                GameObject newId = Instantiate(invDataCellPrefab);
                newId.transform.parent = dataParentTransform;
                newId.name = $"[id]{curInv.id}";
                newId.GetComponentInChildren<TextMeshProUGUI>().text = curInv.id.ToString();

                GameObject newRemain = Instantiate(invDataCellPrefab);
                newRemain.transform.parent = dataParentTransform;
                newRemain.name = $"[remain]{curInv.remain}";
                newRemain.GetComponentInChildren<TextMeshProUGUI>().text = curInv.remain.ToString();
                remainCellDic.Add(curInv.id, newRemain);  //remain要添加到Dic中方便后续修改
                //在本地invDic中新增该数据
                invDic.Add(curInv.id, curInv.remain);
            }

            AdminController.Print($"{curInv.id}的库存信息更新成功！");
            ResetInputField();
        }
        else //失败
        {
            //输出失败
            AdminController.Print($"未找到id为{curInv.id}食品！");
            ResetInputField();
        }
    }

    private bool CheckInputField(bool _checkId, bool _checkRemain)
    {
        //输入检测
        if (_checkId)
        {
            if (idStr == "")
            {
                AdminController.Print("请输入编号！");
                ResetInputField();
                return false;
            }
            else if (!AdminController.CheckIDType(idStr))
            {
                AdminController.Print("请输入4位的ID！");
                ResetInputField();
                return false;
            }
        }
        else if (_checkRemain && remainStr == "")
        {
            AdminController.Print("请输入库存！");
            ResetInputField();
            return false;
        }
        return true;
    }

    /// <summary>
    /// 给MenuManager调用的方法，负责在删除menu后同时更新inv的显示
    /// </summary>
    public void UnShowOneInvData(int _tarMuId)
    {
         //从本地invDic中删除
        if (invDic.ContainsKey(_tarMuId))
        {
            invDic.Remove(_tarMuId);
            //删除对应的显示remaincell和id
            GameObject tarObj = remainCellDic[_tarMuId];
            remainCellDic.Remove(_tarMuId);
            GameObject.Destroy(tarObj);
            GameObject.Destroy(dataParentTransform.Find($"[id]{_tarMuId}").gameObject);
            //AdminController.Print($"级联删除{_tarMuId}的inv数据成功！");
        }
    }

    private void ResetInputField()
    {
        idStr = remainStr= "";
        id.text = remain.text = "";
    }
}
