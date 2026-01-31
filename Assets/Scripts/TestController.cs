// using UnityEngine;
// using UnityEngine.InputSystem;
//
// public class TestController : MonoBehaviour
// {
//     [SerializeField] private float moveSpeed = 5f;
//     
//     private Rigidbody2D rb;
//     private Vector2 moveInput;
//     private PlayerInputActions inputActions;
//
//     void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         inputActions = new PlayerInputActions();
//     }
//
//     void OnEnable()
//     {
//         inputActions.Player.Enable();
//     }
//
//     void OnDisable()
//     {
//         inputActions.Player.Disable();
//     }
//
//     void Update()
//     {
//         // 读取 Move 输入
//         moveInput = inputActions.Player.Move.ReadValue<Vector2>();
//     }
//
//     void FixedUpdate()
//     {
//         // 使用 Rigidbody2D 移动
//         rb.linearVelocity = moveInput * moveSpeed;
//     }
// }