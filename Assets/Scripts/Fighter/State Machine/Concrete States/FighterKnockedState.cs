using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class FighterKnockedState : FighterState
{
    public FighterKnockedState(Fighter fighter, FighterStateMachine fighterStateMachine) : base(fighter, fighterStateMachine)
    {
    }

    public float lastKBStrength;
    public Vector3 lastKBDirection;
    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Entered Knocked State");
        fighter.rb.drag = 0f;
    }

    public override void ExitState()
    {
        base.ExitState();
        Debug.Log("Exiting Knocked State");
        fighter.rb.drag = fighter.StatusManager.rbDrag;

        
        //fighter.Pushback(lastKBStrength,lastKBDirection);
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        if (fighter.StatusManager.knockbackValue <= 0)
        {
            fighter.StateMachine.ChangeState(fighter.ActiveState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public void Knockback(float kbStrength, Vector3 kbDirection)
    {
        fighter.rb.velocity = Vector3.zero;
        float knockbackSpeed = Mathf.Min(kbStrength, fighter.StatusManager.maxKnockbackStrength);
        float finalKBStrength = knockbackSpeed / 100;
        Debug.Log(finalKBStrength);
        fighter.rb.velocity = kbDirection.normalized * finalKBStrength;
        lastKBStrength = kbStrength;
        lastKBDirection = kbDirection;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector3 reflectionDirection = Vector3.Reflect(fighter.rb.velocity.normalized, lastKBDirection);
            float finalKBStrength = lastKBStrength + fighter.StatusManager.knockbackWallAmount;
            fighter.rb.velocity = reflectionDirection * lastKBStrength;
        }
    }
}
