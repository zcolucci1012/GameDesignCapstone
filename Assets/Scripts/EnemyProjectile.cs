using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            collider.GetComponent<PlayerDamage>().PlayerHit(10, 0, this.transform.forward);
            Destroy(this);
        }
        else if (collider.tag != "Robot")
        {
            Destroy(this);
        }
    }
}
