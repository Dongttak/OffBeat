using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Transform firePosition; // 총구 위치
    [SerializeField] private float range = 30f;
    [SerializeField] private LayerMask hitMask = ~0;

    bool attackRequired;

    void OnEnable()
    {
        PlayerInput.AttackEvent += OnAttackRequested;   // OnEnable에서 구독
    }
    void OnDisable()
    {
        PlayerInput.AttackEvent -= OnAttackRequested;   // 해제
    }

    void OnAttackRequested() => attackRequired = true;

    void LateUpdate()
    {
        if (!attackRequired) return;
        attackRequired = false;
        Attack();
    }

    void OnDrawGizmos()
    {
        if (!firePosition) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(firePosition.position, firePosition.forward * range);
    }

    void Attack()
    {
        if (!firePosition)
        {
            Debug.LogWarning("[PlayerAttack] firePosition not assigned");
            return;
        }

        RaycastHit hit;
        // 월드 전방(Vector3.forward) 말고 총구 기준 전방(firePosition.forward) 사용
        if (Physics.Raycast(firePosition.position, firePosition.forward, out hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.CompareTag("Enemy"))
            {
                var mr = hit.transform.GetComponent<MeshRenderer>();
                if (mr) mr.material.color = Color.red;
                Debug.Log($"[PlayerAttack] Hit Enemy: {hit.transform.name}");
            }
            else
            {
                Debug.Log($"[PlayerAttack] Hit: {hit.transform.name} (not Enemy)");
            }
        }
        else
        {
            Debug.Log("[PlayerAttack] Miss");
        }
    }
}
