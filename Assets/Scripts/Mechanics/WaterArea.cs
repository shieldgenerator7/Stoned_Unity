using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterArea : MonoBehaviour
{
    public float minSpeed;//the minimum speed in order to apply dampen
    [Range(0, 1)]
    public float dampenFactor;//how much to dampen momentum by

    private Collider2D coll2d;

    private void Start()
    {
        coll2d = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        Utility.RaycastAnswer rca = coll2d.CastAnswer(Vector2.zero, 0, true);
        for (int i = 0; i < rca.count; i++)
        {
            RaycastHit2D rch2d = rca.rch2ds[i];
            if (!rch2d.collider.isTrigger)
            {
                Rigidbody2D rb2d = rch2d.collider.GetComponent<Rigidbody2D>();
                if (rb2d)
                {
                    float speed = rb2d.velocity.magnitude;
                    if (speed >= minSpeed)
                    {
                        rb2d.velocity = Vector2.Lerp(
                            rb2d.velocity,
                            rb2d.velocity * (1 - dampenFactor),
                            Time.deltaTime
                            );
                    }
                }
            }
        }
    }
}
