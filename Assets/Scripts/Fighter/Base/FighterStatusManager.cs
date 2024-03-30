using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class FighterStatusManager : MonoBehaviour
{
    public Fighter fighter;
    [Header("Stats")]
    public float currentHealth;
    public enum HealthColour { Green, Yellow, Orange, Critical }
    public HealthColour healthColour;
    public float baseMaxHealth = 100f;
    public float baseMoveSpeed = 10f;
    public float knockbackValue;
    public float knockbackThreshold = 2000f;
    public float pushedValue;

    [Header("Modifiers")]
    public float maxHealthModifier = 1f;
    [SerializeField] private float maxHealthTempModifier;
    [SerializeField] private float finalMaxHealthModifier;
    [SerializeField] private float finalMaxHealth;

    public float damageModifier = 1f;
    [SerializeField] private float damageTempModifier;
    [SerializeField] private float finalDamageModifier;

    public float moveSpeedModifier = 1f;
    [SerializeField] private float moveSpeedTempModifier;
    [SerializeField] private float finalMoveSpeedModifier;
    [SerializeField] private float finalMoveSpeed;

    public float damageTakenModifier = 1f;
    [SerializeField] private float damageTakenTempModifier;
    [SerializeField] private float finalDamageTakenModifier;

    [Header("CC Timers")]
    public float stunTimer;
    public float rootTimer;
    public bool isRooted;
    public bool isPushed;

    [Header("Pushback Balancing")]
    public float pushedTimerMultiplier = 3500f;
    public float rbDrag = 3f;

    [Header("Knockback Balancing")]
    public float maxKnockbackValue = 5000f;
    public float maxKnockbackStrength = 3000f;
    public float knockbackWallAmount = 1000f;
    public float yellowHealthModifier = 1.5f;
    public float orangeHealthModifier = 2.5f;
    public float criticalHealthModifier = 3.5f;
    


    #region Modifier Structs
    private struct HealthModifier
    {
        public float modifier;
        public float duration;
    }

    private struct DamageModifier
    {
        public float modifier;
        public float duration;
    }

    private struct SpeedModifier
    {
        public float modifier;
        public float duration;
    }

    private struct DamageTakenModifier
    {
        public float modifier;
        public float duration;
    }
    #endregion

    #region Modifier Queues
    private Queue<HealthModifier> healthModifierQueue = new(); //Queue to store buffs
    private Queue<DamageModifier> damageModifierQueue = new();
    private Queue<SpeedModifier> speedModifierQueue = new();
    private Queue<DamageTakenModifier> damageTakenModifierQueue = new();
    #endregion

    private void Start()
    {
        fighter = GetComponent<Fighter>();
        UpdateStats();
        currentHealth = baseMaxHealth;
        SetHealthColour();
    }
    private void Update()
    {

        UpdateStats();
        ModifierQueueCountdowns();

        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        if (rootTimer > 0)
        {
            if (!isRooted)
            {
                isRooted = true;
            }
            rootTimer -= Time.deltaTime;
            return;
        }
        else if (rootTimer <= 0 && isRooted == true)
        {
            isRooted = false;
            rootTimer = 0;
        }

        if (knockbackValue > 0)
        {
            if (knockbackValue > maxKnockbackValue)
            {
                knockbackValue = maxKnockbackValue;
            }
            knockbackValue -= Time.deltaTime * 1000;
        }
        else if (knockbackValue < 0)
        {
            knockbackValue = 0;
        }

        if (pushedValue > 0)
        {
            
            pushedValue -= Time.deltaTime * pushedTimerMultiplier;
            if (pushedValue > 0)
            {
                isPushed = true;
            } else
            {
                isPushed = false;
                pushedValue = 0;
            }
        } 
    }

    private void ModifierQueueCountdowns()
    {
        if (healthModifierQueue.Count > 0)
        {
            UpdateHealthModifierDurations();
            CalculateMaxHealthModifier();
        }
        else
        {
            maxHealthTempModifier = 1f;
        }

        if (damageModifierQueue.Count > 0)
        {
            UpdateDamageModifierDurations();
            CalculateDamageModifier();
        }
        else
        {
            damageTempModifier = 1f;
        }

        if (speedModifierQueue.Count > 0)
        {
            UpdateSpeedModifierDurations();
            CalculateSpeedModifier();
        }
        else
        {
            moveSpeedTempModifier = 1f;
        }

        if (damageTakenTempModifier > 0)
        {
            UpdateDamageTakenModifierDurations();
            CalculateDamageTakenModifier();
        }
    }

    private void UpdateStats()
    {
        //add the bonus and temp modifiers together to form the final modifier.
        finalMaxHealthModifier = (maxHealthModifier * maxHealthTempModifier);
        finalMaxHealth = (baseMaxHealth * finalMaxHealthModifier);
        finalDamageModifier = (damageModifier * damageTempModifier);
        finalMoveSpeedModifier = (moveSpeedModifier * moveSpeedTempModifier);
        finalMoveSpeed = (baseMoveSpeed * finalMoveSpeedModifier);
        finalDamageTakenModifier = (damageTakenModifier * damageTakenTempModifier);
    }

    public void SetHealthColour()
    {
        HealthColour newState;
        
        if (currentHealth >= 75 && currentHealth <= 100)
        {
            newState = HealthColour.Green;
        }
        else if (currentHealth >= 30 && currentHealth < 75)
        {
            newState = HealthColour.Yellow;
        }
        else if (currentHealth >= 2 && currentHealth < 30)
        {
            newState = HealthColour.Orange;
        }
        else
        {
            newState = HealthColour.Critical;
        }

        if (newState != healthColour)
        {
            healthColour = newState;
            Debug.Log("Health colour changed to: " + newState.ToString());
        } 
        
    }
   
    public float CalculateHealthColourModifiedValue(float baseValue)
    {
        switch (healthColour)
        {
            case HealthColour.Green:
                return baseValue;
            case HealthColour.Yellow:
                return (baseValue * yellowHealthModifier);
            case HealthColour.Orange:
                return (baseValue * orangeHealthModifier);
            case HealthColour.Critical:
                return (baseValue *  criticalHealthModifier);
            default:
                return baseValue;
        }
    }

    #region Health Modifier
    //Update the durations of active health buffs
    private void UpdateHealthModifierDurations()
    {
        // Convert the queue to an array for iteration
        HealthModifier[] buffArray = healthModifierQueue.ToArray();

        List<int> buffsToRemoveIndices = new List<int>();

        // Iterate over the array
        for (int i = 0; i < buffArray.Length; i++)
        {
            // Update the duration of the buff
            buffArray[i] = new HealthModifier { modifier = buffArray[i].modifier, duration = buffArray[i].duration - Time.deltaTime };

            // Check if the duration has expired
            if (buffArray[i].duration <= 0)
            {
                buffsToRemoveIndices.Add(i);
            }
        }

        // Remove expired buffs
        foreach (int index in buffsToRemoveIndices)
        {
            // Convert the index from array to queue index
            int queueIndex = index - buffsToRemoveIndices.IndexOf(index);
            healthModifierQueue.Dequeue(); // Remove the oldest buff from the queue
        }
    }

    // Calculate the current max health modifier
    private void CalculateMaxHealthModifier()
    {
        float totalModifier = 1f;

        foreach (HealthModifier buff in healthModifierQueue)
        {
            totalModifier *= buff.modifier;
        }

        maxHealthTempModifier = totalModifier;
    }

    // Method to add a new modifier
    public void AddHealthModifier(float modifier, float duration)
    {
        HealthModifier newBuff;
        newBuff.modifier = modifier;
        newBuff.duration = duration;

        healthModifierQueue.Enqueue(newBuff);
    }
    #endregion

    #region Damage Modifier
    //Update the durations of active health buffs
    private void UpdateDamageModifierDurations()
    {
        // Convert the queue to an array for iteration
        DamageModifier[] buffArray = damageModifierQueue.ToArray();

        List<int> buffsToRemoveIndices = new List<int>();

        // Iterate over the array
        for (int i = 0; i < buffArray.Length; i++)
        {
            // Update the duration of the buff
            buffArray[i] = new DamageModifier { modifier = buffArray[i].modifier, duration = buffArray[i].duration - Time.deltaTime };

            // Check if the duration has expired
            if (buffArray[i].duration <= 0)
            {
                buffsToRemoveIndices.Add(i);
            }
        }

        // Remove expired buffs
        foreach (int index in buffsToRemoveIndices)
        {
            // Convert the index from array to queue index
            int queueIndex = index - buffsToRemoveIndices.IndexOf(index);
            damageModifierQueue.Dequeue(); // Remove the oldest buff from the queue
        }
    }

    // Calculate the current max health modifier
    private void CalculateDamageModifier()
    {
        float totalModifier = 1f;

        foreach (DamageModifier buff in damageModifierQueue)
        {
            totalModifier *= buff.modifier;
        }

        damageTempModifier = totalModifier;
    }

    // Method to add a new modifier
    public void AddDamageModifier(float modifier, float duration)
    {
        DamageModifier newBuff;
        newBuff.modifier = modifier;
        newBuff.duration = duration;

        damageModifierQueue.Enqueue(newBuff);
    }
    #endregion

    #region Speed Modifier
    //Update the durations of active health buffs
    private void UpdateSpeedModifierDurations()
    {
        // Convert the queue to an array for iteration
        SpeedModifier[] buffArray = speedModifierQueue.ToArray();

        List<int> buffsToRemoveIndices = new List<int>();

        // Iterate over the array
        for (int i = 0; i < buffArray.Length; i++)
        {
            // Update the duration of the buff
            buffArray[i] = new SpeedModifier { modifier = buffArray[i].modifier, duration = buffArray[i].duration - Time.deltaTime };

            // Check if the duration has expired
            if (buffArray[i].duration <= 0)
            {
                buffsToRemoveIndices.Add(i);
            }
        }

        // Remove expired buffs
        foreach (int index in buffsToRemoveIndices)
        {
            // Convert the index from array to queue index
            int queueIndex = index - buffsToRemoveIndices.IndexOf(index);
            speedModifierQueue.Dequeue(); // Remove the oldest buff from the queue
        }
    }

    // Calculate the current max health modifier
    private void CalculateSpeedModifier()
    {
        float totalModifier = 1f;

        foreach (SpeedModifier buff in speedModifierQueue)
        {
            totalModifier *= buff.modifier;
        }

        moveSpeedTempModifier = totalModifier;
    }

    // Method to add a new modifier
    public void AddSpeedModifier(float modifier, float duration)
    {
        SpeedModifier newBuff;
        newBuff.modifier = modifier;
        newBuff.duration = duration;

        speedModifierQueue.Enqueue(newBuff);
    }
    #endregion

    #region Damage Taken Modifier
    //Update the durations of active health buffs
    private void UpdateDamageTakenModifierDurations()
    {
        // Convert the queue to an array for iteration
        DamageTakenModifier[] buffArray = damageTakenModifierQueue.ToArray();

        List<int> buffsToRemoveIndices = new List<int>();

        // Iterate over the array
        for (int i = 0; i < buffArray.Length; i++)
        {
            // Update the duration of the buff
            buffArray[i] = new DamageTakenModifier { modifier = buffArray[i].modifier, duration = buffArray[i].duration - Time.deltaTime };

            // Check if the duration has expired
            if (buffArray[i].duration <= 0)
            {
                buffsToRemoveIndices.Add(i);
            }
        }

        // Remove expired buffs
        foreach (int index in buffsToRemoveIndices)
        {
            // Convert the index from array to queue index
            int queueIndex = index - buffsToRemoveIndices.IndexOf(index);
            damageTakenModifierQueue.Dequeue(); // Remove the oldest buff from the queue
        }
    }

    // Calculate the current max health modifier
    private void CalculateDamageTakenModifier()
    {
        float totalModifier = 1f;

        foreach (DamageTakenModifier buff in damageTakenModifierQueue)
        {
            totalModifier *= buff.modifier;
        }

        damageTakenTempModifier = totalModifier;
    }

    // Method to add a new modifier
    public void AddDamageTakenModifier(float modifier, float duration)
    {
        DamageTakenModifier newBuff;
        newBuff.modifier = modifier;
        newBuff.duration = duration;

        damageTakenModifierQueue.Enqueue(newBuff);
    }
    #endregion

    #region Stun
    public void AddStun(float duration)
    {
        stunTimer = duration;
        //If fighter isn't in the stunned state, enter that state
    }
    #endregion

    #region Root
    public void AddRoot(float duration)
    {
        rootTimer = duration;

    }
    #endregion
}
