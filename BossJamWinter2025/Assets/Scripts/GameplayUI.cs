using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameplayUI : MonoBehaviour {
    private MapInstance map;

    public TMP_Text roundTimer;
    public TMP_Text killFeed;

    protected void Update() {
        // Get a map instance from a singleton type field
        if (map == null) {
            map = MapInstance.ActiveInstance;
        }

        roundTimer.enabled = MapInstance.ActiveInstance != null;
        if (map != null) {
            switch (map.currentState) {
                case GameState.PreGame:
                    roundTimer.text = $"Starting: {map.currentStateTimer.RemainingTime(GameManager.Instance.runner):0.}s";
                    break;
                case GameState.MidGame:
                    roundTimer.text = $"{map.currentStateTimer.RemainingTime(GameManager.Instance.runner):0.}s";
                    break;
                case GameState.PostGame:
                    roundTimer.text = $"Ending: {map.currentStateTimer.RemainingTime(GameManager.Instance.runner):0.}s";
                    break;
            }
        }
    }
}
