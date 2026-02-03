using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    public Player_MoveState(Player Player, StateMachine stateMachine, string stateName) : base(Player, stateMachine, stateName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.PlayLoopSound(player.moveSound);
    }

    public override void Exit()
    {
        base.Exit();
        player.StopLoopSound();
    }

    public override void Update()
    {
        base.Update();


        if (player.moveInput.x == 0 || player.wallDetected) 
            stateMachine.ChangeState(player.idleState);

        player.SetVelocity(player.moveInput.x * player.moveSpeed, rb.linearVelocity.y);
    }
}