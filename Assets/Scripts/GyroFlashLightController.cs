using UnityEngine;
using UnityEngine.Rendering.Universal; // 必须引用 URP 命名空间
using UnityEngine.InputSystem;         // 必须引用 Input System
using UnityEngine.UI;
using TMPro;

public class StationaryFlashlight : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Light2D flashlight;
    [SerializeField] private Slider batterySlider;
    [SerializeField] private TMP_Text batteryText;

    [Header("Settings")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainRate = 2.0f;
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float normalIntensity = 1.0f;
    [SerializeField] private float lowBatteryIntensity = 0.2f;

    [Header("Gyro Mapping")]
    [SerializeField] private float sensitivity = 12f;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool swapXY = false;
    [SerializeField] private Vector3 attitudeOffset = Vector3.zero;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private Vector3 debugAttitudeEuler;
    [SerializeField] private Vector2 debugRawXY;
    [SerializeField] private Vector2 debugFinalXY;

    private float _currentBattery;
    private Quaternion _initialRotation;
    private PlayerInputActions _inputActions;
    private Vector2 _mousePosition;

    void Awake()
    {
        _inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        _inputActions.Enable();
    }

    void OnDisable()
    {
        _inputActions.Disable();
    }

    void Start()
    {
        _currentBattery = maxBattery;
        _initialRotation = transform.rotation;
        Debug.Log($"Initial Rotation: {_initialRotation}");
    }

    void Update()
    {
        // 动态检测并启用传感器，解决 Unity Remote 延迟连接问题
        EnsureSensorsEnabled();
        
        UpdateFlashlightRotation();
        UpdateBattery();
    }

    private void EnsureSensorsEnabled()
    {
        // 尝试启用 AttitudeSensor
        if (AttitudeSensor.current != null && !AttitudeSensor.current.enabled)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
            Debug.Log("AttitudeSensor 已动态激活");
        }

        // 尝试启用 Gyroscope (飞鼠效果需要)
        if (UnityEngine.InputSystem.Gyroscope.current != null && !UnityEngine.InputSystem.Gyroscope.current.enabled)
        {
            InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
            Debug.Log("Gyroscope 已动态激活");
        }

        // 备选：启用重力感应器
        if (GravitySensor.current != null && !GravitySensor.current.enabled)
        {
            InputSystem.EnableDevice(GravitySensor.current);
            Debug.Log("GravitySensor 已动态激活");
        }
    }

    private void UpdateFlashlightRotation()
    {
        if (_currentBattery <= 0) return;

        // 检查 AttitudeSensor 是否可用且已启用
        var attitudeSensor = AttitudeSensor.current;
        if (attitudeSensor != null && attitudeSensor.enabled)
        {
            // 1. 手机姿态控制逻辑 (指哪打哪效果)
            Quaternion rawAttitude = attitudeSensor.attitude.ReadValue();
            
            // 应用初始姿态偏移
            Quaternion offsetRotation = Quaternion.Euler(attitudeOffset);
            Quaternion attitude = rawAttitude * offsetRotation;
            
            debugAttitudeEuler = attitude.eulerAngles; // Debug 显示
            
            // 【核心修正】：在 Input System 的 AttitudeSensor 中，
            // 手机背对用户（摄像头指向屏幕）时，手机的“前方”实际上是 -Vector3.up (竖屏) 或 Vector3.forward (平放)
            // 为了实现“摄像头指哪打哪”，我们使用手机的局部 Z 轴 (Vector3.forward)
            // 并将其转换到世界空间，然后提取其在水平和垂直方向上的投影
            Vector3 cameraDir = attitude * Vector3.forward;
            
            // 使用 Atan2 提取偏航角 (Yaw) 和 俯仰角 (Pitch)
            // 尝试更换偏航角的计算轴：从 (x, z) 改为 (x, y) 或 (z, y) 等，
            // 这里根据用户反馈“偏航轴错误”，通常是因为手机竖屏/横屏状态下 forward 向量的参考系不同。
            // 尝试使用 cameraDir.x 和 cameraDir.y 的组合，或者调整 Atan2 的参数顺序。
            // 修正：使用 x 和 y 来计算水平偏航，这在某些握持姿态下更符合直觉
            float yaw = Mathf.Atan2(cameraDir.x, -cameraDir.y) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(cameraDir.z) * Mathf.Rad2Deg;

            // 将角度映射到屏幕位移
            float rawX = yaw / 45f;   // 假设 45 度达到灵敏度边界
            float rawY = pitch / 45f;
            
            debugRawXY = new Vector2(rawX, rawY); // Debug 显示

            // 根据 Editor 设置进行轴向调整
            float finalX = swapXY ? rawY : rawX;
            float finalY = swapXY ? rawX : rawY;

            finalX *= (invertX ? -1f : 1f);
            finalY *= (invertY ? -1f : 1f);
            debugFinalXY = new Vector2(finalX, finalY); // Debug 显示

            Vector3 targetWorldPos = new Vector3(finalX * sensitivity, finalY * sensitivity, 0);

            // 平滑移动到目标位置
            transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * smoothSpeed);

            // 限制在屏幕范围内
            ClampToScreen();
            return;
        }

        // 2. 使用 PlayerInputActions 的鼠标控制逻辑：跟随光标位置
        Vector2 mouseScreenPos = _inputActions.Player.Point.ReadValue<Vector2>();
        
        if (Time.frameCount % 60 == 0) Debug.Log($"Mouse Screen Pos: {mouseScreenPos}");

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Missing Main Camera!");
            return;
        }

        // 将屏幕坐标转换为世界坐标
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCam.transform.position.z)));
        mouseWorldPos.z = 0;

        // 直接平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, mouseWorldPos, Time.deltaTime * smoothSpeed);
    }

    private void ClampToScreen()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 pos = mainCam.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp01(pos.x);
        pos.y = Mathf.Clamp01(pos.y);
        transform.position = mainCam.ViewportToWorldPoint(pos);
    }

    private void UpdateBattery()
    {
        if (_currentBattery > 0)
        {
            // 消耗电量
            _currentBattery -= drainRate * Time.deltaTime;
            
            // 只有在电量极低时才处理闪烁逻辑，不再根据电量百分比线性调整强度
            if (_currentBattery < 10f)
            {
                flashlight.intensity = (Time.time % 0.2f > 0.1f) ? lowBatteryIntensity : normalIntensity;
            }
            else
            {
                // 正常电量下保持固定强度
                flashlight.intensity = normalIntensity;
            }
        }
        else
        {
            _currentBattery = 0;
            flashlight.enabled = false;
        }

        // 更新 UI
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (batterySlider) batterySlider.value = _currentBattery / maxBattery;
        if (batteryText) batteryText.text = $"Power: {Mathf.CeilToInt(_currentBattery)}%";
    }
}