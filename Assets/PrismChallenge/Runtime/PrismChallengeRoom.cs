using UnityEngine;

namespace DimensionShifter.PrismChallenge
{
    [DefaultExecutionOrder(220)]
    public sealed class PrismChallengeRoom : MonoBehaviour
    {
        public PrismChallengeRules Run { get; } = new PrismChallengeRules();
        public bool Inside { get; private set; }
        public bool Unlocked { get; private set; }
        public float BestTime { get; private set; } = -1;
        public string Notice { get; private set; }
        public float NoticeRemaining { get; private set; }
        public DemoSession Session { get; private set; }
        public PrismwakeReward Reward { get; private set; }
        public Vector3 Origin => roomRoot != null ? roomRoot.transform.position : Vector3.zero;
        public bool CanPlay => Session != null && Session.Started && !Session.Paused &&
            health != null && !health.IsDead && runProgress != null && !runProgress.Completed;
        public bool NearEntrance => entrance != null && CanPlay && !Inside &&
            Vector2.Distance(shape.bounds.center, entrance.transform.position + Vector3.up) < 2.2f;
        public Vector3 EntrancePosition => entrance != null ? entrance.transform.position : Vector3.zero;

        PlayerHealth health;
        PlayerMovement motor;
        PlayerAttack attack;
        RunProgress runProgress;
        DimensionController dimension;
        Rigidbody2D body;
        Collider2D shape;
        CameraFollow follow;
        PrismChallengeVisuals visuals;
        PrismChallengeHUD hud;
        GameObject entrance, roomRoot;
        Vector3 returnPosition;
        Vector2 safeFeet;
        int returnWorld, safeWorld, previousWorld;
        float originalMinX, originalMaxX, nextSearch, inputReady;
        bool cameraBorrowed, gateArmed;
        readonly Vector2[] runePositions = { new Vector2(19.25f, 4.05f), new Vector2(42f, 4.05f), new Vector2(68f, 7.05f) };

        public void Initialize(DemoSession session)
        {
            Session = session;
            health = session.GetComponent<PlayerHealth>();
            motor = session.GetComponent<PlayerMovement>();
            attack = session.GetComponent<PlayerAttack>();
            runProgress = session.GetComponent<RunProgress>();
            dimension = session.GetComponent<DimensionController>();
            body = session.GetComponent<Rigidbody2D>();
            shape = session.GetComponent<Collider2D>();
            if (health == null || motor == null || attack == null || runProgress == null ||
                dimension == null || body == null || shape == null)
            {
                enabled = false;
                return;
            }
            visuals = gameObject.AddComponent<PrismChallengeVisuals>();
            visuals.Initialize(this);
            hud = gameObject.AddComponent<PrismChallengeHUD>();
            hud.Initialize(this);
        }

        void Update()
        {
            if (Session == null || visuals == null) return;
            NoticeRemaining = Mathf.Max(0, NoticeRemaining - Time.deltaTime);
            if (entrance == null && Session.HasSpirit && Time.unscaledTime >= nextSearch)
            {
                nextSearch = Time.unscaledTime + .75f;
                FindEntrance();
            }

            if (Inside)
            {
                // An external respawn/teleport owns its new position. Only release our camera.
                Vector3 local = Session.transform.position - Origin;
                if (local.y < -20 || local.y > 32 || local.x < -18 || local.x > 92)
                {
                    Leave(false);
                    return;
                }
                if (health.IsDead || runProgress.Completed)
                {
                    Leave(true);
                    return;
                }
                if (!CanPlay) return;
                Run.Tick(Time.deltaTime);
                if (Time.unscaledTime >= inputReady && Input.GetKeyDown(KeyCode.E))
                {
                    Leave(true);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.F)) RestartAttempt();
            }
            else if (NearEntrance && Time.unscaledTime >= inputReady && Input.GetKeyDown(KeyCode.E))
                Enter();

            if (Unlocked && CanPlay && Input.GetKeyDown(KeyCode.T) && Reward != null)
            {
                Reward.SetVisible(!Reward.Visible);
                ShowNotice(Reward.Visible ? "PRISMWAKE equipped · T to hide" : "PRISMWAKE hidden · T to equip");
            }
        }

        void FindEntrance()
        {
            // Attach to a semantic landmark, rather than a chunk name or an absolute map coordinate.
            foreach (var portal in FindObjectsByType<QuestInteractable>(FindObjectsSortMode.None))
            {
                if (portal.gameObject.scene != gameObject.scene || portal.kind != QuestInteractable.Kind.ReturnPortal || portal.destination == null)
                    continue;
                Vector3 point = portal.destination.position + Vector3.right * 2f;
                var floor = Physics2D.Raycast(point + Vector3.up * 3, Vector2.down, 8, 1 << 8);
                if (floor.collider == null || floor.collider.isTrigger) continue;
                point.y = floor.point.y + .03f;
                entrance = visuals.BuildEntrance(point);
                return;
            }
        }

        public bool Enter()
        {
            if (!CanPlay || Inside || entrance == null || !Session.HasSpirit || !NearEntrance) return false;
            follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow == null) return false;
            returnPosition = Session.transform.position;
            returnWorld = dimension.Current;
            originalMinX = follow.minX;
            originalMaxX = follow.maxX;
            cameraBorrowed = true;
            if (roomRoot == null)
            {
                float ceiling = entrance.transform.position.y + 48f;
                foreach (var collider in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                    if (collider.gameObject.scene == gameObject.scene && !collider.isTrigger)
                        ceiling = Mathf.Max(ceiling, collider.bounds.max.y);
                roomRoot = visuals.BuildRoom(new Vector3(entrance.transform.position.x - 4f, ceiling + 48f, 0));
            }
            roomRoot.SetActive(true);
            Inside = true;
            follow.minX = Origin.x + 4f;
            follow.maxX = Origin.x + 70f;
            RestartAttempt();
            inputReady = Time.unscaledTime + .4f;
            GameAudio.Play("shift", .5f);
            return true;
        }

        public void RestartAttempt()
        {
            if (!Inside || !CanPlay) return;
            Run.Reset();
            gateArmed = false;
            safeFeet = new Vector2(4, .04f);
            safeWorld = 0;
            ReturnToSafePad();
            ShowNotice("Three prisms. No time limit. Falling costs no health. E leaves at any time.", 6f);
            visuals.RefreshRunes();
        }

        void FixedUpdate()
        {
            if (!Inside || !CanPlay) return;
            Vector3 local = shape.bounds.center - Origin;
            if (shape.bounds.min.y < Origin.y - 3f || local.x < -2f || local.x > 78f)
            {
                if (Run.Finished) ReturnToSafePad();
                else RetrySection("The void returns you to your last resting island.");
                return;
            }
            if (Run.Finished) return;

            if (Run.Runes == 1)
            {
                var gate = new Bounds(Origin + new Vector3(PrismChallengeRules.GateX, 6, 0), new Vector3(.8f, 20, 10));
                if (gate.Intersects(shape.bounds))
                {
                    if (dimension.Current != 0 || !attack.IsDashing || !PrismChallengeRules.GateIsQuiet(Run.Elapsed))
                    {
                        RetrySection(dimension.Current != 0 ? "The prism gate answers in FROST. Q on the resting island." :
                            !PrismChallengeRules.GateIsQuiet(Run.Elapsed) ? "Watch the gold warning. Wait for the quiet window, then B through." :
                            "Jump from the island edge, then B through the prism gate.");
                        return;
                    }
                    gateArmed = true;
                }
                if (gateArmed && shape.bounds.min.x > Origin.x + PrismChallengeRules.GateX + .4f)
                    Run.RegisterGatePass();
            }

            // Local resting islands never replace the main game's checkpoint.
            if (motor.isGrounded && Run.Runes == 1 && local.x >= 25 && local.x <= 31 && Mathf.Abs(shape.bounds.min.y - Origin.y - 3f) < .2f)
                SetSafePad(new Vector2(28, 3.04f), dimension.Current);
            if (motor.isGrounded && Run.Runes >= 2 && local.x >= 39 && local.x <= 46 && Mathf.Abs(shape.bounds.min.y - Origin.y - 3f) < .2f)
                SetSafePad(new Vector2(43, 3.04f), dimension.Current);

            int next = Run.Runes;
            if (next == 2 && Run.AirShifts < 2 && Vector2.Distance(local, runePositions[2]) < 1.05f)
            {
                RetrySection("The last prism needs TWO air shifts. Jump, then Q onto each colored ledge; rest between jumps.");
                return;
            }
            if (next < 3 && Vector2.Distance(local, runePositions[next]) < 1.05f && Run.TryCollectRune(next, dimension.Current))
            {
                visuals.Burst(Origin + (Vector3)runePositions[next], next == 2 ? PrismChallengeVisuals.Gold : PrismChallengeVisuals.Cyan);
                visuals.RefreshRunes();
                GameAudio.Play("pickup", .5f);
                if (next == 0) ShowNotice("I · ECHO awakened. Reach the wide resting island. Shift to FROST for the pulse gate.", 6);
                else if (next == 1)
                {
                    // Rune II sits on neutral terrain: an immediate, safe retry point.
                    SetSafePad(new Vector2(43, 3.04f), 0);
                    ShowNotice("II · Pulse mastered. Q in the AIR onto ECHO, rest, then Q in the AIR onto FROST.", 7);
                }
                else Complete();
            }
        }

        void LateUpdate()
        {
            if (!Inside || dimension == null) return;
            int current = dimension.Current;
            if (CanPlay && current != previousWorld && !motor.isGrounded)
                Run.RegisterAirShift(shape.bounds.center.x - Origin.x);
            previousWorld = current;
        }

        void SetSafePad(Vector2 feet, int world)
        {
            if ((safeFeet - feet).sqrMagnitude < .01f) return;
            safeFeet = feet;
            safeWorld = world;
            visuals.Burst(Origin + (Vector3)feet, PrismChallengeVisuals.Gold, 10);
            GameAudio.Play("checkpoint", .25f);
        }

        void RetrySection(string message)
        {
            Run.Fall();
            gateArmed = false;
            ReturnToSafePad();
            ShowNotice(message, 5);
            GameAudio.Play("shift", .3f);
        }

        void ReturnToSafePad()
        {
            Warp(Origin + (Vector3)safeFeet, true);
            dimension.SetDimension(safeWorld);
            previousWorld = safeWorld;
            foreach (var member in roomRoot.GetComponentsInChildren<DimensionMember>()) member.Apply(safeWorld);
        }

        void Complete()
        {
            safeFeet = new Vector2(68, 6.04f);
            safeWorld = dimension.Current;
            BestTime = BestTime < 0 ? Run.Elapsed : Mathf.Min(BestTime, Run.Elapsed);
            if (!Unlocked)
            {
                Unlocked = true;
                Reward = Session.gameObject.AddComponent<PrismwakeReward>();
                Reward.Initialize(this, visuals);
            }
            visuals.Burst(Origin + new Vector3(68, 8, 0), PrismChallengeVisuals.Gold, 50);
            GameAudio.Play("checkpoint", .65f);
            ShowNotice("PRISMWAKE unlocked · your own prismatic trail. T equips / hides it. E returns to the journey.", 10);
        }

        public void Leave(bool restorePosition)
        {
            if (!Inside && !cameraBorrowed) return;
            Inside = false;
            if (cameraBorrowed && follow != null)
            {
                follow.minX = originalMinX;
                follow.maxX = originalMaxX;
            }
            cameraBorrowed = false;
            if (restorePosition && Session != null && body != null)
            {
                Warp(returnPosition, false);
                dimension.SetDimension(returnWorld);
            }
            if (roomRoot != null) roomRoot.SetActive(false);
            if (follow != null) follow.SnapToPlayer();
            inputReady = Time.unscaledTime + .4f;
            if (restorePosition && CanPlay)
            {
                GameAudio.Play("shift", .4f);
                runProgress.ShowMessage(Unlocked ? "PRISMWAKE acquired · T toggles your cosmetic trail. Your journey continues." : "Returned from the optional crucible. You can try again whenever you like.");
            }
        }

        void Warp(Vector3 target, bool placeFeet)
        {
            // Let the existing dash routine restore its own gravity and movement first.
            bool attackEnabled = attack.enabled;
            attack.enabled = false;
            bool motorEnabled = motor.enabled;
            motor.enabled = false;
            var contact = Session.GetComponent<PlayerEnemyContact>();
            bool contactEnabled = contact != null && contact.enabled;
            if (contact != null) contact.enabled = false;
            body.rotation = 0;
            Physics2D.SyncTransforms();
            if (placeFeet)
                target = Session.transform.position + target - new Vector3(shape.bounds.center.x, shape.bounds.min.y, Session.transform.position.z);
            Session.transform.position = target;
            body.position = target;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            Physics2D.SyncTransforms();
            if (Reward != null) Reward.ClearTrail();
            motor.enabled = motorEnabled && !health.IsDead;
            attack.enabled = attackEnabled && !health.IsDead;
            if (contact != null) contact.enabled = contactEnabled;
            if (follow != null) follow.SnapToPlayer();
        }

        public void ShowNotice(string message, float duration = 4f)
        {
            Notice = message;
            NoticeRemaining = duration;
        }

        void OnDisable()
        {
            if (Session != null && gameObject.scene.isLoaded && (Inside || cameraBorrowed)) Leave(true);
        }

        void OnDestroy()
        {
            if (Reward != null) Destroy(Reward);
        }
    }
}
