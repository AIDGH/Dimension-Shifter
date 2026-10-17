using UnityEngine;

// A camera clear color cannot run out at the edge of a vertical chunk. The forest
// layers feather into this depth color using SoftBackground, so no hard strip remains.
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(110)]
public class AtmosphereBackdrop : MonoBehaviour
{
    Camera cameraView;
    void Awake(){cameraView=GetComponent<Camera>();cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.025f,.055f,.09f,1);}
    void OnDisable(){Shader.SetGlobalFloat("_DSFogBottom",0);Shader.SetGlobalFloat("_DSFogTop",0);}
    void LateUpdate()
    {
        float distance=Mathf.Max(.1f,-cameraView.transform.position.z);
        float bottom=cameraView.ViewportToWorldPoint(new Vector3(.5f,0,distance)).y;
        float upper=cameraView.ViewportToWorldPoint(new Vector3(.5f,.32f,distance)).y;
        Shader.SetGlobalFloat("_DSFogBottom",bottom);Shader.SetGlobalFloat("_DSFogTop",upper);
        bool echo=DimensionController.Instance!=null&&DimensionController.Instance.Current==1;
        Color depth=echo?new Color(.055f,.025f,.095f,1):new Color(.025f,.055f,.09f,1);
        cameraView.backgroundColor=Color.Lerp(cameraView.backgroundColor,depth,Time.deltaTime*5f);
    }
}
