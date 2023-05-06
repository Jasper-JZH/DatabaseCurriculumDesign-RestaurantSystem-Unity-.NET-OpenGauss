using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景控制，对提供切换场景的请求接口，并完成场景切换
/// </summary>
public class SceneController : MonoSingleton<SceneController>
{
    public enum myScene
    {
        MENU,
        ADMINE,
        CUS
    }

    public static void OnReturnButtonClik()
    {
        LoadScene(myScene.MENU);
    }

    public static void LoadScene(myScene _tarScene)
    {
        Debug.Log($"加载场景{_tarScene.ToString()}");
        SceneManager.LoadScene((int)_tarScene);
    }
}
