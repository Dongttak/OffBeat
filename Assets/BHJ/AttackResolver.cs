using System;
using UnityEngine;

public interface IHittable
{
    void Hit(int damage = 1);
}

public class AttackResolver : MonoBehaviour
{
    [Header("Raycast")]
    public Transform fireOrigin;       // PlayerAttack.firePosition과 같은 Transform
    public float range = 30f;
    public LayerMask hittableMask;     // EX: LayerMask.GetMask("Summon","Enemy")

    private void OnEnable() { PlayerInput.AttackEvent += OnAttack; }
    private void OnDisable() { PlayerInput.AttackEvent -= OnAttack; }

    private void OnAttack()
    {
        if (!fireOrigin) return;

        if (Physics.Raycast(
                    fireOrigin.position,
                    Vector3.forward,
                    out var hit,
                    range,
                    hittableMask,
                    QueryTriggerInteraction.Collide))
        {
            // 1) 소환물 판정 (보스 보호/관통 차단)
            var summon = hit.collider.GetComponentInParent<Pattern2>()
                      ?? hit.collider.GetComponent<Pattern2>();
            if (summon != null)
            {
                summon.Hit(1);
                return; // 여기서 종료
            }

            // 2) 그 외 IHittable (Enemy)
            var target = hit.collider.GetComponentInParent<IHittable>()
                      ?? hit.collider.GetComponent<IHittable>();
            if (target != null)
            {
                target.Hit(1);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!fireOrigin) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(fireOrigin.position, fireOrigin.forward * range); 
    }
#endif
}

// PlayerAttack 코드 따로 있어 리팩토링 요망.