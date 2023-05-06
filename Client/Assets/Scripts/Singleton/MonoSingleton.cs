using UnityEngine;
using UnityEngine.SceneManagement;

public class MonoSingleton<T> : MonoBehaviour where T:MonoBehaviour //要求只有继承自Mono的类可以继承该MonoSingleton
{
    //实际单例
    private static T instance;

    //对外接口
    public static T Instance
    {
        get
        {
            if(instance==null)
            {
                instance = GameObject.FindObjectOfType<T>();
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

    private void Awake()
    {
        instance = Instance;    //确保外部获取Instance时不为空
    }


    //场景切换时回收该组件（class）
    //todo:
    //有时候  有的组件场景切换的时候回收的
    public static bool destroyOnLoad = false;
    //添加场景切换时候的事件
    public void AddSceneChangedEvent()
    {
        //SceneManager自带属性activeSceneChanged，是一个委托，可以添加绑定方法
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnSceneChanged(Scene arg0, Scene arg1)
    {
        if (destroyOnLoad == true)
        {
            if (instance != null)
            {
                DestroyImmediate(instance);//立即销毁
                Debug.Log(instance == null);
            }
        }
    }
}
