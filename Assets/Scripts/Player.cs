using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Animator anim {  get; private set; }
    public Rigidbody2D rb {  get; private set; }

    public PlayerInputActions input {  get; private set; }
    private StateMachine stateMachine;
    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    public Player_WallSlideState wallSlideState { get; private set; }
    public Player_WallJumpState wallJumpState { get; private set; }


    


    [Header("Movement details")]
    public float moveSpeed;
    public float jumpForce = 5;
    public Vector2 wallJumpDir;

    [Range(0,1)]
    public float inAirMoveMultiplier = .7f;
    [Range(0, 1)]
    public float wallSlideSlowMultiplier = .7f;


    private bool facingRight = true;
    public int facingDir { get; private set; } = 1; 
    public Vector2 moveInput { get; private set; }

    [Header("Collision detection")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Transform primaryWallCheck;
    [SerializeField] private Transform secondaryWallCheck;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource loopAudioSource;
    [SerializeField] public AudioClip jumpSound;
    [SerializeField] public AudioClip wallJumpSound;
    [SerializeField] public AudioClip landSound;
    [SerializeField] public AudioClip moveSound;
    [SerializeField] public AudioClip wallSlideSound;

    public bool groundDetected { get; private set; }
    public bool wallDetected { get; private set; }

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        stateMachine = new StateMachine();
        input = new PlayerInputActions();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "jumpFall");
        fallState = new Player_FallState(this, stateMachine, "jumpFall");
        wallSlideState = new Player_WallSlideState(this, stateMachine, "wallSlide");
        wallJumpState = new Player_WallJumpState(this, stateMachine, "jumpFall");

    }

    private void OnEnable()
    {
        input.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero; 
    }

    private void OnDisable()
    {
        input.Disable();
    }


    private void Start()
    {
        stateMachine.Initialize(idleState);

    }

    private void Update()
    {
        HandleCollisionDetection();
        stateMachine.UpdateActiveState();
    }


    

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.linearVelocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity);
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlayLoopSound(AudioClip clip)
    {
        if (loopAudioSource != null && clip != null)
        {
            if (loopAudioSource.clip == clip && loopAudioSource.isPlaying) return;
            loopAudioSource.clip = clip;
            loopAudioSource.loop = true;
            loopAudioSource.Play();
            Debug.Log($"[Audio] Playing Loop Sound: {clip.name}");
        }
        else
        {
            Debug.LogWarning($"[Audio] Cannot play loop sound. Source: {loopAudioSource}, Clip: {clip}");
        }
    }

    public void StopLoopSound()
    {
        if (loopAudioSource != null)
        {
            Debug.Log($"[Audio] Stopping Loop Sound: {loopAudioSource.clip?.name}");
            loopAudioSource.Stop();
            loopAudioSource.clip = null;
        }
    }

    private void FlipController(float _xVelocity)
    {
        if (_xVelocity > 0 && !facingRight)
            Flip();
        else if (_xVelocity < 0 && facingRight)
            Flip();
            
    }
    public void Flip()
    {
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
        facingDir = facingDir * -1;
    }
    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检测是否碰到标记为 "Finish" 的物体
        if (collision.CompareTag("Finish"))
        {
            LevelManager.Instance.LoadNextScene();
        }
    }
    

    private void HandleCollisionDetection()
    {
        groundDetected = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround)
                    && Physics2D.Raycast(secondaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -groundCheckDistance));
        Gizmos.DrawLine(primaryWallCheck.position, primaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));
        Gizmos.DrawLine(secondaryWallCheck.position, secondaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));

    }
}