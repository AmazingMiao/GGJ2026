using UnityEngine;

public class Player_JumpState : Player_AiredState
{
    public Player_JumpState(Player Player, StateMachine stateMachine, string animBoolName) : base(Player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(rb.linearVelocity.x, player.jumpForce);

    }

    public override void Update()
    {
        base.Update();

        if (rb.linearVelocity.y < 0 ) 
            stateMachine.ChangeState(player.fallState);

    }
}