using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI; // 引用 UI 命名空间

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private Image fadeImage; 
    [SerializeField] private float fadeDuration = 0.6f; 

    private Material transitionMat;
    private static readonly int CutoffProp = Shader.PropertyToID("_Cutoff");
    private static readonly int CenterProp = Shader.PropertyToID("_Center");
    private static readonly int AspectProp = Shader.PropertyToID("_Aspect");
    
    private bool isTransitioning = false;
    private Player player;

    private void Awake()
    {
        // 确保每个场景只有一个 LevelManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (fadeImage != null && fadeImage.material != null)
        {
            // 实例化材质，避免修改原始资源文件
            transitionMat = Instantiate(fadeImage.material);
            fadeImage.material = transitionMat;
            // 初始设为全黑（Cutoff = 1）
            transitionMat.SetFloat(CutoffProp, 1f);
            
            // 自动设置屏幕比例
            transitionMat.SetFloat(AspectProp, (float)Screen.width / Screen.height);
            
            // 放大 Image 确保覆盖
            fadeImage.rectTransform.localScale = Vector3.one * 5.5f;
        }
    }

    private void Start()
    {
        player = Object.FindFirstObjectByType<Player>();
        if (transitionMat != null)
        {
            UpdateShaderCenter();
            StartCoroutine(Fade(0f));
        }
    }

    private void UpdateShaderCenter()
    {
        if (player == null) player = Object.FindFirstObjectByType<Player>();
        if (player != null && transitionMat != null)
        {
            // 将玩家的世界坐标转换为屏幕坐标 (0-1 范围)
            // 使用 Camera.main.WorldToScreenPoint 并手动归一化，或者使用 WorldToViewportPoint
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(player.transform.position);

            // 确保坐标在 0-1 范围内，并传递给 Shader
            transitionMat.SetVector(CenterProp, new Vector4(viewportPos.x, viewportPos.y, 0, 0));
        }
    }

    void Update()
    {
        // 即使不在转场，也实时更新中心点，确保转场开始瞬间位置准确
        UpdateShaderCenter();

        if (isTransitioning) return;

        // 适配 macOS 和新 Input System 的 R 键监听
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
    }

    public void LoadNextScene()
    {
        if (isTransitioning) return;
        UpdateShaderCenter(); // 记录开始转场时的位置

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        // 检查是否还有下一个场景
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            StartCoroutine(TransitionToScene(nextSceneIndex));
        }
        else
        {
            // 到达最后一关，不再跳转
            Debug.Log("恭喜通关！这是最后一关了。");
            // 这里可以添加显示通关 UI 的逻辑
        }
    }

    public void RestartLevel()
    {
        if (isTransitioning) return;
        UpdateShaderCenter();
        StartCoroutine(TransitionToScene(SceneManager.GetActiveScene().buildIndex));
    }

    private System.Collections.IEnumerator TransitionToScene(int sceneIndex)
    {
        isTransitioning = true;

        // 离开场景：圆圈从中心迅速扩大（Cutoff 从 0 变到 1.5，确保完全覆盖）
        yield return StartCoroutine(Fade(2.5f));

        yield return new WaitForSeconds(0.1f);

        SceneManager.LoadScene(sceneIndex);
    }

    private System.Collections.IEnumerator Fade(float targetValue)
    {
        if (transitionMat == null) yield break;

        float startValue = transitionMat.GetFloat(CutoffProp);
        float timer = 0;

        // 使用更平滑的曲线
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            // 使用 EaseInOutQuad 增加流畅感
            t = t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
            
            float currentValue = Mathf.Lerp(startValue, targetValue, t);
            transitionMat.SetFloat(CutoffProp, currentValue);
            yield return null;
        }

        transitionMat.SetFloat(CutoffProp, targetValue);
    }
}