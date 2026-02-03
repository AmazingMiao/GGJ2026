using UnityEngine;

public class MusicManager : MonoBehaviour
{
    // 静态变量，用于存储唯一的实例
    private static MusicManager instance;

    void Awake()
    {
        // 检查是否已经存在实例
        if (instance == null)
        {
            // 如果没有，则将当前对象设为实例
            instance = this;
            // 核心：告诉 Unity 在切换场景时不销毁此对象
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果实例已存在（比如从第二个场景回到了第一个场景）
            // 销毁新创建的重复对象，保证只有一个音乐播放器
            Destroy(gameObject);
        }
    }
}