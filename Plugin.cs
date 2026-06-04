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
        public const string ModVersion = "0.0.3";

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

        private void ModifyGlassAppearance(GameObject piece, float width, float height)
        {
            Transform high = piece.transform.Find("New/High");

            if (high == null)
            {
                Logger.LogWarning($"High object not found on {piece.name}");
                return;
            }

            // Scale piece
            high.localScale = new Vector3(width, height, 0.15f);

            MeshRenderer renderer = high.GetComponent<MeshRenderer>();

            if (renderer == null)
            {
                Logger.LogWarning($"MeshRenderer not found on {piece.name}");
                return;
            }

            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            Material mat = renderer.material;

            Color c = mat.color;
            c.a = 0.15f;
            mat.color = c;

            // Maintain texture density
            mat.mainTextureScale = new Vector2(3f * width, 3f * height);

            Logger.LogInfo($"Applied glass appearance to {piece.name}");
        }

        private void RegisterGlassPiece(
            string internalName,
            string displayName,
            float width,
            float height)
        {
            PieceConfig pieceConfig = new PieceConfig
            {
                Name = displayName,
                PieceTable = PieceTables.Hammer,
                Category = "BuildingStonecutter"
            };

            CustomPiece customPiece = new CustomPiece(
                internalName,
                "crystal_wall_1x1",
                pieceConfig);

            GameObject glassWindow = customPiece.PiecePrefab;

            if (glassWindow == null)
            {
                Logger.LogError($"Failed to clone crystal_wall_1x1 for {displayName}");
                return;
            }

            ModifyGlassAppearance(glassWindow, width, height);

            Piece piece = glassWindow.GetComponent<Piece>();

            if (piece != null)
            {
                piece.m_name = displayName;
                piece.m_description = "";
            }

            PieceManager.Instance.AddPiece(customPiece);

            Logger.LogInfo($"Registered {displayName}");
        }

        private void OnVanillaPrefabsAvailable()
        {
            Logger.LogInfo("Vanilla prefabs are available.");

            Logger.LogInfo($"Hammer = {PieceTables.Hammer}");

            RegisterGlassPiece(
                "piece_glass_window_1x1",
                "Glass Window 1x1",
                1f,
                1f);

            RegisterGlassPiece(
                "piece_glass_window_2x1",
                "Glass Window 2x1",
                2f,
                1f);

            RegisterGlassPiece(
                "piece_glass_window_1x2",
                "Glass Window 1x2",
                1f,
                2f);

            RegisterGlassPiece(
                "piece_glass_window_2x2",
                "Glass Window 2x2",
                2f,
                2f);
        }
    }
}