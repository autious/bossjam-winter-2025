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
    public GameObject hitEffect;
    public GameObject hitSoundEffect;
    public GameObject hitSoundEffectPlayer;
    public GameObject hitSoundMiss;

    public bool isCosmetic; // TODO hook up

    int hit_count = 0;
    bool hit_player = true;
    Hittable hitPlayer;
    int MAX_BOUNCE = 64;
    Vector3[] line_segment = new Vector3[65];
    float[] distances = new float[65];
    Ray[] ray_sequence = new Ray[65];

    private void Awake() {
        shotLineRenderer.gameObject.SetActive(false);
        previewLineRenderer.gameObject.SetActive(false);
    }

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
        shotLineRenderer.gameObject.SetActive(true);
        previewLineRenderer.gameObject.SetActive(false);
        Recalc();
        line_segment[0] = transform.position;
        StartCoroutine(ShootCoroutine(true,false));
    }

    public void Shoot(Vector3 gunFirePoint, bool cosmetic)
    {
        shotLineRenderer.gameObject.SetActive(true);
        previewLineRenderer.gameObject.SetActive(false);
        Recalc();
        line_segment[0] = gunFirePoint;
        StartCoroutine(ShootCoroutine(cosmetic,true));
    }

    public void Preview()
    {
        Recalc();
        shotLineRenderer.gameObject.SetActive(false);
        previewLineRenderer.gameObject.SetActive(true);
        for(int i = 0; i < hit_count; i++) {
            previewLineRenderer.SetPosition(i, line_segment[i]);
        }
    }

    public float bulletSpeed = 100.0f;
    public float trailingLength = 100.0f;

    private IEnumerator ShootCoroutine(bool cosmetic, bool  destroy_when_done) {
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

                if (bullet_index <= hit_count && dist > distances[bullet_index])
                {
                    if(bullet_index < hit_count) {
                        Instantiate(hitEffect, line_segment[bullet_index], Quaternion.identity);
                        Instantiate(hitSoundEffect, line_segment[bullet_index], Quaternion.identity);

                    } else if(hit_player) {
                        Instantiate(hitSoundEffectPlayer, line_segment[bullet_index], Quaternion.identity);
                        if(hitPlayer != null) {
                            hitPlayer.OnHit(line_segment[bullet_index], Vector3.zero, cosmetic);
                        }
                    } else {
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

        for(int i = 0; i < MAX_BOUNCE; i++) {
            Ray ray = ray_sequence[i];
            int num_hits = Physics.RaycastNonAlloc(ray,hits, 1000.0f, LayerMask.GetMask("Default", "PlayerWeakpoint"));
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
                    hitPlayer = hit.collider.gameObject.GetComponent<Hittable>();
                    break;
                } else {
                    Debug.Log($"Ignored Hit {i}: {hit.collider.name} at {hit.point} normal {hit.normal}");
                    hit_count = i+1;
                    distances[i+1] = distances[i] + 10.0f;
                    line_segment[i+1] = ray.origin + ray.direction * 100.0f;
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

        shotLineRenderer.SetPositions(line_segment);
        shotLineRenderer.positionCount = 0;
        //lineRenderer.widthCurve = new AnimationCurve(
    }
}
