using UnityEngine;
using WAD64.Core;

public class EnemySpriteLook : MonoBehaviour
{
    private Transform target;
    void Start()
    {
        target = CoreReferences.Player.transform;
    }

    void Update()
    {
        Vector3 modifiedTarget = target.position;
        modifiedTarget.y = transform.position.y;
        transform.LookAt(modifiedTarget);
    }
}
