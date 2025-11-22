using Fusion;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerVoiceLines : NetworkBehaviour {
    
    
    [SerializeField] AudioSource audio;
    [SerializeField] float minDelayBetweenVoicelines;
    [SerializeField] List<VoiceLine> voiceLines;
    
    float lastVoicePlayedEvent;
    
    public enum VoiceEvent {
        OnKilledPlayer,
        OnDied,
        OnMissed,
        OnNearMiss,
        OnShotGun,
        OnJump,
        OnSpawned,
    }

    [System.Serializable]
    struct VoiceLine {
        public VoiceEvent triggerEvent;
        public List<AudioClip> clip;
        [Range(0,1f)]public float chance;
    }
    


    public void TryPlayEvent(VoiceEvent voiceEvent) {
        Debug.Log("ccc");
        for (int eventIndex = 0; eventIndex < voiceLines.Count; eventIndex++) {
            Debug.Log($"ccc #{eventIndex}");
            VoiceLine line = voiceLines[eventIndex];
            if (line.triggerEvent == voiceEvent) {
                bool canPlayClip = Random.Range(0f, 1f) <= line.chance;
                Debug.Log($"ddddd FOUND EVENT!! #{eventIndex} - Lucky? {canPlayClip}, timer{lastVoicePlayedEvent < Time.time}");
                if (lastVoicePlayedEvent < Time.time && canPlayClip) {
                    Debug.Log($"eeeeeee canPlay #{eventIndex}");
                    lastVoicePlayedEvent = Time.time + minDelayBetweenVoicelines;
                    int clipIndex = Random.Range(0, line.clip.Count);
                    RPC_PlayVoiceLine(eventIndex,clipIndex);
                }

                break;
            }
        }
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_PlayVoiceLine(int eventIndex, int clipIndex) {
        Debug.Log($"Playing voiceline -> {voiceLines[eventIndex].triggerEvent} #{clipIndex}");
        audio.PlayOneShot(voiceLines[eventIndex].clip[clipIndex]);
    }
    
}
