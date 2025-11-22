using Fusion;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerVoiceLines : NetworkBehaviour {
    
    private static PlayerVoiceLines instance;
    
    [SerializeField] AudioSource audio;
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
        public float chance;
    }

    private void OnEnable() {
        if (HasStateAuthority == false) {
            return;
        }
        instance = this;
    }

    private void OnDisable() {
        if (HasStateAuthority == false) {
            return;
        }
        instance = null;
    }


    public static void TryPlayEvent(VoiceEvent voiceEvent) {
        if (instance == null) return;
        instance.TryPlayEvent_Internal(voiceEvent);
    }

    void TryPlayEvent_Internal(VoiceEvent voiceEvent) {
        for (int eventIndex = 0; eventIndex < voiceLines.Count; eventIndex++) {
            VoiceLine line = voiceLines[eventIndex];
            if (line.triggerEvent == voiceEvent) {
                bool canPlayClip = Random.Range(0f, 1f) <= line.chance;
                if (lastVoicePlayedEvent < Time.realtimeSinceStartup && canPlayClip) {
                    lastVoicePlayedEvent = Time.realtimeSinceStartup;
                    int clipIndex = Random.Range(0, line.clip.Count);
                    RPC_PlayVoiceLine(eventIndex,clipIndex);
                }

                break;
            }
        }
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_PlayVoiceLine(int eventIndex, int clipIndex) {
        audio.PlayOneShot(voiceLines[eventIndex].clip[clipIndex]);
    }
    
}
