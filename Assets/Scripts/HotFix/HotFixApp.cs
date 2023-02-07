using Cysharp.Threading.Tasks;
using Saro;
using Saro.Audio;
using Saro.Core;
using Saro.Localization;
using Saro.UI;
using Saro.Utility;
using Tetris;
using Tetris.Save;
using Tetris.UI;
using UnityEngine;
using System.Linq;

namespace HotFix
{
    public static class HotFixApp
    {
        //"mscorlib.dll",
        //"System.dll",
        //"System.Core.dll", // 如果使用了Linq，需要这个
        //"Newtonsoft.Json.dll",
        //"UniTask.dll",
        //"GameMain.dll",
        //"Saro.MGF.dll",
        //"Saro.MoonAsset.dll",
        //"Saro.Entities.dll",
        //"Saro.Entities.Extension.dll",

        public static void Start()
        {
            if (HybridCLR.HybridCLRUtil.IsHotFix)
            {
                try
                {
                    // aot元数据补充
                    HybridCLR.HybridCLRUtil.LoadMetadataForAOTAssembly();

                    // 反射需要重新加载一下
                    ReCacheAssemblies();
                }
                catch (System.Exception e)
                {
                    Log.ERROR(e);
                }
            }

            // 这后面可以使用非aot泛型了
            // 启动游戏
            HotFixAppInternal.Start().Forget();
        }

        private static void ReCacheAssemblies()
        {
            TypeUtility.ClearCacheAssemblies();
            TypeUtility.CacheAssemblies();

#if ENABLE_LOG
            Debug.Log("ReCacheAssemblies:\n" + string.Join(", ", TypeUtility.AssemblyMap.Values.Select(asm => asm.GetName().Name)));
#endif
        }
    }

    internal static class HotFixAppInternal
    {
        public static async UniTask Start()
        {
            // 设置帧率
#if UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
#else
            Application.targetFrameRate = 60;
#endif

            await SetupLocalization();

            LoadGameSave();

            // test 自由开关
            {
                //Main.Instance.gameObject.AddComponent<DownloaderDebuggerGUI>();
            }

            Main.Register<SceneController>();
            UIManager.Current.CacheUIAttributes(); // ui反射需要重新缓存一下
            var uiLoading = UIManager.Current.LoadAndShowWindowAsync(EGameUI.UIStartWindow);
            var sceneLoading = SceneController.Current.ChangeScene(SceneController.ESceneType.Title);
            await uiLoading; // ui可以先加载
            await sceneLoading;

            { // 弹窗测试
                //Saro.UI.UIManager.Current.QueueAsync(Tetris.UI.ETetrisUI.AboutPanel, 1);
                //Saro.UI.UIManager.Current.QueueAsync(Tetris.UI.ETetrisUI.SettingPanel, -1);

                //Saro.UI.UIManager.Current.QueueAsync(Saro.UI.EDefaultUI.UIAlertDialog, 0, new Saro.UI.AlertDialogInfo
                //{
                //    title = "test title",
                //    content = "content",
                //    rightText = "yes",
                //    leftText = "no",
                //    clickHandler = (click) =>
                //    {
                //        Log.ERROR("click: " + click);
                //    }
                //});
            }
        }

        private static async UniTask SetupLocalization()
        {
            await Main.Register<LocalizationManager>()
                .SetProvider(new LocalizationDataProviderExcel())
                .SetLanguageAsync(ELanguage.ZH);
        }

        private static void LoadGameSave()
        {
            Main.Register<SaveManager>();

            SaveManager.Current.Load();

            AudioManager.Current.ApplySettings();
            LocalizationManager.Current.ApplySettings();
        }
    }
}