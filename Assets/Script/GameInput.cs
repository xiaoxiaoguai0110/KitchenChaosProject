using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class GameInput : MonoBehaviour
{
    private const string GameInputBindingsKey = "GameInputBindings";
    private const int PlayerCount = 2;

    public static GameInput Instance { get; private set; }

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
    public event EventHandler<PlayerActionEventArgs> OnBindingDisplayChanged;
    public event EventHandler OnPauseAction;

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

    private enum InputDeviceKind
    {
        Keyboard,
        Gamepad
    }

    private GameControl gameControl;
    private readonly InputActionMap[] playerMaps = new InputActionMap[PlayerCount];
    private readonly InputAction[] moveActions = new InputAction[PlayerCount];
    private readonly InputAction[] interactActions = new InputAction[PlayerCount];
    private readonly InputAction[] operateActions = new InputAction[PlayerCount];
    private readonly InputAction[] pauseActions = new InputAction[PlayerCount];
    private readonly InputDeviceKind[] lastUsedDevices =
    {
        InputDeviceKind.Keyboard,
        InputDeviceKind.Keyboard
    };

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        gameControl = new GameControl();

        ResolveActionMaps();

        if (PlayerPrefs.HasKey(GameInputBindingsKey))
        {
            gameControl.asset.LoadBindingOverridesFromJson(
                PlayerPrefs.GetString(GameInputBindingsKey));
        }
    }

    private void OnEnable()
    {
        if (gameControl == null)
            return;

        SubscribeToActions();
        InputSystem.onDeviceChange += InputSystem_OnDeviceChange;
        ApplyDeviceAssignments();
        EnableGameplayMaps();
    }

    private void OnDisable()
    {
        if (gameControl == null)
            return;

        InputSystem.onDeviceChange -= InputSystem_OnDeviceChange;
        CancelCurrentRebind();
        DisableGameplayMaps();
        UnsubscribeFromActions();
    }

    private void ResolveActionMaps()
    {
        playerMaps[0] = gameControl.asset.FindActionMap("Player", true);
        playerMaps[1] = gameControl.asset.FindActionMap("Player2");

        // 如果 Unity 还没来得及重新生成 GameControl.cs，运行时仍会建立完整的 P2 Action Map。
        if (playerMaps[1] == null)
            playerMaps[1] = CreatePlayer2Map(gameControl.asset);

        EnsurePlayer1GamepadBindings();

        for (int playerIndex = 0; playerIndex < PlayerCount; playerIndex++)
        {
            InputActionMap map = playerMaps[playerIndex];
            moveActions[playerIndex] = map.FindAction("Move", true);
            interactActions[playerIndex] = map.FindAction("Interact", true);
            operateActions[playerIndex] = map.FindAction("Operate", true);
            pauseActions[playerIndex] = map.FindAction("Pause", true);
        }
    }

    private void SubscribeToActions()
    {
        moveActions[0].performed += MoveP1_Performed;
        moveActions[1].performed += MoveP2_Performed;
        interactActions[0].performed += InteractP1_Performed;
        interactActions[1].performed += InteractP2_Performed;
        operateActions[0].performed += OperateP1_Performed;
        operateActions[1].performed += OperateP2_Performed;
        pauseActions[0].performed += PauseP1_Performed;
        pauseActions[1].performed += PauseP2_Performed;
    }

    private void UnsubscribeFromActions()
    {
        moveActions[0].performed -= MoveP1_Performed;
        moveActions[1].performed -= MoveP2_Performed;
        interactActions[0].performed -= InteractP1_Performed;
        interactActions[1].performed -= InteractP2_Performed;
        operateActions[0].performed -= OperateP1_Performed;
        operateActions[1].performed -= OperateP2_Performed;
        pauseActions[0].performed -= PauseP1_Performed;
        pauseActions[1].performed -= PauseP2_Performed;
    }

    private void EnableGameplayMaps()
    {
        playerMaps[0].Enable();

        // AI 模式中的第二个角色不应该接收真人输入。
        if (GameModeSelection.Current == GameMode.LocalMultiplayer)
            playerMaps[1].Enable();
    }

    private void DisableGameplayMaps()
    {
        foreach (InputActionMap map in playerMaps)
            map?.Disable();
    }

    private void ApplyDeviceAssignments()
    {
        List<InputDevice> player1Devices = new List<InputDevice>();
        List<InputDevice> player2Devices = new List<InputDevice>();

        if (Keyboard.current != null)
        {
            // 一块键盘可以同时分给两个 Map，因为双方的默认按键互不重叠。
            player1Devices.Add(Keyboard.current);
            player2Devices.Add(Keyboard.current);
        }

        if (GameModeSelection.Current == GameMode.LocalMultiplayer)
        {
            if (Gamepad.all.Count == 1)
            {
                // 只有一个手柄时让 P1 用键盘、P2 用手柄，是本地双人最实用的默认分配。
                player2Devices.Add(Gamepad.all[0]);
            }
            else if (Gamepad.all.Count >= 2)
            {
                player1Devices.Add(Gamepad.all[0]);
                player2Devices.Add(Gamepad.all[1]);
            }
        }
        else if (Gamepad.all.Count > 0)
        {
            player1Devices.Add(Gamepad.all[0]);
        }

        // Map.devices 限定每个 Action Map 能监听的物理设备，避免一个手柄同时操纵两个人。
        playerMaps[0].devices = player1Devices.ToArray();
        playerMaps[1].devices = player2Devices.ToArray();

        if (lastUsedDevices[0] == InputDeviceKind.Gamepad && !ContainsGamepad(player1Devices))
            SetLastUsedDevice(0, InputDeviceKind.Keyboard);
        if (lastUsedDevices[1] == InputDeviceKind.Gamepad && !ContainsGamepad(player2Devices))
            SetLastUsedDevice(1, InputDeviceKind.Keyboard);
    }

    private static bool ContainsGamepad(List<InputDevice> devices)
    {
        foreach (InputDevice device in devices)
        {
            if (device is Gamepad)
                return true;
        }

        return false;
    }

    private void InputSystem_OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added
            || change == InputDeviceChange.Removed
            || change == InputDeviceChange.Reconnected
            || change == InputDeviceChange.Disconnected)
        {
            ApplyDeviceAssignments();
        }
    }

    public Vector3 GetMovementDirectionNormalized(int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex) || moveActions[playerIndex] == null)
            return Vector3.zero;

        Vector2 value = moveActions[playerIndex].ReadValue<Vector2>();
        return new Vector3(value.x, 0f, value.y).normalized;
    }

    public void ReBinding(BindingType bindingType, Action onComplete)
    {
        ReBinding(bindingType, 0, onComplete);
    }

    public void ReBinding(BindingType bindingType, int playerIndex, Action onComplete)
    {
        if (!IsValidPlayerIndex(playerIndex))
        {
            onComplete?.Invoke();
            return;
        }

        InputAction inputAction = GetAction(bindingType, playerIndex);
        int bindingIndex = FindBindingIndex(inputAction, bindingType, false);
        if (inputAction == null || bindingIndex < 0)
        {
            Debug.LogWarning($"找不到玩家 {playerIndex + 1} 的 {bindingType} 键盘绑定。");
            onComplete?.Invoke();
            return;
        }

        DisableGameplayMaps();
        CancelCurrentRebind();

        // 这里仅修改键盘槽位；手柄保持行业通用的 A/X/Start 布局。
        rebindingOperation = inputAction.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsExcluding("<Mouse>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation => FinishRebind(operation, playerIndex, onComplete, false))
            .OnComplete(operation => FinishRebind(operation, playerIndex, onComplete, true));

        rebindingOperation.Start();
    }

    private void FinishRebind(
        InputActionRebindingExtensions.RebindingOperation operation,
        int playerIndex,
        Action onComplete,
        bool saveChanges)
    {
        operation.Dispose();
        rebindingOperation = null;

        if (saveChanges)
        {
            PlayerPrefs.SetString(
                GameInputBindingsKey,
                gameControl.asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            OnBindingDisplayChanged?.Invoke(this, new PlayerActionEventArgs(playerIndex));
        }

        EnableGameplayMaps();
        onComplete?.Invoke();
    }

    private void CancelCurrentRebind()
    {
        if (rebindingOperation == null)
            return;

        InputActionRebindingExtensions.RebindingOperation operation = rebindingOperation;
        // Cancel 会同步进入 OnCancel，由 FinishRebind 负责释放，避免重复 Dispose。
        operation.Cancel();
    }

    public string GetBindingDisplayString(BindingType bindingType)
    {
        return GetBindingDisplayString(bindingType, 0);
    }

    public string GetBindingDisplayString(BindingType bindingType, int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex))
            return "--";

        bool useGamepad = lastUsedDevices[playerIndex] == InputDeviceKind.Gamepad;
        if (useGamepad && IsMovementBinding(bindingType))
            return $"<color=#45C6B0>G</color>  LS-{GetDirectionLetter(bindingType)}";

        InputAction action = GetAction(bindingType, playerIndex);
        int bindingIndex = FindBindingIndex(action, bindingType, useGamepad);
        if (action == null || bindingIndex < 0)
            return "--";

        string display = action.GetBindingDisplayString(
            bindingIndex,
            InputBinding.DisplayStringOptions.DontOmitDevice);
        string deviceBadge = useGamepad
            ? "<color=#45C6B0>G</color>"
            : "<color=#F4A340>K</color>";
        return $"{deviceBadge}  {display}";
    }

    private InputAction GetAction(BindingType bindingType, int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex))
            return null;

        return bindingType switch
        {
            BindingType.Up => moveActions[playerIndex],
            BindingType.Down => moveActions[playerIndex],
            BindingType.Left => moveActions[playerIndex],
            BindingType.Right => moveActions[playerIndex],
            BindingType.Interact => interactActions[playerIndex],
            BindingType.Operate => operateActions[playerIndex],
            BindingType.Pause => pauseActions[playerIndex],
            _ => null
        };
    }

    private static int FindBindingIndex(
        InputAction action,
        BindingType bindingType,
        bool findGamepad)
    {
        if (action == null)
            return -1;

        string devicePath = findGamepad ? "<Gamepad>" : "<Keyboard>";
        string partName = bindingType.ToString();

        for (int index = 0; index < action.bindings.Count; index++)
        {
            InputBinding binding = action.bindings[index];
            if (binding.isComposite || !binding.path.StartsWith(devicePath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsMovementBinding(bindingType))
                return index;

            if (findGamepad)
                return index;

            if (binding.isPartOfComposite
                && binding.name.Equals(partName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsMovementBinding(BindingType bindingType)
    {
        return bindingType == BindingType.Up
            || bindingType == BindingType.Down
            || bindingType == BindingType.Left
            || bindingType == BindingType.Right;
    }

    private static string GetDirectionLetter(BindingType bindingType)
    {
        return bindingType switch
        {
            BindingType.Up => "U",
            BindingType.Down => "D",
            BindingType.Left => "L",
            BindingType.Right => "R",
            _ => string.Empty
        };
    }

    private void RecordDevice(int playerIndex, InputAction.CallbackContext context)
    {
        InputDeviceKind kind = context.control.device is Gamepad
            ? InputDeviceKind.Gamepad
            : InputDeviceKind.Keyboard;
        SetLastUsedDevice(playerIndex, kind);
    }

    private void SetLastUsedDevice(int playerIndex, InputDeviceKind kind)
    {
        if (lastUsedDevices[playerIndex] == kind)
            return;

        lastUsedDevices[playerIndex] = kind;
        OnBindingDisplayChanged?.Invoke(this, new PlayerActionEventArgs(playerIndex));
    }

    private void MoveP1_Performed(InputAction.CallbackContext context) => RecordDevice(0, context);
    private void MoveP2_Performed(InputAction.CallbackContext context) => RecordDevice(1, context);

    private void InteractP1_Performed(InputAction.CallbackContext context)
    {
        RecordDevice(0, context);
        OnInteractAction?.Invoke(this, new PlayerActionEventArgs(0));
    }

    private void InteractP2_Performed(InputAction.CallbackContext context)
    {
        RecordDevice(1, context);
        OnInteractAction?.Invoke(this, new PlayerActionEventArgs(1));
    }

    private void OperateP1_Performed(InputAction.CallbackContext context)
    {
        RecordDevice(0, context);
        OnOperateAction?.Invoke(this, new PlayerActionEventArgs(0));
    }

    private void OperateP2_Performed(InputAction.CallbackContext context)
    {
        RecordDevice(1, context);
        OnOperateAction?.Invoke(this, new PlayerActionEventArgs(1));
    }

    private void PauseP1_Performed(InputAction.CallbackContext context)
    {
        RecordDevice(0, context);
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void PauseP2_Performed(InputAction.CallbackContext context)
    {
        RecordDevice(1, context);
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsValidPlayerIndex(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < PlayerCount;
    }

    private void EnsurePlayer1GamepadBindings()
    {
        InputActionMap map = playerMaps[0];
        InputAction move = map.FindAction("Move", true);
        InputAction interact = map.FindAction("Interact", true);
        InputAction operate = map.FindAction("Operate", true);
        InputAction pause = map.FindAction("Pause", true);

        AddBindingIfMissing(move, "<Gamepad>/leftStick");
        AddBindingIfMissing(move, "<Gamepad>/dpad");
        AddBindingIfMissing(interact, "<Gamepad>/buttonSouth");
        AddBindingIfMissing(operate, "<Gamepad>/buttonWest");
        AddBindingIfMissing(pause, "<Gamepad>/start");
    }

    private static void AddBindingIfMissing(InputAction action, string path)
    {
        foreach (InputBinding binding in action.bindings)
        {
            if (binding.path.Equals(path, StringComparison.OrdinalIgnoreCase))
                return;
        }

        action.AddBinding(path, groups: "Gamepad");
    }

    private static InputActionMap CreatePlayer2Map(InputActionAsset asset)
    {
        InputActionMap map = new InputActionMap("Player2");
        InputAction move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        InputAction interact = map.AddAction("Interact", InputActionType.Button);
        InputAction operate = map.AddAction("Operate", InputActionType.Button);
        InputAction pause = map.AddAction("Pause", InputActionType.Button);

        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/numpad8")
            .With("Down", "<Keyboard>/numpad5")
            .With("Left", "<Keyboard>/numpad4")
            .With("Right", "<Keyboard>/numpad6");
        move.AddBinding("<Gamepad>/leftStick");
        move.AddBinding("<Gamepad>/dpad");

        interact.AddBinding("<Keyboard>/rightCtrl");
        interact.AddBinding("<Keyboard>/numpad7");
        interact.AddBinding("<Gamepad>/buttonSouth");
        operate.AddBinding("<Keyboard>/rightShift");
        operate.AddBinding("<Keyboard>/numpad9");
        operate.AddBinding("<Gamepad>/buttonWest");
        pause.AddBinding("<Keyboard>/backspace");
        pause.AddBinding("<Gamepad>/start");

        asset.AddActionMap(map);
        return map;
    }

    private void OnDestroy()
    {
        CancelCurrentRebind();
        gameControl?.Dispose();

        if (Instance == this)
            Instance = null;
    }
}
