using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;
using System;

public class BounceRay : MonoBehaviour
{
    public GameObject hitEffect;

    void Start()
    {
    }

    void Update()
    {
    }

    int hit_count = 0;
    Vector3[] line_segment = new Vector3[65];
    float[] distances = new float[64];
    Ray[] ray_sequence = new Ray[64];

    private void OnDrawGizmos()
    {
        for(int i = 0; i < hit_count; i++)
        {
            Gizmos.DrawLine(line_segment[i], line_segment[i+1]);
        }
    }

    RaycastHit[] hits = new RaycastHit[8];

    [Button("Shoot")]
    public void Shoot()
    {
        Recalc();
        StartCoroutine(ShootCoroutine());
    }

    public float bulletSpeed;

    private IEnumerator ShootCoroutine()
    {
        float timeStart = Time.time;
        while(true) {
            float dist = (Time.time - timeStart) * bulletSpeed;
            for(int i = 0; i < hit_count; i++)
            {
                if(distances[i] <= dist && dist < distances[i+1])
                {
                    float segmentDist = dist - distances[i];
                    Vector3 dir = (line_segment[i+1] - line_segment[i]).normalized;
                    Vector3 pos = line_segment[i] + dir * segmentDist;
                    transform.position = pos;
                    transform.rotation = Quaternion.LookRotation(dir);
                    break;
                }
            }
            yield return null;
        }
        //Instantiate(hitEffect, hit.point, Quaternion.identity);
    }

    [Button("Recalc")]
    public void Recalc()
    {
        ray_sequence[0] = new Ray(transform.position, transform.forward);
        line_segment[0] = transform.position;
        distances[0] = 0.0f;

        for(int i = 0; i < ray_sequence.Length; i++)
        {
            Ray ray = ray_sequence[i];
            int num_hits = Physics.RaycastNonAlloc(ray,hits);

            if(num_hits > 0) {
                RaycastHit hit = hits[0];

                Debug.Log($"Hit {i}: {hit.collider.name} at {hit.point} normal {hit.normal}");

                Vector3 out_vector = Vector3.Reflect(ray.direction, hit.normal);

                distances[i+1] = distances[i] + hit.distance;
                ray_sequence[i+1] = new Ray(hit.point + out_vector * 0.01f, out_vector);
                line_segment[i+1] = hit.point;
            }
            else
            {
                hit_count = i+1;
                line_segment[i+1] = ray.origin + ray.direction * 100.0f;
                break;
            }
        }
    }
}
