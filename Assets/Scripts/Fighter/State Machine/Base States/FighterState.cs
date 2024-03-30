using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterState
{
    protected Fighter fighter;
    protected FighterStateMachine fighterStateMachine;
    

    public FighterState(Fighter fighter, FighterStateMachine fighterStateMachine)
    {
        this.fighter = fighter;
        this.fighterStateMachine = fighterStateMachine;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
    public virtual void PhysicsUpdate() { }
  
}
