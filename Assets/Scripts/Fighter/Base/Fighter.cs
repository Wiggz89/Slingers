using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;

public class Fighter : MonoBehaviour
{

    #region State Machine Variables

    public FighterStateMachine StateMachine { get; set; }
    public FighterActiveState ActiveState { get; set; } 
    public FighterKnockedState KnockedState { get; set; }
    #endregion

    public FighterStatusManager StatusManager { get; set; }
    public Rigidbody rb {  get; set; }


    private void Awake()
    {
        StateMachine = new FighterStateMachine();
        StatusManager = GetComponent<FighterStatusManager>();
        rb = GetComponent<Rigidbody>();
        ActiveState = new FighterActiveState(this, StateMachine);
        KnockedState = new FighterKnockedState(this, StateMachine);
        rb.drag = StatusManager.rbDrag;
    }

    private void Start()
    {
        StateMachine.Initialize(ActiveState);
    }

    private void Update()
    {
        StateMachine.CurrentFighterState.FrameUpdate();
        if (StatusManager.currentHealth < 0)
        {
           //Die();
        }
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentFighterState.PhysicsUpdate();
    }

    public void Attacked (float damageAmount, float kBBase, float kBScaling, bool canKnock, Vector3 knockbackDirection)
    {
        

        float scaledKBScaling = StatusManager.CalculateHealthColourModifiedValue(kBScaling);
        float kBTotal = kBBase + scaledKBScaling;
        Debug.Log("Health Modified Knockback: " + kBTotal);

        if (this.gameObject != null && rb != null)
        {
            
            if (canKnock)
            {
                if (StateMachine.CurrentFighterState == KnockedState)
                {
                    StatusManager.knockbackValue = kBTotal;
                    float finalKBStrength = Mathf.Min(kBTotal, StatusManager.maxKnockbackValue);

                    //Test this. If need be, we could add half the remaining knockbackValue if it feels weird to lower the timer when hitting a knocked opponent.
                    KnockedState.Knockback(finalKBStrength, knockbackDirection);
                } 
                else if (StateMachine.CurrentFighterState != KnockedState)
                {
                    float finalKBStrength = Mathf.Min(StatusManager.knockbackValue + kBTotal, StatusManager.maxKnockbackValue);
                    StatusManager.knockbackValue = finalKBStrength;
                    if (StatusManager.knockbackValue > StatusManager.knockbackThreshold)
                    {
                        StateMachine.ChangeState(KnockedState);
                        KnockedState.Knockback(StatusManager.knockbackValue, knockbackDirection);
                    }
                    else
                    {
                        Pushback(kBTotal, knockbackDirection);
                    }
                }
                
            } 
            else if (!canKnock && StateMachine.CurrentFighterState != KnockedState)
            {
                //If the attack can't knock, just add to the knockbackValue (if not in the knocked state)
                StatusManager.knockbackValue += kBTotal;
            }
        }

        StatusManager.currentHealth -= damageAmount;
        StatusManager.SetHealthColour();

    }


    public void RandomAttack()
    {
        Vector3 randomVector = new(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        
        
        float knockBaseStrength = Random.Range(200, 401);
        float knockScalingStrength = Random.Range(800, 1201);
        
        Attacked(Random.Range(0, 50), knockBaseStrength, knockScalingStrength, true, randomVector);
    }

    public void Pushback(float pushbackStrength, Vector3 pushbackDirection)
    {
        StatusManager.isPushed = true;
        StatusManager.pushedValue = pushbackStrength;
        float pushForce = pushbackStrength / 100;
        rb.velocity = pushbackDirection.normalized * pushForce;
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
