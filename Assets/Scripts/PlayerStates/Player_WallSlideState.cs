using UnityEngine;

public class Player_WallSlideState : EntityState
{
    public Player_WallSlideState(Player Player, StateMachine stateMachine, string animBoolName) : base(Player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.PlayLoopSound(player.wallSlideSound);
        player.anim.transform.localScale = new Vector3(-1, 1, 1);

    }

    public override void Exit()
    {
        base.Exit();
        player.StopLoopSound();
        player.anim.transform.localScale = new Vector3(1, 1, 1);
        
    }

    public override void Update()
    {
        base.Update();
        HandleWallSlide();

        if (input.Player.Jump.WasPressedThisFrame())
            stateMachine.ChangeState(player.wallJumpState);


        if (!player.wallDetected)
            stateMachine.ChangeState(player.fallState);
        
        

        if (player.groundDetected)
        {
            stateMachine.ChangeState(player.idleState);
            if (player.facingDir != player.moveInput.x)
                player.Flip();
        }
    }

    private void HandleWallSlide()
    {
        if(player.moveInput.y<0)
            player.SetVelocity(player.moveInput.x, rb.linearVelocity.y);
        else
            player.SetVelocity(player.moveInput.x, rb.linearVelocity.y * player.wallSlideSlowMultiplier);
    }

}