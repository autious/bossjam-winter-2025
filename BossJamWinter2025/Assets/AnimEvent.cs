using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimEvent : MonoBehaviour {
    [SerializeField] List<UnityEvent> animEvent;
    
    public void TriggerEvent(int index) {
        animEvent[index]?.Invoke();
    }
}
