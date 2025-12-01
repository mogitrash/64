using UnityEngine;
using WAD64.Core;

public class AngleToPlayer : MonoBehaviour
{
    private Vector3 targetPos;
    private Vector3 targetDir;
    private SpriteRenderer spriteRenderer;


    private float angle;
    public int lastIndex;
    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        MonoBehaviour player = CoreReferences.Player;
        // Get Target Position and Direction
        targetPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
        targetDir = targetPos - transform.position;
        // Get Angle
        angle = Vector3.SignedAngle(from: targetDir, to: transform.forward, axis: Vector3.up);

        Vector3 tempScale = Vector3.one;
        if (angle > 0.5f)
        {
            tempScale.x = -1f;
        }

        spriteRenderer.transform.localScale = tempScale;

        lastIndex = GetIndex(angle);
    }

    private int GetIndex(float angle)
    {
        //front
        if (angle > -22.5f && angle < 22.6f)
            return 0;
        if (angle >= 22.5f && angle < 67.5f)
            return 7;
        if (angle >= 67.5f && angle < 112.5f)
            return 6;
        if (angle >= 112.5f && angle < 157.5f)
            return 5;

        //back
        if (angle <= -157.5 ||
        angle >= 157.5f)
            return 4;
        if (angle >= -157.4f &&
        angle < -112.5f)
            return 3;
        if (angle >= -112.5f && angle < -67.5f)
            return 2;
        if (angle >= -67.5f && angle <= -22.5f)
            return 1;

        return lastIndex;
    }
}
