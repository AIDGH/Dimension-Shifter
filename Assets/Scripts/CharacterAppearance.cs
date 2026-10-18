using UnityEngine;
[DefaultExecutionOrder(50)]
public class CharacterAppearance : MonoBehaviour
{
 public RuntimeAnimatorController redController,knightController;
 public Sprite redIdle,knightIdle;
 public bool useKnight=true;
 public string Label=>useKnight?"KNIGHT":"RED";
 void Start(){Apply(useKnight);}
 void Update(){if(Input.GetKeyDown(KeyCode.H))Toggle();}
 public void Toggle(){if(GetComponent<PlayerAttack>().IsDashing)return;Apply(!useKnight);}
 public void Apply(bool knight){if(redController==null||knightController==null)return;useKnight=knight;
 var a=GetComponent<Animator>();a.runtimeAnimatorController=knight?knightController:redController;
 var sr=GetComponent<SpriteRenderer>();sr.sprite=knight?knightIdle:redIdle;sr.flipX=knight;
 var m=GetComponent<PlayerMovement>();m.spriteFacesRight=!knight;m.alignToSlope=!knight;
 }
}
