using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterStateMachine
{
    public FighterState CurrentFighterState { get; set; }

    public void Initialize(FighterState startingState)
    {
        CurrentFighterState = startingState;
        CurrentFighterState.EnterState();
    }

    public void ChangeState(FighterState newState)
    {
        CurrentFighterState.ExitState();
        CurrentFighterState = newState;
        CurrentFighterState.EnterState();
    }
}
