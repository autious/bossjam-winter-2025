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
    private void ShootDebug()
    {
        Shoot(false);
    }
    public void Shoot(bool cosmetic)
    {
        Recalc();
        StartCoroutine(ShootCoroutine(cosmetic));
    }

    public float bulletSpeed = 100.0f;
    public float trailingLength = 100.0f;

    private IEnumerator ShootCoroutine(bool cosmetic) {
        if(Application.isPlaying) {
            float timeStart = Time.time;
            int trailing_index = 0;
            int bullet_index = 1;
            while(trailing_index <= hit_count) {
                float dist = (Time.time - timeStart) * bulletSpeed;
                float trailing_dist = Mathf.Max(0.0f, dist - trailingLength);

                if(trailing_dist > distances[trailing_index] && trailing_index <= hit_count)
                {
                    trailing_index++;
                }

                float segment_trailing_dist = trailing_dist - (trailing_index > 0 ? distances[trailing_index-1] : 0);
                Vector3 trailing_pos = ray_sequence[trailing_index].origin + ray_sequence[trailing_index].direction * segment_trailing_dist;

                if (dist > distances[bullet_index] && bullet_index <= hit_count)
                {
                    if(bullet_index < hit_count) {
                        Instantiate(hitEffect, line_segment[bullet_index], Quaternion.identity);
                        Instantiate(hitSoundEffect, line_segment[bullet_index], Quaternion.identity);

                        float segmentDist = dist - distances[bullet_index];
                        Vector3 pos = ray_sequence[bullet_index].origin + ray_sequence[bullet_index].direction * segmentDist;
                    } else if(hit_player) {
                        Instantiate(hitSoundEffectPlayer, line_segment[bullet_index], Quaternion.identity);
                    } else {
                        Instantiate(hitSoundMiss, line_segment[bullet_index], Quaternion.identity);
                    }
                    bullet_index++;

                }

                int segment_count = Math.Min(bullet_index, Math.Max(0, bullet_index - trailing_index));
                lineRenderer.positionCount = segment_count;
                for(int k = 0; k < segment_count; k++) {
                    int source_index = k + bullet_index - segment_count;
                    lineRenderer.SetPosition(k, line_segment[source_index]);
                }

                yield return null;
            }
        }
        //Instantiate(hitEffect, hit.point, Quaternion.identity);
    }

    public void UpdateLines()
    {
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
                    distances[i+1] = distances[i] + hit.distance;
                    line_segment[i+1] = ray.origin + ray.direction * hit.distance;
                    hit_player = true;
                    break;
                }
            }
            else
            {
                hit_count = i+1;
                distances[i+1] = distances[i] + 10.0f;
                line_segment[i+1] = ray.origin + ray.direction * 100.0f;
                break;
            }
        }

        lineRenderer.SetPositions(line_segment);
        lineRenderer.positionCount = 0;
        //lineRenderer.widthCurve = new AnimationCurve(
    }
}
