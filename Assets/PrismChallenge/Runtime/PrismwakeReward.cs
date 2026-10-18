using UnityEngine;

namespace DimensionShifter.PrismChallenge
{
    // Run-only cosmetic. It never writes the player's renderer, stats or material.
    public sealed class PrismwakeReward : MonoBehaviour
    {
        public bool Visible { get; private set; } = true;
        PrismChallengeRoom owner;
        GameObject root;
        TrailRenderer ribbon;
        SpriteRenderer sigil;
        Vector3 previousPosition;

        public void Initialize(PrismChallengeRoom room, PrismChallengeVisuals visuals)
        {
            owner = room;
            root = new GameObject("Prismwake • cosmetic reward");
            root.transform.SetParent(transform, false);
            ribbon = root.AddComponent<TrailRenderer>();
            ribbon.sharedMaterial = visuals.Material;
            ribbon.time = .38f;
            ribbon.minVertexDistance = .06f;
            ribbon.widthCurve = AnimationCurve.Linear(0, .17f, 1, 0);
            ribbon.numCapVertices = 3;
            ribbon.sortingOrder = 46;
            ribbon.emitting = false;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(PrismChallengeVisuals.Cyan, 0),
                new GradientColorKey(PrismChallengeVisuals.Violet, .5f),
                new GradientColorKey(PrismChallengeVisuals.Gold, 1)
            }, new[] { new GradientAlphaKey(.65f, 0), new GradientAlphaKey(0, 1) });
            ribbon.colorGradient = gradient;
            sigil = visuals.Picture("Companion prism", root.transform, Vector3.up * .9f, new Vector2(.12f, .2f), PrismChallengeVisuals.Gold, visuals.Diamond, 51);
            previousPosition = transform.position;
        }

        void LateUpdate()
        {
            if (root == null || owner == null) return;
            bool active = Visible && owner.CanPlay;
            // Remove teleport streaks, including external checkpoints and portals.
            if ((transform.position - previousPosition).sqrMagnitude > 9f) ClearTrail();
            bool moving = (transform.position - previousPosition).sqrMagnitude > .00001f;
            previousPosition = transform.position;
            var shape = GetComponent<Collider2D>();
            if (shape != null) root.transform.position = shape.bounds.center;
            // Cancel character facing/scale so the cosmetic keeps a consistent size.
            root.transform.rotation = Quaternion.identity;
            Vector3 scale = transform.lossyScale;
            root.transform.localScale = new Vector3(1f / Mathf.Max(.01f, Mathf.Abs(scale.x)) * Mathf.Sign(scale.x), 1f / Mathf.Max(.01f, Mathf.Abs(scale.y)), 1);
            ribbon.emitting = active && moving;
            sigil.enabled = active;
            if (active) sigil.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 2) * .35f, .85f + Mathf.Sin(Time.time * 3) * .08f, 0);
            if (!active) ribbon.Clear();
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            ClearTrail();
        }

        public void ClearTrail()
        {
            if (ribbon != null) ribbon.Clear();
            previousPosition = transform.position;
        }

        void OnDisable()
        {
            if (ribbon != null) { ribbon.emitting = false; ribbon.Clear(); }
            if (sigil != null) sigil.enabled = false;
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root);
        }
    }
}
