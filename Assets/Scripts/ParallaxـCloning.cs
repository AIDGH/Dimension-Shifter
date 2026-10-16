// using UnityEngine;

// [DefaultExecutionOrder(100)]
// public class Parallax_Cloning : MonoBehaviour
// {
//     [Header("Camera Reference")]
//     [SerializeField] private Transform cam;

//     [Header("Parallax Settings")]
//     [Range(0f, 1f)]
//     public float parallaxEffect;

//     private float length;
//     private float startposX;
//     private float startposY;

//     void Start()
//     {
//         if (cam == null) cam = Camera.main.transform;
        
//         startposX = transform.position.x;
//         startposY = transform.position.y;
        
//         // پاکسازیِ کلون‌های قدیمیِ تو ادیتور
//         foreach (Transform child in transform) {
//             if (child.name.Contains("_Clone")) {
//                 Destroy(child.gameObject);
//             }
//         }

//         SpriteRenderer mySR = GetComponent<SpriteRenderer>();
        
//         // 💡 طول واقعی عکس تو دنیای بازی (برای تشخیص زمانِ تلپورت دوربین)
//         length = mySR.bounds.size.x; 

//         // 💡 عرضِ خالصِ خود عکس (بدون اسکیل) برای چسبوندن دقیقِ کلون‌ها لبه‌به‌لبه
//         float spriteWidth = mySR.sprite.bounds.size.x;

//         for (int i = -1; i <= 1; i += 2) 
//         {
//             GameObject clone = new GameObject(gameObject.name + "_Clone");
//             clone.transform.SetParent(transform);
            
//             // اسکیل بچه باید ۱ باشه تا فقط اسکیل مادر روش اعمال بشه
//             clone.transform.localScale = Vector3.one;

//             // 💡 شاهکارِ ریاضی: کپی‌ها به اندازه‌ی عرضِ خالصِ عکس میرن چپ و راست
//             // 💡 ترفند هم‌پوشانی: یه مقدار خیلی ناچیز از عرض عکس کم می‌کنیم تا واگن‌ها کسری از میلی‌متر برن تو شکم هم!
//             float overlap = 0.005f;
//             clone.transform.localPosition = new Vector3((spriteWidth - overlap) * i, 0, 0);

//             // کپی کردن گرافیک‌ها
//             SpriteRenderer cloneSR = clone.AddComponent<SpriteRenderer>();
//             cloneSR.sprite = mySR.sprite;
//             cloneSR.color = mySR.color;
//             cloneSR.sortingLayerID = mySR.sortingLayerID;
//             cloneSR.sortingOrder = mySR.sortingOrder;
//             cloneSR.material = mySR.material;
            
//             // ❌ بزرگترین درس امروز: به کلون‌ها اسکریپت Parallax رو نمی‌دیم چون خودشون با مادر حرکت می‌کنن!
//         }
//     }

//     void LateUpdate()
//     {
//         if (cam == null) return;

//         float temp = (cam.position.x * (1 - parallaxEffect));
//         float distX = (cam.position.x * parallaxEffect);
//         float distY = (cam.position.y * parallaxEffect);

//         transform.position = new Vector3(startposX + distX, startposY + distY, transform.position.z);

//         // جادوی تلپورتِ قطار
//         if (temp > startposX + length)
//         {
//             startposX += length;
//         }
//         else if (temp < startposX - length)
//         {
//             startposX -= length;
//         }
//     }
// }