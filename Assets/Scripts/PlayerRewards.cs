using UnityEngine;

public class PlayerRewards : MonoBehaviour
{
    public int Score { get; private set; }
    public int Experience { get; private set; }
    public int Level => 1 + Experience / 100;
    // Modest milestones keep early enemies readable and cannot grow without limit.
    public int DamageBonus => Mathf.Min(3, (Level - 1) / 2);
    public float DashCooldownReduction => Mathf.Min(.6f, (Level - 1) * .15f);
    public float LevelProgress => (Experience % 100) / 100f;
    public int Defeated { get; private set; }
    public int Relics { get; private set; }
    public void EnemyDefeated(int points, int xp)
    { Defeated++; Gain(points,xp); }
    public void Collect(int points,int xp) { Relics++; Gain(points,xp); GameAudio.Play("pickup",.5f); }
    void Gain(int points,int xp)
    {
        int before=Level;Score+=Mathf.Max(0,points);Experience+=Mathf.Max(0,xp);
        if(Level>before)
        {
            // Healing and capped combat growth are per run; respawning does not multiply them.
            GetComponent<PlayerHealth>().Heal(20);
            var run=GetComponent<RunProgress>();if(run!=null)run.ShowMessage("Resonance "+Level+"  •  +20 vitality  •  Blade/Cast +"+DamageBonus+"  •  Dash cooldown −"+DashCooldownReduction.ToString("F2")+"s");
            GameAudio.Play("checkpoint",.45f);
        }
    }
}
