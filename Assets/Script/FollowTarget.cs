using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] private int followPlayerIndex;

    void FixedUpdate()
    {
        Player target = Player.GetInstance(followPlayerIndex);
        if (target == null)
            target = Player.Instance;
        if (target == null)
            return;
        transform.position = target.transform.position;
    }
}
