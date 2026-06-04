using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
namespace VanillaGlass
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "revenga.valheim.vanillaglass";
        public const string ModName = "Vanilla Glass";
        public const string ModVersion = "0.0.1";

        internal static Plugin Instance;

        private Harmony harmony;

        private void Awake()
        {
            Instance = this;

            Logger.LogInfo($"{ModName} loading...");

            harmony = new Harmony(ModGUID);
            harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;

            Logger.LogInfo($"{ModName} loaded.");
        }

        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;

            harmony?.UnpatchSelf();
        }

        private void OnVanillaPrefabsAvailable()
        {
            Logger.LogInfo("Vanilla prefabs are available.");

            Logger.LogInfo($"Hammer = {PieceTables.Hammer}");

            PieceConfig pieceConfig = new PieceConfig
            {
                Name = "Glass Window 1x1",
                PieceTable = PieceTables.Hammer,
                Category = "BuildingStonecutter"
            };

            CustomPiece customPiece = new CustomPiece(
                "piece_glass_window_1x1",
                "crystal_wall_1x1",
                pieceConfig);

            GameObject glassWindow = customPiece.PiecePrefab;

            if (glassWindow == null)
            {
                Logger.LogError("Failed to clone crystal_wall_1x1");
                return;
            }

            Logger.LogInfo($"Cloned prefab: {glassWindow.name}");

            // Modify High LOD visual appearance
            Transform high = glassWindow.transform.Find("New/High");

            if (high != null)
            {
                // Reduce thickness
                high.localScale = new Vector3(1f, 1f, 0.15f);

                MeshRenderer renderer = high.GetComponent<MeshRenderer>();

                if (renderer != null)
                {
                    // Disable shadows
                    renderer.shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;

                    Material mat = renderer.material;

                    // Reduce opacity
                    Color c = mat.color;
                    c.a = 0.15f;
                    mat.color = c;

                    // Reduce texture repetition
                    mat.mainTextureScale = new Vector2(3f, 3f);

                    Logger.LogInfo("Modified High glass renderer");
                }
                else
                {
                    Logger.LogWarning("High MeshRenderer not found");
                }
            }
            else
            {
                Logger.LogWarning("High object not found");
            }

            Piece piece = glassWindow.GetComponent<Piece>();

            if (piece != null)
            {
                piece.m_name = "Glass Window 1x1";
                piece.m_description = "";
            }

            PieceManager.Instance.AddPiece(customPiece);

            Logger.LogInfo("Registered Glass Window 1x1");
        }
    }
}