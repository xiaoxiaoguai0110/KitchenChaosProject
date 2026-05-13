using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class GameInput : MonoBehaviour
{
    public static GameInput Instance {  get; private set; }

    private const string GAMEINPUT_BINDINGS = "GameInputBindings";

    public class PlayerActionEventArgs : EventArgs
    {
        public int PlayerIndex { get; }

        public PlayerActionEventArgs(int playerIndex)
        {
            PlayerIndex = playerIndex;
        }
    }

    public event EventHandler<PlayerActionEventArgs> OnInteractAction;
    public event EventHandler<PlayerActionEventArgs> OnOperateAction;
    public event EventHandler OnPauseAction;

    private GameControl gameControl;

    public enum BindingType
    {
        Up,
        Down,
        Left,
        Right,
        Interact,
        Operate,
        Pause
    }

    private void Awake()
    {
        Instance = this;
        gameControl = new GameControl();

        if (PlayerPrefs.HasKey(GAMEINPUT_BINDINGS))
        {
            gameControl.LoadBindingOverridesFromJson(PlayerPrefs.GetString(GAMEINPUT_BINDINGS));
        }

        gameControl.Player.Enable();

        gameControl.Player.Interact.performed += InteractP1_Performed;
        gameControl.Player.Operate.performed += OperateP1_Performed;
        gameControl.Player.Pause.performed += Pause_Performed;
    }

    public void ReBinding(BindingType bindingType,Action onComplete)
    {
        gameControl.Player.Disable();
        InputAction inputAction = null;
        int index = -1;
        switch (bindingType)
        {
            case BindingType.Up:
                index = 1;
                inputAction = gameControl.Player.Move;
                break;
            case BindingType.Down:
                index = 2;
                inputAction = gameControl.Player.Move;
                break;
            case BindingType.Left:
                index = 3;
                inputAction = gameControl.Player.Move;
                break;
            case BindingType.Right:
                index = 4;
                inputAction = gameControl.Player.Move;
                break;
            case BindingType.Interact:
                index = 0;
                inputAction = gameControl.Player.Interact;
                break;
            case BindingType.Operate:
                index = 0;
                inputAction = gameControl.Player.Operate;
                break;
            case BindingType.Pause:
                index = 0;
                inputAction = gameControl.Player.Pause;
                break;
            default:
                break;
        }
        inputAction.PerformInteractiveRebinding(index).OnComplete(callback =>
        {
            callback.Dispose();
            gameControl.Player.Enable();
            onComplete?.Invoke();

            //print(gameControl.SaveBindingOverridesAsJson());
            PlayerPrefs.SetString(GAMEINPUT_BINDINGS, gameControl.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();

        }).Start();

    }

    public string GetBindingDisplayString(BindingType bindingType)
    {
        switch (bindingType)
        {
            case BindingType.Up:
                return gameControl.Player.Move.bindings[1].ToDisplayString();
            case BindingType.Down:
                return gameControl.Player.Move.bindings[2].ToDisplayString();
            case BindingType.Left:
                return gameControl.Player.Move.bindings[3].ToDisplayString();
            case BindingType.Right:
                return gameControl.Player.Move.bindings[4].ToDisplayString();
            case BindingType.Interact:
                return gameControl.Player.Interact.bindings[0].ToDisplayString();
            case BindingType.Operate:
                return gameControl.Player.Operate.bindings[0].ToDisplayString();
            case BindingType.Pause:
                return gameControl.Player.Pause.bindings[0].ToDisplayString();
            default:
                break;
        }
        return null;
    }

    private void OnDestroy()
    {
        gameControl.Player.Interact.performed -= InteractP1_Performed;
        gameControl.Player.Operate.performed -= OperateP1_Performed;
        gameControl.Player.Pause.performed -= Pause_Performed;

        gameControl.Dispose();
    }

    private void Pause_Performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void OperateP1_Performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnOperateAction?.Invoke(this, new PlayerActionEventArgs(0));
    }

    private void InteractP1_Performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, new PlayerActionEventArgs(0));
    }

    private void Update()
    {
        CheckPlayer2KeyboardInput();
    }

    private void CheckPlayer2KeyboardInput()
    {
        Keyboard k = Keyboard.current;
        if (k == null)
            return;

        if (k.digit7Key.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame)
            OnInteractAction?.Invoke(this, new PlayerActionEventArgs(1));

        if (k.digit9Key.wasPressedThisFrame || k.numpad9Key.wasPressedThisFrame)
            OnOperateAction?.Invoke(this, new PlayerActionEventArgs(1));
    }

    public Vector3 GetMovementDirectionNormalized(int playerIndex)
    {
        if (playerIndex == 0)
        {
            Vector2 v = gameControl.Player.Move.ReadValue<Vector2>();
            return new Vector3(v.x, 0f, v.y).normalized;
        }

        if (playerIndex == 1)
        {
            return GetPlayer2MoveFromKeyboard();
        }

        return Vector3.zero;
    }

    private static Vector3 GetPlayer2MoveFromKeyboard()
    {
        Keyboard k = Keyboard.current;
        if (k == null)
            return Vector3.zero;

        float x = 0f;
        float y = 0f;
        if (k.digit4Key.isPressed || k.numpad4Key.isPressed)
            x -= 1f;
        if (k.digit6Key.isPressed || k.numpad6Key.isPressed)
            x += 1f;
        if (k.digit8Key.isPressed || k.numpad8Key.isPressed)
            y += 1f;
        if (k.digit5Key.isPressed || k.numpad5Key.isPressed)
            y -= 1f;

        return new Vector3(x, 0f, y).normalized;
    }

    
}
