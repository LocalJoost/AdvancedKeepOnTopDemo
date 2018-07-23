using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideInRuntime : MonoBehaviour
{
    void Start()
    {
#if !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }
}


