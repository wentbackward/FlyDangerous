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
        PopulateDropdown();
        this.gameObject.SetActive(true);
        goButton.Select();
        this._animator.SetBool("Open", true);
    }
    
    private void OnEnable() {
        _levelData = null;
    }
    
    void PopulateDropdown ()
    {
        CircuitList circuits;
        var jsonTextFile = Resources.Load<TextAsset>("Circuits/circuit-list");
        circuits = JsonUtility.FromJson<CircuitList>(jsonTextFile.text);
        
        circuitSelector.ClearOptions ();
        circuitSelector.AddOptions(circuits.circuits);
    }
    
    public void StartCircuit()
    {
        var jsonTextFile = Resources.Load<TextAsset>("Circuits/" + circuitSelector.captionText.text);
        // Debug.Log("Item: " + circuitSelector.captionText.text + " : " + circuitSelector.itemText.text);
        // TODO Report an error if we can't find the file
        _levelData ??= LevelData.FromJsonString(jsonTextFile.text);
        Game.Instance.StartGame(_levelData);
    }
}
