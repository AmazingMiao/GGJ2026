using UnityEngine;

public class Player_GroundedState : EntityState
{
    public Player_GroundedState(Player Player, StateMachine stateMachine, string animBoolName) : base(Player, stateMachine, animBoolName)
    {
    }
    

    public override void Update()
    {
        base.Update();
        float xInput = input.Player.Move.ReadValue<Vector2>().x;
        
        if (Mathf.Abs(xInput) < 0.01f && player.groundDetected)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
        }

        if (rb.linearVelocityY<0 && player.groundDetected == false)
            stateMachine.ChangeState(player.fallState);

        if (input.Player.Jump.WasPressedThisFrame())
            stateMachine.ChangeState(player.jumpState);

        
    }
}