using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DimensionShifter.PrismChallenge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Copy into Assets/Editor in an ISOLATED project copy, as with the other Tools checks.
// Unity -batchmode -projectPath <copy> -executeMethod PrismChallengeChecks.Run -logFile <log>
public static class PrismChallengeChecks
{
    const string Pending = "PrismChallengeChecks.Pending";
    static readonly List<string> report = new List<string>();
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static bool started;
    static PlayerMovement motor;
    static Rigidbody2D body;
    static PrismChallengeRoom room;

    public static void Run()
    {
        try
        {
            RuleChecks();
            Check(Resources.Load<Material>("PrismChallenge/PrismUnlit") != null, "Trail material packaged in Resources");
            Check(Resources.Load<Material>("PrismChallenge/PrismSprites") != null, "Sprite material packaged in Resources");
            File.WriteAllLines("prism-challenge-results.txt", report);
            EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");
            SessionState.SetBool(Pending, true);
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Watch;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Finish(e); }
    }

    [InitializeOnLoadMethod]
    static void ResumeAcrossDomainReload()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        report.Clear();
        if (File.Exists("prism-challenge-results.txt")) report.AddRange(File.ReadAllLines("prism-challenge-results.txt"));
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.update -= Watch;
        EditorApplication.update += Watch;
    }

    static double deadline;
    static void Watch()
    {
        if (EditorApplication.timeSinceStartup > deadline) Finish(new Exception("Play-mode validation timed out"));
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || started) return;
        started = true;
        EditorApplication.delayCall += PlayChecks;
    }

    static void RuleChecks()
    {
        var run = new PrismChallengeRules();
        Check(!run.TryCollectRune(2, 0), "Final prism cannot be collected out of order");
        Check(!run.TryCollectRune(0, 0), "First prism requires Echo");
        Check(run.TryCollectRune(0, 1), "Echo prism advances the run");
        Check(!run.TryCollectRune(0, 1), "Collected prisms cannot repeat");
        Check(!run.TryCollectRune(1, 0), "Second prism requires a gate crossing");
        run.RegisterGatePass();
        Check(!run.TryCollectRune(1, 1), "Second prism requires Frost");
        Check(run.TryCollectRune(1, 0), "Valid gate crossing unlocks second prism");
        run.RegisterAirShift(46);
        Check(run.AirShifts == 0, "Air shifts outside final section do not count");
        run.RegisterAirShift(49);
        Check(!run.TryCollectRune(2, 0), "One air shift cannot unlock final prism");
        run.Fall();
        Check(run.Runes == 2 && run.AirShifts == 0 && run.Falls == 1, "Fall preserves prisms and resets final section skill count");
        run.RegisterAirShift(49);
        run.RegisterAirShift(56);
        Check(run.TryCollectRune(2, 0) && run.Finished, "Two air shifts complete the trial");
        float elapsed = run.Elapsed;
        run.Tick(2);
        Check(run.Elapsed == elapsed, "Finish freezes the timer");
        run.Reset();
        run.Tick(float.NaN);
        run.Tick(float.PositiveInfinity);
        run.Tick(-1);
        Check(run.Elapsed == 0, "Invalid deltas cannot corrupt the timer");
        run.Tick(54);
        Check(run.Medal == "GOLD", "Gold threshold");
        run.Fall();
        Check(run.Medal == "SILVER", "A fall prevents Gold");
        run.Tick(40);
        Check(run.Medal == "BRONZE", "All successful runs receive a medal");
        Check(PrismChallengeRules.GateIsQuiet(0) && !PrismChallengeRules.GateIsQuiet(2.8f) &&
            !PrismChallengeRules.GateIsQuiet(3.9f) && PrismChallengeRules.GateIsQuiet(4.7f), "Pulse windows and cycle wrap");
        run.Reset();
        Check(run.Runes == 0 && run.AirShifts == 0 && run.Falls == 0 && !run.GatePassed && run.Elapsed == 0, "Fresh attempts reset only local run state");
    }

    static void PlayChecks()
    {
        try
        {
            var session = Object.FindAnyObjectByType<DemoSession>();
            session.Begin();
            session.ClaimSpirit();
            var generator = Object.FindAnyObjectByType<LevelGenerator>();
            var sanctuaryPrefab = generator.encounterChunks.First(p => p.GetComponentsInChildren<QuestInteractable>(true).Any(q => q.kind == QuestInteractable.Kind.ReturnPortal));
            var sanctuary = Object.Instantiate(sanctuaryPrefab, new Vector3(1000, 0), Quaternion.identity);
            room = Object.FindAnyObjectByType<PrismChallengeRoom>();
            Check(room != null && PrismChallengeBootstrap.Install(session.gameObject.scene) == room, "Bootstrap is automatic and idempotent");
            Invoke(room, "FindEntrance");
            motor = session.GetComponent<PlayerMovement>();
            body = session.GetComponent<Rigidbody2D>();
            body.position = room.EntrancePosition + Vector3.up * .7f;
            Physics2D.SyncTransforms();
            Vector3 returnPosition = session.transform.position;
            var follow = Camera.main.GetComponent<CameraFollow>();
            float min = follow.minX, max = follow.maxX;
            var health = session.GetComponent<PlayerHealth>();
            var progress = session.GetComponent<RunProgress>();
            float hp = health.currentHealth;
            Vector3 checkpoint = progress.CheckpointPosition;
            Check(room.Enter() && room.Inside, "Entrance reaches an isolated optional room");
            Check(health.currentHealth == hp && progress.CheckpointPosition == checkpoint && session.HasSpirit, "Entry preserves health, main checkpoint and quest discoveries");

            Physics2D.simulationMode = SimulationMode2D.Script;
            foreach (bool acceleration in new[] { false, true })
            {
                motor.useAcceleration = acceleration;
                // Actual player controller, capsule and new island colliders, not an analytic approximation.
                Hop(7.2f, 0, 10.5f, 15, 1.5f, 0);
                Hop(14.2f, 1.5f, 17, 21.5f, 3, 1);
                Hop(20.7f, 3, 24, 32, 3, 0);
                Hop(46.2f, 3, 50, 54.5f, 4.5f, 1);
                Hop(53.7f, 4.5f, 57, 61.5f, 6, 0);
                Hop(60.7f, 6, 64, 75, 6, 0);
            }

            room.RestartAttempt();
            room.Run.TryCollectRune(0, 1);
            WarpFeet(28, 3);
            Invoke(room, "FixedUpdate");
            WarpFeet(31.2f, 3);
            session.GetComponent<DimensionController>().SetDimension(0);
            for (int i = 0; i < 15; i++) Step(0);
            Set(motor, "jumpBufferRemaining", .12f);
            Set(motor, "bufferedJumpReleased", false);
            for (int i = 0; i < 23; i++) Step(1);
            var dash = (IEnumerator)typeof(PlayerAttack).GetMethod("DashAttackRoutine", Private).Invoke(session.GetComponent<PlayerAttack>(), null);
            bool dashing = dash.MoveNext();
            while (dashing)
            {
                Physics2D.Simulate(Time.fixedDeltaTime);
                Invoke(room, "FixedUpdate");
                dashing = dash.MoveNext();
            }
            for (int i = 0; i < 120; i++) Step(body.position.x < room.Origin.x + 40 ? 1 : 0);
            Check(room.Run.GatePassed && room.Run.Falls == 0 && motor.isGrounded && body.position.x >= room.Origin.x + 37, "Real jump + dash clears the pulse gap with acceleration enabled");

            // A failure uses the local island, preserving main health and checkpoint.
            WarpFeet(33, -4);
            Invoke(room, "FixedUpdate");
            Check(room.Run.Falls == 1 && session.transform.position.y > room.Origin.y, "Void fall retries locally");
            Check(health.currentHealth == hp && progress.CheckpointPosition == checkpoint, "Retry costs no health and never overwrites the main checkpoint");
            room.Run.RegisterGatePass();
            room.Run.TryCollectRune(1, 0);
            room.Run.RegisterAirShift(49);
            room.Run.RegisterAirShift(56);
            WarpFeet(68, 6);
            Invoke(room, "FixedUpdate");
            Check(room.Unlocked && room.Reward != null && room.Run.Finished, "Final prism creates the cosmetic once");
            room.Leave(true);
            Check(!room.Inside && follow.minX == min && follow.maxX == max, "Leaving restores camera limits");
            Check(Vector3.Distance(session.transform.position, returnPosition) < .01f, "Leaving restores the exact trail position");
            Check(room.Unlocked && room.Reward.Visible, "Reward survives returning to the trail");
            room.Reward.SetVisible(false);
            Check(!room.Reward.Visible, "Cosmetic can be hidden independently");
            Object.Destroy(sanctuary);
            Finish(null);
        }
        catch (Exception e) { Finish(e); }
    }

    static void Hop(float fromX, float fromY, float targetLeft, float targetRight, float targetY, int world)
    {
        var dimension = motor.GetComponent<DimensionController>();
        dimension.SetDimension(world);
        // Neutralize only the source platform for this isolated movement check.
        var source = Physics2D.Raycast(room.Origin + new Vector3(fromX, fromY + .2f), Vector2.down, .5f, 1 << 8);
        if (source.collider != null && source.collider.isTrigger) source.collider.isTrigger = false;
        // The actual source belongs to the opposite world on the two middle jumps.
        foreach (var member in Object.FindObjectsByType<DimensionMember>(FindObjectsSortMode.None))
            if (member.transform.IsChildOf(room.transform) && Mathf.Abs(member.transform.position.y - room.Origin.y - fromY) < .1f &&
                member.GetComponent<Collider2D>().bounds.min.x <= room.Origin.x + fromX && member.GetComponent<Collider2D>().bounds.max.x >= room.Origin.x + fromX)
                member.GetComponent<Collider2D>().isTrigger = false;
        WarpFeet(fromX, fromY);
        for (int i = 0; i < 20; i++) Step(0);
        Check(motor.isGrounded, "Hop starts grounded at " + fromX);
        Set(motor, "jumpBufferRemaining", .12f);
        Set(motor, "bufferedJumpReleased", false);
        for (int i = 0; i < 190; i++)
        {
            if (i == 33) { Set(motor, "jumpBufferRemaining", .12f); Set(motor, "bufferedJumpReleased", false); }
            Step(body.position.x < room.Origin.x + targetLeft + 1.1f ? 1 : 0);
        }
        var bounds = motor.GetComponent<Collider2D>().bounds;
        Check(motor.isGrounded && bounds.center.x >= room.Origin.x + targetLeft && bounds.center.x <= room.Origin.x + targetRight && Mathf.Abs(bounds.min.y - room.Origin.y - targetY) < .12f,
            "Reachable island " + targetLeft + " acceleration=" + motor.useAcceleration);
    }

    static void WarpFeet(float x, float y) => typeof(PrismChallengeRoom).GetMethod("Warp", Private).Invoke(room, new object[] { room.Origin + new Vector3(x, y + .04f), true });
    static void Step(float input)
    {
        Set(motor, "moveInput", input);
        if (motor.enabled) Invoke(motor, "FixedUpdate");
        Physics2D.Simulate(Time.fixedDeltaTime);
    }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    static void Check(bool pass, string label)
    {
        if (!pass) throw new Exception(label);
        report.Add("PASS " + label);
    }
    static void Finish(Exception error)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Watch;
        EditorApplication.playModeStateChanged -= Changed;
        if (error != null) { report.Add("FAIL " + error); Debug.LogException(error); }
        File.WriteAllLines("prism-challenge-results.txt", report);
        EditorApplication.Exit(error == null ? 0 : 1);
    }
}
