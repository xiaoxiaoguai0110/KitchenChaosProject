using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : KitchenObjectHolder
{
    private const int MaxPlayers = 2;
    private static readonly Player[] s_Instances = new Player[MaxPlayers];

    /// <summary>??? 1?????????????????????</summary>
    public static Player Instance => s_Instances[0];

    [SerializeField] private int playerIndex;
    [SerializeField]private float moveSpeed = 5;
    [SerializeField]private float rotateSpeed = 10;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask counterLayerMask;
    private bool isWalking = false;
    private BaseCounter selectedCounter;
    private GameInput subscribedInput;

    private GameInput ResolvedInput => gameInput != null ? gameInput : GameInput.Instance;

    public static Player GetInstance(int index)
    {
        if (index < 0 || index >= MaxPlayers)
            return null;
        return s_Instances[index];
    }

    void Start()
    {
        subscribedInput = ResolvedInput;
        if (subscribedInput == null)
        {
            Debug.LogError("Player: ???? Inspector ??? GameInput??????????????? GameInput ?????");
            return;
        }
        subscribedInput.OnInteractAction += GameInput_OnInteractAction;
        subscribedInput.OnOperateAction += GameInput_OnOperateAction;
    }

    private void Awake()
    {
        if (playerIndex < 0 || playerIndex >= MaxPlayers)
        {
            Debug.LogError($"Player: playerIndex {playerIndex} ????????? 0??{MaxPlayers - 1}??");
            return;
        }
        if (s_Instances[playerIndex] != null)
            Debug.LogWarning($"Player: ????? playerIndex {playerIndex}??");
        s_Instances[playerIndex] = this;
    }

    private void OnDestroy()
    {
        if (subscribedInput != null)
        {
            subscribedInput.OnInteractAction -= GameInput_OnInteractAction;
            subscribedInput.OnOperateAction -= GameInput_OnOperateAction;
        }
        if (playerIndex >= 0 && playerIndex < MaxPlayers && s_Instances[playerIndex] == this)
            s_Instances[playerIndex] = null;
    }

    private void GameInput_OnOperateAction(object sender, GameInput.PlayerActionEventArgs e)
    {
        if (e.PlayerIndex != playerIndex)
            return;
        selectedCounter?.InteractOperate(this);
    }

    private void GameInput_OnInteractAction(object sender, GameInput.PlayerActionEventArgs e)
    {
        if (e.PlayerIndex != playerIndex)
            return;
        selectedCounter?.Interact(this);
    }

    private void Update()
    {
        HandleInteraction();
    }
    private void FixedUpdate()
    {
        HandleMovement();
    }
    public bool IsWalking
    {
        get
        {
            return isWalking;
        }
    }

    public void SetIsWalking(bool value)
    {
        isWalking = value;
    }
    private void HandleMovement()
    {
        GameInput input = ResolvedInput;
        if (input == null)
            return;
        Vector3 direction = input.GetMovementDirectionNormalized(playerIndex);
        isWalking = direction != Vector3.zero;
        transform.position += direction * moveSpeed * Time.deltaTime;
        if (direction != Vector3.zero)
        {
            transform.forward = Vector3.Slerp(transform.forward, direction, Time.deltaTime * (rotateSpeed));
        }
    }
    private void HandleInteraction()
    {
        RaycastHit hitinfo;
        bool isCollider = Physics.Raycast(transform.position, transform.forward, out hitinfo, 2.0f,counterLayerMask);
        if (isCollider) 
        {
            //print(hitinfo.collider.gameObject);
            if(hitinfo.transform.TryGetComponent<BaseCounter>(out BaseCounter counter))
            {
                //counter.Interact();
                SetSelectedCounter(counter);
            }
            else
            {
                SetSelectedCounter(null);
            }
        }
        else
        {
            SetSelectedCounter(null);
        }
    }
    public void SetSelectedCounter(BaseCounter counter)
    {
        if (counter != selectedCounter)
        {
            selectedCounter?.CancelSelect();
            counter?.SelectCounter();
            this.selectedCounter = counter;
        }
        
    }
}
