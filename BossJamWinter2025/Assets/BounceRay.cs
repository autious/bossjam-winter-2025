using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;
using System;
using WebSocketSharp;

public class BounceRay : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public GameObject hitEffect;
    public GameObject hitSoundEffect;
    public GameObject hitSoundEffectPlayer;
    public GameObject hitSoundMiss;

    int hit_count = 0;
    bool hit_player = true;
    Vector3[] line_segment = new Vector3[65];
    float[] distances = new float[64];
    Ray[] ray_sequence = new Ray[64];

    private void OnDrawGizmos()
    {
        for(int i = 0; i < hit_count; i++)
        {
            //Gizmos.DrawLine(line_segment[i], line_segment[i+1]);
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
        int i = 1;
        while(i <= hit_count) {
            float dist = (Time.time - timeStart) * bulletSpeed;
            if(dist > distances[i])
            {
                if(i < hit_count) {
                    Instantiate(hitEffect, line_segment[i], Quaternion.identity);
                    Instantiate(hitSoundEffect, line_segment[i], Quaternion.identity);

                    float segmentDist = dist - distances[i];
                    Vector3 pos = ray_sequence[i].origin + ray_sequence[i].direction * segmentDist;
                } else if(hit_player) {
                    Instantiate(hitSoundEffectPlayer, line_segment[i], Quaternion.identity);
                } else {
                    Instantiate(hitSoundMiss, line_segment[i], Quaternion.identity);
                }
                i++;
                lineRenderer.SetPositions(line_segment);
                lineRenderer.positionCount = i+1;
            }
            yield return null;
        }

        if(hit_player)
            Debug.Log("Hit Player!");
        //Instantiate(hitEffect, hit.point, Quaternion.identity);
    }

    [Button("Recalc")]
    public void Recalc()
    {
        ray_sequence[0] = new Ray(transform.position, transform.forward);
        line_segment[0] = transform.position;
        hit_count = 0;
        distances[0] = 0.0f;
        hit_player = false;

        for(int i = 0; i < ray_sequence.Length; i++) {
            Ray ray = ray_sequence[i];
            int num_hits = Physics.RaycastNonAlloc(ray,hits);
            Debug.Log($"Num Hits {num_hits}");


            if (num_hits > 0) {
                RaycastHit hit = hits[0];

                //Get closest hit
                for(int k = 1; k < num_hits; k++) {
                    if(hits[k].distance < hit.distance) {
                        hit = hits[k];
                    }
                }

                if(hit.collider.gameObject.layer == 0) {
                    Debug.Log($"Hit {i}: {hit.collider.name} at {hit.point} normal {hit.normal}");

                    Vector3 out_vector = Vector3.Reflect(ray.direction, hit.normal);

                    distances[i+1] = distances[i] + hit.distance;
                    ray_sequence[i+1] = new Ray(hit.point + out_vector * 0.01f, out_vector);
                    line_segment[i+1] = hit.point;
                } else if(hit.collider.gameObject.layer == 10) {
                    hit_count = i+1;
                    line_segment[i+1] = ray.origin + ray.direction * hit.distance;
                    hit_player = true;
                    break;
                }
            }
            else
            {
                hit_count = i+1;
                line_segment[i+1] = ray.origin + ray.direction * 100.0f;
                break;
            }
        }

        lineRenderer.SetPositions(line_segment);
        lineRenderer.positionCount = 0;
        //lineRenderer.widthCurve = new AnimationCurve(
    }
}
