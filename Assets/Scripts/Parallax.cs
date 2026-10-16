using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class Parallax : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [Range(0f, 1f)] public float parallaxEffect;
    private SpriteRenderer source;
    private Camera viewCamera;
    private Vector3 startPosition;
    private float tileWidth;
    private float worldTileWidth;
    private float authoredWidth;

    void Start()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        source = GetComponent<SpriteRenderer>();
        if (cam == null || source.sprite == null) { enabled = false; return; }
        viewCamera = cam.GetComponent<Camera>();
        startPosition = transform.position;
        tileWidth = source.sprite.rect.width / source.sprite.pixelsPerUnit;
        worldTileWidth = tileWidth * Mathf.Abs(transform.lossyScale.x);
        authoredWidth = source.drawMode == SpriteDrawMode.Simple ? tileWidth : source.size.x;
        if (worldTileWidth <= 0f) { enabled = false; return; }
        // The authored backgrounds already contain three tiled repetitions.
        // A single tiled renderer avoids alpha overlap between fog copies.
        if (source.drawMode == SpriteDrawMode.Simple) {
            source.size = source.sprite.rect.size / source.sprite.pixelsPerUnit;
            source.drawMode = SpriteDrawMode.Tiled;
        }
        source.tileMode = SpriteTileMode.Continuous;
        ResizeStrip();
    }

    void LateUpdate()
    {
        if (cam == null || source == null || worldTileWidth <= 0f) return;
        float relativeCamera = cam.position.x * (1f - parallaxEffect) - startPosition.x;
        float tile = Mathf.Round(relativeCamera / worldTileWidth);
        // Preserve the original scene's absolute camera offsets: its layers were
        // positioned for this calibration, including the vertical forest overlap.
        transform.position = new Vector3(startPosition.x + cam.position.x * parallaxEffect + tile * worldTileWidth,
            startPosition.y + cam.position.y * parallaxEffect, startPosition.z);
        ResizeStrip();
    }

    private void ResizeStrip()
    {
        float viewWidth = worldTileWidth;
        if (viewCamera != null) {
            float depth = Vector3.Dot(transform.position - cam.position, cam.forward);
            viewWidth = Mathf.Abs(viewCamera.ViewportToWorldPoint(new Vector3(1,.5f,depth)).x
                - viewCamera.ViewportToWorldPoint(new Vector3(0,.5f,depth)).x);
        }
        float offset = Mathf.Abs(source.bounds.center.x - cam.position.x);
        float needed = (viewWidth + 2f * offset + 2f * worldTileWidth) / Mathf.Abs(transform.lossyScale.x);
        // Whole texture repeats preserve the phase when the strip recenters.
        int tiles = Mathf.Max(3, Mathf.CeilToInt(Mathf.Max(authoredWidth, needed) / tileWidth));
        int authoredTiles = Mathf.Max(1, Mathf.RoundToInt(authoredWidth / tileWidth));
        // Grow symmetrically by two repeats so the authored texture phase stays fixed.
        if (tiles % 2 != authoredTiles % 2) tiles++;
        source.size = new Vector2(tiles * tileWidth, source.size.y);
    }
}
