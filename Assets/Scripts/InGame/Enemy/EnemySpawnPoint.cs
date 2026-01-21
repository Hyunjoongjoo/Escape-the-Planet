using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, 9f);
    }
#endif
}
