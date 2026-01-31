using Unity.VisualScripting;
using UnityEngine;

public abstract class EntityState
{
    protected Player player;
    protected StateMachine stateMachine;
    protected string animBoolName;

    protected Animator anim;
    protected Rigidbody2D rb;
    protected PlayerInputActions input;

    protected float stateTimer;
    protected bool triggerCalled;

    public EntityState(Player Player,StateMachine stateMachine, string animBoolName)
    {
        this.player = Player; 
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;

        anim = player.anim;
        rb = player.rb;
        input = player.input;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        anim.SetFloat("yVelocity", rb.linearVelocityY);
        

    }

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);

    }

    public void CallAnimationTrigger()
    {
        triggerCalled = true;
    }
    
}
