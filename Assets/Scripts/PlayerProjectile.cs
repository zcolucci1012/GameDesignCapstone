using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Robot")
        {
            collider.GetComponent<RobotAI>().EnemyHit(20, 0, this.transform.forward, false);
            Destroy(this.gameObject);
        }
        else if (collider.tag != "Player")
        {
            Destroy(this.gameObject);
        }
    }
}
