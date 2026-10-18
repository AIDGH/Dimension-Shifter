using UnityEngine;

public class WorldMusic : MonoBehaviour
{
    [Range(0,1)] public float volume=.24f;
    AudioSource frost,echo;
    public bool Muted { get; private set; }
    void Start()
    {
        frost=Create("Frost");echo=Create("Echo");
        double start=AudioSettings.dspTime+.1;
        if(frost.clip!=null)frost.PlayScheduled(start);
        if(echo.clip!=null)echo.PlayScheduled(start);
    }
    AudioSource Create(string clip)
    {var a=gameObject.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=true;a.spatialBlend=0;a.volume=0;a.clip=Resources.Load<AudioClip>("Music/"+clip);return a;}
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))Muted=!Muted;
        if(frost==null)return;
        bool other=DimensionController.Instance!=null&&DimensionController.Instance.Current==1;
        float mix=volume*GameAudio.MusicMix;
        frost.volume=Mathf.MoveTowards(frost.volume,Muted||other?0:mix,Time.unscaledDeltaTime*.6f);
        echo.volume=Mathf.MoveTowards(echo.volume,Muted||!other?0:mix,Time.unscaledDeltaTime*.6f);
    }
}
