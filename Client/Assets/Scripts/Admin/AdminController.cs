using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 处理Admin,对三个子模块的调用
/// </summary>
public class AdminController : MonoBehaviour
{
    //返回按钮
    private Button returnButton;
    private TextMeshProUGUI outputText; //输出

    private static string outputStr;

    private void Awake()
    {
        returnButton = transform.Find("return[Button]").GetComponent<Button>();
        outputText = transform.Find("output[T]").GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        //获取inventory列表
        InvManager.Instance.GetAllInvData();

        //返回按钮绑定
        returnButton.onClick.AddListener(() => { SceneController.OnReturnButtonClik(); });
    }

    /// <summary>
    /// 检查ID是否符合格式要求
    /// </summary>
    public static bool CheckIDType(string _id)
    {
        return _id.Length == 4 ? true : false;
    }

    private void Update()
    {
        outputText.text = outputStr;
    }

    public static void Print(string _str)
    {
        outputStr = _str;
    }


}
