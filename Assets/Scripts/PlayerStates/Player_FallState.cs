using UnityEngine;

public class Player_FallState : Player_AiredState
{
    public Player_FallState(Player Player, StateMachine stateMachine, string animBoolName) : base(Player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();
        if (player.groundDetected)
            stateMachine.ChangeState(player.idleState);

        if(player.wallDetected)
            stateMachine.ChangeState(player.wallSlideState);
    }


}