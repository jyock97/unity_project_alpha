using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public enum GameState
    {
        UI,
        GAMEPLAY
    }

    public GameState gameState;

    [SerializeField] private float blackoutWaitTime;

    private CreaturesManager _creaturesManager;
    private CameraTransition _cameraTransition;
    private BattleStageController _battleStageController;
    private CreatureGenerator _creatureGenerator;
    private int _battleCurrentPlayerCreatures;
    private int _battleCurrentEnemyCreatures;

    private GameObject player;

    private void Awake()
    {
        _creaturesManager = FindObjectOfType<CreaturesManager>();
        _cameraTransition = FindObjectOfType<CameraTransition>();
        _battleStageController = FindObjectOfType<BattleStageController>();
        _creatureGenerator = FindObjectOfType<CreatureGenerator>();

        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Start()
    {
        gameState = GameState.GAMEPLAY;
        _battleStageController.InitializeBattleStage();
        _creaturesManager.InitPlayerCreatures();
    }

    public void StartBattle(GameObject preCreature)
    {
        _creatureGenerator.previousCreature = preCreature;
        StartCoroutine(IE_StartBattle());
    }

    private IEnumerator IE_StartBattle()
    {
        // turn off player input
        // camera transition black
        _cameraTransition.FipBlackout();
        yield return new WaitForSeconds(blackoutWaitTime);
        // spawn enemies
        _battleCurrentEnemyCreatures = _creatureGenerator.GenerateCreatures();
        // camera transition 
        _cameraTransition.FlipCameras();
        _cameraTransition.FipBlackout();
        // simulate battle
        foreach (CreatureController creatureController in _creaturesManager.playerCreatures)
        {
            creatureController.StartBehaviour();
        }
        foreach (CreatureController creatureController in _creaturesManager.enemyCreatures)
        {
            creatureController.StartBehaviour();
        }
    }

    public void PlayerCreatureDefeated()
    {
        _battleCurrentPlayerCreatures--;
        if (_battleCurrentPlayerCreatures <= 0)
        {
            StartCoroutine(EndBattle());
            player.GetComponent<Movement>().anim.SetInteger("battleWon", 0);
            player.GetComponent<Movement>().fakePlayer.GetComponent<Animator>().SetInteger("battleWon", 0);
        }
    }

    public void EnemyCreatureDefeated()
    {
        _battleCurrentEnemyCreatures--;
        if (_battleCurrentEnemyCreatures <= 0)
        {
            StartCoroutine(EndBattle());
            player.GetComponent<Movement>().anim.SetInteger("battleWon", 1);
            player.GetComponent<Movement>().fakePlayer.GetComponent<Animator>().SetInteger("battleWon", 1);
        }
    }


    private IEnumerator EndBattle()
    {
        // stop creature behaviour
        foreach (CreatureController creatureController in _creaturesManager.playerCreatures)
        {
            creatureController.EndBehaviour();
        }
        foreach (CreatureController creatureController in _creaturesManager.enemyCreatures)
        {
            creatureController.EndBehaviour();
        }

        // collect Items
        yield return new WaitForSeconds(3); // TODO make this a variable value

        //take off player of the battle
        player.GetComponent<Movement>().inBattle = false;
        player.GetComponent<Movement>().anim.SetInteger("battleWon", -1);
        player.GetComponent<Movement>().fakePlayer.GetComponent<Animator>().SetInteger("battleWon", -1);

        // camera transition black
        _cameraTransition.FipBlackout();
        yield return new WaitForSeconds(blackoutWaitTime);

        // camera transition 
        _cameraTransition.FlipCameras();
        _cameraTransition.FipBlackout();

        if (_creatureGenerator.previousCreature.GetComponent<CreatureController>().isBoss)
        {
            Destroy(_creatureGenerator.previousCreature);
            //TODO: when the Boss dies, the game is finished.
        }
        else
        {
            GameObject previousSpawner = _creatureGenerator.previousCreature.GetComponent<CreatureIA>().parent;
            if (previousSpawner.GetComponent<CreatureSpawner>())
                previousSpawner.GetComponent<CreatureSpawner>().deleteCreature();
        }
    }
}
