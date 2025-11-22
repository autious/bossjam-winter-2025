using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;
using System;
using WebSocketSharp;

public class BounceRay : MonoBehaviour
{
    public LineRenderer shotLineRenderer;
    public LineRenderer previewLineRenderer;
    public GameObject fireEffect;
    public GameObject hitEffect;
    public GameObject hitSoundEffect;
    public GameObject hitSoundEffectPlayer;
    public GameObject hitSoundMiss;

    public float bulletSpeed = 100.0f;
    public float trailingLength = 100.0f;
    public int shot_bounce_limit = 8;
    public int laser_bounce_limit = 5;

    int MAX_BOUNCE = 0;
    int hit_count = 0;
    bool hit_player = false;
    bool out_of_bounds = false;
    float max_dist = 0.0f;
    Hittable hitPlayer;

    Vector3[] line_segment = null;
    float[] distances = null;
    Ray[] ray_sequence = null;

    private void Awake() {
        shotLineRenderer.gameObject.SetActive(false);
        previewLineRenderer.gameObject.SetActive(false);

        Realloc();
    }

    private void Realloc(){
        int NEW_MAX_BOUNCE = Math.Max(shot_bounce_limit, laser_bounce_limit)+2;

        if(NEW_MAX_BOUNCE > MAX_BOUNCE) {
            MAX_BOUNCE  = NEW_MAX_BOUNCE;
            line_segment = new Vector3[MAX_BOUNCE+1];
            distances = new float[MAX_BOUNCE+1];
            ray_sequence = new Ray[MAX_BOUNCE+1];
        }
    }



    private void OnDrawGizmos()
    {
        for(int i = 0; i < hit_count; i++)
        {
            //Gizmos.DrawLine(line_segment[i], line_segment[i+1]);
        }
    }

    RaycastHit[] hits = new RaycastHit[16];

    [Button("Shoot")]
    private void ShootDebug()
    {
        shotLineRenderer.gameObject.SetActive(true);
        previewLineRenderer.gameObject.SetActive(false);
        Recalc();
        line_segment[0] = transform.position;
        StartCoroutine(ShootCoroutine(true,false));
    }

    public void Shoot(Vector3 gunFirePoint, bool cosmetic, Color bulletColor)
    {
        shotLineRenderer.gameObject.SetActive(true);
        previewLineRenderer.gameObject.SetActive(false);
        Recalc();
        line_segment[0] = gunFirePoint;
        shotLineRenderer.startColor = bulletColor;
        shotLineRenderer.endColor = bulletColor;
        StartCoroutine(ShootCoroutine(cosmetic,true));
    }

    public void Preview(Vector3 gunFirePoint)
    {
        Recalc();
        shotLineRenderer.gameObject.SetActive(false);
        previewLineRenderer.gameObject.SetActive(true);
        previewLineRenderer.positionCount = Math.Min(laser_bounce_limit + 2, hit_count + 1);
        if(hit_count > 0) {
            previewLineRenderer.SetPosition(0, gunFirePoint);
            for(int i = 1; i < previewLineRenderer.positionCount; i++) {
                previewLineRenderer.SetPosition(i, line_segment[i]);
            }
        }
    }


    private IEnumerator ShootCoroutine(bool cosmetic, bool  destroy_when_done) {
        if(Application.isPlaying) {
            Instantiate(fireEffect, line_segment[0], Quaternion.identity);
            float timeStart = Time.time;
            int trailing_index = 0;
            int bullet_index = 1;
            hit_count = Math.Min(shot_bounce_limit, hit_count);
            while(trailing_index <= hit_count) {
                float dist = (Time.time - timeStart) * bulletSpeed;
                float trailing_dist = Mathf.Max(0.0f, dist - trailingLength);
                dist = Mathf.Min(max_dist, dist);
                trailing_dist = Mathf.Min(max_dist, trailing_dist);

                if(trailing_dist >= distances[trailing_index] && trailing_index <= hit_count)
                {
                    trailing_index++;
                }

                Vector3 trailing_pos = line_segment[0];
                if(trailing_index >= hit_count) {
                    trailing_pos = line_segment[hit_count];
                } else if(trailing_index > 0) {
                    float segment_trailing_dist = trailing_dist - distances[trailing_index-1];
                    trailing_pos = ray_sequence[trailing_index-1].origin + ray_sequence[trailing_index-1].direction * segment_trailing_dist;
                }

                Vector3 bullet_pos = line_segment[hit_count];
                //Ensure bullet doesn't go beyond player.
                if(!hit_player || bullet_index <= hit_count) {
                    float segmentDist = dist - distances[bullet_index-1];
                    bullet_pos = ray_sequence[bullet_index - 1].origin + ray_sequence[bullet_index - 1].direction * segmentDist;
                }

                if (bullet_index <= hit_count && dist >= distances[bullet_index])
                {
                    if(bullet_index < hit_count) {
                        Instantiate(hitEffect, line_segment[bullet_index], Quaternion.identity);
                        Instantiate(hitSoundEffect, line_segment[bullet_index], Quaternion.identity);

                    } else if(hit_player) {
                        Instantiate(hitSoundEffectPlayer, line_segment[bullet_index], Quaternion.identity);
                        if(hitPlayer != null) {
                            hitPlayer.OnHit(line_segment[bullet_index], Vector3.zero, cosmetic);
                        }
                    } else if(out_of_bounds) {
                        Instantiate(hitSoundMiss, line_segment[bullet_index], Quaternion.identity);
                    }
                    bullet_index++;
                }

                int segment_count = Math.Max(0, bullet_index - trailing_index);
                shotLineRenderer.positionCount = segment_count+1+1;
                shotLineRenderer.SetPosition(0, trailing_pos);
                for(int k = 0; k < segment_count; k++) {
                    int source_index = k + bullet_index - segment_count;
                    shotLineRenderer.SetPosition(k+1, line_segment[source_index]);
                }
                shotLineRenderer.SetPosition(segment_count+1, bullet_pos);

                yield return null;
            }
            if(destroy_when_done) {
                Destroy(gameObject);
            }
        }
    }

    [Button("Recalc")]
    public void Recalc()
    {
        Realloc();
        ray_sequence[0] = new Ray(transform.position, transform.forward);
        line_segment[0] = transform.position;
        hit_count = 0;
        distances[0] = 0.0f;
        hit_player = false;
        out_of_bounds = false;

        for(int i = 0; i < MAX_BOUNCE; i++) {
            Ray ray = ray_sequence[i];
            int num_hits = Physics.RaycastNonAlloc(ray, hits, 10000.0f, LayerMask.GetMask("Default", "PlayerWeakpoint", "HitConsume"));
            //int num_hits = Physics.SphereCastNonAlloc(ray, 0.25f, hits, 1000.0f, LayerMask.GetMask("Default", "PlayerWeakpoint"));

            if (num_hits > 0) {
                RaycastHit hit = hits[0];

                //Get closest hit
                for(int k = 1; k < num_hits; k++) {
                    if(hits[k].distance < hit.distance) {
                        hit = hits[k];
                    }
                }

                if(hit.collider.gameObject.layer == 0) {
                    // Debug.Log($"Hit {i}: {hit.collider.name} at {hit.point} normal {hit.normal}");

                    Vector3 out_vector = Vector3.Reflect(ray.direction, hit.normal);

                    hit_count = i+1;
                    distances[i+1] = distances[i] + hit.distance;
                    max_dist = distances[i+1];
                    ray_sequence[i+1] = new Ray(hit.point + hit.normal * 0.01f + out_vector * 0.01f, out_vector);
                    line_segment[i+1] = hit.point + hit.normal * 0.01f;
                } else if(hit.collider.gameObject.layer == 10) {
                    hit_count = i+1;
                    distances[i+1] = distances[i] + hit.distance;
                    max_dist = distances[i+1];
                    line_segment[i+1] = ray.origin + ray.direction * hit.distance;
                    ray_sequence[i+1] = new Ray(hit.point + hit.normal * 0.01f, Vector3.zero);
                    hit_player = true;
                    hitPlayer = hit.collider.gameObject.GetComponent<Hittable>();
                    break;
                } else {
                    if(hit.collider.gameObject.layer != 6) {
                        Debug.LogWarning($"Ignored Hit {i}: {hit.collider.name} at {hit.point} normal {hit.normal}");
                    }
                    Debug.Log($"Hit {hit.collider.name}");
                    hit_count = i+1;
                    distances[i+1] = distances[i] + hit.distance;
                    max_dist = distances[i+1];
                    line_segment[i+1] = ray.origin + ray.direction * hit.distance;
                    ray_sequence[i+1] = new Ray(hit.point + hit.normal * 0.01f, Vector3.zero);
                    break;
                }
            }
            else
            {
                out_of_bounds = true;
                hit_count = i+1;
                distances[i+1] = distances[i] + 10.0f;
                max_dist = distances[i+1];
                line_segment[i+1] = ray.origin + ray.direction * 100.0f;
                break;
            }
        }

        shotLineRenderer.SetPositions(line_segment);
        shotLineRenderer.positionCount = 0;
        //lineRenderer.widthCurve = new AnimationCurve(
    }
}
