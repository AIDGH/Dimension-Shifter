using System.Collections.Generic;
using UnityEngine;
public class TrialArena : MonoBehaviour
{
    public GameObject slimePrefab,mushroomPrefab;
    public Transform[] spawnPoints;
    public Collider2D entrance;
    public float width=60;
    public int Wave { get; private set; }
    public bool Active { get; private set; }
    readonly List<GameObject> enemies=new List<GameObject>();
    float nextWave;
    public string Status
    {
        get
        {
            int frost=0,echo=0;
            foreach(var g in enemies){if(g==null)continue;var health=g.GetComponentInChildren<EnemyHealth>(true);if(health==null||health.IsDead)continue;if(g.GetComponent<DimensionActor>().dimension==0)frost++;else echo++;}
            return "WAVE "+Wave+" / 3  ·  FROST "+frost+"  ·  ECHO "+echo+" remaining";
        }
    }
    void Start(){if(entrance!=null)entrance.enabled=false;}
    void Update()
    {
        var s=DemoSession.Instance;if(s==null||!s.Started||s.TrialCleared)return;
        if(s.GetComponent<PlayerHealth>().IsDead||(Active&&(s.transform.position.x<transform.position.x-2||s.transform.position.x>transform.position.x+width+2))){if(Active)ResetTrial();return;}
        if(!Active)
        {if(s.transform.position.x>transform.position.x+4&&s.transform.position.x<transform.position.x+width-4){Active=true;if(entrance!=null)entrance.enabled=true;nextWave=Time.time+1;}return;}
        enemies.RemoveAll(g=>g==null||g.GetComponentInChildren<EnemyHealth>(true)==null||g.GetComponentInChildren<EnemyHealth>(true).IsDead);
        if(enemies.Count>0||Time.time<nextWave)return;
        if(Wave>=3){s.TrialCleared=true;Active=false;if(entrance!=null)entrance.enabled=false;s.GetComponent<RunProgress>().ShowMessage("TRIAL COMPLETE · the Warden awaits.");s.GetComponent<PlayerHealth>().Heal(35);return;}
        Wave++;s.GetComponent<RunProgress>().ShowMessage("PROVING GROUNDS · wave "+Wave+" / 3. Enemies inhabit BOTH worlds; Q to find the survivors.");
        for(int i=0;i<Wave+2;i++)
        {
            int dimension=i%2;var anchor=new GameObject(dimension==0?"Trial frost enemy":"Trial echo enemy");anchor.transform.position=spawnPoints[i%spawnPoints.Length].position;
            var child=Instantiate(dimension==0?slimePrefab:mushroomPrefab,anchor.transform);child.transform.localPosition=dimension==0?Vector3.up*.48f:Vector3.zero;
            var member=anchor.AddComponent<DimensionActor>();member.dimension=dimension;member.content=child;member.Apply(DimensionController.Instance.Current);enemies.Add(anchor);
        }
        nextWave=Time.time+2;
    }
    void ResetTrial(){foreach(var g in enemies)if(g!=null)Destroy(g);enemies.Clear();Wave=0;Active=false;if(entrance!=null)entrance.enabled=false;}
}
