using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace VanillaGlass
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "revenga.valheim.vanillaglass";
        public const string ModName = "Vanilla Glass";
        public const string ModVersion = "0.0.7";

        internal static Plugin Instance;

        private Harmony harmony;

        private enum GlassPieceType
        {
            Window,
            Roof26,
            Roof45
        }

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

        private float GetPitchDegrees(GlassPieceType pieceType)
        {
            switch (pieceType)
            {
                case GlassPieceType.Roof26:
                    return -63.435f;
                case GlassPieceType.Roof45:
                    return -45f;
                default:
                    return 0f;
            }
        }

        private void ModifyGlassAppearance(GameObject piece, float width, float height, GlassPieceType pieceType)
        {
            float pitchDegrees = GetPitchDegrees(pieceType);
            Transform newRoot = piece.transform.Find("New");
            Transform high = piece.transform.Find("New/High");
            Transform low = piece.transform.Find("New/Low");

            if (high == null)
            {
                Logger.LogWarning($"High object not found on {piece.name}");
                return;
            }

            // Scale mesh and collider
            high.localScale = new Vector3(width, height, 0.15f);

            // Rotate visual geometry for roof/ceiling variants
            if (newRoot != null && !Mathf.Approximately(pitchDegrees, 0f))
            {
                newRoot.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
                Logger.LogInfo($"Rotated visual geometry on {piece.name} by {pitchDegrees} degrees");
            }

            MeshRenderer renderer = high.GetComponent<MeshRenderer>();

            if (renderer == null)
            {
                Logger.LogWarning($"MeshRenderer not found on {piece.name}");
                return;
            }

            // Remove Low LOD mesh so distant glass disappears instead of showing opaque crystal
            if (low != null)
            {
                MeshFilter lowMeshFilter = low.GetComponent<MeshFilter>();

                if (lowMeshFilter != null)
                {
                    lowMeshFilter.sharedMesh = null;
                    Logger.LogInfo($"Removed Low LOD mesh on {piece.name}");
                }
                else
                {
                    Logger.LogWarning($"Low LOD MeshFilter not found on {piece.name}");
                }
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

        private void AdjustSnapPoints(GameObject piece, float width, float height, GlassPieceType pieceType)
        {
            Transform top1 = piece.transform.Find("$hud_snappoint_top 1");
            Transform bottom1 = piece.transform.Find("$hud_snappoint_bottom 1");
            Transform top2 = piece.transform.Find("$hud_snappoint_top 2");
            Transform bottom2 = piece.transform.Find("$hud_snappoint_bottom 2");

            // Roof/ceiling variants need dedicated roof-style snap coordinates.
            // Do not rotate wall/window snap points.
            switch (pieceType)
            {
                case GlassPieceType.Roof26:
                    AdjustRoof26SnapPoints(piece, width, top1, bottom1, top2, bottom2);
                    return;
                case GlassPieceType.Roof45:
                    AdjustRoof45SnapPoints(piece, width, top1, bottom1, top2, bottom2);
                    return;
            }

            float left = -width / 2f;
            float right = width / 2f;

            float bottom;
            float top;

            if (height <= 1f)
            {
                bottom = 0f;
                top = 1f;
            }
            else
            {
                bottom = -0.5f;
                top = height - 0.5f;
            }

            if (top1 != null)
                top1.localPosition = new Vector3(left, top, 0f);

            if (bottom1 != null)
                bottom1.localPosition = new Vector3(left, bottom, 0f);

            if (top2 != null)
                top2.localPosition = new Vector3(right, top, 0f);

            if (bottom2 != null)
                bottom2.localPosition = new Vector3(right, bottom, 0f);

            Logger.LogInfo($"Adjusted window snap points on {piece.name}");
        }

        private void AdjustRoof26SnapPoints(
            GameObject piece,
            float width,
            Transform top1,
            Transform bottom1,
            Transform top2,
            Transform bottom2)
        {
            AdjustRoofSnapPoints(
                piece,
                width,
                top1,
                bottom1,
                top2,
                bottom2,
                1f,
                0f,
                "26°");
        }

        private void AdjustRoof45SnapPoints(
            GameObject piece,
            float width,
            Transform top1,
            Transform bottom1,
            Transform top2,
            Transform bottom2)
        {
            AdjustRoofSnapPoints(
                piece,
                width,
                top1,
                bottom1,
                top2,
                bottom2,
                1.5f,
                -0.5f,
                "45°");
        }

        private void AdjustRoofSnapPoints(
            GameObject piece,
            float width,
            Transform top1,
            Transform bottom1,
            Transform top2,
            Transform bottom2,
            float topY,
            float bottomY,
            string roofLabel)
        {
            float right = width / 2f;
            float left = -width / 2f;

            // Vanilla roof pieces cover a 2x2 hole.
            // Our glass roof pieces cover a 1x2 hole, so X is halved.
            // Y/Z are copied from the matching vanilla roof snap layout.

            if (top1 != null)
            {
                top1.localPosition = new Vector3(right, topY, -1f);
                top1.localRotation = Quaternion.identity;
            }

            if (bottom1 != null)
            {
                bottom1.localPosition = new Vector3(right, bottomY, 1f);
                bottom1.localRotation = Quaternion.identity;
            }

            if (top2 != null)
            {
                top2.localPosition = new Vector3(left, topY, -1f);
                top2.localRotation = Quaternion.identity;
            }

            if (bottom2 != null)
            {
                bottom2.localPosition = new Vector3(left, bottomY, 1f);
                bottom2.localRotation = Quaternion.identity;
            }

            Logger.LogInfo($"Adjusted roof {roofLabel} snap points on {piece.name}");
        }

        private Sprite LoadEmbeddedIcon(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Logger.LogError($"Could not find embedded icon: {resourceName}");
                    return null;
                }

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                Texture2D texture = AssetUtils.LoadImage(data);

                if (texture == null)
                {
                    Logger.LogError($"Could not load embedded icon image: {resourceName}");
                    return null;
                }

                texture.name = resourceName;

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));

                sprite.name = resourceName;

                return sprite;
            }
        }

        private void RegisterGlassPiece(
            string internalName,
            string displayName,
            string iconResource,
            float width,
            float height,
            GlassPieceType pieceType = GlassPieceType.Window)
        {
            PieceConfig pieceConfig = new PieceConfig
            {
                Name = displayName,
                PieceTable = PieceTables.Hammer,
                Category = "BuildingStonecutter"
            };

            pieceConfig.AddRequirement(
                "Crystal",
                Mathf.RoundToInt(width * height),
                true);

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

            ModifyGlassAppearance(glassWindow, width, height, pieceType);
            AdjustSnapPoints(glassWindow, width, height, pieceType);

            Piece piece = glassWindow.GetComponent<Piece>();

            if (piece != null)
            {
                piece.m_name = displayName;
                piece.m_description = "";
                piece.m_icon = LoadEmbeddedIcon(iconResource);
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
                "VanillaGlass.Assets.glass_window_1x1.png",
                1f,
                1f);

            RegisterGlassPiece(
                "piece_glass_window_2x1",
                "Glass Window 2x1",
                "VanillaGlass.Assets.glass_window_2x1.png",
                2f,
                1f);

            RegisterGlassPiece(
                "piece_glass_window_1x2",
                "Glass Window 1x2",
                "VanillaGlass.Assets.glass_window_1x2.png",
                1f,
                2f);

            RegisterGlassPiece(
                "piece_glass_window_2x2",
                "Glass Window 2x2",
                "VanillaGlass.Assets.glass_window_2x2.png",
                2f,
                2f);

            RegisterGlassPiece(
                "piece_glass_roof_26_1x2",
                "Glass Roof 26° 1x2",
                "VanillaGlass.Assets.glass_window_1x2.png",
                1f,
                Mathf.Sqrt(5f),
                GlassPieceType.Roof26);

            RegisterGlassPiece(
                "piece_glass_roof_45_1x2",
                "Glass Roof 45° 1x2",
                "VanillaGlass.Assets.glass_window_1x2.png",
                1f,
                Mathf.Sqrt(8f),
                GlassPieceType.Roof45);
        }
    }
}
