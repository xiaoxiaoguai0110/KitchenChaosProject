using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : KitchenObjectHolder
{
    public static Player Instance { get; private set; }

    [SerializeField]private float moveSpeed = 5;
    [SerializeField]private float rotateSpeed = 10;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask counterLayerMask;
    private bool isWalking = false;
    private BaseCounter selectedCounter;
    // Start is called before the first frame update
    void Start()
    {
        gameInput.OnInteractAction += GameInput_OnInteractAction;
        gameInput.OnOperateAction += GameInput_OnOperateAction;
    }

    

    private void Awake()
    {
        Instance = this;
    }

    private void GameInput_OnOperateAction(object sender, System.EventArgs e)
    {
        selectedCounter?.InteractOperate(this);
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e)
    {
        selectedCounter?.Interact(this);
    }

    private void Update()
    {
        HandleInteraction();
    }
    //物理系统
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
    private void HandleMovement()
    {
        Vector3 direction = gameInput.GetMovementDirectionNormalized();
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
