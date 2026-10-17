using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One scalable Canvas replaces the debug IMGUI boxes and the old solid red bar.
public class GameHUD : MonoBehaviour
{
    public Font font;
    Canvas canvas;
    PlayerHealth health; PlayerRewards rewards; DimensionController dimension; PlayerAttack attack; RunProgress run;
    TMP_Text vitality,score,rank,world,shiftTime,dashTime,hint,checkpoint,ending;
    Image[] shards=new Image[5]; Image xpFill,shiftFill,dashFill;
    CanvasGroup toast,modal;
    Sprite rounded,diamond;
    TMP_FontAsset displayFont;
    Color cyan=new Color(.39f,.92f,1),violet=new Color(.76f,.54f,1),gold=new Color(1,.8f,.4f);
    void Start()
    {
        health=GetComponent<PlayerHealth>();rewards=GetComponent<PlayerRewards>();dimension=GetComponent<DimensionController>();attack=GetComponent<PlayerAttack>();run=GetComponent<RunProgress>();
        if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(health.healthBarFill!=null)health.healthBarFill.transform.root.gameObject.SetActive(false);
        if(attack.cooldownText!=null)attack.cooldownText.gameObject.SetActive(false);
        displayFont=TMP_FontAsset.CreateFontAsset(font);
        rounded=MakeShape(false);diamond=MakeShape(true);
        var go=new GameObject("Dimension HUD",typeof(RectTransform));canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var status=Panel("Vitality card",canvas.transform,new Vector2(0,1),new Vector2(28,-28),new Vector2(360,145));
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
        var skills=Panel("Dimension card",canvas.transform,new Vector2(1,1),new Vector2(-28,-28),new Vector2(330,145));
        world=Label(skills,"",new Vector2(20,-13),new Vector2(290,28),22,cyan);
        Label(skills,"Q  SHIFT",new Vector2(20,-53),new Vector2(145,24),17,Color.white);
        shiftTime=Label(skills,"",new Vector2(220,-53),new Vector2(90,24),17,cyan);shiftTime.alignment=TextAlignmentOptions.MidlineRight;
        Picture(skills,"Shift track",new Vector2(20,-83),new Vector2(290,6),new Color(.13f,.2f,.28f),rounded);
        shiftFill=Picture(skills,"Shift ready",new Vector2(20,-83),new Vector2(290,6),cyan,rounded);shiftFill.type=Image.Type.Filled;shiftFill.fillMethod=Image.FillMethod.Horizontal;
        Label(skills,"B  PHASE DASH",new Vector2(20,-103),new Vector2(190,24),16,new Color(.7f,.8f,.9f));
        dashTime=Label(skills,"",new Vector2(220,-103),new Vector2(90,24),16,Color.white);dashTime.alignment=TextAlignmentOptions.MidlineRight;
        dashFill=Picture(skills,"Dash ready",new Vector2(20,-133),new Vector2(290,3),new Color(.7f,.8f,.9f),rounded);dashFill.type=Image.Type.Filled;dashFill.fillMethod=Image.FillMethod.Horizontal;
        var note=Panel("Trail note",canvas.transform,new Vector2(.5f,0),new Vector2(0,80),new Vector2(660,105));toast=note.gameObject.AddComponent<CanvasGroup>();
        Label(note,"TRAIL NOTE",new Vector2(24,-12),new Vector2(610,24),15,gold);
        hint=Label(note,"",new Vector2(24,-40),new Vector2(610,53),20,Color.white);hint.textWrappingMode=TextWrappingModes.Normal;
        checkpoint=Label(canvas.transform,"",new Vector2(28,-195),new Vector2(440,30),17,new Color(.65f,.8f,.9f));
        var controls=Panel("Controls",canvas.transform,new Vector2(.5f,0),new Vector2(0,18),new Vector2(680,40));
        var help=Label(controls,"A / D  Move     SPACE  Jump     V  Strike     M  Music",new Vector2(12,-4),new Vector2(656,30),17,new Color(.72f,.81f,.9f));help.alignment=TextAlignmentOptions.Midline;
        var finish=Panel("Outcome",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(540,220));modal=finish.gameObject.AddComponent<CanvasGroup>();
        ending=Label(finish,"",new Vector2(25,-24),new Vector2(490,174),26,Color.white);ending.alignment=TextAlignmentOptions.Midline;
        toast.alpha=modal.alpha=0;
    }
    void Update()
    {
        if(canvas==null)return;
        float fraction=health.maxHealth>0?health.currentHealth/health.maxHealth:0;
        for(int i=0;i<5;i++)shards[i].color=Color.Lerp(new Color(.13f,.2f,.28f),health.IsInvincible?Color.white:cyan,Mathf.Clamp01(fraction*5-i));
        vitality.text=Mathf.CeilToInt(health.currentHealth)+" / "+Mathf.CeilToInt(health.maxHealth);
        score.text=rewards.Score.ToString("N0")+"  pts";rank.text="RESONANCE  "+rewards.Level;xpFill.fillAmount=rewards.LevelProgress;
        Color accent=dimension.Current==0?cyan:violet;world.text=dimension.Current==0?"FROST  /  waking world":"ECHO  /  spirit world";world.color=accent;
        shiftTime.text=dimension.CooldownRemaining>0?dimension.CooldownRemaining.ToString("F1")+" s":"READY";
        shiftFill.fillAmount=1-dimension.CooldownRemaining/Mathf.Max(.1f,dimension.cooldown);shiftFill.color=accent;
        dashTime.text=attack.DashCooldownRemaining>0?attack.DashCooldownRemaining.ToString("F1")+" s":"READY";
        dashFill.fillAmount=1-attack.DashCooldownRemaining/Mathf.Max(.1f,attack.dashCooldown);
        hint.text=run.ActiveMessage;toast.alpha=Mathf.MoveTowards(toast.alpha,string.IsNullOrEmpty(hint.text)?0:1,Time.unscaledDeltaTime*4);
        checkpoint.text="SANCTUARY  /  "+run.CheckpointName;
        bool over=health.IsDead||run.Completed;modal.alpha=Mathf.MoveTowards(modal.alpha,over?1:0,Time.unscaledDeltaTime*4);
        ending.text=run.Completed?"RIFT RESTORED\n\n"+rewards.Score.ToString("N0")+" points   •   "+rewards.Relics+" relics\nR  —  Journey again":"YOUR LIGHT FADES\n\nReturn to "+run.CheckpointName+"\nR  —  Try again";
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
