using UnityEngine;

public class Player_AiredState : EntityState
{
    public Player_AiredState(Player Player, StateMachine stateMachine, string animBoolName) : base(Player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if(player.moveInput.x != 0)
            player.SetVelocity(player.moveInput.x * (player.moveSpeed * player.inAirMoveMultiplier), rb.linearVelocity.y);
        
    }
}