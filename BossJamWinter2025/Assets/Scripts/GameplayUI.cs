using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class GameplayUI : MonoBehaviour {

    float cycleAngryEmoteTime = 0.34f;
    float currentAngryEmoteTime = 0;

    //Show Message time in miliseconds
    const float messageTime = 0.6f;
    float currentMessageTime = 0;
    private MapInstance map;

    public TMP_Text roundTimer;
    public TMP_Text feed;
    public TMP_Text scoreboard;
    public RawImage playerCam;
    public Texture2D[] angry;
    public Texture2D idle;
    public Texture2D dead;

    protected void Update() {
        // Get a map instance from a singleton type field
        if (map == null) {
            map = MapInstance.ActiveInstance;
        }
        var player = GameObject.FindObjectsOfType<QuickPlayerController>().FirstOrDefault((x) => x.HasStateAuthority);

        if(player != null && player.gunCdTimer > 0) {

            currentAngryEmoteTime += Time.deltaTime;
            currentAngryEmoteTime %= cycleAngryEmoteTime;
            
            playerCam.texture = angry[(int)((currentAngryEmoteTime/cycleAngryEmoteTime) * angry.Length)];
            
        }
        else{
            playerCam.texture = idle;
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
            // Who cares about efficiency, just send it (This is for the entire feed)
            // var entryText = string.Join("\n", GameManager.Instance.feed.Select((x) => x.message));

            // This is for the last added element
            var entryText = GameManager.Instance.feed.Select((x) => x.message).LastOrDefault();
            if (string.IsNullOrEmpty(entryText)) {
                entryText = GameManager.Instance.uiPlayerName.text;
            }
            
            if (feed.text != entryText) {
                feed.text = entryText;
                feed.maxVisibleCharacters = 0;
                currentMessageTime = 0;
            }
        }

        if (feed.maxVisibleCharacters != feed.text.Length) {
            currentMessageTime += Time.deltaTime;

            feed.maxVisibleCharacters = (int) (Easing.Linear(currentMessageTime / messageTime) * feed.text.Length);
        }

    }
    
    private void AngryEmote() {
        playerCam.texture = angry[0];
    }
}
