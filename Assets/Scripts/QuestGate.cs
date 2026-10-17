using UnityEngine;
public class QuestGate : MonoBehaviour
{
    public enum Requirement { Spirit, Relays, Trial, Boss }
    public Requirement requirement;
    public Collider2D barrier;
    public Renderer glow;
    public bool Open { get; private set; }
    void Update()
    {
        var s=DemoSession.Instance;if(s==null)return;
        Open=requirement==Requirement.Spirit?s.HasSpirit:requirement==Requirement.Relays?s.Relays==3:requirement==Requirement.Trial?s.TrialCleared:s.BossDefeated;
        if(barrier!=null)barrier.enabled=!Open;
        if(glow!=null)glow.enabled=!Open;
    }
}
