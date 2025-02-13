using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AxisDropdown : MonoBehaviour, ISubmitHandler, IPointerClickHandler {

    public bool isOpen;
    public Text icon;
    public GameObject container;
    public ContentFitterRefresh parentContentFitter;

    void Start() {
        isOpen = false;
        container.SetActive(false);
    }

    public void OnSubmit(BaseEventData eventData) {
        Toggle();
    }

    public void OnPointerClick(PointerEventData eventData) {
        Toggle();
    }

    private void Toggle() {
        isOpen = !isOpen;
        HandleToggleLogic();
    }

    private void HandleToggleLogic() {
        if (isOpen) {
            icon.text = "-";
            AudioManager.Instance.Play("ui-dialog-open");
            container.SetActive(true);
        }
        else {
            icon.text = "+";
            AudioManager.Instance.Play("ui-cancel");
            container.SetActive(false);
        }
        parentContentFitter.RefreshContentFitters();
    }
}
