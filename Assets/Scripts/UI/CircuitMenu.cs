using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Engine;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Environment = Engine.Environment;

public class CircuitMenu : MonoBehaviour {
    public Button goButton;

    [CanBeNull] private LevelData _levelData;
    [SerializeField] private Dropdown circuitSelector;

    private Animator _animator;
        
    private void Awake() {
        this._animator = this.GetComponent<Animator>();
    }

    public void Hide() {
        this.gameObject.SetActive(false);
    }

    public void Show() {
        this.gameObject.SetActive(true);
        goButton.Select();
        this._animator.SetBool("Open", true);
    }
    
    private void OnEnable() {
        _levelData = null;
    }
    
    public void StartCircuit()
    {
        String levelJson = "";
        
        switch (circuitSelector.value) {
            case 0: 
                levelJson = "{\r\n  \"version\": 1,\r\n  \"name\": \"\",\r\n  \"location\": 4,\r\n  \"environment\": 9,\r\n   \"terrainSeed\": \"\",\r\n  \"startPosition\": {\r\n    \"x\": 0,\r\n    \"y\": -100,\r\n    \"z\": 0\r\n  },\r\n  \"startRotation\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"raceType\": 0,\r\n}";
                break;
            case 1:
            case 2:
            case 3:
            case 4:
            default:
                levelJson = "{\r\n  \"version\": 1,\r\n  \"name\": \"\",\r\n  \"location\": 4,\r\n  \"environment\": 9,\r\n   \"terrainSeed\": \"\",\r\n  \"startPosition\": {\r\n    \"x\": 0,\r\n    \"y\": 1000,\r\n    \"z\": 0\r\n  },\r\n  \"startRotation\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"raceType\": 0,\r\n}";
                break;
        }

        _levelData ??= LevelData.FromJsonString(levelJson);
        Game.Instance.StartGame(_levelData);
    }    
}
