using System;
using System.Linq;
using Audio;
using Engine;
using Menus;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class User : MonoBehaviour {
    
    [SerializeField] public Ship playerShip;
    
    [SerializeField] public PauseMenu pauseMenu;
    [SerializeField] public Canvas uiCanvas;
    [SerializeField] public Camera flatScreenCamera;
    [SerializeField] public Camera uiCamera;
    [SerializeField] public XRRig xrRig;

    [SerializeField] public InputSystemUIInputModule pauseUIInputModule;
    [SerializeField] public MouseWidget mouseWidget;
    [SerializeField] public TimeDisplay totalTimeDisplay;
    [SerializeField] public TimeDisplay splitTimeDisplay;
    private bool _alternateFlightControls = false;

    private Vector2 _mousePositionScreen;
    private Vector2 _mousePositionNormalized;
    private Vector2 _mousePositionDelta;

    private float _pitch;
    private float _roll;
    private float _yaw;
    private float _throttle;
    private float _lateralH;
    private float _lateralV;
    private bool _boost;

    [SerializeField]
    public bool movementEnabled = false;
    public bool pauseMenuEnabled = true;
    public bool boostButtonEnabledOverride = false;

    private Action<InputAction.CallbackContext> _cancelAction;

    /** Boostrap global ESC / cancel action in UI */
    public void Awake() {
        _cancelAction = context => { pauseMenu.OnGameMenuToggle(); };
        DisableGameInput();
        ResetMouseToCentre();
    }

    public void Start() {
        // if there's no controlling gamestate loaded, enable own input (usually in editor) 
        if (!FindObjectOfType<Game>()) {
            EnableGameInput();
        }
    }

    public void OnEnable() {
        pauseUIInputModule.cancel.action.performed += _cancelAction;
        Game.OnGraphicsSettingsApplied += OnGraphicsSettingsApplied;
        Game.OnVRStatus += SetVRStatus;
        ResetMouseToCentre();
        Console.Instance.Clear();
    }

    public void OnDisable() {
        pauseUIInputModule.cancel.action.performed -= _cancelAction;
        Game.OnGraphicsSettingsApplied -= OnGraphicsSettingsApplied;
        Game.OnVRStatus -= SetVRStatus;
    }

    public void OnGraphicsSettingsApplied() {
        flatScreenCamera.fieldOfView = Preferences.Instance.GetFloat("graphics-field-of-view");
    }

    public void Update() {
        if (movementEnabled) {
            var pitch = _pitch;
            var roll = _roll;
            var yaw = _yaw;
            var throttle = _throttle;
            var lateralH = _lateralH;
            var lateralV = _lateralV;

            if (!pauseMenu.IsPaused && Preferences.Instance.GetBool("enableMouseFlightControls")) {
                CalculateMouseInput(out var mousePitch, out var mouseRoll, out var mouseYaw);
                pitch += mousePitch;
                roll += mouseRoll;
                yaw += mouseYaw;
            }

            playerShip.SetPitch(pitch);
            playerShip.SetRoll(roll);
            playerShip.SetYaw(yaw);
            playerShip.SetThrottle(throttle);
            playerShip.SetLateralH(lateralH);
            playerShip.SetLateralV(lateralV);
            playerShip.Boost(_boost);
        }

        if (boostButtonEnabledOverride) {
            playerShip.Boost(_boost);
        }
    }

    /**
     * Enable and Disable input modules depending on context (in-game vs UI).
     * This prevents conflicts between the two, especially when rebinding keys...
     */
    public void EnableGameInput() {
        var playerInput = GetComponent<PlayerInput>();
        playerInput.ActivateInput();
        movementEnabled = true;
        pauseMenuEnabled = true;
        boostButtonEnabledOverride = false;
        
        Console.Instance.LogMessage("** USER INPUT ENABLED **");
        foreach (var inputDevice in InputSystem.devices) {
            Console.Instance.LogMessage(inputDevice.name + " with path <" + inputDevice.device.path + ">" + " detected");
            if (!playerInput.devices.Contains(inputDevice)) {
                Console.Instance.LogMessage(inputDevice.name + " not paired to user! Pairing ...");
                InputUser.PerformPairingWithDevice(inputDevice, playerInput.user);
            }
        }

        Console.Instance.LogMessage("---");
        foreach (var playerInputDevice in playerInput.devices) {
            Console.Instance.LogMessage(playerInputDevice.name + " paired");
        }

        ResetMouseToCentre();
    }

    public void DisableGameInput() {
        GetComponent<PlayerInput>().DeactivateInput();
        movementEnabled = false;
        pauseMenuEnabled = false;
        boostButtonEnabledOverride = false;

        Console.Instance.LogMessage("** USER INPUT DISABLED **");
    }

    public void EnableUIInput() {
        pauseUIInputModule.enabled = true;
    }

    public void DisableUIInput() {
        pauseUIInputModule.enabled = false;
    }

    public void SetVRStatus(bool isVREnabled) {
        // if VR is enabled, we need to swap our active cameras and make UI panels operate in world space
        if (isVREnabled) {
            var pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
            pauseMenuCanvas.renderMode = RenderMode.WorldSpace;
            uiCanvas.renderMode = RenderMode.WorldSpace;
            var pauseMenuRect = pauseMenuCanvas.GetComponent<RectTransform>();
            pauseMenuRect.sizeDelta = new Vector2(1280, 1000);
            pauseMenuRect.localScale /= 2;
            flatScreenCamera.enabled = false;
            uiCamera.enabled = false;
            xrRig.gameObject.SetActive(true);
        }
        else {
            var pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
            pauseMenuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            var pauseMenuRect = pauseMenuCanvas.GetComponent<RectTransform>();
            pauseMenuRect.sizeDelta = new Vector2(1920, 1080);
            pauseMenuRect.localScale *= 2;
            flatScreenCamera.enabled = true;
            uiCamera.enabled = true;
            xrRig.gameObject.SetActive(false);
        }
    }

    public void ResetMouseToCentre() {
        var warpedPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Mouse.current.WarpCursorPosition(warpedPosition);
        InputState.Change(Mouse.current.position, warpedPosition);
        _mousePositionScreen = warpedPosition;
        _mousePositionNormalized = new Vector2(0, 0);
        _mousePositionDelta = new Vector2(0, 0); 
    }

    /**
     * Event responders for PlayerInput, only valid in-game.
     *  UI Requires additional bootstrap as above because UI events in Unity are fucking bonkers.
     */
    public void OnShowGameMenu() {
        if (pauseMenuEnabled) {
            pauseMenu.OnGameMenuToggle();
        }
    }

    public void OnRestartTrack() {
        if (movementEnabled) {
            Game.Instance.RestartLevel();
        }
    }

    public void OnRestartFromLastCheckpoint() {
        if (movementEnabled) {
            Debug.Log("Lol there are no checkpoints yet ^_^");
        }
    }

    public void OnPitch(InputValue value) {
        if (!_alternateFlightControls) _pitch = value.Get<float>();
    }

    public void OnPitchAlt(InputValue value) {
        if (_alternateFlightControls) _pitch = value.Get<float>();
    }

    public void OnRoll(InputValue value) {
        if (!_alternateFlightControls) _roll = value.Get<float>();
    }

    public void OnRollAlt(InputValue value) {
        if (_alternateFlightControls) _roll = value.Get<float>();
    }

    public void OnYaw(InputValue value) {
        if (!_alternateFlightControls) _yaw = value.Get<float>();
    }

    public void OnYawAlt(InputValue value) {
        if (_alternateFlightControls) _yaw = value.Get<float>();
    }

    public void OnThrottle(InputValue value) {
        if (!_alternateFlightControls) _throttle = value.Get<float>();
    }

    public void OnThrottleAlt(InputValue value) {
        if (_alternateFlightControls) _throttle = value.Get<float>();
    }

    public void OnLateralH(InputValue value) {
        if (!_alternateFlightControls) _lateralH = value.Get<float>();
    }

    public void OnLateralHAlt(InputValue value) {
        if (_alternateFlightControls) _lateralH = value.Get<float>();
    }

    public void OnLateralV(InputValue value) {
        if (!_alternateFlightControls) _lateralV = value.Get<float>();
    }

    public void OnLateralVAlt(InputValue value) {
        if (_alternateFlightControls) _lateralV = value.Get<float>();
    }

    public void OnBoost(InputValue value) {
        _boost = value.isPressed;
    }

    public void OnVelocityLimiter(InputValue value) {
        playerShip.VelocityLimiterIsPressed(value.isPressed);
    }

    public void OnAllFlightAssistToggle(InputValue value) {
        playerShip.AllFlightAssistToggle();
    }

    public void OnVectorFlightAssistToggle(InputValue value) {
        playerShip.FlightAssistVectorControlToggle();   
    }

    public void OnRotationalFlightAssistToggle(InputValue value) {
        playerShip.FlightAssistRotationalDampeningToggle();
    }
    
    public void OnShipLightsToggle(InputValue value) {
        playerShip.ShipLightsToggle();
    }

    public void OnAltFlightControlsToggle(InputValue value) {
        _pitch = 0;
        _roll = 0;
        _yaw = 0;
        _throttle = 0;
        _lateralH = 0;
        _lateralV = 0;
        _alternateFlightControls = !_alternateFlightControls;
        if (_alternateFlightControls) {
            AudioManager.Instance.Play("ship-alternate-flight-on");
        }
        else {
            AudioManager.Instance.Play("ship-alternate-flight-off");
        }
    }

    public void OnMouseRawDelta(InputValue value) {
        _mousePositionDelta = value.Get<Vector2>();
        _mousePositionScreen.x += _mousePositionDelta.x;
        _mousePositionScreen.y += _mousePositionDelta.y;
        _mousePositionNormalized = new Vector2(
            ((_mousePositionScreen.x / Screen.width * 2) - 1),
            (_mousePositionScreen.y / Screen.height * 2 - 1)
        );
    }

    public void OnResetHMDView(InputValue value) {
        if (xrRig) {
            Game.Instance.ResetHMDView(xrRig, transform);
        }
    }

    public void OnToggleConsole(InputValue value) {
        if (Console.Instance.Visible) {
            Console.Instance.Hide();
        }
        else {
            Console.Instance.Show();
        }
    }

    private void CalculateMouseInput(out float pitchMouseInput, out float rollMouseInput, out float yawMouseInput) {

        float pitch = 0, roll = 0, yaw = 0;
        
        var mouseXAxisBind = Preferences.Instance.GetString("mouseXAxis");
        var mouseYAxisBind = Preferences.Instance.GetString("mouseYAxis");

        float sensitivityX = Preferences.Instance.GetFloat("mouseXSensitivity");
        float sensitivityY = Preferences.Instance.GetFloat("mouseYSensitivity");

        bool mouseXInvert = Preferences.Instance.GetBool("mouseXInvert");
        bool mouseYInvert = Preferences.Instance.GetBool("mouseXInvert");
        
        var mouseXIsRelative = Preferences.Instance.GetBool("relativeMouseXAxis");
        var mouseYIsRelative = Preferences.Instance.GetBool("relativeMouseYAxis");

        float mouseXRelativeRate = Preferences.Instance.GetFloat("mouseXRelativeRate");
        float mouseYRelativeRate = Preferences.Instance.GetFloat("mouseYRelativeRate");

        float mouseDeadzone = Preferences.Instance.GetFloat("mouseDeadzone");
        float mousePowerCurve = Preferences.Instance.GetFloat("mousePowerCurve");

        // // get deadzone as a pixel value including sensitivity change
        var mouseDeadzoneX = mouseDeadzone * Mathf.Pow(sensitivityX, -1);
        var mouseDeadzoneY = mouseDeadzone * Mathf.Pow(sensitivityY, -1);

        // calculate continuous input including deadzone and sensitivity
        // TODO: power curve??
        var continuousMouseX = 0f;
        var continuousMouseY = 0f;
        if (_mousePositionNormalized.x > mouseDeadzoneX) {
            continuousMouseX = (_mousePositionNormalized.x - mouseDeadzone) * sensitivityX;
        }
        if (_mousePositionNormalized.x < -mouseDeadzoneX) {
            continuousMouseX = (_mousePositionNormalized.x + mouseDeadzone) * sensitivityX;
        }
        if (_mousePositionNormalized.y > mouseDeadzoneY) {
            continuousMouseY = (_mousePositionNormalized.y - mouseDeadzone) * sensitivityY;
        }
        if (_mousePositionNormalized.y < -mouseDeadzoneY) {
            continuousMouseY = (_mousePositionNormalized.y + mouseDeadzone) * sensitivityY;
        }

        // calculate relative input from deltas including sensitivity
        // TODO: relative rate
        var relativeMouseX = _mousePositionDelta.x * sensitivityX;
        var relativeMouseY = _mousePositionDelta.y * sensitivityY;
        
        // set the input for a given axis 
        Action<string, float, bool> setInput = (axis, amount, shouldInvert) => {
            var invert = shouldInvert ? -1 : 1;
            
            switch (axis) {
                case "pitch":
                    pitch += amount * -1 * invert;
                    break;
                case "roll":
                    roll += amount * invert;
                    break;
                case "yaw":
                    yaw += amount * invert;
                    break;
            }
        };

        // send input depending on mouse mode
        if (mouseXIsRelative) {
            setInput(mouseXAxisBind, relativeMouseX, mouseXInvert);
        }
        else {
            setInput(mouseXAxisBind, continuousMouseX, mouseXInvert);
        }

        if (mouseYIsRelative) {
            setInput(mouseYAxisBind, relativeMouseY, mouseYInvert);
        }
        else {
            setInput(mouseYAxisBind, continuousMouseY, mouseYInvert);
        }

        // update widget graphics
        Vector2 widgetPosition = new Vector2(
            mouseXIsRelative ? (_mousePositionDelta.x * 0.01f) : continuousMouseX,
            mouseYIsRelative ? (_mousePositionDelta.y * 0.01f) : continuousMouseY
        );
        mouseWidget.UpdateWidgetSprites(widgetPosition);
        
        // clamp to virtual screen 
        var extentsX = Screen.width * Mathf.Pow(sensitivityX, -1);
        var extentsY = Screen.height * Mathf.Pow(sensitivityY, -1);
        _mousePositionScreen.x = Math.Max(-extentsX, Math.Min(extentsX, _mousePositionScreen.x));
        _mousePositionScreen.y = Math.Max(-extentsY, Math.Min(extentsY, _mousePositionScreen.y));

        // we're done
        pitchMouseInput = pitch;
        rollMouseInput = roll;
        yawMouseInput = yaw;
    }
}
