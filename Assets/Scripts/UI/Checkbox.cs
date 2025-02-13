using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Engine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI {

    public interface ICheckboxHandler {
        void OnEnabled();
        void OnDisabled();
    }
    
    public class Checkbox : MonoBehaviour, ISubmitHandler, IPointerClickHandler {

        public string preference;
        public bool isChecked;
        public Image statusImage;
        public ICheckboxHandler handler;

        public void Update() {
            statusImage.enabled = isChecked;
        }

        public void OnSubmit(BaseEventData eventData) {
            Toggle();
        }


        public void OnPointerClick(PointerEventData eventData) {
            Toggle();
        }

        private void Toggle() {
            
            isChecked = !isChecked;
            if (isChecked) {
                AudioManager.Instance.Play("ui-confirm");
                handler?.OnEnabled();
            }
            else {
                AudioManager.Instance.Play("ui-cancel");
                handler?.OnDisabled();
            }
        }
    }
}