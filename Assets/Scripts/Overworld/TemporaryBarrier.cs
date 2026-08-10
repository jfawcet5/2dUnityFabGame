using System.Collections;
using System.Collections.Generic;
using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Overworld
{
    public class TemporaryBarrier : MonoBehaviour
    {
        [SerializeField] private string RequiredEventFlag;

        void Awake()
        {
            if (!string.IsNullOrEmpty(RequiredEventFlag) && SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(RequiredEventFlag))
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            
        }

        void Update()
        {
            
        }
    }
}
