public class Singleton<T> where T:new() //约束只能是一般（非Mono）的class
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
                instance = new T();
            }
            return instance;
        }
    }
}
