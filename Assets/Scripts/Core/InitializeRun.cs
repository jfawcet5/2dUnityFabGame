using System.Collections;
using System.Collections.Generic;
using BeyProject.Core;
using UnityEngine;

public class InitializeRun : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance?.EndRun();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
