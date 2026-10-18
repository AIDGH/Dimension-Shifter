using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// One scalable Canvas replaces the debug IMGUI boxes and the old solid red bar.
public class GameHUD : MonoBehaviour
{
    public Font font;
    Canvas canvas;
    PlayerHealth health; PlayerRewards rewards; DimensionController dimension; PlayerAttack attack; RunProgress run;
    TMP_Text vitality,score,rank,world,shiftTime,dashTime,hint,checkpoint,ending;
    TMP_Text outcomeSubtitle,outcomeScore,outcomeStats,primaryLabel;
    Button primaryButton, restartButton;
    RectTransform outcomeCard;
    Image outcomeAccent;
    bool wasOver;
    DemoSession demo;
    CanvasGroup startMenu,bossCard,pauseMenu;
    Button resumeButton;
    bool wasPaused;
    TMP_Text journeyClock,objective,bossTitle,bossState;
    Image bossFill;
    RiftWarden warden;
    TrialArena trial;
    float nextBossSearch,nextInteractionSearch,noticeFlash;
    QuestInteractable[] interactions;
    RectTransform interactionCard;
    CanvasGroup interactionGroup;
    TMP_Text interactionLabel;
    string previousNotice;

    public bool OutcomeVisible => modal != null && modal.alpha > .9f;
    Image[] shards=new Image[5]; Image xpFill,shiftFill,dashFill;
    CanvasGroup toast,modal;
    Sprite rounded,diamond;
    TMP_FontAsset displayFont;
    TMP_Text power;
    Color cyan=new Color(.39f,.92f,1),violet=new Color(.76f,.54f,1),gold=new Color(1,.8f,.4f);
    void Start()
    {
        health=GetComponent<PlayerHealth>();rewards=GetComponent<PlayerRewards>();dimension=GetComponent<DimensionController>();attack=GetComponent<PlayerAttack>();run=GetComponent<RunProgress>();
        if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(health.healthBarFill!=null)health.healthBarFill.transform.root.gameObject.SetActive(false);
        if(attack.cooldownText!=null)attack.cooldownText.gameObject.SetActive(false);
        displayFont=TMP_FontAsset.CreateFontAsset(font);
        rounded=MakeShape(false);diamond=MakeShape(true);
        var go=new GameObject("Dimension HUD",typeof(RectTransform));canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;go.AddComponent<GraphicRaycaster>();
        var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var status=Panel("Vitality card",canvas.transform,new Vector2(0,1),new Vector2(28,-28),new Vector2(360,176));
        Label(status,"VITALITY",new Vector2(22,-12),new Vector2(160,24),16,new Color(.65f,.76f,.85f));
        for(int i=0;i<5;i++)
        {
            var back=Picture(status,"Empty shard",new Vector2(23+i*43,-47),new Vector2(30,34),new Color(.15f,.23f,.31f),diamond);
            shards[i]=Picture(status,"Vitality shard",new Vector2(26+i*43,-50),new Vector2(24,28),cyan,diamond);
        }
        vitality=Label(status,"",new Vector2(247,-52),new Vector2(96,30),20,Color.white);
        score=Label(status,"",new Vector2(22,-92),new Vector2(140,28),19,gold);
        rank=Label(status,"",new Vector2(175,-93),new Vector2(162,25),16,new Color(.7f,.8f,.9f));
        Picture(status,"XP track",new Vector2(22,-125),new Vector2(316,5),new Color(.13f,.2f,.28f),rounded);
        xpFill=Picture(status,"XP",new Vector2(22,-125),new Vector2(316,5),gold,rounded);xpFill.type=Image.Type.Filled;xpFill.fillMethod=Image.FillMethod.Horizontal;
        power=Label(status,"",new Vector2(22,-142),new Vector2(316,25),15,new Color(.7f,.85f,.95f));
        var skills=Panel("Dimension card",canvas.transform,new Vector2(1,1),new Vector2(-28,-28),new Vector2(330,145));
        world=Label(skills,"",new Vector2(20,-13),new Vector2(290,28),22,cyan);
        Label(skills,"Q  SHIFT",new Vector2(20,-53),new Vector2(145,24),17,Color.white);
        shiftTime=Label(skills,"",new Vector2(220,-53),new Vector2(90,24),17,cyan);shiftTime.alignment=TextAlignmentOptions.MidlineRight;
        Picture(skills,"Shift track",new Vector2(20,-83),new Vector2(290,6),new Color(.13f,.2f,.28f),rounded);
        shiftFill=Picture(skills,"Shift ready",new Vector2(20,-83),new Vector2(290,6),cyan,rounded);shiftFill.type=Image.Type.Filled;shiftFill.fillMethod=Image.FillMethod.Horizontal;
        Label(skills,"B  PHASE DASH",new Vector2(20,-103),new Vector2(190,24),16,new Color(.7f,.8f,.9f));
        dashTime=Label(skills,"",new Vector2(220,-103),new Vector2(90,24),16,Color.white);dashTime.alignment=TextAlignmentOptions.MidlineRight;
        dashFill=Picture(skills,"Dash ready",new Vector2(20,-133),new Vector2(290,3),new Color(.7f,.8f,.9f),rounded);dashFill.type=Image.Type.Filled;dashFill.fillMethod=Image.FillMethod.Horizontal;
        var note=Panel("Trail note",canvas.transform,new Vector2(.5f,0),new Vector2(0,105),new Vector2(820,120));toast=note.gameObject.AddComponent<CanvasGroup>();
        Label(note,"TRAIL NOTE",new Vector2(24,-12),new Vector2(770,24),18,gold);
        hint=Label(note,"",new Vector2(24,-40),new Vector2(770,63),23,Color.white);hint.textWrappingMode=TextWrappingModes.Normal;
        checkpoint=Label(canvas.transform,"",new Vector2(28,-226),new Vector2(440,30),17,new Color(.65f,.8f,.9f));
        var controls=Panel("Controls",canvas.transform,new Vector2(.5f,0),new Vector2(0,18),new Vector2(960,40));
        var help=Label(controls,"A / D Move   SPACE Jump   V Strike   C Cast   E Use   M Music   ESC Pause",new Vector2(12,-4),new Vector2(936,30),17,new Color(.72f,.81f,.9f));help.alignment=TextAlignmentOptions.Midline;
        Picture(note,"Notice accent",new Vector2(0,-5),new Vector2(6,110),gold,rounded);
        interactionCard=Panel("Nearby interaction",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(400,64));
        interactionCard.GetComponent<Image>().color=new Color(.025f,.13f,.2f,.97f);
        interactionGroup=interactionCard.gameObject.AddComponent<CanvasGroup>();interactionGroup.alpha=0;interactionGroup.blocksRaycasts=false;
        interactionLabel=Label(interactionCard,"",new Vector2(10,-5),new Vector2(380,54),23,cyan);interactionLabel.alignment=TextAlignmentOptions.Midline;interactionLabel.textWrappingMode=TextWrappingModes.Normal;
        BuildOutcome();
        BuildDemoUI();
        BuildPauseUI();
        toast.alpha=modal.alpha=0;modal.interactable=false;modal.blocksRaycasts=false;

    }
    void Update()
    {
        if(canvas==null)return;
        UpdateDemoUI();
        UpdateInteraction();
        float fraction=health.maxHealth>0?health.currentHealth/health.maxHealth:0;
        for(int i=0;i<5;i++)shards[i].color=Color.Lerp(new Color(.13f,.2f,.28f),health.IsInvincible?Color.white:cyan,Mathf.Clamp01(fraction*5-i));
        vitality.text=Mathf.CeilToInt(health.currentHealth)+" / "+Mathf.CeilToInt(health.maxHealth);
        score.text=rewards.Score.ToString("N0")+"  pts";rank.text="RESONANCE  "+rewards.Level;xpFill.fillAmount=rewards.LevelProgress;
        power.text="BLADE "+attack.EffectiveNormalDamage+"   DASH "+attack.EffectiveDashDamage+"   RECOVERY "+attack.EffectiveDashCooldown.ToString("F1")+"s";
        Color accent=dimension.Current==0?cyan:violet;world.text=dimension.Current==0?"FROST  /  waking world":"ECHO  /  spirit world";world.color=accent;
        shiftTime.text=dimension.CooldownRemaining>0?dimension.CooldownRemaining.ToString("F1")+" s":"READY";
        shiftFill.fillAmount=1-dimension.CooldownRemaining/Mathf.Max(.1f,dimension.cooldown);shiftFill.color=accent;
        dashTime.text=attack.DashCooldownRemaining>0?attack.DashCooldownRemaining.ToString("F1")+" s":"READY";
        dashFill.fillAmount=1-attack.DashCooldownRemaining/Mathf.Max(.1f,attack.EffectiveDashCooldown);
        hint.text=run.ActiveMessage;
        if(hint.text!=previousNotice){previousNotice=hint.text;noticeFlash=1;}
        noticeFlash=Mathf.Max(0,noticeFlash-Time.unscaledDeltaTime*.7f);
        toast.GetComponent<Image>().color=Color.Lerp(new Color(.035f,.065f,.11f,.96f),new Color(.12f,.21f,.28f,.98f),noticeFlash);
        hint.color=Color.Lerp(Color.white,gold,noticeFlash*.6f);toast.alpha=Mathf.MoveTowards(toast.alpha,string.IsNullOrEmpty(hint.text)?0:1,Time.unscaledDeltaTime*4);
        checkpoint.text="SANCTUARY  /  "+run.CheckpointName;
        bool over=health.IsDead||run.Completed;
        modal.alpha=Mathf.MoveTowards(modal.alpha,over?1:0,Time.unscaledDeltaTime*2.8f);
        modal.interactable=over;modal.blocksRaycasts=over;
        outcomeCard.localScale=Vector3.one*Mathf.Lerp(.95f,1,modal.alpha);
        if(over)
        {
            bool won=run.Completed;Color light=won?gold:violet;
            ending.text=won?"RIFT RESTORED":"YOUR LIGHT FADES";ending.color=light;
            outcomeSubtitle.text=demo!=null ? "TIME  "+demo.Clock+"     ·     FALLS  "+demo.Deaths : won?"Two worlds. One path home.":"The sanctuary remembers you. Your progress is safe.";
            outcomeScore.text=rewards.Score.ToString("N0")+"  POINTS";
            outcomeStats.text=rewards.Defeated+"  ENEMIES     •     "+rewards.Relics+"  RELICS     •     RESONANCE "+rewards.Level;
            primaryLabel.text=won?"R   JOURNEY AGAIN":"R   RETURN TO SANCTUARY";
            restartButton.gameObject.SetActive(!won);outcomeAccent.color=light;
            if(!wasOver&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(primaryButton.gameObject);
            if(Input.GetKeyDown(KeyCode.N))RestartJourney();
        }
        wasOver=over;

    }
    void BuildDemoUI()
    {
        demo=GetComponent<DemoSession>();if(demo==null)return;
        var top=Panel("Journey clock",canvas.transform,new Vector2(.5f,1),new Vector2(0,-28),new Vector2(660,114));
        journeyClock=Label(top,"00:00",new Vector2(20,-8),new Vector2(620,34),27,gold);journeyClock.alignment=TextAlignmentOptions.Midline;
        objective=Label(top,"",new Vector2(20,-48),new Vector2(620,56),21,cyan);objective.alignment=TextAlignmentOptions.Midline;objective.textWrappingMode=TextWrappingModes.Normal;
        var boss=Panel("Warden vitality",canvas.transform,new Vector2(.5f,1),new Vector2(0,-160),new Vector2(620,92));bossCard=boss.gameObject.AddComponent<CanvasGroup>();bossCard.alpha=0;
        bossTitle=Label(boss,"RIFT WARDEN",new Vector2(20,-8),new Vector2(580,28),22,gold);bossTitle.alignment=TextAlignmentOptions.Midline;
        Picture(boss,"Armor track",new Vector2(20,-43),new Vector2(580,8),new Color(.12f,.14f,.2f),rounded);
        bossFill=Picture(boss,"Core vitality",new Vector2(20,-43),new Vector2(580,8),violet,rounded);bossFill.type=Image.Type.Filled;bossFill.fillMethod=Image.FillMethod.Horizontal;
        bossState=Label(boss,"",new Vector2(20,-57),new Vector2(580,24),16,Color.white);bossState.alignment=TextAlignmentOptions.Midline;
        var veil=new GameObject("Start menu",typeof(RectTransform));veil.transform.SetParent(canvas.transform,false);var rect=veil.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var shade=veil.AddComponent<Image>();shade.color=new Color(.012f,.02f,.05f,.91f);startMenu=veil.AddComponent<CanvasGroup>();
        var card=Panel("Journey begins",veil.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,650));
        Picture(card,"Echo crest",new Vector2(414,-30),new Vector2(72,80),violet,diamond);
        var title=Label(card,"DIMENSION SHIFTER",new Vector2(30,-130),new Vector2(840,70),48,Color.white);title.alignment=TextAlignmentOptions.Midline;
        var sub=Label(card,"HOLLOW DIMENSIONS",new Vector2(30,-207),new Vector2(840,35),23,gold);sub.alignment=TextAlignmentOptions.Midline;
        var story=Label(card,"A key in one world. A door in another.\nFind the lost spirit and break the Warden's seal.",new Vector2(65,-267),new Vector2(770,78),23,new Color(.77f,.84f,.94f));story.alignment=TextAlignmentOptions.Midline;story.textWrappingMode=TextWrappingModes.Normal;
        var keys=Label(card,"A / D Move   ·   SPACE Jump / Air jump   ·   V Strike\nB Phase dash   ·   Q Change world   ·   E Interact\nC Spirit bolt (unlock in the hidden sanctuary)",new Vector2(65,-370),new Vector2(770,90),20,new Color(.6f,.84f,.94f));keys.alignment=TextAlignmentOptions.Midline;keys.textWrappingMode=TextWrappingModes.Normal;
        TMP_Text label;var button=ActionButton(card,"Begin journey",new Vector2(200,-505),new Vector2(500,62),new Color(.2f,.28f,.46f),()=>demo.Begin(),out label);label.text="ENTER   BEGIN JOURNEY";
        var foot=Label(card,"DEMO "+Application.version+"  ·  Checkpoints remember discoveries during this run.",new Vector2(60,-592),new Vector2(780,28),17,new Color(.64f,.71f,.83f));foot.alignment=TextAlignmentOptions.Midline;
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
    void UpdateDemoUI()
    {
        if(demo==null)return;
        if(!demo.Started&&Input.GetKeyDown(KeyCode.Return))demo.Begin();
        bool paused=demo.Paused;
        if(pauseMenu!=null)
        {
            pauseMenu.alpha=paused?1:0;pauseMenu.interactable=pauseMenu.blocksRaycasts=paused;
            if(paused!=wasPaused&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(paused?resumeButton.gameObject:null);
            wasPaused=paused;
        }
        bool menu=!demo.Started;startMenu.alpha=menu?1:0;startMenu.interactable=startMenu.blocksRaycasts=menu;
        journeyClock.text=demo.Clock;objective.text=demo.Objective;
        if(demo.Relays==3&&!demo.TrialCleared){if(trial==null)trial=FindAnyObjectByType<TrialArena>();if(trial!=null&&trial.Active)objective.text=trial.Status;}
        if(warden==null&&Time.unscaledTime>=nextBossSearch){warden=FindAnyObjectByType<RiftWarden>();nextBossSearch=Time.unscaledTime+.5f;}
        bossCard.alpha=warden!=null&&warden.Engaged?1:0;
        if(warden!=null&&warden.Engaged)
        {
            var hp=warden.GetComponent<EnemyHealth>();bossFill.fillAmount=(float)hp.CurrentHealth/hp.maxHealth;
            bossFill.color=warden.CoreDimension==0?cyan:violet;
            bossTitle.text=warden.Enraged?"RIFT WARDEN  /  ENRAGED":"RIFT WARDEN";
            bossState.text=warden.State==RiftWarden.Phase.Exposed?(warden.CoreDimension==0?"BLUE CORE OPEN · attack in FROST":"VIOLET CORE OPEN · attack in ECHO"):warden.State==RiftWarden.Phase.BeamWindup?"ENERGY LANCE · aim locks, then DASH or JUMP":warden.State==RiftWarden.Phase.Leap?"LEAP STRIKE · move out of the landing path":warden.State==RiftWarden.Phase.Windup?"HEAVY STRIKE INCOMING · jump or phase dash":"ARMORED · evade, then punish its recovery";
        }
    }
    void UpdateInteraction()
    {
        interactionGroup.alpha=0;
        if(demo==null||!demo.Started||demo.Paused||health.IsDead||run.Completed)return;
        if(Time.unscaledTime>=nextInteractionSearch){interactions=FindObjectsByType<QuestInteractable>();nextInteractionSearch=Time.unscaledTime+.4f;}
        QuestInteractable nearest=null;float distance=3.4f;
        if(interactions!=null)foreach(var q in interactions)
        {
            if(q==null||q.kind==QuestInteractable.Kind.Relay||q.kind==QuestInteractable.Kind.EchoKey||(q.kind==QuestInteractable.Kind.Spirit&&demo.HasSpirit))continue;
            float d=Vector2.Distance(transform.position,q.transform.position);if(d<distance){distance=d;nearest=q;}
        }
        if(nearest==null)return;
        bool wrong=nearest.dimension>=0&&nearest.dimension!=dimension.Current;
        interactionLabel.text=wrong?"Q  →  SWITCH TO FROST":nearest.kind==QuestInteractable.Kind.BlueDoor?(demo.HasEchoKey?"[ E ]   ENTER SANCTUARY":"LOCKED · FIND THE ECHO KEY"):nearest.kind==QuestInteractable.Kind.Spirit?"SPIRIT BOLT\nWALK INTO THE LIGHT":!demo.HasSpirit?"COLLECT THE SPIRIT FIRST":"[ E ]   RETURN TO TRAIL";
        interactionLabel.color=wrong?violet:nearest.kind==QuestInteractable.Kind.BlueDoor&&!demo.HasEchoKey?gold:cyan;
        Vector3 anchor=nearest.transform.position+Vector3.up*1.6f;
        if(nearest.visual!=null)anchor.y=nearest.visual.bounds.max.y+.5f;
        var screen=Camera.main.WorldToScreenPoint(anchor);if(screen.z<0)return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(),screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:Camera.main,out var local);
        var bounds=canvas.GetComponent<RectTransform>().rect;
        interactionCard.anchoredPosition=new Vector2(Mathf.Clamp(local.x,bounds.xMin+210,bounds.xMax-210),Mathf.Clamp(local.y,bounds.yMin+220,bounds.yMax-235));
        interactionGroup.alpha=1;
    }
    void BuildPauseUI()
    {
        if(demo==null)return;
        var veil=new GameObject("Pause menu",typeof(RectTransform));veil.transform.SetParent(canvas.transform,false);
        var rect=veil.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        veil.AddComponent<Image>().color=new Color(.012f,.02f,.045f,.88f);
        pauseMenu=veil.AddComponent<CanvasGroup>();pauseMenu.alpha=0;pauseMenu.interactable=pauseMenu.blocksRaycasts=false;
        var card=Panel("Pause card",veil.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,460));
        Picture(card,"Pause crest",new Vector2(337,-32),new Vector2(46,52),cyan,diamond);
        var title=Label(card,"BETWEEN WORLDS",new Vector2(30,-110),new Vector2(660,55),38,Color.white);title.alignment=TextAlignmentOptions.Midline;
        var note=Label(card,"Take a breath. Your journey is paused.",new Vector2(30,-174),new Vector2(660,35),21,new Color(.74f,.82f,.92f));note.alignment=TextAlignmentOptions.Midline;
        TMP_Text label;resumeButton=ActionButton(card,"Resume journey",new Vector2(110,-238),new Vector2(500,58),new Color(.16f,.29f,.42f),()=>demo.SetPaused(false),out label);label.text="ESC   RESUME JOURNEY";
        ActionButton(card,"Return to title",new Vector2(160,-318),new Vector2(400,48),new Color(.12f,.15f,.24f),()=>{demo.SetPaused(false);SceneManager.LoadScene(SceneManager.GetActiveScene().name);},out label);label.text="RETURN TO TITLE";
        var foot=Label(card,"Returning to title resets this run.",new Vector2(30,-396),new Vector2(660,28),17,new Color(.64f,.71f,.83f));foot.alignment=TextAlignmentOptions.Midline;
    }
    void BuildOutcome()
    {
        var veil=new GameObject("Outcome menu",typeof(RectTransform));veil.transform.SetParent(canvas.transform,false);
        var rect=veil.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var shade=veil.AddComponent<Image>();shade.color=new Color(.012f,.02f,.045f,.87f);shade.raycastTarget=true;
        modal=veil.AddComponent<CanvasGroup>();
        outcomeCard=Panel("Sanctuary outcome",veil.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(760,530));
        Picture(outcomeCard,"Crest glow",new Vector2(349,-34),new Vector2(62,70),new Color(.32f,.2f,.55f),diamond);
        outcomeAccent=Picture(outcomeCard,"Crest",new Vector2(363,-49),new Vector2(34,40),violet,diamond);
        ending=Label(outcomeCard,"",new Vector2(30,-125),new Vector2(700,60),43,Color.white);ending.alignment=TextAlignmentOptions.Midline;
        outcomeSubtitle=Label(outcomeCard,"",new Vector2(35,-192),new Vector2(690,35),20,new Color(.74f,.8f,.9f));outcomeSubtitle.alignment=TextAlignmentOptions.Midline;
        Picture(outcomeCard,"Divider",new Vector2(100,-247),new Vector2(560,2),new Color(.35f,.5f,.65f,.5f),rounded);
        outcomeScore=Label(outcomeCard,"",new Vector2(35,-268),new Vector2(690,42),32,gold);outcomeScore.alignment=TextAlignmentOptions.Midline;
        outcomeStats=Label(outcomeCard,"",new Vector2(35,-316),new Vector2(690,32),17,new Color(.73f,.81f,.9f));outcomeStats.alignment=TextAlignmentOptions.Midline;
        primaryButton=ActionButton(outcomeCard,"Return to sanctuary",new Vector2(130,-373),new Vector2(500,54),new Color(.16f,.29f,.42f),RetryFromOutcome,out primaryLabel);
        TMP_Text secondary;restartButton=ActionButton(outcomeCard,"Restart journey",new Vector2(230,-445),new Vector2(300,40),new Color(.12f,.15f,.24f),RestartJourney,out secondary);secondary.text="N   RESTART JOURNEY";secondary.fontSize=17;
    }
    Button ActionButton(Transform parent,string name,Vector2 p,Vector2 size,Color color,UnityEngine.Events.UnityAction action,out TMP_Text label)
    {
        var image=Picture(parent,name,p,size,color,rounded);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.highlightedColor=new Color(1.25f,1.25f,1.25f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.7f,.8f,1);button.colors=colors;
        button.onClick.AddListener(action);label=Label(image.transform,"",new Vector2(8,-3),size-new Vector2(16,6),20,Color.white);label.alignment=TextAlignmentOptions.Midline;return button;
    }
    public void RetryFromOutcome()
    {
        if(run.Completed){RestartJourney();return;}
        if(!health.IsDead)return;
        run.Respawn();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
    }
    public void RestartJourney()
    {
        if(!health.IsDead&&!run.Completed)return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=anchor;rt.pivot=anchor;rt.anchoredPosition=pos;rt.sizeDelta=size;
        var img=go.AddComponent<Image>();img.sprite=rounded;img.type=Image.Type.Sliced;img.color=new Color(.035f,.065f,.11f,.91f);img.raycastTarget=false;
        var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.35f,.6f,.75f,.28f);outline.effectDistance=new Vector2(1,-1);return rt;
    }
    RectTransform Element(string name,Transform parent,Vector2 pos,Vector2 size)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=pos;rt.sizeDelta=size;return rt;}
    Image Picture(Transform parent,string name,Vector2 pos,Vector2 size,Color color,Sprite sprite)
    {var image=Element(name,parent,pos,size).gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;if(sprite==rounded)image.type=Image.Type.Sliced;return image;}
    TMP_Text Label(Transform parent,string text,Vector2 pos,Vector2 size,int fontSize,Color color)
    {var t=Element("Label",parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=displayFont;t.fontSize=fontSize;t.color=color;t.text=text;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Overflow;t.textWrappingMode=TextWrappingModes.NoWrap;return t;}
    // Native UI primitives: anti-aliased rounded panels and original vitality diamonds.
    Sprite MakeShape(bool gem)
    {
        const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
        var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {float dx=Mathf.Abs(x-31.5f),dy=Mathf.Abs(y-31.5f);float distance=gem?(dx+dy-29):new Vector2(Mathf.Max(0,dx-20),Mathf.Max(0,dy-20)).magnitude-11;pixels[y*n+x]=new Color(1,1,1,Mathf.Clamp01(.5f-distance));}
        texture.SetPixels(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
    }
    void OnDestroy(){if(displayFont!=null){foreach(var atlas in displayFont.atlasTextures)if(atlas!=null)Destroy(atlas);if(displayFont.material!=null)Destroy(displayFont.material);Destroy(displayFont);}if(canvas!=null)Destroy(canvas.gameObject);if(rounded!=null){Destroy(rounded.texture);Destroy(rounded);}if(diamond!=null){Destroy(diamond.texture);Destroy(diamond);}}
}
