using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterActiveState : FighterState
{
    public FighterActiveState(Fighter fighter, FighterStateMachine fighterStateMachine) : base(fighter, fighterStateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Entered Active State");
    }

    public override void ExitState()
    {
        base.ExitState();
        Debug.Log("Exiting Active State");
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        //What to do with inputs here, I guess
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
