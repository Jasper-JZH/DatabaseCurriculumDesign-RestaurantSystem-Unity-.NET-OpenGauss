using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主菜单界面UI，并调用对应接口
/// </summary>

public class Menu : MonoBehaviour
{
    [SerializeField] private Transform transform;

    [SerializeField] private TMP_InputField id; //id输入区域
    [SerializeField] private TMP_InputField name; //name输入区域
    [SerializeField] private TMP_InputField phone;   //密码输入区域

    [SerializeField] private TextMeshProUGUI output;    //输出

    [SerializeField] private Button customerSign;   //顾客注册按钮
    [SerializeField] private Button customerLogin;  //顾客登录按钮
    [SerializeField] private Button adminLogin;   //管理员登录按钮
    [SerializeField] private Button quit;   //退出按钮

    private string idStr;   //账号    
    private string nameStr; //名字
    private string phoneStr; //密码
    private string outputStr;  //输出



    public void updateOutPut(string _output)
    {
        output.text = _output;
    }

    private void Awake()
    {
        transform = GetComponent<Transform>();
        //UI绑定
        id = transform.Find("_id").GetComponentInChildren<TMP_InputField>();
        name = transform.Find("_name").GetComponentInChildren<TMP_InputField>();
        phone = transform.Find("_ps").GetComponentInChildren<TMP_InputField>();
        output = transform.Find("_output").GetComponentInChildren<TextMeshProUGUI>();

        //获取所有按钮组件并绑定
        Button[] ButtonArray = transform.Find("_button").GetComponentsInChildren<Button>();
        foreach(var button in ButtonArray)
        {
            switch(button.name)
            {
                case "customerSign[Button]":
                    customerSign = button;break;
                case "customerLogin[Button]":
                    customerLogin = button; break;
                case "adminLogin[Button]":
                    adminLogin = button; break;
                case "quit[Button]":
                    quit = button; break;
                default:Debug.Log("get unknown button!");break;
            }
        }
    }

    private void Start()
    {
        //初始化
        idStr = phoneStr = "";
        //按钮绑定
        customerSign.onClick.AddListener(() => { CustomerSign(); });
        customerLogin.onClick.AddListener(() => { CustomerLogin(); });
        adminLogin.onClick.AddListener(() => { AdminLogin(); });
        quit.onClick.AddListener(() => { Quit(); });

        //事件订阅
        Authentication.AdminLoginEvent += OnAuthenticate;
        Authentication.CusLoginEvent += OnAuthenticate;
        Authentication.CusSignEvent += OnAuthenticate;
    }

    private void Update()
    {
        //实时同步输入框的内容
        idStr = id.text;
        nameStr = name.text;
        phoneStr = phone.text;
    }

    /// <summary>
    /// 打印输出
    /// </summary>
    /// <param name="_str"></param>
    private void Print(string _str)
    {
        outputStr = _str;
        output.text = outputStr;
    }

    /// <summary>
    /// 检查电话号码的格式是否正确
    /// </summary>
    /// <param name="_phoneNum"></param>
    private bool CheckPhoneType(string _phoneNum)
    {
        if (_phoneNum.Length != 11)
        {
            Print("请输入正确格式的phone！");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 注册
    /// </summary>
    private void CustomerSign()
    {
        //输入检测
        if (idStr == "" || phoneStr == "" || nameStr == "")
        {
            if (idStr == "") Print("请输入id");
            else if (phoneStr == "") Print("请输入phone");
            else Print("请输入name");
            return;
        }
        else if(CheckPhoneType(phoneStr))
            Authentication.Instance.CusSign(idStr, nameStr, phoneStr);
    }

    /// <summary>
    /// 登录
    /// </summary>
    private void CustomerLogin()
    {
        //输入检测
        if (idStr == "")
        {
            Print("请输入id");
            return;
        } 
        else if (phoneStr == "")
        {
            Print("请输入phone");
            return;
        }
        else if (CheckPhoneType(phoneStr))
            Authentication.Instance.CusLogin(idStr, phoneStr);
    }

    private void AdminLogin()
    {
        //输入检测
        if (idStr == "")
        {
            Print("请输入id");
            return;
        }
        else if (phoneStr == "")
        {
            Print("请输入phone");
            return;
        }
        else if (CheckPhoneType(phoneStr))
            Authentication.Instance.AdminLogin(idStr, phoneStr);
    }

    public void OnAuthenticate(Authentication.Result _authenticateResult)
    {
        switch (_authenticateResult)
        {
            case Authentication.Result.idNull:
                {
                    Print("id不存在！");
                }
                break;
            case Authentication.Result.passwordMis:
                {
                    Print("密码错误！");
                }
                break;
            case Authentication.Result.loginAsAdmin:
                {
                    Print("管理员登录成功！");
                    //跳转管理员场景
                    SceneController.LoadScene(SceneController.myScene.ADMINE);
                }
                break;
            case Authentication.Result.loginAsCustomer:
                {
                    Print("顾客登录成功！");
                    //跳转顾客场景
                    SceneController.LoadScene(SceneController.myScene.CUS);
                }
                break;
            case Authentication.Result.signAsCustomer:
                {
                    Print("顾客注册成功！");
                }
                break;
            case Authentication.Result.signFailAsIdUsed:
                {
                    Print("此ID已被注册！");
                }
                break;
        }
        ResetInputField();
    }
    /// <summary>
    /// 清空输入区域
    /// </summary>
    private void ResetInputField()
    {
        idStr = phoneStr = nameStr = "";
        id.text = phone.text = name.text = "";
    }


    /// <summary>
    /// 退出程序
    /// </summary>
    public void Quit()
    {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        //Client.Instance.DisConnect();
    }
}
