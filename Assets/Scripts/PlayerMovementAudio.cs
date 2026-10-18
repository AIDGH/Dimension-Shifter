using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerMovementAudio : MonoBehaviour
{
    public float stepDistance = .95f;
    PlayerMovement motor;PlayerAttack attack;PlayerHealth health;
    Vector3 previous;float walked,airTime;bool wasGrounded;
    void Awake(){motor=GetComponent<PlayerMovement>();attack=GetComponent<PlayerAttack>();health=GetComponent<PlayerHealth>();previous=transform.position;}
    void LateUpdate()
    {
        float distance=Vector2.Distance(transform.position,previous);previous=transform.position;
        if(health!=null&&health.IsDead)return;
        bool grounded=motor.isGrounded&&motor.enabled;
        if(!grounded)airTime+=Time.deltaTime;
        if(grounded&&!wasGrounded){if(airTime>.12f)GameAudio.Play("land",.32f);airTime=0;walked=0;}
        if(grounded && (attack==null||!attack.IsDashing) && distance<.5f)
        {
            walked+=distance;
            if(walked>=stepDistance){walked%=stepDistance;GameAudio.Play(StepCue(),.42f);}
        }
        else walked=0;
        wasGrounded=grounded;
    }
    string StepCue()
    {
        var hit=Physics2D.Raycast(transform.position,Vector2.down,1.2f,(1<<8)|(1<<9));
        var map=hit.collider!=null?hit.collider.GetComponent<Tilemap>():null;
        if(map!=null)
        {
            var sprite=map.GetSprite(map.WorldToCell(hit.point+Vector2.down*.03f));
            if(sprite!=null && int.TryParse(sprite.name.Substring(sprite.name.LastIndexOf('_')+1),out int id))
            {
                if(sprite.name.StartsWith("Snowy") && (id==124||id==125||id==143||id==144||id==154||id==157||id==158||id==160||id==161||id==162||id==163||id==168))return "step_wood";
            }
        }
        return SceneManager.GetActiveScene().name=="PaintedDimension"?"step_concrete":"step_snow";
    }
}
