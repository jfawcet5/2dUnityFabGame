using System.Collections;
using System.Collections.Generic;
using BeyProject.Combat;
using BeyProject.Core;
using UnityEngine;

public class HazardController : MonoBehaviour, IDamageable
{
    public string hazardControllerId;
    public List<HazardEffect> targetHazards = new List<HazardEffect>();

    public float health;
    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            OnDestroyHazardController();
        }
    }

    private void OnDestroyHazardController()
    {
        foreach (HazardEffect hazard in targetHazards)
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.SetFlag($"hazard_disabled_{hazard.GetHazardId()}");
                
                if (hazard != null && hazard.gameObject != null)
                {
                    Destroy(hazard.gameObject);
                }
            }
        }

        SaveSystem.Instance.SetFlag($"hazard_disabled_{hazardControllerId}");
        Destroy(gameObject);
    }

    public void TakeDamage(float amount, Vector2 hitFromPosition)
    {
        TakeDamage(amount);
    }

    public void TakeDamage(float amount, bool bypassInvulnerability = false)
    {
        TakeDamage(amount);
    }

    void Awake()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag($"hazard_disabled_{hazardControllerId}"))
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
