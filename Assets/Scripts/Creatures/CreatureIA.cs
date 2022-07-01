using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CreatureIA : MonoBehaviour
{
    //Change when all the code works. Change the value of radius in the editor.
    [SerializeField] float walkRadius = 25;
    [SerializeField] float sightRadius = 10;
    [SerializeField] float waitTime = 2;

    [SerializeField] float stoppingDistance;
    [SerializeField] float baseVelocity;
    [SerializeField] float rammingVelocity;

    Collider[] hitColliders;
    [SerializeField] LayerMask layerPlayer;
    public bool isRamming = false;
    Vector3 rammingPoint;
    NavMeshAgent navAgent;

    public GameObject parent;

    private GameController _gameController;

    void Start()
    {
        if(!this.GetComponent<CreatureController>().isBoss)
            parent = transform.parent.gameObject;

        navAgent = GetComponent<NavMeshAgent>();
        _gameController = FindObjectOfType<GameController>();

        StartPatrolState();
    }

    void Update()
    {
        SightSense();
    }

    void StartPatrolState()
    {
        navAgent.isStopped = false;
        navAgent.speed = baseVelocity;
        navAgent.SetDestination(GetRandomPointInNavmesh());
        StartCoroutine(SetNewTargetPos());
    }

    void SightSense()
    {
        hitColliders = Physics.OverlapSphere(transform.position, sightRadius, layerPlayer);

        if (hitColliders.Length > 0)
        {
            if (!hitColliders[0].gameObject.GetComponent<Movement>().inBattle)
            {
                if (!isRamming)
                {
                    StopAllCoroutines();
                    isRamming = true;
                    rammingPoint = hitColliders[0].gameObject.transform.position;
                    StartCoroutine(SeekPlayer(rammingPoint));
                }
            }
        }

        else
        {
            isRamming = false;
        }
    }

    IEnumerator SetNewTargetPos()
    {
        yield return new WaitUntil(() => navAgent.remainingDistance <= stoppingDistance);

        navAgent.SetDestination(GetRandomPointInNavmesh());
        StartCoroutine(SetNewTargetPos());
    }

    Vector3 GetRandomPointInNavmesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1);
        return hit.position;
    }

    IEnumerator SeekPlayer(Vector3 target)
    {
        transform.LookAt(target);

        navAgent.SetDestination(target);
        navAgent.speed = rammingVelocity;

        yield return new WaitUntil(() => navAgent.remainingDistance <= stoppingDistance);
        navAgent.isStopped = true;
        yield return new WaitForSeconds(waitTime);
        navAgent.isStopped = false;

        if (hitColliders.Length > 0)
        {
            rammingPoint = hitColliders[0].gameObject.transform.position;
            StartCoroutine(SeekPlayer(rammingPoint));
        }

        else
        {
            isRamming = false;
            StartPatrolState();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (!other.GetComponent<Movement>().inBattle)
            {
                navAgent.isStopped = true;

                other.GetComponent<Movement>().fakePlayer.GetComponent<Animator>().SetTrigger("surprised");
                other.GetComponent<Movement>().anim.SetTrigger("surprised");
                other.GetComponent<Movement>().inBattle = true;

                StartCoroutine(WaitTimeToTransition());
            }

            else
            {
                isRamming = false;
                StartPatrolState();
            }
        }
    }

    IEnumerator WaitTimeToTransition()
    {
        yield return new WaitForSeconds(1.0f);

        _gameController.StartBattle(this.gameObject);

        this.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, walkRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
    }
}
