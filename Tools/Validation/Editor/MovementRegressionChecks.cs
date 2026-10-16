using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Copy to Assets/Editor in an isolated project; run with -executeMethod MovementRegressionChecks.Run.
public static class MovementRegressionChecks
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> results = new List<string>();
    static int failures;
    static PlayerMovement motor;
    static Rigidbody2D body;
    static PhysicsMaterial2D friction;

    public static void Run()
    {
        Physics2D.simulationMode = SimulationMode2D.Script;
        Check("flat ground: stable and upright", () => {
            Setup(); Floor(); Step(80);
            Require(motor.isGrounded, "Not grounded");
            Require(Mathf.Abs(body.position.y - 0.5f) < 0.015f, "Floats or sinks: " + body.position);
            Require(Mathf.Abs(body.rotation) < 0.01f, "Rotated");
        });
        Check("instant movement keeps configured speed", () => {
            Setup(); Floor(); Step(20); motor.useAcceleration = false; InputX(1); Step(1);
            Require(Mathf.Abs(body.linearVelocity.x - motor.moveSpeed) < 0.05f, "Wrong immediate speed");
            InputX(0); Step(1); Require(Mathf.Abs(body.linearVelocity.x) < 0.05f, "Did not stop");
        });
        Check("accelerated movement ramps and brakes", () => {
            Setup(); Floor(); Step(20); motor.useAcceleration = true; InputX(1); Step(1);
            Require(body.linearVelocity.x > 0 && body.linearVelocity.x < motor.moveSpeed, "No ramp");
            Step(30); Require(Mathf.Abs(body.linearVelocity.x-motor.moveSpeed) < 0.05f, "No top speed");
            InputX(0); Step(30); Require(Mathf.Abs(body.linearVelocity.x) < 0.05f, "No braking");
        });
        Check("walking off an edge restores falling", () => {
            Setup(); Box(new Vector2(0,-0.5f), new Vector2(2,1)); Step(20);
            motor.useAcceleration = false; InputX(1); Step(60);
            Require(!motor.isGrounded && body.position.y < -1, "Still floating at edge");
        });
        Check("wall is not ground; holding toward wall still falls", () => {
            Setup(new Vector2(0,4)); Box(new Vector2(1,0), new Vector2(1,100));
            motor.useAcceleration = false; InputX(1); Step(60);
            Require(!motor.isGrounded, "Wall counted as ground");
            Require(body.position.y < 0, "Stuck to wall: " + body.position);
            Require(body.position.x < 0.1f, "Passed wall: " + body.position);
        });
        Check("jump and one extra air jump, no unlimited jumps", () => {
            Setup(); Floor(); Step(20); Jump(); Step(1);
            Require(body.linearVelocity.y > 10f && !motor.isGrounded, "First jump failed");
            Step(12); Jump(); Step(1); Require(body.linearVelocity.y > 10f, "Air jump failed");
            Step(5); float before=body.linearVelocity.y; Jump(); Step(1);
            Require(body.linearVelocity.y < before, "Third jump incorrectly launched");
        });
        Check("releasing jump reduces upward velocity", () => {
            Setup(); Floor(); Step(20); Jump(); Step(2); float before=body.linearVelocity.y;
            Set("jumpReleased",true); Step(1);
            Require(body.linearVelocity.y < before*0.65f, "Jump cut failed");
        });
        Check("coyote jump works after leaving edge", () => {
            Setup(); var floor=Floor(); Step(20); UnityEngine.Object.DestroyImmediate(floor); Step(2);
            motor.extraAirJumps=0; Set("airJumpsRemaining",0); Jump(); Step(1);
            Require(body.linearVelocity.y>10, "Coyote jump failed");
        });
        Check("buffered jump triggers on landing", () => {
            Setup(new Vector2(0,0.9f)); Floor(); motor.extraAirJumps=0; Set("airJumpsRemaining",0);
            body.linearVelocity=new Vector2(0,-4); Jump(); Step(5);
            Require(body.linearVelocity.y>8, "Buffered jump failed: " + body.linearVelocity);
        });
        Check("ramp ascent and descent follow the surface angle", () => {
            Setup(new Vector2(-3,-0.8f));
            var slope=new GameObject("Slope"); slope.layer=8;
            var poly=slope.AddComponent<PolygonCollider2D>();
            poly.points=new[]{new Vector2(-10,-8),new Vector2(10,-8),new Vector2(10,5),new Vector2(-10,-5)};
            poly.sharedMaterial=friction; Step(30); motor.useAcceleration=false; InputX(1); Step(70);
            Require(body.position.x>1, "Could not climb: " + body.position);
            Require(body.position.y>0.5f, "Did not climb slope");
            Require(Mathf.Abs(Mathf.DeltaAngle(body.rotation,26.565f))<2f, "Did not align to slope: " + body.rotation);
            InputX(-1); Step(60); Require(body.position.x<0, "Could not descend");
        });
        Check("stone corner never rotates character", () => {
            Setup(new Vector2(-1,2)); Floor(); Box(new Vector2(0,0.35f),new Vector2(1,0.7f));
            motor.useAcceleration=false; InputX(1);
            for(int i=0;i<90;i++){Step(1);Require(Mathf.Abs(body.rotation)<0.01f,"Rotated on corner");}
        });
        Check("normal attack does not teleport or freeze body", () => {
            Setup(); Floor(); Step(20); var attack=motor.gameObject.AddComponent<PlayerAttack>(); Call(attack,"Awake");
            Vector2 position=body.position; var constraints=body.constraints;
            var routine=(IEnumerator)Call(attack,"NormalAttackRoutine"); routine.MoveNext();
            Require(body.position==position,"Attack teleported");
            Require(body.constraints==constraints,"Attack froze body");
            Require(motor.enabled,"Normal attack disabled movement");
            Call(attack,"FinishAttack");
        });
        Check("dash stops at wall and restores movement", () => {
            Setup(); Floor(); Step(20); Box(new Vector2(1,1),new Vector2(1,4));
            var attack=motor.gameObject.AddComponent<PlayerAttack>(); Call(attack,"Awake");
            var routine=(IEnumerator)Call(attack,"DashAttackRoutine"); int steps=0;
            while(routine.MoveNext() && steps++<100){Physics2D.SyncTransforms();Physics2D.Simulate(Time.fixedDeltaTime);}
            Require(motor.enabled,"Movement stayed disabled"); Require(body.position.x<0.1f,"Dash crossed wall");
            Require(Mathf.Abs(body.position.y-0.5f)<0.12f,"Dash lifted body");
        });
        Check("interrupted dash restores movement and gravity", () => {
            Setup(new Vector2(0,4)); var attack=motor.gameObject.AddComponent<PlayerAttack>(); Call(attack,"Awake");
            var routine=(IEnumerator)Call(attack,"DashAttackRoutine"); routine.MoveNext();
            Require(!motor.enabled,"Dash did not take movement ownership"); Call(attack,"OnDisable");
            Require(motor.enabled,"Interrupted dash left movement disabled");
            Require(Mathf.Abs(body.gravityScale-4)<0.01f,"Gravity not restored");
        });
        Check("slime turns away from a wall", () => {
            Setup(); Floor(); motor.enabled=false; body.simulated=false;
            Box(new Vector2(0,1),new Vector2(1,3));
            var enemy=Slime(new Vector2(-2,.55f),true);
            for(int i=0;i<80;i++){Physics2D.SyncTransforms();Call(enemy,"FixedUpdate");Physics2D.Simulate(Time.fixedDeltaTime);}
            Require(!enemy.movingRight,"Slime did not turn");
            Require(enemy.transform.position.x < -1,"Slime stuck at wall");
        });
        Check("slime avoids a ledge", () => {
            Setup(); Box(new Vector2(0,-.5f),new Vector2(4,1));motor.enabled=false;body.simulated=false;
            var enemy=Slime(new Vector2(0,.55f),true);
            for(int i=0;i<55;i++){Physics2D.SyncTransforms();Call(enemy,"FixedUpdate");Physics2D.Simulate(Time.fixedDeltaTime);}
            Require(!enemy.movingRight,"Slime did not turn at edge");
            Require(enemy.transform.position.y>0.3f,"Slime fell off");
        });
        Check("airborne slime does not oscillate directions", () => {
            Setup();motor.enabled=false;body.simulated=false;var enemy=Slime(new Vector2(0,5),false);
            for(int i=0;i<30;i++){Physics2D.SyncTransforms();Call(enemy,"FixedUpdate");Physics2D.Simulate(Time.fixedDeltaTime);Require(!enemy.movingRight,"Direction oscillated in air");}
        });
        Check("camera tracks a long fall", () => {
            Setup();var go=new GameObject("Camera");var follow=go.AddComponent<CameraFollow>();
            follow.player=motor.transform;follow.playerMovement=motor;Call(follow,"Start");
            motor.transform.position=new Vector3(0,-15,0);motor.isGrounded=false;Call(follow,"LateUpdate");
            float target=(float)typeof(CameraFollow).GetField("targetY",Private).GetValue(follow);
            Require(target < -5,"Camera target stayed above the falling player");
        });
        Check("parallax preserves the original absolute camera calibration", () => {
            Setup();var camera=new GameObject("Camera");camera.transform.position=new Vector3(12,4,-10);
            var go=new GameObject("Background");go.transform.position=new Vector3(5,2,0);
            var texture=new Texture2D(32,32);var sprite=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),16);
            go.AddComponent<SpriteRenderer>().sprite=sprite;var parallax=go.AddComponent<Parallax>();
            typeof(Parallax).GetField("cam",Private).SetValue(parallax,camera.transform);parallax.parallaxEffect=.5f;
            Call(parallax,"Start");Call(parallax,"LateUpdate");
            Require(Mathf.Abs(go.transform.position.y-4f)<.0001f,"Original vertical calibration changed");
            UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);
        });
        Check("ground probe closes small gaps instead of hovering", () => {
            foreach(float gap in new[]{0.02f,0.05f,0.075f}) {
                Setup(new Vector2(0,.5f+gap));Floor();Step(15);
                Require(Mathf.Abs(body.position.y-.5f)<.015f,"Hover gap remains: "+body.position);
            }
        });
        Check("slope stop and reversal stay in contact", () => {
            Setup(new Vector2(-3,-.8f));
            var slope=new GameObject("Slope");slope.layer=8;
            var poly=slope.AddComponent<PolygonCollider2D>();
            poly.points=new[]{new Vector2(-10,-8),new Vector2(10,-8),new Vector2(10,5),new Vector2(-10,-5)};
            poly.sharedMaterial=friction;Step(30);motor.useAcceleration=false;InputX(1);Step(40);
            InputX(0);Step(30);
            Require(poly.Distance(motor.GetComponent<Collider2D>()).distance<.02f,"Hovering on slope");
            InputX(-1);Step(40);
            Require(poly.Distance(motor.GetComponent<Collider2D>()).distance<.02f,"Hovering on descent");
        });
        Check("parallax tiled strip covers both legs without overlapping alpha layers", () => {
            Setup();var camera=new GameObject("Camera");var view=camera.AddComponent<Camera>();
            view.orthographic=true;view.orthographicSize=4;view.aspect=2;
            camera.transform.position=new Vector3(12,4,-10);
            var go=new GameObject("Background");go.transform.position=new Vector3(12,2,0);
            var texture=new Texture2D(32,32);var sprite=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),16);
            go.AddComponent<SpriteRenderer>().sprite=sprite;var parallax=go.AddComponent<Parallax>();
            typeof(Parallax).GetField("cam",Private).SetValue(parallax,camera.transform);
            foreach(float effect in new[]{0f,.4f,1f}) {
                camera.transform.position=new Vector3(12,4,-10);parallax.parallaxEffect=effect;
                // Start once; the camera's authored starting position stays fixed.
                if(effect==0f)Call(parallax,"Start");
                Call(parallax,"LateUpdate");Vector3 expectedReturn=go.transform.position;
                foreach(float x in new[]{12f,35f,80f,200f,900f,200f,80f,35f,12f,-50f,12f}) {
                    camera.transform.position=new Vector3(x,4,-10);Call(parallax,"LateUpdate");
                    var sprites=go.GetComponentsInChildren<SpriteRenderer>();
                    for(float sample=x-8;sample<=x+8;sample+=.125f) {
                        bool covered=false;
                        foreach(var sr in sprites)if(sample>=sr.bounds.min.x-.001f && sample<=sr.bounds.max.x+.001f)covered=true;
                        Require(covered,"Gap at camera "+x+" factor "+effect+" sample "+sample);
                    }
                }
                Require((go.transform.position-expectedReturn).sqrMagnitude<.001f,"Return did not restore initial position");
            }
            Require(go.transform.childCount==0,"Overlapping alpha clones: "+go.transform.childCount);
            UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);
        });
        Check("authored scene backgrounds cover the full outbound and return route", () => {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Single);
            var view=Camera.main;Require(view!=null,"Scene camera missing");
            var follow=view.GetComponent<CameraFollow>();
            Vector3 initial=view.transform.position;
            var layers=UnityEngine.Object.FindObjectsByType<Parallax>(FindObjectsSortMode.None);
            Require(layers.Length>0,"No background layers");
            foreach(var layer in layers)Call(layer,"Start");
            for(int step=0;step<=200;step++) {
                float t=step<=100?step/100f:(200-step)/100f;
                view.transform.position=new Vector3(Mathf.Lerp(follow.minX,follow.maxX,t),initial.y,initial.z);
                foreach(var layer in layers) {
                    if(!layer.enabled)continue;
                    Call(layer,"LateUpdate");
                    float depth=Vector3.Dot(layer.transform.position-view.transform.position,view.transform.forward);
                    float left=view.ViewportToWorldPoint(new Vector3(0,.5f,depth)).x;
                    float right=view.ViewportToWorldPoint(new Vector3(1,.5f,depth)).x;
                    var renderers=layer.GetComponentsInChildren<SpriteRenderer>();
                    for(int sample=0;sample<=100;sample++) {
                        float x=Mathf.Lerp(left,right,sample/100f);bool covered=false;
                        foreach(var sr in renderers)if(sr.enabled && x>=sr.bounds.min.x-.001f && x<=sr.bounds.max.x+.001f)covered=true;
                        Require(covered,layer.name+" missing at step "+step+" x "+x);
                    }
                    Require(layer.transform.childCount<20,"Excess copies on "+layer.name);
                }
            }
        });
        Check("standing beside rock has no invisible support", () => {
            Setup(new Vector2(-.7f,1.14f));
            Box(new Vector2(0,.35f),new Vector2(1,.7f));
            InputX(0);Step(1);Require(!motor.isGrounded,"Rock corner is treated as support");
            Step(25);Require(body.position.y<.6f,"Still hanging beside rock: "+body.position);
        });
        Check("slope angle stays constant over tile seams", () => {
            Setup(new Vector2(-3,-.8f));
            for(int i=-10;i<10;i++) {
                var go=new GameObject("Ramp tile");go.layer=8;
                var poly=go.AddComponent<PolygonCollider2D>();
                poly.points=new[]{new Vector2(i,-8),new Vector2(i+1,-8),new Vector2(i+1,(i+1)*.5f),new Vector2(i,i*.5f)};
                poly.sharedMaterial=friction;
            }
            Step(25);motor.useAcceleration=false;InputX(1);
            for(int i=0;i<100;i++) {
                var oldPosition=body.position;var oldVelocity=body.linearVelocity;
                var oldNormal=(Vector2)typeof(PlayerMovement).GetMethod("SurfaceNormal",Private).Invoke(motor,new object[]{0f});
                Step(1);if(!motor.isGrounded) {
                    var hits=new RaycastHit2D[16]; var filter=new ContactFilter2D();filter.SetLayerMask(1<<8);filter.useTriggers=false;
                    int count=motor.GetComponent<Collider2D>().Cast(Vector2.down,filter,hits,.2f);
                    string detail=string.Join(";",System.Linq.Enumerable.Select(System.Linq.Enumerable.Take(hits,count),h=>$"n={h.normal} p={h.point} d={h.distance}"));
                    throw new Exception("Lost support at seam "+body.position+" v="+body.linearVelocity+" before="+oldPosition+" previousV="+oldVelocity+" centreRay="+oldNormal+" hits="+detail);
                }
                Require(Mathf.Abs(Mathf.DeltaAngle(body.rotation,26.565f))<2f,"Slope angle flickers at "+body.position+": "+body.rotation);
            }
            Require(body.position.x>3f,"Stopped at a tile seam: "+body.position);
        });
        string report=string.Join("\n",results)+"\nFailures: "+failures;
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(),"physics-results.txt"),report);
        Debug.Log(report);
        EditorApplication.Exit(failures==0?0:1);
    }
    static void Setup(Vector2? position=null)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        if(friction!=null) UnityEngine.Object.DestroyImmediate(friction);
        friction=new PhysicsMaterial2D("Test zero friction"){friction=0,bounciness=0};
        var go=new GameObject("Player"); go.transform.position=position??new Vector2(0,0.55f);
        body=go.AddComponent<Rigidbody2D>(); body.gravityScale=4;
        var capsule=go.AddComponent<CapsuleCollider2D>(); capsule.size=Vector2.one; capsule.sharedMaterial=friction;
        motor=go.AddComponent<PlayerMovement>(); motor.groundLayer=1<<8; Call(motor,"Awake");
        Physics2D.SyncTransforms();
    }
    static EnemyPatrol Slime(Vector2 position,bool right)
    {
        var go=new GameObject("Slime");go.layer=10;go.transform.position=position;
        go.AddComponent<Rigidbody2D>().gravityScale=1;
        var collider=go.AddComponent<CapsuleCollider2D>();collider.size=Vector2.one;collider.sharedMaterial=friction;
        var detector=new GameObject("LedgeDetector");detector.transform.SetParent(go.transform,false);detector.transform.localPosition=new Vector3(-.6f,0,0);
        var enemy=go.AddComponent<EnemyPatrol>();enemy.ledgeDetector=detector.transform;enemy.walkableLayers=1<<8;enemy.movingRight=right;
        Call(enemy,"Awake");return enemy;
    }
    static GameObject Floor()=>Box(new Vector2(0,-0.5f),new Vector2(100,1));
    static GameObject Box(Vector2 p,Vector2 size)
    {
        var go=new GameObject("Solid"); go.layer=8; go.transform.position=p;
        var c=go.AddComponent<BoxCollider2D>();c.size=size;c.sharedMaterial=friction;Physics2D.SyncTransforms();return go;
    }
    static void Step(int count)
    {
        for(int i=0;i<count;i++)
        {Physics2D.SyncTransforms();Call(motor,"FixedUpdate");Physics2D.Simulate(Time.fixedDeltaTime);}
    }
    static void InputX(float value)=>Set("moveInput",value);
    static void Jump(){Set("jumpBufferRemaining",0.12f);Set("bufferedJumpReleased",false);}
    static void Set(string field,object value)=>typeof(PlayerMovement).GetField(field,Private).SetValue(motor,value);
    static object Call(object obj,string method)=>obj.GetType().GetMethod(method,Private).Invoke(obj,null);
    static void Require(bool condition,string reason){if(!condition)throw new Exception(reason);}
    static void Check(string name,Action check)
    {
        try{check();results.Add("PASS "+name);}catch(Exception e){failures++;results.Add("FAIL "+name+": "+e.GetBaseException().Message);}
    }
}
