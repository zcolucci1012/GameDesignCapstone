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

    public float attackDistance = 2f;
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


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        //anim.fireEvents = false;
        player = GameObject.FindGameObjectWithTag("Player");
        randomInterval = Random.Range(2.0f, 4.0f);

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

        agent.SetDestination(nextDestination);
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
    }

    void UpdateChaseState()
    {
        anim.SetInteger("animState", 2);

        nextDestination = player.transform.position;

        agent.stoppingDistance = attackDistance - 0.5f;
        agent.speed = enemySpeed + 1;

        if (distanceToPlayer <= attackDistance)
        {
            currentState = FSMStates.Attack;
            attackType = Random.Range(3, 5);
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

        if (/*attackTime > 2 && */distanceToPlayer > attackDistance && distanceToPlayer <= chaseDistance)
        {
            currentState = FSMStates.Chase;
            attackTime = 0;
        }

        //attackTime += Time.deltaTime;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        directionToTarget.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10 * Time.deltaTime);
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

        if (tries == 15) { print("hmmm"); }
        else { nextDestination = nextPoint; }
    }

    bool PlayerInFOV()
    {
        RaycastHit hit;
        Vector3 directionToPlayer = player.transform.position - enemyEyes.transform.position;

        if (Vector3.Angle(directionToPlayer, enemyEyes.transform.forward) <= fov)
        {
            if (Physics.Raycast(enemyEyes.transform.position, directionToPlayer, out hit, chaseDistance))
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
        if (distanceToPlayer <= attackDistance)
        {
            Debug.Log("damage player");
        }
    }

    public void EndAttack()
    {
        attackType = Random.Range(3, 5);
    }

    public void OnFootstep()
    {
        //nothing
    }
}
