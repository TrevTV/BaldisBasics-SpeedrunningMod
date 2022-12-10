using MelonLoader;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiSpeedrun
{
    public static class BuildInfo
    {
        public const string Name = "BaldiSpeedrun";
        public const string Author = "trev";
        public const string Company = null;
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
    }

    public class Core : MelonMod
    {
        public PlayerMovement playerMovement;

        public GameObject timerPrefab;
        public Text timerText;
        public static bool timerActive;
        private static readonly Stopwatch stopwatch = new Stopwatch();

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("OnInitializeMelon");

            Stream stream = MelonAssembly.Assembly.GetManifestResourceStream("BaldiSpeedrun.speedruntimer");

            byte[] data;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                data = ms.ToArray();
            }

            AssetBundle bundle = AssetBundle.LoadFromMemory(data);
            timerPrefab = bundle.LoadAsset<GameObject>("TimerCanvas.prefab");
            timerPrefab.hideFlags = HideFlags.DontUnloadUnusedAsset;

            HarmonyInstance.Patch(typeof(ClassicWin).GetMethod("Initialize"), typeof(Core).GetMethod(nameof(EndTimer)).ToNewHarmonyMethod());
            HarmonyInstance.Patch(typeof(GlobalCam).GetMethod("Transition"), typeof(Core).GetMethod(nameof(NoTransition)).ToNewHarmonyMethod());
        }

        public static bool NoTransition()
        {
            return false;
        }

        public static void EndTimer()
        {
            MelonLogger.Msg("EndTimer called!");
            if (timerActive)
            {
                stopwatch.Stop();
                timerActive = false;
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            MelonLogger.Msg(sceneName + " : " + buildIndex);
            if (sceneName == "Game")
            {
                timerText = GameObject.Instantiate(timerPrefab).GetComponentInChildren<Text>();
                timerText.fontSize /= 2;
                timerText.gameObject.AddComponent<Outline>();
                stopwatch.Reset();
                MelonCoroutines.Start(MovementCheck());
            }
        }

        private IEnumerator MovementCheck()
        {
            var wait = new WaitForSecondsRealtime(1);
            while (playerMovement == null)
            {
                playerMovement = GameObject.FindObjectOfType<PlayerMovement>();
                yield return wait;
            }

            MelonLogger.Msg("(hopefully) found player " + playerMovement.name);


            while ((playerMovement?.realVelocity ?? 0) == 0)
            {
                yield return null;
            }

            timerActive = true;
            stopwatch.Start();
        }

        public override void OnUpdate()
        {
            /*if (Input.GetKeyDown(KeyCode.L))
            {
                EndTimer();
            }*/

            if (timerActive)
            {
                try
                {
                    string elapsed = stopwatch.Elapsed.ToString(@"mm\:ss\.fff");
                    timerText.text = elapsed;
                }
                catch { timerActive = false; }
            }
        }
    }
}
