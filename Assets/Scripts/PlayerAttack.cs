using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Transform firePosition;
    [SerializeField] private float range = 30f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Hit Feedback (optional)")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private bool flashOnHit = true;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashTime = 0.08f;

    public void Attack()
    {
        Vector3 origin = firePosition ? firePosition.position : transform.position;
        Vector3 dir = firePosition ? firePosition.forward : transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, triggerInteraction))
        {
            Debug.DrawLine(origin, hit.point, Color.yellow, 0.25f);

            if (hit.transform.CompareTag(enemyTag))
            {
                var rend = hit.transform.GetComponent<Renderer>();
                if (flashOnHit && rend && rend.material && rend.material.HasProperty("_Color"))
                    StartCoroutine(Flash(rend));
            }
        }
        else
        {
            Debug.DrawRay(origin, dir * range, Color.cyan, 0.25f);
        }
    }

    private System.Collections.IEnumerator Flash(Renderer rend)
    {
        var mat = rend.material;
        if (!mat.HasProperty("_Color")) yield break;
        Color original = mat.color;
        mat.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        mat.color = original;
    }
}
