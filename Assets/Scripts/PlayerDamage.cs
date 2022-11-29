using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerDamage : MonoBehaviour
{
    int hp = 100;
    CharacterController cc;
    Animator anim;
    StarterAssets.ThirdPersonController tpc;
    bool invincible = false;
    public Slider healthBar;

    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        tpc = GetComponent<StarterAssets.ThirdPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        invincible = tpc.GetInvincible();
        healthBar.value = hp;
    }

    public void PlayerHit(int damage, float knockback, Vector3 knockbackDirection)
    {
        if (invincible) return;
        hp -= damage;
        if (hp <= 0)
        {
            Invoke("Die", 0.7f);
        }

        //Debug.Log(knockback * knockbackDirection);
        cc.Move(knockbackDirection * knockback);

        Debug.Log("damaged for " + damage);
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
