using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;
    private enum State
    {
        WaitingToStart,
        CountDownToStart,
        GamePlaying,
        GameOver
    }

    [FormerlySerializedAs("player")]
    [SerializeField] private Player player1;
    [SerializeField] private Player player2;

    private State state;

    private float waitingToStartTimer = 1;
    private float countDownToStartTimer = 3;
    private float gamePlayingTimer = 60;
    private float gamePlayingTimeTotal;

    private bool isGamePause = false;
    private bool countDownTimerReachedZero;
    private bool gamePlayingTimerReachedZero;
    private AIPlayer aiPlayer;
    private GameInput subscribedInput;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    // Start is called before the first frame update
    private void Awake()
    {
        // 场景可能从暂停界面跳转而来；新一局开始前先恢复全局时间，避免整个场景被冻结。
        Time.timeScale = 1f;
        isGamePause = false;
        Instance = this;
        gamePlayingTimeTotal = gamePlayingTimer;
        aiPlayer = player2 != null ? player2.GetComponent<AIPlayer>() : null;
        ConfigureGameMode();
    }
    private void Start()
    {
        TurnToWaitingToStart();
        SubscribeToInput();
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // GameManager 销毁时不把暂停状态泄漏到下一个场景。
            Time.timeScale = 1f;
            isGamePause = false;
            Instance = null;
        }
    }

    private void SubscribeToInput()
    {
        if (subscribedInput != null || GameInput.Instance == null)
            return;

        subscribedInput = GameInput.Instance;
        subscribedInput.OnPauseAction += GameInput_OnPauseAction;
    }

    private void UnsubscribeFromInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnPauseAction -= GameInput_OnPauseAction;
        subscribedInput = null;
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        ToggleGame();
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.WaitingToStart:
                waitingToStartTimer -= Time.deltaTime;
                if (waitingToStartTimer <= 0)
                {
                    TurnToCountDownToStart();
                }
                break;
            case State.CountDownToStart:
                countDownToStartTimer -= Time.deltaTime;
                if (countDownToStartTimer <= 0)
                {
                    if (countDownTimerReachedZero)
                    {
                        TurnToGamePlaying();
                        countDownTimerReachedZero = false;
                    }
                    else
                    {
                        countDownToStartTimer = 0;
                        countDownTimerReachedZero = true;
                    }
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer -= Time.deltaTime;
                if (gamePlayingTimer <= 0)
                {
                    if (gamePlayingTimerReachedZero)
                    {
                        TurnToGameOver();
                        gamePlayingTimerReachedZero = false;
                    }
                    else
                    {
                        gamePlayingTimer = 0;
                        gamePlayingTimerReachedZero = true;
                    }
                }
                break;
            case State.GameOver:
                break;
            default:
                break;
        }
    }


    private void TurnToWaitingToStart()
    {
        state = State.WaitingToStart;
        DisablePlayer();
        OnStateChanged?.Invoke(this,EventArgs.Empty);
    }
    private void TurnToCountDownToStart()
    {
        state = State.CountDownToStart;
        DisablePlayer();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    private void TurnToGamePlaying()
    {
        state = State.GamePlaying;
        EnablePlayer();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    private void TurnToGameOver()
    {
        // 正常流程不会在暂停时进入结算，这里仍主动恢复，保证异常切换也不会冻结结算 UI。
        Time.timeScale = 1f;
        isGamePause = false;
        state = State.GameOver;
        DisablePlayer();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DisablePlayer()
    {
        if (player1 != null)
            player1.enabled = false;
        if (player2 != null)
            player2.enabled = false;
    }
    private void EnablePlayer()
    {
        if (player1 != null)
            player1.enabled = true;
        if (player2 != null)
            player2.enabled = GameModeSelection.Current == GameMode.LocalMultiplayer;
    }

    private void ConfigureGameMode()
    {
        bool usesSecondPlayer = GameModeSelection.Current != GameMode.SinglePlayer;

        if (player2 != null)
            player2.gameObject.SetActive(usesSecondPlayer);

        if (aiPlayer != null)
            aiPlayer.enabled = GameModeSelection.Current == GameMode.SinglePlayerWithAI;
    }
    public bool IsWaitingToStartState()
    {
        return state == State.WaitingToStart;
    }
    public bool IsCountDownState()
    {
        return state == State.CountDownToStart; 
    }
    public bool IsGamePlayingState()
    {
        return state == State.GamePlaying;
    }

    /// <summary>
    /// 只有正式游玩且未暂停时，订单、加工、角色等玩法模拟才允许继续推进。
    /// </summary>
    public bool IsGameSimulationRunning()
    {
        return state == State.GamePlaying && !isGamePause;
    }
    public bool IsGameOverState()
    {
        return state == State.GameOver;
    }
    public float GetCountDownTimer()
    {
        return countDownToStartTimer;
    }

    public void ToggleGame()
    {
        // 倒计时和结算界面不允许暂停，否则可能把 Time.timeScale=0 带进下一局。
        // 暂停时 state 仍是 GamePlaying，所以再次按键仍能正常恢复。
        if (!IsGamePlayingState())
            return;

        isGamePause = !isGamePause;
        if (isGamePause) 
        {
            Time.timeScale = 0;
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1;
            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    public float GetGamePlayingTimer()
    {
        return gamePlayingTimer;
    }
    public float GetGamePlayingTimerNormalized()
    {
        return gamePlayingTimer / gamePlayingTimeTotal;
    }

}
