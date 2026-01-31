using UnityEngine;

public class Player_WallJumpState : EntityState

{
    public Player_WallJumpState(Player Player, StateMachine stateMachine, string animBoolName) : base(Player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(player.wallJumpDir.x * -player.facingDir, player.wallJumpDir.y);

    }

    public override void Update()
    {
        base.Update();

        if (rb.linearVelocity.y < 0)
            stateMachine.ChangeState(player.fallState);

        if(player.wallDetected)
            stateMachine.ChangeState(player.wallSlideState);
    }

}