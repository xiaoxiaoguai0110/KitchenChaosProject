using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private const string MUSICMANAGER_VOLUME = "MusicManagerVolume";

    private AudioSource audioSource;
    private float originalVolume;
    private int volume = 5;

    private void Awake()
    {
        Instance = this;
        LoadVolume();
    }

    void Start()
    {
        audioSource  = GetComponent<AudioSource>();
        originalVolume = audioSource.volume;
        UpdateVolume();
    }

    public void ChangeVolume()
    {
        volume++;
        if(volume > 10)
        {
            volume = 0;
        }
        SaveVolume();
        UpdateVolume();
    } 

    private void UpdateVolume()
    {
        if(volume == 0)
        {
            audioSource.enabled = false;
        }
        else
        {
            audioSource.enabled = true;
            audioSource.volume = originalVolume * (volume / 10.0f);
        }
    }

    public int GetVolume()
    {
        return volume;
    }

    private void SaveVolume()
    {
        PlayerPrefs.SetInt(MUSICMANAGER_VOLUME, volume);
    }
    private void LoadVolume()
    {
        volume = PlayerPrefs.GetInt(MUSICMANAGER_VOLUME,volume);
    }

}
