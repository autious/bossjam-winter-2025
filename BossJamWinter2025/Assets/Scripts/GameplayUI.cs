using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameplayUI : MonoBehaviour {
    private MapInstance map;

    public TMP_Text roundTimer;
    public TMP_Text feed;
    public TMP_Text scoreboard;

    protected void Update() {
        // Get a map instance from a singleton type field
        if (map == null) {
            map = MapInstance.ActiveInstance;
        }

        roundTimer.enabled = MapInstance.ActiveInstance != null;
        scoreboard.enabled = MapInstance.ActiveInstance != null;
        if (map != null && map.Object != null && map.Object.IsValid) {
            var time = map.currentStateTimer.RemainingTime(GameManager.Instance.runner).GetValueOrDefault(0);
            var mins = Mathf.FloorToInt(time / 60.0f);
            var sec = Mathf.FloorToInt(time % 60.0f);
            var timeString = $"{mins:00.}:{sec:00.}";
            switch (map.currentState) {
                case GameState.PreGame:
                    roundTimer.text = $"Starting: {timeString}";
                    break;
                case GameState.MidGame:
                    roundTimer.text = $"{timeString}";
                    break;
                case GameState.PostGame:
                    roundTimer.text = $"Ending: {timeString}";
                    break;
            }

            scoreboard.text = string.Join("\n", map.kills.Select((x) => {
                if (NetworkPlayerData.TryGet(out NetworkPlayerData data, x.Key)) {
                    return $"{data.playerName}: {x.Value}";
                }
                return $"Unknown: {x.Value}";
            }));
        }

        feed.enabled = GameManager.Instance != null;
        if (GameManager.Instance != null) {
            // Who cares about efficiency, just send it
            feed.text = string.Join("\n", GameManager.Instance.feed.Select((x) => x.message));
        }
    }
}
