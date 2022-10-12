using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDamage : MonoBehaviour
{
    int hp = 100;
    CharacterController cc;
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayerHit(int damage, float knockback, Vector3 knockbackDirection)
    {
        hp -= damage;
        if (hp <= 0)
        {
            Invoke("Die", 0.7f);
        }

        //Debug.Log(knockback * knockbackDirection);
        cc.Move(knockbackDirection * knockback);

        anim.SetBool("GotHit", true);
        Invoke("ResetGotHit", 0.05f);
    }

    void ResetGotHit()
    {
        anim.SetBool("GotHit", false);
    }

    void Die()
    {
        Destroy(this.gameObject);
        SceneManager.LoadScene(0);
    }

}
