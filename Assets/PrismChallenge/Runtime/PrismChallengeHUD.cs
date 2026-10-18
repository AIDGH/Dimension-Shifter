using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DimensionShifter.PrismChallenge
{
    public sealed class PrismChallengeHUD : MonoBehaviour
    {
        PrismChallengeRoom owner;
        Canvas canvas;
        GameObject header, footer, controls, invitation;
        TMP_Text title, stats, objective, notice, prompt;
        readonly Image[] runeIcons = new Image[3];
        Image gateClock;
        Color cyan => PrismChallengeVisuals.Cyan;
        Color gold => PrismChallengeVisuals.Gold;

        public void Initialize(PrismChallengeRoom room)
        {
            owner = room;
            var visuals = GetComponent<PrismChallengeVisuals>();
            var root = new GameObject("Prism Crucible HUD", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var top = Panel("Challenge status", new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(730, 134));
            header = top.gameObject;
            Label(top, "OPTIONAL SANCTUARY", new Vector2(0, -15), new Vector2(700, 20), 13, new Color(.62f, .75f, .9f), visuals.Font);
            title = Label(top, "THE PRISM CRUCIBLE", new Vector2(0, -41), new Vector2(710, 32), 26, gold, visuals.Font);
            stats = Label(top, "", new Vector2(0, -75), new Vector2(710, 24), 17, Color.white, visuals.Font);
            for (int i = 0; i < 3; i++)
            {
                var rt = Element("Prism progress", top, new Vector2((i - 1) * 31, -109), new Vector2(14, 19));
                runeIcons[i] = rt.gameObject.AddComponent<Image>();
                runeIcons[i].sprite = visuals.Diamond;
                runeIcons[i].raycastTarget = false;
            }

            var bottom = Panel("Trial guidance", new Vector2(.5f, 0), new Vector2(0, 232), new Vector2(880, 132));
            footer = bottom.gameObject;
            objective = Label(bottom, "", new Vector2(0, -17), new Vector2(835, 29), 23, cyan, visuals.Font);
            notice = Label(bottom, "", new Vector2(0, -53), new Vector2(820, 48), 19, new Color(.83f, .88f, .96f), visuals.Font);
            notice.textWrappingMode = TextWrappingModes.Normal;
            var track = Element("Pulse window", bottom, new Vector2(0, -109), new Vector2(850, 3));
            gateClock = track.gameObject.AddComponent<Image>();
            gateClock.raycastTarget = false;

            var keys = Panel("Challenge controls", new Vector2(.5f, 0), new Vector2(0, 58), new Vector2(990, 40));
            controls = keys.gameObject;
            Label(keys, "SPACE Jump     Q Shift     B Dash     F Restart trial     E Leave     T Cosmetic", new Vector2(0, -20), new Vector2(970, 32), 17, Color.white, visuals.Font);

            var invite = Panel("Optional invitation", new Vector2(.5f, 0), new Vector2(0, 232), new Vector2(850, 132));
            invitation = invite.gameObject;
            Label(invite, "A RIFT OFF THE BEATEN PATH", new Vector2(0, -20), new Vector2(820, 26), 17, gold, visuals.Font);
            prompt = Label(invite, "", new Vector2(0, -61), new Vector2(810, 54), 21, Color.white, visuals.Font);
            prompt.textWrappingMode = TextWrappingModes.Normal;
            header.SetActive(false);
            footer.SetActive(false);
            controls.SetActive(false);
            invitation.SetActive(false);
        }

        void LateUpdate()
        {
            if (owner == null || canvas == null) return;
            canvas.enabled = owner.CanPlay;
            bool inside = owner.Inside && owner.CanPlay;
            header.SetActive(inside);
            footer.SetActive(inside);
            controls.SetActive(inside);
            bool showInvitation = owner.NearEntrance || (!owner.Inside && owner.Unlocked && owner.NoticeRemaining > 0);
            invitation.SetActive(showInvitation && owner.CanPlay);
            if (showInvitation)
            {
                prompt.text = owner.NearEntrance ?
                    (owner.Unlocked ? "E  Re-enter the crucible  ·  beat your best time\nT  Toggle your unlocked Prismwake trail" :
                    "E  Enter the Prism Crucible\nThree movement trials  ·  unlock a prismatic trail") : owner.Notice;
            }
            if (!inside) return;
            var run = owner.Run;
            title.text = run.Finished ? "CRUCIBLE COMPLETE  /  " + run.Medal : "THE PRISM CRUCIBLE";
            stats.text = Clock(run.Elapsed) + "     ·     " + run.Falls + " returns     ·     " + run.Runes + " / 3 prisms" +
                (owner.BestTime >= 0 ? "     ·     BEST " + Clock(owner.BestTime) : "");
            for (int i = 0; i < runeIcons.Length; i++)
                runeIcons[i].color = i < run.Runes ? gold : i == run.Runes ? cyan : new Color(.25f, .31f, .45f);
            objective.text = run.Finished ? "PRISMWAKE UNLOCKED  ·  T EQUIP / HIDE" :
                run.Runes == 0 ? "I  /  TWIN ASCENT  ·  REACH THE VIOLET PRISM" :
                run.Runes == 1 ? "II  /  PULSE CROSSING  ·  FROST + DASH + QUIET WINDOW" :
                "III  /  SKYWEAVE  ·  " + Mathf.Min(run.AirShifts, 2) + " / 2 AIR SHIFTS";
            notice.text = owner.NoticeRemaining > 0 ? owner.Notice : run.Finished ?
                "Your reward stays with you for this journey. E returns to the trail; F starts a fresh attempt." :
                run.Runes == 0 ? "Ghost steps have no footing. Land on FROST, wait for Q, then jump and shift onto ECHO." :
                run.Runes == 1 ? "Rest on the wide island. In FROST, jump from the right edge and B through while the gate is cyan." :
                "Switch in midair onto each colored ledge. Wait for Q between jumps. Two air shifts awaken the last prism.";
            float phase = PrismChallengeRules.GatePhase(run.Elapsed);
            gateClock.color = run.Runes == 1 ? (PrismChallengeRules.GateIsQuiet(run.Elapsed) ? cyan : gold) : new Color(.2f, .35f, .5f);
            gateClock.rectTransform.sizeDelta = new Vector2(run.Runes == 1 ? 850 * (1 - phase / PrismChallengeRules.GatePeriod) : 850, 3);
        }

        static string Clock(float seconds) => System.TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.ff");

        RectTransform Panel(string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = Element(name, canvas.transform, Vector2.zero, size);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, 1);
            rt.anchoredPosition = position;
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(.018f, .035f, .075f, .98f);
            image.raycastTarget = false;
            var edge = Element("Gold edge", rt, new Vector2(0, -1), new Vector2(size.x, 2));
            var accent = edge.gameObject.AddComponent<Image>();
            accent.color = new Color(.82f, .65f, .31f, .85f);
            accent.raycastTarget = false;
            return rt;
        }

        static RectTransform Element(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        static TMP_Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, TMP_FontAsset font)
        {
            var rt = Element("Label", parent, position, size);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }
    }
}
