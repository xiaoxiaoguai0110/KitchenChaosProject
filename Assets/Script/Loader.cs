using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        GameMenuScene,
        LoadingScene,
        GameScene
    }

    private static Scene targetScene;

    public static void Load(Scene target)
    {
        Time.timeScale = 1;
        targetScene = target;
        SceneManager.LoadScene((int)Scene.LoadingScene);
    }

    public static void LoadBack()
    {
        // LoadBack 也可能被其他脚本直接调用，因此不能只依赖 Load() 恢复时间。
        Time.timeScale = 1;
        SceneManager.LoadScene((int)targetScene);
    }

}
