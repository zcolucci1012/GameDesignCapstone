using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterAWhile : MonoBehaviour
{
    public float destroyDelay = 2f;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, destroyDelay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
