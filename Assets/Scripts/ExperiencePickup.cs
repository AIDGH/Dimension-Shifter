using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    public int points=50, experience=20;
    [Tooltip("-1: both dimensions; 0: Frost; 1: Echo")] public int dimension=-1;
    public Transform visual;
    public bool Collected { get; private set; }
    Vector3 origin;
    Renderer[] renderers;
    Collider2D shape;
    void Awake(){origin=visual!=null?visual.localPosition:Vector3.zero;renderers=GetComponentsInChildren<Renderer>();shape=GetComponent<Collider2D>();}
    void Update()
    {
        bool present=!Collected&&(dimension<0||DimensionController.Instance==null||DimensionController.Instance.Current==dimension);
        foreach(var renderer in renderers)renderer.enabled=present;
        shape.enabled=present;
        if(visual!=null)visual.localPosition=origin+Vector3.up*(Mathf.Sin(Time.time*2.2f+transform.position.x)*.1f);
    }
    public bool TryCollect(PlayerRewards rewards)
    {
        if(Collected||rewards==null||rewards.GetComponent<PlayerHealth>().IsDead)return false;
        if(dimension>=0&&DimensionController.Instance!=null&&DimensionController.Instance.Current!=dimension)return false;
        Collected=true;rewards.Collect(points,experience);gameObject.SetActive(false);return true;
    }
    void OnTriggerEnter2D(Collider2D other)=>TryCollect(other.GetComponentInParent<PlayerRewards>());
    void OnTriggerStay2D(Collider2D other)=>TryCollect(other.GetComponentInParent<PlayerRewards>());
}
