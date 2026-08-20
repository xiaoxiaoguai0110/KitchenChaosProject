using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSceneUIBuilder
{
    private const string GameScenePath = "Assets/Scenes/2-GameScene.unity";
    private const string IceFontPath = "Assets/Fonts/ICE SDF.asset";
    // Codex 通过 Library 下的一次性标记请求编辑器安全地重建并保存场景 UI。
    private const string ApplyRequestFile = "CodexApplyGameUI.flag";

    private static readonly Color32 OverlayColor = new Color32(18, 14, 18, 218);
    private static readonly Color32 PanelColor = new Color32(58, 32, 21, 246);
    private static readonly Color32 CardColor = new Color32(77, 44, 28, 245);
    private static readonly Color32 OrangeColor = new Color32(238, 77, 5, 255);
    private static readonly Color32 OrangeDarkColor = new Color32(190, 52, 8, 255);
    private static readonly Color32 TealColor = new Color32(0, 105, 88, 255);
    private static readonly Color32 CreamColor = new Color32(255, 231, 184, 255);
    private static readonly Color32 MutedCreamColor = new Color32(220, 205, 183, 255);
    private static readonly Color32 DangerColor = new Color32(87, 16, 13, 255);

    private static TMP_FontAsset iceFont;

    [InitializeOnLoadMethod]
    private static void RegisterRequestedBuild()
    {
        EditorApplication.delayCall += TryRunRequestedBuild;
    }

    private static void TryRunRequestedBuild()
    {
        string requestPath = GetRequestPath();
        if (!File.Exists(requestPath))
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRunRequestedBuild;
            return;
        }

        File.Delete(requestPath);
        ApplyGameUIStyle();
    }

    [MenuItem("Tools/Kitchen Chaos/Apply GameMenu Style To Game UI")]
    public static void ApplyGameUIStyle()
    {
        iceFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(IceFontPath);
        if (iceFont == null)
        {
            Debug.LogError($"[GameSceneUIBuilder] 找不到字体：{IceFontPath}");
            return;
        }

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene gameScene = SceneManager.GetSceneByPath(GameScenePath);
        bool sceneWasAlreadyOpen = gameScene.IsValid() && gameScene.isLoaded;

        try
        {
            if (!sceneWasAlreadyOpen)
                gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);

            ApplyToScene(gameScene);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            Debug.Log("[GameSceneUIBuilder] 游戏内 UI 已按 GameMenuUI 风格更新并保存。");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (!sceneWasAlreadyOpen && gameScene.IsValid() && gameScene.isLoaded)
                EditorSceneManager.CloseScene(gameScene, true);

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
        }
    }

    private static void ApplyToScene(Scene scene)
    {
        GameObject canvasObject = FindInScene(scene, "Canvas");
        if (canvasObject == null)
            throw new InvalidOperationException("2-GameScene 中找不到 Canvas。");

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
        }

        Transform canvas = canvasObject.transform;
        StyleOrderList(FindDeep(canvas, "OrderListUI"));
        StyleGameClock(FindDeep(canvas, "GameClockUI"));
        StyleInteractionPrompts(canvas);
        StyleCountDown(FindDeep(canvas, "CountDownUI"));
        StylePause(FindDeep(canvas, "GamePauseUI"));
        StyleSettings(FindDeep(canvas, "SettingsUI"));
        StyleGameOver(FindDeep(canvas, "GameOverUI"));

        // HUD 保持底层，模态弹窗依次覆盖；结算界面放到最上层，避免仍能点击暂停菜单。
        MoveToTop(canvas, "CountDownUI");
        MoveToTop(canvas, "GamePauseUI");
        MoveToTop(canvas, "SettingsUI");
        MoveToTop(canvas, "GameOverUI");
    }

    private static void StyleOrderList(Transform root)
    {
        if (root == null)
            return;

        SetLayerRecursively(root.gameObject, 5);
        SetAnchoredRect((RectTransform)root, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(38f, -38f), new Vector2(330f, 510f));

        Image panel = EnsureImage(root.gameObject);
        panel.color = PanelColor;
        panel.raycastTarget = false;
        EnsureOutline(root.gameObject, OrangeColor, new Vector2(3f, -3f));

        Transform title = FindDeep(root, "Title");
        StyleText(title, "今日订单", 34f, CreamColor, TextAlignmentOptions.MidlineLeft);
        SetAnchoredRect((RectTransform)title, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -14f), new Vector2(-36f, 58f));

        TextMeshProUGUI englishLabel = GetOrCreateText(root, "OrderCaption", "ORDERS  •  TODAY");
        englishLabel.fontSize = 15f;
        englishLabel.color = OrangeColor;
        englishLabel.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchoredRect(englishLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -61f), new Vector2(-36f, 26f));

        Transform recipeList = FindDeep(root, "RecipeList");
        SetStretch((RectTransform)recipeList, 18f, 18f, 18f, 94f);
        VerticalLayoutGroup verticalLayout = EnsureComponent<VerticalLayoutGroup>(recipeList.gameObject);
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);
        verticalLayout.spacing = 12f;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        Transform template = FindDeep(recipeList, "RecipeTemplate");
        if (template == null)
            return;

        LayoutElement templateLayout = EnsureComponent<LayoutElement>(template.gameObject);
        templateLayout.preferredHeight = 96f;
        Image card = EnsureImage(template.gameObject);
        card.color = CardColor;
        card.raycastTarget = false;

        Image accent = GetOrCreateImage(template, "Accent");
        accent.color = OrangeColor;
        accent.raycastTarget = false;
        accent.transform.SetAsFirstSibling();
        SetAnchoredRect(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, .5f), Vector2.zero, new Vector2(7f, 0f));

        Transform recipeName = FindDeep(template, "RecipeNmae");
        StyleText(recipeName, null, 24f, CreamColor, TextAlignmentOptions.MidlineLeft);
        SetAnchoredRect((RectTransform)recipeName, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, 1f), new Vector2(10f, -8f), new Vector2(-34f, 36f));

        Transform objectList = FindDeep(template, "KitchenObjectList");
        SetAnchoredRect((RectTransform)objectList, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(.5f, 0f), new Vector2(10f, 8f), new Vector2(-34f, 42f));
        HorizontalLayoutGroup horizontalLayout = EnsureComponent<HorizontalLayoutGroup>(objectList.gameObject);
        horizontalLayout.spacing = 8f;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childControlHeight = false;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = false;

        Transform iconTemplate = FindDeep(objectList, "IconTemplate");
        if (iconTemplate != null)
            ((RectTransform)iconTemplate).sizeDelta = new Vector2(38f, 38f);
    }

    private static void StyleGameClock(Transform root)
    {
        if (root == null)
            return;

        SetLayerRecursively(root.gameObject, 5);

        Transform canvas = root.parent;
        Transform oldParent = FindDeep(root, "UIParent");
        Transform clockPanelTransform = FindDirectChild(canvas, "ClockPanel");
        if (clockPanelTransform == null)
            clockPanelTransform = CreateUIObject("ClockPanel", canvas).transform;

        RectTransform clockPanel = (RectTransform)clockPanelTransform;
        SetAnchoredRect(clockPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-38f, -38f), new Vector2(174f, 174f));
        clockPanelTransform.SetSiblingIndex(Mathf.Min(root.GetSiblingIndex() + 1, canvas.childCount - 1));

        // 原层级使用普通 Transform，无法可靠使用 Canvas 锚点；视觉节点移到独立 RectTransform，
        // GameClockUI 仍保留在原对象上，只把它控制的 uiParent 指向新的右上角面板。
        MoveDirectChild(oldParent, clockPanelTransform, "Background");
        MoveDirectChild(oldParent, clockPanelTransform, "ProgressImage");
        MoveDirectChild(oldParent, clockPanelTransform, "ClockCaption");
        if (oldParent != null)
            oldParent.gameObject.SetActive(false);

        Transform background = FindDirectChild(clockPanelTransform, "Background");
        Image backgroundImage = EnsureImage(background.gameObject);
        backgroundImage.color = PanelColor;
        backgroundImage.raycastTarget = false;
        SetFullStretch((RectTransform)background);
        EnsureOutline(background.gameObject, CreamColor, new Vector2(3f, -3f));

        Transform progress = FindDirectChild(clockPanelTransform, "ProgressImage");
        Image progressImage = progress.GetComponent<Image>();
        if (progressImage != null)
            progressImage.color = OrangeColor;
        SetAnchoredRect((RectTransform)progress, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), new Vector2(0f, 6f), new Vector2(138f, 138f));

        Transform timeText = FindDeep(progress, "TimeText");
        StyleText(timeText, null, 48f, CreamColor, TextAlignmentOptions.Center);
        SetFullStretch((RectTransform)timeText);
        EnsureShadow(timeText.gameObject, new Color32(20, 10, 5, 180), new Vector2(3f, -3f));

        TextMeshProUGUI caption = GetOrCreateText(clockPanelTransform, "ClockCaption", "剩余时间");
        caption.fontSize = 16f;
        caption.color = MutedCreamColor;
        caption.alignment = TextAlignmentOptions.Center;
        SetAnchoredRect(caption.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(.5f, 0f), new Vector2(0f, 5f), new Vector2(150f, 28f));

        GameClockUI clockUI = root.GetComponent<GameClockUI>();
        AssignReference(clockUI, "uiParent", clockPanelTransform.gameObject);
    }

    private static void StyleCountDown(Transform root)
    {
        if (root == null)
            return;

        SetLayerRecursively(root.gameObject, 5);
        SetFullStretch((RectTransform)root);

        Transform number = FindDeep(root, "Number");
        StyleText(number, null, 260f, CreamColor, TextAlignmentOptions.Center);
        SetAnchoredRect((RectTransform)number, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), Vector2.zero, new Vector2(420f, 420f));
        EnsureShadow(number.gameObject, new Color32(45, 18, 8, 220), new Vector2(9f, -9f));

        TextMeshProUGUI caption = GetOrCreateText(number, "CountDownCaption", "准备开火");
        caption.fontSize = 34f;
        caption.color = CreamColor;
        caption.alignment = TextAlignmentOptions.Center;
        SetAnchoredRect(caption.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 0f), new Vector2(0f, -30f), new Vector2(320f, 64f));
    }

    private static void StyleInteractionPrompts(Transform canvas)
    {
        CreateInteractionPrompt(canvas, 0, "Player1InteractionPrompt", -240f, OrangeColor);
        CreateInteractionPrompt(canvas, 1, "Player2InteractionPrompt", 240f, TealColor);
    }

    private static void CreateInteractionPrompt(
        Transform canvas,
        int playerIndex,
        string objectName,
        float horizontalPosition,
        Color accentColor)
    {
        Transform existingRoot = FindDirectChild(canvas, objectName);
        GameObject rootObject = existingRoot != null
            ? existingRoot.gameObject
            : CreateUIObject(objectName, canvas);
        RectTransform root = (RectTransform)rootObject.transform;
        SetAnchoredRect(root, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(.5f, 0f), new Vector2(horizontalPosition, 34f), new Vector2(440f, 132f));

        Image promptPanel = GetOrCreateImage(root, "PromptPanel");
        promptPanel.color = PanelColor;
        promptPanel.raycastTarget = false;
        SetAnchoredRect(promptPanel.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(.5f, 0f), Vector2.zero, new Vector2(410f, 58f));
        EnsureOutline(promptPanel.gameObject, accentColor, new Vector2(2f, -2f));

        TextMeshProUGUI promptText = GetOrCreateText(promptPanel.transform, "PromptText", "[E] 交互");
        promptText.fontSize = 24f;
        promptText.color = CreamColor;
        promptText.alignment = TextAlignmentOptions.Center;
        SetFullStretch(promptText.rectTransform);

        Image feedbackPanel = GetOrCreateImage(root, "FeedbackPanel");
        feedbackPanel.color = new Color32(87, 16, 13, 245);
        feedbackPanel.raycastTarget = false;
        SetAnchoredRect(feedbackPanel.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(.5f, 0f), new Vector2(0f, 72f), new Vector2(410f, 52f));
        EnsureOutline(feedbackPanel.gameObject, OrangeColor, new Vector2(2f, -2f));
        CanvasGroup feedbackCanvasGroup = EnsureComponent<CanvasGroup>(feedbackPanel.gameObject);

        TextMeshProUGUI feedbackText = GetOrCreateText(
            feedbackPanel.transform,
            "FeedbackText",
            "当前无法交互");
        feedbackText.fontSize = 21f;
        feedbackText.color = CreamColor;
        feedbackText.alignment = TextAlignmentOptions.Center;
        SetFullStretch(feedbackText.rectTransform);

        InteractionPromptUI promptUI = EnsureComponent<InteractionPromptUI>(rootObject);
        AssignInt(promptUI, "playerIndex", playerIndex);
        AssignReference(promptUI, "promptPanel", promptPanel.gameObject);
        AssignReference(promptUI, "promptText", promptText);
        AssignReference(promptUI, "feedbackPanel", feedbackPanel.gameObject);
        AssignReference(promptUI, "feedbackText", feedbackText);
        AssignReference(promptUI, "feedbackCanvasGroup", feedbackCanvasGroup);
    }

    private static void StylePause(Transform root)
    {
        if (root == null)
            return;

        SetLayerRecursively(root.gameObject, 5);
        SetFullStretch((RectTransform)root);

        Transform uiParent = FindDeep(root, "UIParent");
        SetFullStretch((RectTransform)uiParent);
        Transform background = FindDirectChild(uiParent, "Background");
        StyleOverlay(background);

        RectTransform panel = GetOrCreatePanel(uiParent, "PausePanel", new Vector2(520f, 650f));
        ReparentDirectChild(uiParent, panel, "Text (TMP)");
        ReparentDirectChild(uiParent, panel, "ResumeButton");
        ReparentDirectChild(uiParent, panel, "RestartButton");
        ReparentDirectChild(uiParent, panel, "SettingButton");
        ReparentDirectChild(uiParent, panel, "MenuButton");

        Transform title = FindDirectChild(panel, "Text (TMP)");
        StyleText(title, "暂停", 74f, CreamColor, TextAlignmentOptions.Center);
        SetAnchoredRect((RectTransform)title, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -70f), new Vector2(430f, 110f));
        EnsureShadow(title.gameObject, new Color32(25, 10, 5, 180), new Vector2(5f, -5f));

        TextMeshProUGUI caption = GetOrCreateText(panel, "PauseCaption", "KITCHEN BREAK");
        caption.fontSize = 18f;
        caption.color = OrangeColor;
        caption.alignment = TextAlignmentOptions.Center;
        SetAnchoredRect(caption.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -162f), new Vector2(420f, 34f));

        Button resumeButton = FindDirectChild(panel, "ResumeButton").GetComponent<Button>();
        Button restartButton = GetOrCreateButton(panel, "RestartButton");
        Button settingButton = FindDirectChild(panel, "SettingButton").GetComponent<Button>();
        Button menuButton = FindDirectChild(panel, "MenuButton").GetComponent<Button>();
        PlaceAndStyleButton(resumeButton, new Vector2(0f, 95f), new Vector2(390f, 72f), OrangeColor, "继续");
        PlaceAndStyleButton(restartButton, new Vector2(0f, 0f), new Vector2(390f, 72f), OrangeDarkColor, "重新开始");
        PlaceAndStyleButton(settingButton, new Vector2(0f, -95f), new Vector2(390f, 72f), TealColor, "设置");
        PlaceAndStyleButton(menuButton, new Vector2(0f, -190f), new Vector2(390f, 72f), DangerColor, "返回菜单");

        UIPopupAnimator popup = ConfigurePopup(uiParent, panel);
        GamePauseUI pauseUI = root.GetComponent<GamePauseUI>();
        AssignReference(pauseUI, "restartButton", restartButton);
        AssignReference(pauseUI, "popupAnimator", popup);
    }

    private static void StyleGameOver(Transform root)
    {
        if (root == null)
            return;

        SetLayerRecursively(root.gameObject, 5);
        SetFullStretch((RectTransform)root);

        Transform uiParent = FindDeep(root, "UIParent");
        SetFullStretch((RectTransform)uiParent);
        Transform background = FindDirectChild(uiParent, "Background");
        StyleOverlay(background);

        RectTransform panel = GetOrCreatePanel(uiParent, "GameOverPanel", new Vector2(620f, 760f));
        ReparentDirectChild(uiParent, panel, "GameOverText");
        ReparentDirectChild(uiParent, panel, "LabelText");
        ReparentDirectChild(uiParent, panel, "NumberText");

        Transform title = FindDirectChild(panel, "GameOverText");
        StyleText(title, "营业结束", 70f, CreamColor, TextAlignmentOptions.Center);
        SetAnchoredRect((RectTransform)title, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -82f), new Vector2(520f, 106f));
        EnsureShadow(title.gameObject, new Color32(25, 10, 5, 180), new Vector2(5f, -5f));

        TextMeshProUGUI caption = GetOrCreateText(panel, "GameOverCaption", "SHIFT COMPLETE");
        caption.fontSize = 18f;
        caption.color = OrangeColor;
        caption.alignment = TextAlignmentOptions.Center;
        SetAnchoredRect(caption.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -172f), new Vector2(500f, 34f));

        Transform label = FindDirectChild(panel, "LabelText");
        StyleText(label, "本局统计", 32f, MutedCreamColor, TextAlignmentOptions.Center);
        SetAnchoredRect((RectTransform)label, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), new Vector2(0f, 82f), new Vector2(500f, 60f));

        Transform number = FindDirectChild(panel, "NumberText");
        StyleText(number, null, 38f, CreamColor, TextAlignmentOptions.Center);
        SetAnchoredRect((RectTransform)number, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), new Vector2(0f, -18f), new Vector2(480f, 170f));
        EnsureShadow(number.gameObject, new Color32(25, 10, 5, 180), new Vector2(6f, -6f));

        Button restartButton = GetOrCreateButton(panel, "RestartButton");
        Button menuButton = GetOrCreateButton(panel, "GameOverMenuButton");
        PlaceAndStyleButton(restartButton, new Vector2(0f, -190f), new Vector2(430f, 78f), OrangeColor, "再来一局");
        PlaceAndStyleButton(menuButton, new Vector2(0f, -290f), new Vector2(430f, 70f), DangerColor, "返回菜单");

        UIPopupAnimator popup = ConfigurePopup(uiParent, panel);
        GameOverUI gameOverUI = root.GetComponent<GameOverUI>();
        AssignReference(gameOverUI, "restartButton", restartButton);
        AssignReference(gameOverUI, "menuButton", menuButton);
        AssignReference(gameOverUI, "popupAnimator", popup);
    }

    private static void StyleSettings(Transform root)
    {
        if (root == null)
            return;

        SetLayerRecursively(root.gameObject, 5);
        SetFullStretch((RectTransform)root);

        Transform uiParent = FindDeep(root, "UIParent");
        SetFullStretch((RectTransform)uiParent);
        Transform background = FindDirectChild(uiParent, "Background");
        StyleOverlay(background);

        RectTransform panel = GetOrCreatePanel(uiParent, "SettingsPanel", new Vector2(780f, 920f));
        string[] childrenToMove =
        {
            "SettingsTitle", "SoundButton", "MusicButton", "UpText", "UpKeyButton",
            "DownText", "DownKeyButton", "LeftText", "LeftKeyButton", "RightText",
            "RightKeyButton", "InteractText", "InteractButton", "OperateText",
            "OperateButton", "PauseText", "PauseButton", "CloseButton", "PlayerSelectButton"
        };
        foreach (string childName in childrenToMove)
            ReparentDirectChild(uiParent, panel, childName);

        Transform title = FindDirectChild(panel, "SettingsTitle");
        StyleText(title, "设置", 68f, CreamColor, TextAlignmentOptions.Center);
        SetAnchoredRect((RectTransform)title, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -55f), new Vector2(680f, 90f));
        EnsureShadow(title.gameObject, new Color32(25, 10, 5, 180), new Vector2(5f, -5f));

        TextMeshProUGUI audioCaption = GetOrCreateText(panel, "AudioCaption", "AUDIO  •  音频");
        StyleSectionCaption(audioCaption, new Vector2(-250f, 298f));
        TextMeshProUGUI controlsCaption = GetOrCreateText(panel, "ControlsCaption", "CONTROLS  •  操作");
        StyleSectionCaption(controlsCaption, new Vector2(-250f, 112f));
        Button playerSelectButton = GetOrCreateButton(panel, "PlayerSelectButton");
        PlaceAndStyleButton(playerSelectButton, new Vector2(145f, 112f), new Vector2(250f, 44f), CardColor, "当前：玩家 1");

        Button soundButton = FindDirectChild(panel, "SoundButton").GetComponent<Button>();
        Button musicButton = FindDirectChild(panel, "MusicButton").GetComponent<Button>();
        PlaceAndStyleButton(soundButton, new Vector2(0f, 246f), new Vector2(560f, 58f), OrangeColor, null);
        PlaceAndStyleButton(musicButton, new Vector2(0f, 178f), new Vector2(560f, 58f), TealColor, null);

        StyleBindingRow(panel, "UpText", "UpKeyButton", "向上", 66f);
        StyleBindingRow(panel, "DownText", "DownKeyButton", "向下", 8f);
        StyleBindingRow(panel, "LeftText", "LeftKeyButton", "向左", -50f);
        StyleBindingRow(panel, "RightText", "RightKeyButton", "向右", -108f);
        StyleBindingRow(panel, "InteractText", "InteractButton", "交互", -166f);
        StyleBindingRow(panel, "OperateText", "OperateButton", "操作", -224f);
        StyleBindingRow(panel, "PauseText", "PauseButton", "暂停", -282f);

        Button closeButton = FindDirectChild(panel, "CloseButton").GetComponent<Button>();
        PlaceAndStyleButton(closeButton, new Vector2(0f, -368f), new Vector2(560f, 66f), DangerColor, "关闭");

        Transform rebindingHint = FindDeep(root, "RebindingHint");
        SetFullStretch((RectTransform)rebindingHint);
        Image hintBackground = EnsureImage(rebindingHint.gameObject);
        hintBackground.color = new Color32(18, 14, 18, 238);
        hintBackground.raycastTarget = true;
        Transform hintText = FindDeep(rebindingHint, "RebindingInfo");
        StyleText(hintText, "等待输入...\n按下新的按键", 36f, CreamColor, TextAlignmentOptions.Center);
        SetAnchoredRect((RectTransform)hintText, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), Vector2.zero, new Vector2(620f, 180f));

        UIPopupAnimator popup = ConfigurePopup(uiParent, panel);
        SettingsUI settingsUI = root.GetComponent<SettingsUI>();
        AssignReference(settingsUI, "playerSelectButton", playerSelectButton);
        AssignReference(settingsUI, "playerSelectButtonText", playerSelectButton.GetComponentInChildren<TextMeshProUGUI>());
        AssignReference(settingsUI, "popupAnimator", popup);
    }

    private static void StyleBindingRow(Transform panel, string labelName, string buttonName, string labelText, float y)
    {
        Transform label = FindDirectChild(panel, labelName);
        StyleText(label, labelText, 27f, MutedCreamColor, TextAlignmentOptions.MidlineLeft);
        SetAnchoredRect((RectTransform)label, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), new Vector2(-165f, y), new Vector2(220f, 48f));

        Button button = FindDirectChild(panel, buttonName).GetComponent<Button>();
        PlaceAndStyleButton(button, new Vector2(145f, y), new Vector2(250f, 48f), CardColor, null);
    }

    private static void StyleSectionCaption(TextMeshProUGUI text, Vector2 position)
    {
        text.fontSize = 18f;
        text.color = OrangeColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchoredRect(text.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), position, new Vector2(180f, 32f));
    }

    private static void StyleOverlay(Transform background)
    {
        if (background == null)
            return;

        background.SetAsFirstSibling();
        SetFullStretch((RectTransform)background);
        Image image = EnsureImage(background.gameObject);
        image.color = OverlayColor;
        image.raycastTarget = true;
    }

    private static RectTransform GetOrCreatePanel(Transform parent, string name, Vector2 size)
    {
        Transform existing = FindDirectChild(parent, name);
        RectTransform panel;
        if (existing == null)
        {
            GameObject panelObject = CreateUIObject(name, parent, typeof(Image), typeof(Outline));
            panel = panelObject.GetComponent<RectTransform>();
        }
        else
        {
            panel = (RectTransform)existing;
        }

        SetAnchoredRect(panel, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), Vector2.zero, size);
        Image image = EnsureImage(panel.gameObject);
        image.color = PanelColor;
        image.raycastTarget = true;
        EnsureOutline(panel.gameObject, OrangeColor, new Vector2(4f, -4f));

        Image accent = GetOrCreateImage(panel, "TopAccent");
        accent.color = OrangeColor;
        accent.raycastTarget = false;
        SetAnchoredRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, 1f), Vector2.zero, new Vector2(0f, 7f));
        accent.transform.SetAsFirstSibling();
        panel.SetAsLastSibling();
        return panel;
    }

    private static UIPopupAnimator ConfigurePopup(Transform uiParent, RectTransform panel)
    {
        CanvasGroup canvasGroup = EnsureComponent<CanvasGroup>(uiParent.gameObject);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        UIPopupAnimator popup = EnsureComponent<UIPopupAnimator>(uiParent.gameObject);
        AssignReference(popup, "canvasGroup", canvasGroup);
        AssignReference(popup, "panel", panel);
        return popup;
    }

    private static void PlaceAndStyleButton(Button button, Vector2 position, Vector2 size, Color baseColor, string label)
    {
        if (button == null)
            return;

        SetAnchoredRect((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), position, size);

        Image image = EnsureImage(button.gameObject);
        image.color = Color.white;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors(baseColor);

        if (button.GetComponent<MenuButtonVisual>() == null)
            button.gameObject.AddComponent<MenuButtonVisual>();

        Transform textTransform = FindDeep(button.transform, "Text (TMP)");
        if (textTransform == null)
            textTransform = GetOrCreateText(button.transform, "Text (TMP)", label).transform;

        StyleText(textTransform, label, 30f, CreamColor, TextAlignmentOptions.Center);
        SetFullStretch((RectTransform)textTransform);
    }

    private static ColorBlock CreateButtonColors(Color baseColor)
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, .14f);
        colors.selectedColor = Color.Lerp(baseColor, Color.white, .14f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, .18f);
        colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, .38f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .12f;
        return colors;
    }

    private static Button GetOrCreateButton(Transform parent, string name)
    {
        Transform existing = FindDirectChild(parent, name);
        if (existing != null)
            return existing.GetComponent<Button>();

        GameObject buttonObject = CreateUIObject(name, parent, typeof(Image), typeof(Button), typeof(MenuButtonVisual));
        Button button = buttonObject.GetComponent<Button>();
        TextMeshProUGUI label = GetOrCreateText(buttonObject.transform, "Text (TMP)", name);
        SetFullStretch(label.rectTransform);
        return button;
    }

    private static TextMeshProUGUI GetOrCreateText(Transform parent, string name, string value)
    {
        Transform existing = FindDirectChild(parent, name);
        TextMeshProUGUI text;
        if (existing == null)
        {
            GameObject textObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
            text = textObject.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            text = existing.GetComponent<TextMeshProUGUI>();
        }

        text.font = iceFont;
        text.text = value;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static Image GetOrCreateImage(Transform parent, string name)
    {
        Transform existing = FindDirectChild(parent, name);
        if (existing != null)
            return EnsureImage(existing.gameObject);

        GameObject imageObject = CreateUIObject(name, parent, typeof(Image));
        return imageObject.GetComponent<Image>();
    }

    private static void StyleText(Transform transform, string value, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        if (transform == null)
            return;

        TextMeshProUGUI text = transform.GetComponent<TextMeshProUGUI>();
        if (text == null)
            return;

        text.font = iceFont;
        if (value != null)
            text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private static Image EnsureImage(GameObject gameObject)
    {
        Image image = gameObject.GetComponent<Image>();
        return image != null ? image : gameObject.AddComponent<Image>();
    }

    private static Outline EnsureOutline(GameObject gameObject, Color color, Vector2 distance)
    {
        Outline outline = EnsureComponent<Outline>(gameObject);
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
        return outline;
    }

    private static Shadow EnsureShadow(GameObject gameObject, Color color, Vector2 distance)
    {
        Shadow shadow = gameObject.GetComponent<Shadow>();
        if (shadow == null || shadow is Outline)
            shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
        return shadow;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static GameObject CreateUIObject(string name, Transform parent, params Type[] componentTypes)
    {
        Type[] types = new Type[componentTypes.Length + 1];
        types[0] = typeof(RectTransform);
        Array.Copy(componentTypes, 0, types, 1, componentTypes.Length);

        GameObject gameObject = new GameObject(name, types);
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void AssignReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (target == null)
            return;

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[GameSceneUIBuilder] {target.name} 缺少字段 {propertyName}。");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignInt(UnityEngine.Object target, string propertyName, int value)
    {
        if (target == null)
            return;

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[GameSceneUIBuilder] {target.name} 缺少字段 {propertyName}。");
            return;
        }

        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void ReparentDirectChild(Transform oldParent, Transform newParent, string childName)
    {
        Transform child = FindDirectChild(oldParent, childName);
        if (child != null && child != newParent)
            child.SetParent(newParent, false);
    }

    private static void MoveDirectChild(Transform oldParent, Transform newParent, string childName)
    {
        if (oldParent == null || newParent == null)
            return;

        Transform child = FindDirectChild(oldParent, childName);
        if (child != null)
            child.SetParent(newParent, false);
    }

    private static void MoveToTop(Transform canvas, string objectName)
    {
        Transform target = FindDirectChild(canvas, objectName);
        target?.SetAsLastSibling();
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == name)
                return child;
        }
        return null;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent == null)
            return null;
        if (parent.name == name)
            return parent;

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform result = FindDeep(parent.GetChild(index), name);
            if (result != null)
                return result;
        }
        return null;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindDeep(root.transform, name);
            if (result != null)
                return result.gameObject;
        }
        return null;
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetStretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static string GetRequestPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, "Library", ApplyRequestFile);
    }
}
