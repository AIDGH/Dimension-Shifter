using System.Collections.Generic;
using UnityEngine;

public class GameAudio : MonoBehaviour
{
    [Range(0f,1f)] public float masterVolume = .8f;
    static GameAudio instance;
    readonly Dictionary<string,List<AudioClip>> cues = new Dictionary<string,List<AudioClip>>();
    readonly System.Random variation = new System.Random();
    AudioSource[] voices;
    int cursor;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }
    void Awake()
    {
        foreach(var clip in Resources.LoadAll<AudioClip>("SFX"))
        {
            int split=clip.name.LastIndexOf('_');if(split<0)continue;
            string cue=clip.name.Substring(0,split);
            if(!cues.TryGetValue(cue,out var list))cues[cue]=list=new List<AudioClip>();list.Add(clip);
        }
        var replaced=new HashSet<string>();
        foreach(var clip in Resources.LoadAll<AudioClip>("HollowSFX"))
        {
            int split=clip.name.LastIndexOf('_');if(split<0)continue;string cue=clip.name.Substring(0,split);
            if(replaced.Add(cue))cues[cue]=new List<AudioClip>();cues[cue].Add(clip);
        }
        voices=new AudioSource[12];
        for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
    }
    public static void Warmup(){if(instance==null)instance=new GameObject("Game SFX").AddComponent<GameAudio>();}
    public static void Play(string cue,float volume=1f)
    {
        if(!Application.isPlaying)return;
        if(instance==null)instance=new GameObject("Game SFX").AddComponent<GameAudio>();
        instance.PlayCue(cue,volume);
        if(cue=="dash") instance.PlayCue("swing",volume*.55f);
    }
    void PlayCue(string cue,float volume)
    {
        if(!cues.TryGetValue(cue,out var clips)||clips.Count==0)return;
        AudioSource voice=null;
        foreach(var candidate in voices)if(!candidate.isPlaying){voice=candidate;break;}
        if(voice==null)voice=voices[cursor++%voices.Length];
        voice.pitch=.94f+(float)variation.NextDouble()*.12f;

        voice.clip=clips[variation.Next(clips.Count)];voice.volume=Mathf.Clamp01(volume*masterVolume);voice.Play();
    }
}
