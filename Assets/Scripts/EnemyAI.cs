using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum FSMStates
    {
        Patrol,
        Chase,
        Attack,
    }

    public FSMStates currentState;

    public float attackDistance = 1.25f;
    public float chaseDistance = 6f;
    public float enemySpeed = 1f;
    Vector3 nextDestination;
    float distanceToPlayer;
    float fov = 100;
    float elapsedTime = 0;
    float randomInterval = 3.0f;
    float attackTime = 0;
    int attackType = 3;

    public GameObject player;
    public GameObject enemyEyes;
    NavMeshAgent agent;
    Animator anim;
    Rigidbody rb;

    bool canMove = true;
    int hp = 10;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        //anim.fireEvents = false;
        player = GameObject.FindGameObjectWithTag("Player");
        randomInterval = Random.Range(2.0f, 4.0f);
        rb = GetComponent<Rigidbody>();

        currentState = FSMStates.Patrol;
        Invoke("FindNextPoint", 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        switch (currentState)
        {
            case FSMStates.Patrol:
                UpdatePatrolState();
                break;
            case FSMStates.Chase:
                UpdateChaseState();
                break;
            case FSMStates.Attack:
                UpdateAttackState();
                break;
        }

        if (canMove && agent.enabled)
        {
            agent.SetDestination(nextDestination);
        }
        FaceTarget(nextDestination);

        elapsedTime += Time.deltaTime;
    }

    void UpdatePatrolState()
    {
        anim.SetInteger("animState", 1);

        agent.stoppingDistance = 0;
        agent.speed = enemySpeed;

        if (elapsedTime > randomInterval)
        {
            randomInterval = Random.Range(2.0f, 4.0f);
            elapsedTime = 0;
            FindNextPoint();
        } else if (distanceToPlayer <= chaseDistance && PlayerInFOV())
        {
            currentState = FSMStates.Chase;
        } else if (distanceToPlayer <= attackDistance && PlayerInFOV())
        {
            currentState = FSMStates.Attack;
        }

        Debug.DrawLine(this.transform.position, nextDestination, Color.red);
    }

    void UpdateChaseState()
    {
        anim.SetInteger("animState", 2);

        nextDestination = player.transform.position;
        agent.speed = enemySpeed + 2;

        if (distanceToPlayer <= attackDistance)
        {
            currentState = FSMStates.Attack;
            attackType = Random.Range(3, 4); // change to 5 for kick
        }
        else if (distanceToPlayer > chaseDistance)
        {
            FindNextPoint();
            currentState = FSMStates.Patrol;
        }
    }

    void UpdateAttackState()
    {
        anim.SetInteger("animState", attackType);

        nextDestination = player.transform.position;
        
        agent.speed = 0;

        if (attackTime > 1 && distanceToPlayer > attackDistance && distanceToPlayer <= chaseDistance)
        {
            currentState = FSMStates.Chase;
            attackTime = 0;
        }

        attackTime += Time.deltaTime;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 directionToTarget;
        if (currentState == FSMStates.Patrol)
        {
            directionToTarget = agent.desiredVelocity.normalized;
        }
        else
        {
            directionToTarget = (target - transform.position).normalized;
        }
        
        directionToTarget.y = 0;
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10 * Time.deltaTime);
        }   
    }

    void FindNextPoint()
    {
        RaycastHit hit;
        Vector3 nextPoint;
        int tries = 0;
        bool loop = false;

        do
        {
            float angle = Random.Range(0, 360);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            nextPoint = this.transform.position + direction * 5;
            tries++;
            loop = !Physics.Raycast(nextPoint, Vector3.down, out hit, 100);
        } while ((loop ||
                !hit.collider.CompareTag("Floor")) &&
                tries < 15);

        if (tries == 15) { Debug.Log("hmmm"); }
        else { nextDestination = nextPoint; }
    }

    bool PlayerInFOV()
    {
        RaycastHit hit;
        Vector3 eyesPosition = enemyEyes.transform.position;
        eyesPosition.y += 1f;
        Vector3 playerPosition = player.transform.position;
        playerPosition.y += 0.5f;
        Vector3 directionToPlayer = playerPosition - eyesPosition;
        //Debug.DrawLine(playerPosition, eyesPosition);

        if (Vector3.Angle(directionToPlayer, enemyEyes.transform.forward) <= fov)
        {
            if (Physics.Raycast(eyesPosition, directionToPlayer, out hit, chaseDistance))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    print("Player in sight");
                    return true;
                }
            }
        }

        return false;
    }

    public void Attack()
    {
        //Vector3 directionToPlayer = (player.transform.position - this.transform.position).normalized;
        //directionToPlayer.y = 0;
        if (distanceToPlayer <= attackDistance)
        {
            //Debug.Log("damage player");
            player.GetComponent<PlayerDamage>().PlayerHit(10, 0.25f, this.transform.forward);
        }
    }

    public void EndAttack()
    {
        attackType = Random.Range(3, 4); // change to 5 for kick
    }

    public void OnFootstep()
    {
        //nothing
    }

    public void EnemyHit(int damage, float knockback, Vector3 knockbackDirection)
    {
        hp -= damage;
        if (hp <= 0)
        {
            Invoke("Die", .7f);
        }
        //agent.enabled = false;
        //rb.isKinematic = false;
        //rb.AddForce(100 * knockback * knockbackDirection);
        currentState = FSMStates.Chase;

        anim.SetBool("GotHit", true);
        Invoke("ResetGotHit", 0.05f);
    }

    void ResetGotHit()
    {
        anim.SetBool("GotHit", false);
    }

    void CanMove()
    {
        canMove = true;
        //agent.enabled = true;
        //rb.isKinematic = true;
    }

    void CantMove()
    {
        canMove = false;
        agent.SetDestination(this.transform.position);
        //agent.enabled = false;
    }

    void Die()
    {
        Destroy(this.gameObject);
    }

}
