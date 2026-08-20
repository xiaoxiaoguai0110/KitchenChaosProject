using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    private const string SOUNDMANAGER_VOLUME = "SoundManagerVolume";

    [SerializeField]private AudioClipRefsSO audioClipRefsSO;

    private int volume = 5;
    private OrderManager subscribedOrderManager;
    private bool subscribedToStaticEvents;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        LoadVolume();
    }
    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void SubscribeToEvents()
    {
        // 静态事件不需要等待场景单例；实例事件则在 Start 再补一次，兼容 Awake 顺序。
        if (!subscribedToStaticEvents)
        {
            CuttingCounter.OnCut += CuttingCounter_OnCut;
            KitchenObjectHolder.OnDrop += KitchenObjectHolder_OnDrop;
            KitchenObjectHolder.OnPickup += KitchenObjectHolder_OnPickup;
            TrashCounter.OnObjectTrashed += TrashCounter_OnObjectTrashed;
            subscribedToStaticEvents = true;
        }

        if (subscribedOrderManager == null && OrderManager.Instance != null)
        {
            subscribedOrderManager = OrderManager.Instance;
            subscribedOrderManager.OnRecipeSuccessed += OrderManager_OnRecipeSuccessed;
            subscribedOrderManager.OnRecipeFailed += OrderManager_OnRecipeFailed;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (subscribedToStaticEvents)
        {
            CuttingCounter.OnCut -= CuttingCounter_OnCut;
            KitchenObjectHolder.OnDrop -= KitchenObjectHolder_OnDrop;
            KitchenObjectHolder.OnPickup -= KitchenObjectHolder_OnPickup;
            TrashCounter.OnObjectTrashed -= TrashCounter_OnObjectTrashed;
            subscribedToStaticEvents = false;
        }

        if (subscribedOrderManager != null)
        {
            subscribedOrderManager.OnRecipeSuccessed -= OrderManager_OnRecipeSuccessed;
            subscribedOrderManager.OnRecipeFailed -= OrderManager_OnRecipeFailed;
            subscribedOrderManager = null;
        }
    }

    private void TrashCounter_OnObjectTrashed(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trash);
    }

    private void KitchenObjectHolder_OnPickup(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.objectPickup);
    }

    private void KitchenObjectHolder_OnDrop(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.objectDrop);
    }

    private void CuttingCounter_OnCut(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.chop);
    }

    private void OrderManager_OnRecipeFailed(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.deliveryFail);
    }

    private void OrderManager_OnRecipeSuccessed(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.deliverySuccess);
    }

    public void PlayWarningSound()
    {
        PlaySound(audioClipRefsSO.warning);
    }
    public void PlayCountDownSound()
    {
        PlaySound(audioClipRefsSO.warning);
    }
    private void PlaySound(AudioClip[] clips, float volumeMutipler = 1.0f)
    {
        PlaySound(clips, Camera.main.transform.position, volumeMutipler);
    }

    private void PlaySound(AudioClip[] clips,Vector3 position,float volumeMutipler = 1.0f)
    {
        if(volume == 0)
        {
            return;
        }
        int index = Random.Range(0,clips.Length);
        AudioSource.PlayClipAtPoint(clips[index],position, volumeMutipler*(volume / 10.0f));
    }

    public void PlayStepSound(float volumeMutipler = 1.0f)
    {
        PlaySound(audioClipRefsSO.footstep, volumeMutipler);
    }

    public void ChangeVolume()
    {
        volume++;
        if (volume > 10) 
        {
            volume = 0;
        }
        SaveVolume();
    }

    public int GetVolume()
    {
        return volume;
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetInt(SOUNDMANAGER_VOLUME,volume);
    }
    public void LoadVolume()
    {
        volume = PlayerPrefs.GetInt(SOUNDMANAGER_VOLUME,volume);
    }

}
