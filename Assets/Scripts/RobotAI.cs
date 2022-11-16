using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RobotAI : MonoBehaviour
{
    public enum FSMStates
    {
        Patrol,
        Chase,
        Attack,
        Wait
    }

    public FSMStates currentState;

    public float attackDistance = 5;
    public float chaseDistance = 10;
    public float enemySpeed = 1f;
    public GameObject player;
    public GameObject bullet;
    public GameObject rifleEnd;
    public float fireRate = 2.0f;
    public GameObject enemyEyes;
    public float fov = 45f;
    public float health = 40;
    public AudioClip seePlayer;
    public AudioClip engaging;


    Vector3 nextDestination;
    Animator anim;
    float distanceToPlayer;
    float elapsedTime = 0;
    NavMeshAgent agent;
    AudioSource source;
    private Vector3 startPoint;
    private float findNextPointTimer = 0;

    private float playDistanceFromPlayer = 0.65f;
    int hp = 100;
    bool canMove = true;
    float reengageTimer = 0;
    float randomWaitTime = 0;
    float randomWaitTimer = 0;
    bool waitedAlready = false;
    Vector3 lastSeenPosition;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        source = GetComponent<AudioSource>();
        startPoint = this.transform.position;

        currentState = FSMStates.Patrol;
        Invoke("FindNextPoint", 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (!GetComponent<NavMeshAgent>().enabled) return;
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
            case FSMStates.Wait:
                UpdateWaitState();
                break;
        }

        FaceTarget(nextDestination);
        try
        {
            if (canMove)
            {
                agent.SetDestination(nextDestination);
            }
        }
        catch
        {

        }

        Debug.DrawLine(this.transform.position, nextDestination, Color.red);
    }

    void UpdatePatrolState()
    {
        findNextPointTimer += Time.deltaTime;
        //print("Patrolling");
        anim.SetInteger("animState", 1);

        agent.stoppingDistance = 0;
        agent.speed = enemySpeed;

        Vector3 groundPosition = transform.position;
        groundPosition.y = 0;

        if (Vector3.Distance(groundPosition, nextDestination) < 1 || findNextPointTimer > 4)
        {
            FindNextPoint();
            findNextPointTimer = 0;
        }
        else if (distanceToPlayer <= chaseDistance && PlayerInFOV())
        {
            var pos = Vector3.Lerp(transform.position, player.transform.position, playDistanceFromPlayer);
            AudioSource.PlayClipAtPoint(seePlayer, pos);
            currentState = FSMStates.Chase;
        }
        else if (distanceToPlayer <= attackDistance && PlayerInFOV())
        {
            var pos = Vector3.Lerp(transform.position, player.transform.position, playDistanceFromPlayer);
            AudioSource.PlayClipAtPoint(engaging, pos);
            currentState = FSMStates.Attack;
        }
    }

    void UpdateChaseState()
    {
        //print("Chasing");
        anim.SetInteger("animState", 1);

        nextDestination = player.transform.position;
        
        agent.stoppingDistance = 2;
        agent.speed = enemySpeed;

        if (distanceToPlayer <= attackDistance)
        {
            if (PlayerInFOV())
            {
                var pos = Vector3.Lerp(transform.position, player.transform.position, playDistanceFromPlayer);
                if (reengageTimer > 2.0f)
                {
                    AudioSource.PlayClipAtPoint(engaging, pos);
                    reengageTimer = 0;
                }
                currentState = FSMStates.Attack;
            }
            else if (!waitedAlready)
            {
                randomWaitTime = Random.Range(0.5f, 2f);
                randomWaitTimer = 0;
                lastSeenPosition = player.transform.position;
                currentState = FSMStates.Wait;
            }
        }
        else if (distanceToPlayer > chaseDistance)
        {
            FindNextPoint();
            currentState = FSMStates.Patrol;
        }

        reengageTimer += Time.deltaTime;
    }

    void UpdateAttackState()
    {
        //print("Attacking");
        anim.SetInteger("animState", 0);

        nextDestination = player.transform.position;

        agent.stoppingDistance = 0;
        agent.speed = 0;

        if (distanceToPlayer > attackDistance && distanceToPlayer <= chaseDistance)
        {
            currentState = FSMStates.Chase;
        }
        if (!PlayerInFOV())
        {
            currentState = FSMStates.Wait;
            randomWaitTimer = 0;
            randomWaitTime = Random.Range(0.5f, 2f);
            lastSeenPosition = player.transform.position;
        }

        EnemyShoot();
        elapsedTime += Time.deltaTime;
    }

    void UpdateWaitState()
    {
        anim.SetInteger("animState", 0);

        nextDestination = lastSeenPosition;

        agent.stoppingDistance = 0;
        agent.speed = 0;

        if (PlayerInFOV() && distanceToPlayer < attackDistance)
        {
            currentState = FSMStates.Attack;
            waitedAlready = false;
        }
        else if (PlayerInFOV() && distanceToPlayer > attackDistance && distanceToPlayer <= chaseDistance ||
            randomWaitTimer > randomWaitTime)
        {
            currentState = FSMStates.Chase;
            waitedAlready = true;
        }

        randomWaitTimer += Time.deltaTime;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        directionToTarget.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        if (currentState == FSMStates.Attack) //adjust for rifle end
        {
            lookRotation *= Quaternion.AngleAxis(Vector3.Angle(rifleEnd.transform.forward, transform.forward), Vector3.up);
        } 
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10 * Time.deltaTime);
    }

    void EnemyShoot()
    {
        if (elapsedTime >= fireRate)
        {
            Invoke("Shoot", 0.2f);
            elapsedTime = 0.0f;
        }
    }

    void Shoot()
    {
        if (currentState == FSMStates.Attack)
        {
            GameObject newBullet = Instantiate(bullet, rifleEnd.transform.position, rifleEnd.transform.rotation);
            Vector3 directionToPlayer = (player.transform.position + new Vector3(0, 1, 0) - rifleEnd.transform.position).normalized;
            newBullet.GetComponent<Rigidbody>().AddForce(directionToPlayer * 50, ForceMode.Impulse);
            source.Play();
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
            loop = !Physics.Raycast(nextPoint + new Vector3(0f, 2f, 0f), Vector3.down, out hit, 100);
        } while ((loop ||
                !hit.collider.CompareTag("Floor")) &&
                tries < 15);

        if (tries == 15) { Debug.Log("hmmm"); }
        else { nextDestination = nextPoint; }
    }

    bool PlayerInFOV()
    {
        RaycastHit hit;
        Vector3 directionToPlayer = player.transform.position + new Vector3(0, 0.5f, 0) - enemyEyes.transform.position;

        Debug.DrawLine(player.transform.position, enemyEyes.transform.position);

        if (Vector3.Angle(directionToPlayer, enemyEyes.transform.forward) <= fov)
        {
            if (Physics.Raycast(enemyEyes.transform.position, directionToPlayer, out hit, chaseDistance))
            {
                if (hit.collider.gameObject.tag == "Player")
                {
                    return true;
                }
            }
        }

        return false;
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
        elapsedTime = 0;

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
