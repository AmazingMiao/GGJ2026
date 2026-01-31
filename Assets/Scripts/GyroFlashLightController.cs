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

        // 1. 修正：InputSystem.settings.sensorSamplingRate 是正确的 API
        // if (InputSystem.settings != null)
        // {
        //     InputSystem.settings.sensorSamplingRate = 50f;
        // }

        // 2. 尝试手动启用传感器设备
        var sensor = AttitudeSensor.current;
        if (sensor != null)
        {
            InputSystem.EnableDevice(sensor);
            Debug.Log("传感器已成功激活");
        }
        else
        {
            // 3. 如果依然为 null，尝试列出所有设备诊断
            Debug.LogError("未检测到姿态传感器。请检查：\n" +
                           "1. iPhone 是否已信任此电脑\n" +
                           "2. iTunes 是否能看到手机\n" +
                           "3. Unity Remote 5 是否在 Play 之前已启动");
        
            foreach (var device in InputSystem.devices)
            {
                if (device.name.Contains("Remote")) Debug.Log($"发现远程设备: {device.name}");
            }
        }
        // 记录初始旋转，用于相对偏移
        _initialRotation = transform.rotation;
        // 暂时使用一下以消除警告
        Debug.Log($"Initial Rotation: {_initialRotation}");
    }

    void Update()
    {
        // 获取鼠标位置（如果需要通过 Action 获取）
        // 注意：如果 PlayerInputActions 中没有定义 MousePosition Action，
        // 也可以直接在 Update 中使用 Mouse.current.position.ReadValue()
        // 这里演示如何通过 Action 获取（假设你在配置中添加了 Point 或 Position Action）
        
        UpdateFlashlightRotation();
        UpdateBattery();
    }

    private void UpdateFlashlightRotation()
    {
        if (_currentBattery <= 0) return;

        // 强制检查：如果传感器没连上或者没启用，就走鼠标逻辑
        bool isGyroAvailable = AttitudeSensor.current != null && AttitudeSensor.current.enabled;

        if (isGyroAvailable)
        {
            // 1. 手机传感器控制逻辑 (如果需要陀螺仪控制移动，这里可以根据姿态计算位移，目前保留逻辑结构)
            Quaternion attitude = AttitudeSensor.current.attitude.ReadValue();
            // 暂时保留传感器逻辑，但如果用户确定完全不需要旋转且传感器只用于旋转，此处可根据需求修改为位移
        }
        else
        {
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