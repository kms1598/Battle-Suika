using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioClip[] sfxClips;
    int sfxIndex;
    const int channels = 20;
    AudioSource[] sfxPlayers;
    public enum Sfx { Click, Drop, LevelUp, SkillGauge, Skill1, Skill2, GameOver}

    void Awake()
    {
        if(instance != this && instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }


        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for(int i = 0; i < channels; i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake = false;
            sfxPlayers[i].volume = 1;
        }
    }

    public void PlayerSfx(Sfx sfx)
    {
        for(int i = 0; i < channels; i++)
        {
            int loopIndex = (i + sfxIndex) % channels;

            if (sfxPlayers[loopIndex].isPlaying)
            {
                continue;
            }

            sfxIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }
}
