using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using TMPro;

public class CusController : MonoSingleton<CusController>
{
    private Transform transform;    //整个_inv区域
    private Transform orderTrans;   //order区域的trans
    private Transform cusCellParTrans; //实例化后的父
    private Transform orderCellParTrans; //实例化后的父

    private TMP_InputField food;      
    private TMP_InputField size;
    private TMP_InputField address;

    private Button addButton;   //增加按钮
    private Button confirmButton;   //下单按钮

    private TextMeshProUGUI sumText;    //总价
    private TextMeshProUGUI outputText; //输出

    //返回按钮
    private Button returnButton;
    private string foodStr;          //输入的编号
    private string sizeStr;
    private string addressStr;
    private float sumPrice;   //计算得到的总价
    private static string outPutStr = "";

    private int cusID;  //客户的ID
    private int curOId; //当前订单的id

    private GameObject cusCellPrefab;
    private GameObject orderCellPrefab;


    //管理所有实例的_cell的对象，通过id来索引
    private List<GameObject> orderCellObjList;
    private Dictionary<int, GameObject> cusCellObjDic;

    //本地缓存的
    private Dictionary<string, Dictionary<string,Tsize>> sizeDic; //用来索引size
    private List<Tsize> curList; //存放当前Order中的s_id和对应price



    private void Awake()
    {
        //加载Prefab
        cusCellPrefab= Resources.Load<GameObject>("Prefabs/_cusCell");
        orderCellPrefab = Resources.Load<GameObject>("Prefabs/_orderCell");

        transform = GetComponent<Transform>();
        orderTrans = transform.GetChild(1);
        cusCellParTrans = GameObject.FindGameObjectWithTag("menuData").transform;
        orderCellParTrans = GameObject.FindGameObjectWithTag("orderData").transform;

        //UI绑定
        returnButton = transform.parent.Find("return[Button]").GetComponent<Button>();
        outputText = transform.parent.Find("output[T]").GetComponent<TextMeshProUGUI>();
        addButton = orderTrans.Find("_addArea").GetComponentInChildren<Button>();
        TMP_InputField[] addInputFields = orderTrans.Find("_addArea").GetComponentsInChildren<TMP_InputField>();
        food = addInputFields[0];
        size = addInputFields[1];
        //Button[] buttenArray = transform.Find("_button").GetComponentsInChildren<Button>();

        sumText = orderTrans.Find("_sum").GetComponentsInChildren<TextMeshProUGUI>()[1];

        address = orderTrans.Find("_confirmOrder").GetComponentInChildren<TMP_InputField>();
        confirmButton = orderTrans.Find("_confirmOrder").GetComponentInChildren<Button>();


        //初始化
        orderCellObjList = new();
        cusCellObjDic = new();
        sizeDic = new();
        curList = new();

        //添加到Client
        Client.ReceiveCbDic.Add(DataDisposer.Ins.select_cusMenuView, CusController.Instance.ShowCusMenu);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.insert_Order, CusController.Instance.OnOrderAdded);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.insert_List, CusController.Instance.OnListSent);
    }

    private void Start()
    {
        //按键绑定
        addButton.onClick.AddListener(() => { AddOne2Order(); });
        confirmButton.onClick.AddListener(() => { ConfirmOrder(); });
        //返回按钮绑定
        returnButton.onClick.AddListener(() => { SceneController.OnReturnButtonClik(); });
        //获取顾客ID
        cusID = Authentication.Instance.GetCurCusID();
        //获取顾客菜单视图
        GetCusMenu();
    }

    private void Update()
    {
        //实时同步输入框的内容
        foodStr = food.text;
        sizeStr = size.text;
        addressStr = address.text;
        outputText.text = outPutStr;
        sumText.text = sumPrice.ToString();
    }

    #region【获取客户菜单视图的数据】

    public void GetCusMenu()
    {
        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("s_id", "0")
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.select_cusMenuView, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.select_cusMenuView);
    }

    public void ShowCusMenu(string _str)
    {
        //1. 接收并解析
        List<VcusMenu> cusMenuList = DataDisposer.DisposeVcusMenuListFromJson2Objs(_str);

        //2. 转换为obj显示并存一份sizeDic
        //检查是否已加载资源
        if (cusCellPrefab == null)
        {
            //Print("预制体加载失败！");
            return;
        }

        //遍历cusMenuList,并通过去掉s_id的末尾得到mu_id
        //对于已存在的mu_id，则修改obj; 否则添加新的obj
        foreach (var item in cusMenuList)
        {
            int muId = item.s_id / 10;  //除以10去掉个位就是mu_id

            if (cusCellObjDic.TryGetValue(muId, out GameObject obj))   //已有mu_id
            {
              
                //存入sizeDic的子字典
                sizeDic[item.mu_food].Add(item.s_size, new(item.s_id, muId, item.s_size, item.s_price));

                //只用修改size和price部分的显示
                Transform sizeTrans = obj.transform.GetChild(1);
                Transform priceTrans = obj.transform.GetChild(2);

                sizeTrans.GetComponentInChildren<TextMeshProUGUI>().text += item.s_size + "/";
                priceTrans.GetComponentInChildren<TextMeshProUGUI>().text += item.s_price.ToString() + "/";
            }
            else  //新的mu_id
            {
                //sizeDic新建一项
                sizeDic.Add(item.mu_food, new Dictionary<string, Tsize>());
                //存入sizeDic的子字典
                sizeDic[item.mu_food].Add(item.s_size, new(item.s_id, muId, item.s_size, item.s_price));

                GameObject newCellObj = Instantiate(cusCellPrefab);
                newCellObj.transform.parent = cusCellParTrans;
                Transform foodTrans = newCellObj.transform.GetChild(0);
                Transform sizeTrans = newCellObj.transform.GetChild(1);
                Transform priceTrans = newCellObj.transform.GetChild(2);
                Transform descTrans = newCellObj.transform.GetChild(3);

                foodTrans.GetComponentInChildren<TextMeshProUGUI>().text = item.mu_food;
                foodTrans.gameObject.name = $"[food]";
                sizeTrans.GetComponentInChildren<TextMeshProUGUI>().text = item.s_size + "/";
                sizeTrans.gameObject.name = $"[size]";
                priceTrans.GetComponentInChildren<TextMeshProUGUI>().text = item.s_price.ToString() + "/";
                priceTrans.gameObject.name = $"[price]";
                descTrans.GetComponentInChildren<TextMeshProUGUI>().text = item.mu_desc;
                descTrans.gameObject.name = $"[desc]";

                cusCellObjDic.Add(muId, newCellObj);//存入字典
            }
        }
    }

    #endregion

    #region【添加食物到订单中】
    private void AddOne2Order()
    {
        if (!CheckInputField(true, true)) return;
        //先检查输入的food是否存在
        if(sizeDic.ContainsKey(foodStr))
        {
            //添加到curList中
            Tsize tarSize = sizeDic[foodStr][sizeStr];
            curList.Add(tarSize);

            //实例化obj来显示
            GameObject newCell = Instantiate(orderCellPrefab);
            newCell.transform.parent = orderCellParTrans;

            Transform foodTrans = newCell.transform.GetChild(0);
            Transform sizeTrans = newCell.transform.GetChild(1);
            Button deleteButton = newCell.transform.GetComponentInChildren<Button>();

            foodTrans.GetComponentInChildren<TextMeshProUGUI>().text = foodStr;
            foodTrans.gameObject.name = $"[food]";
            sizeTrans.GetComponentInChildren<TextMeshProUGUI>().text = sizeStr;
            sizeTrans.gameObject.name = tarSize.id.ToString();
            //动态绑定按钮
            deleteButton.onClick.AddListener(() => OnDeleteButtonClick());
            orderCellObjList.Add(newCell);
            sumPrice += tarSize.price; //更新总价
            ResetInputField();
        }
        else
        {
            Print($"{foodStr}不存在！");
        }
    }

    /// <summary>
    /// 订单中的删除按钮按下后调用：负责删除自身所在cell
    /// </summary>
    private void OnDeleteButtonClick()
    {
        GameObject cellObj = EventSystem.current.currentSelectedGameObject.transform.parent.gameObject;
        int sId = Convert.ToInt32(cellObj.transform.GetChild(1).gameObject.name);
        
        foreach(var item in curList)//从当前List中移除
        {
            if (item.id == sId)
            {
                curList.Remove(item);
                sumPrice -= item.price; //更新总价
                break;
            }  
        }
        orderCellObjList.Remove(cellObj);   //从字典中移除
        //删除父物体（就是当前cell）
        Destroy(cellObj);
    }

    #endregion

    # region 【确认订单】
    
    public void ConfirmOrder()
    {
        //检查order和地址是否为空
        if (orderCellObjList.Count <= 0)
        {
            Print("请先添加食品！"); return;
        }
        if (!CheckInputField(false, false, true)) return;

        //获取系统时间作为订单的ID
        DateTime nowTime = DateTime.Now;
        string o_id = nowTime.Month.ToString() + nowTime.Day.ToString() + nowTime.Hour.ToString() + nowTime.Minute.ToString() + nowTime.Second.ToString();
        curOId = Convert.ToInt32(o_id);
        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("o_id", o_id),
            DataDisposer.ConvertStrProperty2Json("o_cus_id", cusID.ToString()),
            DataDisposer.ConvertStrProperty2Json("o_price", sumPrice.ToString()),
            DataDisposer.ConvertStrProperty2Json("o_address", addressStr)
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.insert_Order, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.insert_Order);
    }

    public void OnOrderAdded(string _str)
    {
        if (_str == "1") //成功
        {
            
            Print($"下单成功！");
            ResetInputField();
            //将详细的List发给数据库
            SendAllList();
        }
        else //失败
        {
            Print($"下单失败！");
        }
    }
    #endregion

    #region 【将详细的List发给数据库】
    
    public void SendAllList()
    {
        //处理List数据,将每一项用字符/分开后整合到两个很长的字符串中，在服务器做切割
        string long_o_id = ""; //每一项由“id+/”组成
        string long_s_id = "";
        foreach (var item in curList)
        {
            long_o_id += $"{curOId}/";
            long_s_id += $"{item.id}/";
        }
        Debug.Log($"[long_o_id]{long_o_id};[long_s_id]{long_s_id}");
        //封装并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("l_o_id", long_o_id),
            DataDisposer.ConvertStrProperty2Json("l_s_id", long_s_id)
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.insert_List, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.insert_List);
    }

    public void OnListSent(string _str)
    {
        if (_str != "0") //成功
        {
            Debug.Log($"全部List信息发送成功！");
        }
        else //失败
        {
            Debug.Log($"全部List信息发送失败！");
        }
    }

    #endregion

    private bool CheckInputField(bool _checkFood = false, bool _checkSize = false, bool _checkAddress = false)
    {
        //输入检测
        if (_checkFood && foodStr == "")
        {
            Print("请输入食物！");
            return false;
        }
        if (_checkSize && sizeStr == "")
        {
            Print("请输入规格！");
            return false;
        }
        if (_checkAddress && addressStr == "")
        {
            Print("请输入收货地址！");
            return false;
        }
        return true;
    }

    private void ResetInputField()
    {
        foodStr = addressStr = sizeStr = "";
        food.text = size.text = address.text = "";
    }

    public static void Print(string _str)
    {
        outPutStr = _str;
    }
}

