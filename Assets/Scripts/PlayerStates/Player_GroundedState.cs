using UnityEngine;

public class Player_GroundedState : EntityState
{
    public Player_GroundedState(Player Player, StateMachine stateMachine, string animBoolName) : base(Player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (rb.linearVelocityY<0 && player.groundDetected == false)
            stateMachine.ChangeState(player.fallState);

        if (input.Player.Jump.WasPressedThisFrame())
            stateMachine.ChangeState(player.jumpState);

        
    }
}