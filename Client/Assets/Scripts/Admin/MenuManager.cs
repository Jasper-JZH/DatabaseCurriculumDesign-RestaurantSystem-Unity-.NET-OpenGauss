using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using TMPro;

public class MenuManager : MonoSingleton<MenuManager>
{
    private Transform transform;    //整个_inv区域
    private Transform cellParentTransform; //实例化后的父

    private TMP_InputField id;      //id输入区域
    private TMP_InputField size;    
    private TMP_InputField price;    
    private TMP_InputField food;    //food输入区域
    private TMP_InputField desc;    //desc输入区域

    private Button addButton;   //增加按钮
    private Button deleteButton;
    private Button updateButton;
    //[SerializeField] private Button quit;   //退出按钮
    private string idStr;          //输入的编号
    private string sizeStr;        
    private string priceStr;       
    private string foodStr;        //输入的食物
    private string descStr;        //输入的简介


    private GameObject cellPrefab;

    //本地存放一份表的数据
    private List<Tmenu> menuList;
    private List<Tsize> sizeList;
    private Dictionary<int, MenuCell> menuDic;

    public class MenuCell
    {
        public string food;
        public Dictionary<string, float> size_price_dic;   //规格和对应的价格
        public string desc;

        public MenuCell()
        {
            food = desc = "";
            size_price_dic = new();
        }

        public MenuCell(string _food, string _desc, Dictionary<string,float> _size_price_dic)
        {
            food = _food;
            desc = _desc;
            size_price_dic = new(_size_price_dic);
        }

        public MenuCell(MenuCell _menuCell)
        {
            food = _menuCell.food;
            desc = _menuCell.food;
            size_price_dic = new(_menuCell.size_price_dic); //值拷贝而不是地址拷贝
        }
    }

    //管理所有实例的_cell的对象，通过id来索引
    private Dictionary<int, GameObject> menuCellObjDic;

    //当前处理的
    private Tmenu curMenu;
    private Tsize curSize;

    //当前输入框内输入的MenuCell,若未输入则使用原来的值
    private MenuCell curMenuCell;


    private void Awake()
    {
        transform = GetComponent<Transform>();
        cellParentTransform = GameObject.FindGameObjectWithTag("menuData").transform;
        //UI绑定
        TMP_InputField[] inputFields = transform.Find("_inputField").GetComponentsInChildren<TMP_InputField>();
        id = inputFields[0];
        food = inputFields[1];
        size = inputFields[2];
        price = inputFields[3];
        desc = inputFields[4];

        Button[] buttenArray = transform.Find("_button").GetComponentsInChildren<Button>();
        addButton = buttenArray[0];
        deleteButton = buttenArray[1];
        updateButton = buttenArray[2];

        //加载Prefab
        cellPrefab = Resources.Load<GameObject>("Prefabs/_cell");

        //初始化
        menuList = new();
        sizeList = new();

        menuCellObjDic = new();
        menuDic = new();

        //字典索引添加
        Client.ReceiveCbDic.Add(DataDisposer.Ins.select_All_Menu, MenuManager.Instance.SetMenuData);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.insert_Menu, MenuManager.Instance.SetAndShowNewFoodData);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.delete_Menu, MenuManager.Instance.UnShowOneFoodData);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.update_Menu, MenuManager.Instance.UpdateOneFoodDataShow);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.update_Size, MenuManager.Instance.UpdateOneFoodSizePriceShow);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.select_All_size, MenuManager.Instance.SetSizeData);
        Client.ReceiveCbDic.Add(DataDisposer.Ins.insert_Size, MenuManager.Instance.SetAndShowNewPriceSize);

    }

    private void Start()
    {
        //初始化
        idStr = foodStr = descStr = "";

        //按钮绑定
        addButton.onClick.AddListener(() => { CheckAddType(); });
        deleteButton.onClick.AddListener(() => { DeleteOneFoodData(); });
        updateButton.onClick.AddListener(() => { CheckUpdateType(); });
    }

    private void Update()
    {
        //实时同步输入框的内容
        idStr = id.text;
        foodStr = food.text;
        descStr = desc.text;
        sizeStr = size.text;
        priceStr = price.text;
    }

    #region 【获取所有食品的数据】

    /// <summary>
    /// 获取整个Menu的所有信息
    /// </summary>
    public void GetMenuData()
    {
        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("mu_id", "0")
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.select_All_Menu, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.select_All_Menu);
    }

    public void SetMenuData(string _dataStr)
    {
        //只缓存本地menu
        menuList = DataDisposer.DisposeTmenuListFromJson2Objs(_dataStr);

        //确定menu接收完毕后才发送size的请求
        GetSizeData();
    }

    /// <summary>
    /// 获取整个Size的所有信息
    /// </summary>
    public void GetSizeData()
    {
        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("s_id", "0")
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.select_All_size, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.select_All_size);
    }

    public void SetSizeData(string _dataStr)
    {
        //只缓存本地size
        sizeList = DataDisposer.DisposeTsizeListFromJson2Objs(_dataStr);

        //确定size接收完毕后才
        SetMenuCell();
        ShowMenuCell();
    }

    /// <summary>
    /// 根据装配的menu和size的list装配menucell
    /// </summary>
    public void SetMenuCell()
    {
        //遍历sizelist来装配
        foreach(var item in sizeList)
        {
            //先尝试从Dic中找，如果找的到就直接修改，找不到就加一个新的到Dic中
            int curMuid = item.mu_id;
            if (menuDic.TryGetValue(curMuid, out MenuCell cell)) //已经有了，直接改
            {
                cell.size_price_dic.Add(item.size,item.price);
            }
            else 
            {
                MenuCell newCell = new();
                //在menuList中找到对应的food，desc信息
                foreach(var menu in menuList)
                {
                    if(menu.id == item. mu_id)
                    {
                        newCell.food = menu.food;
                        newCell.desc = menu.description;
                        break;
                    }
                }
                //装配其它信息
                newCell.size_price_dic.Add(item.size, item.price);
                menuDic.Add(item.mu_id, newCell);  //装入menu字典中本地存储
            }
        }
    }

    /// <summary>
    /// 显示所有menucell
    /// </summary>
    public void ShowMenuCell()
    {
        //检查是否已加载资源
        if (cellPrefab == null) return;
        //遍历MenuDic来装配MenuObjectDic
        foreach (var menuCell in menuDic)
        {
            GameObject newCellObj = Instantiate(cellPrefab);
            newCellObj.transform.parent = cellParentTransform;
            Transform idTrans = newCellObj.transform.GetChild(0);
            Transform foodTrans = newCellObj.transform.GetChild(1);
            Transform sizeTrans = newCellObj.transform.GetChild(2);
            Transform priceTrans = newCellObj.transform.GetChild(3);
            Transform descTrans = newCellObj.transform.GetChild(4);
            idTrans.GetComponentInChildren<TextMeshProUGUI>().text = menuCell.Key.ToString();
            idTrans.gameObject.name = $"[id]";
            foodTrans.GetComponentInChildren<TextMeshProUGUI>().text = menuCell.Value.food;
            foodTrans.gameObject.name = $"[food]";

            //处理size的显示
            string sizes = "", prices = "";
            foreach(var item in menuCell.Value.size_price_dic)
            {
                sizes += $"{item.Key}/";
                prices += $"{item.Value}/";
            }
            sizeTrans.GetComponentInChildren<TextMeshProUGUI>().text = sizes;
            sizeTrans.gameObject.name = $"[size]";
            priceTrans.GetComponentInChildren<TextMeshProUGUI>().text = prices;
            priceTrans.gameObject.name = $"[price]";
            descTrans.GetComponentInChildren<TextMeshProUGUI>().text = menuCell.Value.desc;
            descTrans.gameObject.name = $"[desc]";

            //放到MenuCellObjDic中管理
            menuCellObjDic.Add(menuCell.Key, newCellObj);
        }
    }

    #endregion

    #region 【添加一个食品的数据】

    /// <summary>
    /// 根据输入框的输入情况判断添加的类型
    /// </summary>
    public void CheckAddType()
    {
        //两种可能

        //根据InputField的输入情况来区分
        if(foodStr == "" && descStr == "")  //已有ID，添加新的size和price（需要id,size,price）
        {
            AddOneFoodSizePrice();
        }
        else  //全新的food(新的id)（不输入size和price）
        {
            AddOneFoodData();
        }
    }

    /// <summary>
    /// 添加一个食物的到Menu中
    /// </summary>
    public void AddOneFoodData()
    {
        if (!CheckInputField(true, false, false, true, true)) return;
        //缓存当前输入的menuCell
        Dictionary<string, float> new_size_price_Dic = new();   //new一个空的dic
        curMenuCell = new(foodStr, descStr, new_size_price_Dic);
        //封装并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("mu_id", idStr),
            DataDisposer.ConvertStrProperty2Json("mu_food", foodStr),
            DataDisposer.ConvertStrProperty2Json("mu_description", descStr)
         });
        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.insert_Menu, Jproperty);
        //发送
        Client.Instance.SendMsg(jsonInstruction);
        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.insert_Menu);
    }

    /// <summary>
    /// 添加一种已有食物的新的size和price
    /// </summary>
    public void AddOneFoodSizePrice()
    {
        if (!CheckInputField(true, true, true)) return;
        
        //找到对应的Menu，如果找不到说明没有先创建foodData，自然不能设置sizeprice,返回失败
        if(!menuDic.TryGetValue(Convert.ToInt16(idStr),out MenuCell cell))
        {
            //没有对应ID的menu
            AdminController.Print($"找不到ID为{idStr}的食物！");
            return;
        }

        //能找到对应menu,则更新封装的menucell
        cell.size_price_dic.Add(sizeStr, (float)Convert.ToDouble(priceStr));
        curMenuCell = new(cell);
        int s_id = GetSizeID(idStr, sizeStr);

        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("s_id", s_id.ToString()),
            DataDisposer.ConvertStrProperty2Json("s_mu_id", idStr),
            DataDisposer.ConvertStrProperty2Json("s_size", sizeStr),
            DataDisposer.ConvertStrProperty2Json("s_price", priceStr)
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.insert_Size, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.insert_Size);
    }

    /// <summary>
    /// 添加新的Food并显示
    /// </summary>
    /// <param name="_str"></param>
    public void SetAndShowNewFoodData(string _str)
    {
        //str为2表示更改成功(因为有两个insert，影响2行)，为0表示失败
        if (_str == "2") //成功
        {
            int newId = Convert.ToInt16(idStr);
            //需要将curMenuCell放入menuCellList
            menuDic.Add(newId, curMenuCell);

            GameObject newCellObj = Instantiate(cellPrefab);
            newCellObj.transform.parent = cellParentTransform;

            Transform idTrans = newCellObj.transform.GetChild(0);
            Transform foodTrans = newCellObj.transform.GetChild(1);
            Transform sizeTrans = newCellObj.transform.GetChild(2);
            Transform priceTrans = newCellObj.transform.GetChild(3);
            Transform descTrans = newCellObj.transform.GetChild(4);

            idTrans.GetComponentInChildren<TextMeshProUGUI>().text = idStr;
            idTrans.gameObject.name = $"[id]";
            foodTrans.GetComponentInChildren<TextMeshProUGUI>().text = curMenuCell.food;
            foodTrans.gameObject.name = $"[food]";

            //size和price此时为空
            sizeTrans.GetComponentInChildren<TextMeshProUGUI>().text = "";
            sizeTrans.gameObject.name = $"[size]";
            priceTrans.GetComponentInChildren<TextMeshProUGUI>().text = "";
            priceTrans.gameObject.name = $"[price]";
            descTrans.GetComponentInChildren<TextMeshProUGUI>().text = curMenuCell.desc;
            descTrans.gameObject.name = $"[desc]";

            //obj放到MenuCellObjDic中管理
            menuCellObjDic.Add(newId, newCellObj);

            AdminController.Print($"编号为{idStr}的食品添加成功！");
            ResetInputField();
        }
        else //失败
        {
            //输出失败
            AdminController.Print($"id为{idStr}的食品已存在！");
            ResetInputField();
        }
    }

    public void SetAndShowNewPriceSize(string _str)
    {
        //str为1表示更改成功，为0表示失败
        if (_str == "1") //成功
        {
            int muId = Convert.ToInt16(idStr);
            //更新显示
            GameObject tarCellObj = menuCellObjDic[muId];
            Transform sizeTrans = tarCellObj.transform.GetChild(2);
            Transform priceTrans = tarCellObj.transform.GetChild(3);

            string sizes = "", prices = "";
            foreach (var item in curMenuCell.size_price_dic)
            {
                sizes += $"{item.Key}/";
                prices += $"{item.Value}/";
            }
            sizeTrans.GetComponentInChildren<TextMeshProUGUI>().text = sizes;
            priceTrans.GetComponentInChildren<TextMeshProUGUI>().text = prices;

            //更新menuCellDic中的数据
            menuDic[muId] = curMenuCell;

            AdminController.Print($"添加新的规格成功！");
            ResetInputField();
        }
        else //失败
        {
            //输出失败
            AdminController.Print($"添加新的规格失败！");
            ResetInputField();
        }
    }


    #endregion

    #region 【修改一个食品的数据】

    /// <summary>
    /// 根据输入的情况判断需要更新哪个表格（menu/size/menu+size）
    /// </summary>
    public void CheckUpdateType()
    {
        if (!CheckInputField(true)) return;        //至少有id
        //先进行id的check
        if (menuDic.TryGetValue(Convert.ToInt16(idStr), out MenuCell tarCell))
        {
            //尝试用有输入的数据修改本地数据
            if (foodStr != "" || descStr != "")//判断是否需要更新menu表
            {
                Tmenu tarMenu = new(Convert.ToInt16(idStr), tarCell.food, tarCell.desc);
                if (foodStr != "") tarMenu.food = foodStr;
                if (descStr != "") tarMenu.description = descStr;
                UpdateOneFoodData(tarMenu);
            }

            //判断是否需要更新size表
            if (sizeStr != "")
            {
                if (!CheckInputField(true, true, true)) return; //确保同时有size和price
                int sizeId = GetSizeID(idStr, sizeStr);
                Tsize tarSize = new(sizeId, Convert.ToInt16(idStr), sizeStr, (float)Convert.ToDouble(priceStr));
                UpdateOneFoodSizePrice(tarSize);
            }
        }
    }

    /// <summary>
    /// 修改一个Menu中的食物信息
    /// </summary>
    public void UpdateOneFoodData(Tmenu tarMenu)
    {
        curMenu = tarMenu;    //缓存当前输入的menu
        //封装id并打包成Ins指令
        JArray Jproperty =
        DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("mu_id", idStr),
            DataDisposer.ConvertStrProperty2Json("mu_food", curMenu.food),
            DataDisposer.ConvertStrProperty2Json("mu_description", curMenu.description),
        });

        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.update_Menu, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.update_Menu);
    }

    /// <summary>
    /// 修改一个食物的规格信息
    /// </summary>
    public void UpdateOneFoodSizePrice(Tsize _tarSize)
    {
        curSize = _tarSize;
        JArray Jproperty =
        DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("s_id", curSize.id.ToString()),
            DataDisposer.ConvertStrProperty2Json("s_price", curSize.price.ToString()),
            DataDisposer.ConvertStrProperty2Json("s_size", curSize.size),
        });

        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.update_Size, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.update_Size);
    }

    /// <summary>
    /// 更新一个已有食物的数据的显示
    /// </summary>
    /// <param name="_str"></param>
    public void UpdateOneFoodDataShow(string _str)
    {
        //str为1表示更改成功，为0表示失败
        if (_str == "1") //成功
        {
            //修改本地数据menulist和menucelldic
            foreach (var item in menuList)
            {
                if (item.id == curMenu.id)
                {
                    item.food = curMenu.food;
                    item.description = curMenu.description;
                    break;
                }
            }
            menuDic[curMenu.id].food = curMenu.food;
            menuDic[curMenu.id].desc = curMenu.description;

            //修改显示（menucellObjDic）
            GameObject tarMenuCell = menuCellObjDic[curMenu.id];
            Transform foodTrans = tarMenuCell.transform.GetChild(1);
            Transform descTrans = tarMenuCell.transform.GetChild(4);
            foodTrans.GetComponentInChildren<TextMeshProUGUI>().text = curMenu.food;
            descTrans.GetComponentInChildren<TextMeshProUGUI>().text = curMenu.description;

            AdminController.Print($"编号为{curMenu.id}的食品信息更新成功！");
            ResetInputField();

        }
        else //失败
        {
            AdminController.Print($"未找到id为{curMenu.id}的食品！");
            ResetInputField();
        }
    }

    public void UpdateOneFoodSizePriceShow(string _str)
    {       
        //str为1表示更改成功，为0表示失败
        if (_str == "1") //成功
        {
            //修改本地sizelist和menucelldic
            foreach (var item in sizeList)
            {
                if (item.id == curSize.id)
                {
                    item.size = curSize.size;
                    item.price = curSize.price;
                    break;
                }
            }
            menuDic[curSize.mu_id].size_price_dic[curSize.size] = curSize.price;

            //修改显示（menucellObjDic）
            GameObject tarMenuCell = menuCellObjDic[curSize.mu_id];
            Transform sizeTrans = tarMenuCell.transform.GetChild(2);
            Transform priceTrans = tarMenuCell.transform.GetChild(3);
            //处理size的显示
            string sizes = "", prices = "";
            foreach (var item in menuDic[curSize.mu_id].size_price_dic)
            {
                sizes += $"{item.Key}/";
                prices += $"{item.Value}/";
            }
            sizeTrans.GetComponentInChildren<TextMeshProUGUI>().text = sizes;
            priceTrans.GetComponentInChildren<TextMeshProUGUI>().text = prices;

            AdminController.Print($"编号为{curSize.id}的规格信息更新成功！");
            ResetInputField();

        }
        else //失败
        {
            //输出失败
            AdminController.Print($"未找到id为{curSize.id}的食品！");
            ResetInputField();
        }
    }
    #endregion

    #region 【删除一个食品的数据】
    /// <summary>
    /// 删除一个Menu中的食物
    /// </summary>
    public void DeleteOneFoodData()
    {
        //只需要输入ID
        if (!CheckInputField(true)) return;


        //封装id并打包成Ins指令
        JArray Jproperty =
         DataDisposer.CombineProperty2Json(new JObject[] {
            DataDisposer.ConvertStrProperty2Json("mu_id", idStr)
         });

        //打包指令
        string jsonInstruction = DataDisposer.Combine2Instruction(
            DataDisposer.Ins.delete_Menu, Jproperty);

        //发送
        Client.Instance.SendMsg(jsonInstruction);

        //接收解析serv传来的json
        Client.Instance.ReceiveAndDispose(DataDisposer.Ins.delete_Menu);

    }

    /// <summary>
    /// 删除一个已有食物的数据的显示
    /// </summary>
    /// <param name="_str"></param>
    public void UnShowOneFoodData(string _str)
    {
        //str为1表示成功，为0表示失败
        if (_str == "1") //成功
        {
            //数据库中的size和inventory会级联删除不用管，这边处理字典中和显示的内容的删除
            //menu这边的删除
            //先删字典，sizeList、menuList、menuDic
            int tarMuId = Convert.ToInt16(idStr);
            foreach(var item in sizeList)
            {
                if(item.mu_id == tarMuId)
                {
                    sizeList.Remove(item);
                    break;
                }
            }
            foreach (var item in menuList)
            {
                if (item.id == tarMuId)
                {
                    menuList.Remove(item);
                    break;
                }
            }
            menuDic.Remove(tarMuId);

            //删除显示
            GameObject tarObj = menuCellObjDic[tarMuId];
            menuCellObjDic.Remove(tarMuId);
            GameObject.Destroy(tarObj);

            //删除Inventory中的显示（调用Inv的方法）
            InvManager.Instance.UnShowOneInvData(tarMuId);

            AdminController.Print($"编号为{idStr}的食品信息删除成功！");
            ResetInputField();

        }
        else //失败
        {
            //输出失败
            AdminController.Print($"未找到id为{idStr}的食品！");
        }
    }

    #endregion

    private static int GetSizeID(string _muId, string _size)
    {
        //生成s_id，s_id = s_mu_id+s_size(小0、中1、大2)
        string sizeNumStr = "";
        switch (_size)
        {
            case "大": sizeNumStr = "2"; break;
            case "中": sizeNumStr = "1"; break;
            case "小": sizeNumStr = "0"; break;
        }
        return Convert.ToInt32(_muId + sizeNumStr);
    }


    /// <summary>
    /// 检查是否按要求输入数据
    /// </summary>
    /// <param name="_checkId"></param>
    /// <param name="_checkFood"></param>
    /// <param name="_checkDesc"></param>
    /// <returns></returns>
    private bool CheckInputField(bool _checkId = true, bool _checkSize = false, bool _checkPrice = false, bool _checkFood = false, bool _checkDesc = false)
    {
        //输入检测
        if (_checkId)
        {
            if(idStr == "")
            {
                AdminController.Print("请输入编号！");
                return false;
            }
            else if(!AdminController.CheckIDType(idStr))
            {
                AdminController.Print("请输入4位的ID！");
                ResetInputField();
                return false;
            }
        }
        if(_checkSize && sizeStr == "")
        {
            AdminController.Print("请输入规格！");
            return false;
        }
        if (_checkPrice && priceStr == "")
        {
            AdminController.Print("请输入单价！");
            return false;
        }
        if (_checkFood && foodStr == "")
        {
            AdminController.Print("请输入食品！");
            return false;
        }
        if (_checkDesc && descStr == "")
        {
            AdminController.Print("请输入简介！");
            return false;
        }
        return true;
    }


    private void ResetInputField()
    {
        idStr = foodStr = descStr = sizeStr = priceStr = "";
        id.text = food.text = desc.text = size.text = price.text = "";
    }


}
