using UnityEngine;
public class HollowHazard:MonoBehaviour
{
 void OnCollisionEnter2D(Collision2D c)=>Hit(c.collider);
 void OnCollisionStay2D(Collision2D c)=>Hit(c.collider);
 void OnTriggerEnter2D(Collider2D c)=>Hit(c);
 void Hit(Collider2D c){var p=c.GetComponentInParent<PlayerHealth>();if(p!=null)p.TryTakeDamage(20);}
}
