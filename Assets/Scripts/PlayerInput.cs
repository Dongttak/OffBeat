using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using static UnityEngine.GraphicsBuffer;

public class PlayerInput : MonoBehaviour
{
    private int posIndex = 1;
    public List<Transform> positions;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (posIndex > 0)
                if (TimingManager.instance.IsOntheBeat())
                {
                    transform.DOMove(positions[--posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                }
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (posIndex < 2)
                if (TimingManager.instance.IsOntheBeat())
                {
                    transform.DOMove(positions[++posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (TimingManager.instance.IsOntheBeat())
            {
                BulletManager.instance.Attack();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (TimingManager.instance.IsOntheBeat())
            {
                Debug.Log("Evade");
            }
        }
    }

}
