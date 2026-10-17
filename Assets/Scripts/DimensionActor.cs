using UnityEngine;

// The anchor stays active so a hidden enemy can be found on the return shift.
public class DimensionActor : MonoBehaviour
{
    [Range(0,1)] public int dimension;
    public GameObject content;
    void Start() { Apply(DimensionController.Instance != null ? DimensionController.Instance.Current : 0); }
    public void Apply(int current)
    {
        if(content == null) return;
        bool show = current == dimension;
        if(content.activeSelf == show)return;
        var body=content.GetComponent<Rigidbody2D>();
        if(body != null && body.simulated)body.linearVelocity=Vector2.zero;
        content.SetActive(show);
    }
}
