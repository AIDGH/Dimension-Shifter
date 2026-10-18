using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class StoryIntro:MonoBehaviour
{
 Canvas canvas;CanvasGroup group;Image art;Text title,body,footer;int page;float age;bool dismissed;
 static readonly string[] titles={"THE KEEPER'S APPRENTICE","THE NIGHT THE WORLD SPLIT","A WAY HOME"};
 static readonly string[] words={"You grew up beneath the ancient tree, tending its lanterns and listening to the echoes carried by its roots.","One night, a lantern answered from a world that should not exist. You reached for its light — and the rift swallowed the path home.","Frost and Echo now share the same broken forest. Find the lost spirit, overcome the Rift Warden, and bring its sigil to the tree. Your way home waits beyond its roots."};
 Sprite[] images;Font font;
 void Start(){font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");images=Resources.LoadAll<Sprite>("Story");var g=new GameObject("Story prologue",typeof(RectTransform));canvas=g.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=250;g.AddComponent<GraphicRaycaster>();var sc=g.AddComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1600,900);group=g.AddComponent<CanvasGroup>();
 var bg=new GameObject("Backdrop",typeof(RectTransform),typeof(Image));bg.transform.SetParent(g.transform,false);var br=bg.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;bg.GetComponent<Image>().color=new Color(.025f,.04f,.08f,1);
 var pic=new GameObject("Story artwork",typeof(RectTransform),typeof(Image));pic.transform.SetParent(g.transform,false);art=pic.GetComponent<Image>();art.preserveAspect=true;var pr=pic.GetComponent<RectTransform>();pr.sizeDelta=new Vector2(1400,610);pr.anchoredPosition=new Vector2(0,100);
 title=Text(g.transform,36,new Vector2(0,-150),new Vector2(1200,65));body=Text(g.transform,25,new Vector2(0,-245),new Vector2(1100,120));footer=Text(g.transform,18,new Vector2(0,-370),new Vector2(1100,40));footer.text="SPACE / CLICK: NEXT     ·     ESC: SKIP     ·     H: CHANGE CHARACTER AT ANY TIME";Show();}
 Text Text(Transform p,int size,Vector2 pos,Vector2 bounds){var g=new GameObject("Story text",typeof(RectTransform));g.transform.SetParent(p,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=bounds;r.anchoredPosition=pos;var t=g.AddComponent<Text>();t.font=font;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.color=new Color(.87f,.93f,1);return t;}
 void Show(){age=0;title.text=titles[page];body.text=page==2?"Frost and Echo share the broken forest. Find the violet key and the lost spirit, cross into the sanctum, and overcome the Rift Warden. The final sanctuary holds your way home.":words[page];art.sprite=images.Length>0?images[Mathf.Min(page,images.Length-1)]:null;art.enabled=art.sprite!=null;}
 void Update(){if(canvas==null||dismissed)return;if(DemoSession.Instance==null||DemoSession.Instance.Started){Dismiss();return;}age+=Time.unscaledDeltaTime;group.alpha=Mathf.Clamp01(age/.65f);art.color=new Color(.6f,.75f,.9f,.65f);if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.Return)){Dismiss();return;}if(age>.3f&&(Input.GetKeyDown(KeyCode.Space)||Input.GetMouseButtonDown(0))||age>10){if(++page>=titles.Length)Dismiss();else Show();}}
 public void Dismiss(){dismissed=true;if(canvas!=null)canvas.gameObject.SetActive(false);}
 void OnDestroy(){if(canvas!=null)Destroy(canvas.gameObject);}
}
