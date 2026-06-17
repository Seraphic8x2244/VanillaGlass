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
        public const string ModVersion = "0.0.10";

        private const string GlassItemPrefabName = "VanillaGlass_Glass";

        private const float GlassAlpha = 0.15f;
        private const float GlassThickness = 0.15f;
        private const float GlassTextureScale = 3f;

        private const float GlassItemSize = 0.5f;
        private const float GlassItemThickness = 0.075f;

        private const float Roof26Pitch = -63.435f;
        private const float Roof45Pitch = -45f;
        private const float SkylightPitch = -90f;

        private const float Roof26TopY = 1f;
        private const float Roof26BottomY = 0f;
        private const float Roof45TopY = 1.5f;
        private const float Roof45BottomY = -0.5f;
        private const float SkylightSnapY = 0.5f;

        internal static Plugin Instance;

        private Harmony harmony;

        private enum GlassPieceType
        {
            Window,
            Roof26,
            Roof45,
            Skylight
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
                    return Roof26Pitch;
                case GlassPieceType.Roof45:
                    return Roof45Pitch;
                case GlassPieceType.Skylight:
                    return SkylightPitch;
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

            high.localScale = new Vector3(width, height, GlassThickness);

            if (newRoot != null && !Mathf.Approximately(pitchDegrees, 0f))
            {
                newRoot.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
                Logger.LogInfo($"Rotated visual geometry on {piece.name} by {pitchDegrees} degrees");
            }

            ApplyGlassMaterial(high, width, height);

            if (low != null)
            {
                RemoveLowLodMesh(low, piece.name);
            }

            Logger.LogInfo($"Applied glass appearance to {piece.name}");
        }

        private void ApplyGlassMaterial(Transform high, float width, float height)
        {
            MeshRenderer renderer = high.GetComponent<MeshRenderer>();

            if (renderer == null)
            {
                Logger.LogWarning($"MeshRenderer not found on {high.name}");
                return;
            }

            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            Material mat = renderer.material;

            Color c = mat.color;
            c.a = GlassAlpha;
            mat.color = c;

            mat.mainTextureScale = new Vector2(
                GlassTextureScale * width,
                GlassTextureScale * height);
        }

        private void RemoveLowLodMesh(Transform low, string pieceName)
        {
            MeshFilter lowMeshFilter = low.GetComponent<MeshFilter>();

            if (lowMeshFilter != null)
            {
                lowMeshFilter.sharedMesh = null;
                Logger.LogInfo($"Removed Low LOD mesh on {pieceName}");
            }
            else
            {
                Logger.LogWarning($"Low LOD MeshFilter not found on {pieceName}");
            }
        }

        private void AdjustSnapPoints(GameObject piece, float width, float height, GlassPieceType pieceType)
        {
            Transform top1 = piece.transform.Find("$hud_snappoint_top 1");
            Transform bottom1 = piece.transform.Find("$hud_snappoint_bottom 1");
            Transform top2 = piece.transform.Find("$hud_snappoint_top 2");
            Transform bottom2 = piece.transform.Find("$hud_snappoint_bottom 2");

            switch (pieceType)
            {
                case GlassPieceType.Roof26:
                    AdjustRoof26SnapPoints(piece, width, top1, bottom1, top2, bottom2);
                    return;
                case GlassPieceType.Roof45:
                    AdjustRoof45SnapPoints(piece, width, top1, bottom1, top2, bottom2);
                    return;
                case GlassPieceType.Skylight:
                    AdjustSkylightSnapPoints(piece, width, height, top1, bottom1, top2, bottom2);
                    return;
            }

            AdjustWindowSnapPoints(piece, width, height, top1, bottom1, top2, bottom2);
        }

        private void AdjustWindowSnapPoints(
            GameObject piece,
            float width,
            float height,
            Transform top1,
            Transform bottom1,
            Transform top2,
            Transform bottom2)
        {
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
                Roof26TopY,
                Roof26BottomY,
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
                Roof45TopY,
                Roof45BottomY,
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

        private void AdjustSkylightSnapPoints(
            GameObject piece,
            float width,
            float height,
            Transform top1,
            Transform bottom1,
            Transform top2,
            Transform bottom2)
        {
            float right = width / 2f;
            float left = -width / 2f;
            float front = height / 2f;
            float back = -height / 2f;

            if (top1 != null)
            {
                top1.localPosition = new Vector3(right, SkylightSnapY, back);
                top1.localRotation = Quaternion.identity;
            }

            if (bottom1 != null)
            {
                bottom1.localPosition = new Vector3(right, SkylightSnapY, front);
                bottom1.localRotation = Quaternion.identity;
            }

            if (top2 != null)
            {
                top2.localPosition = new Vector3(left, SkylightSnapY, back);
                top2.localRotation = Quaternion.identity;
            }

            if (bottom2 != null)
            {
                bottom2.localPosition = new Vector3(left, SkylightSnapY, front);
                bottom2.localRotation = Quaternion.identity;
            }

            Logger.LogInfo($"Adjusted skylight snap points on {piece.name}");
        }

        private void ReplaceGlassItemModel(GameObject itemPrefab)
        {
            HideChild(itemPrefab, "Cube");
            HideChild(itemPrefab, "interior");
            HideChild(itemPrefab, "Point light");

            GameObject crystalWall = PrefabManager.Instance.GetPrefab("crystal_wall_1x1");

            if (crystalWall == null)
            {
                Logger.LogWarning("Could not find crystal_wall_1x1 for Glass item model");
                return;
            }

            Transform wallVisual = crystalWall.transform.Find("New");

            if (wallVisual == null)
            {
                Logger.LogWarning("Could not find New visual object on crystal_wall_1x1");
                return;
            }

            GameObject glassVisual = Instantiate(wallVisual.gameObject);
            glassVisual.name = "GlassItemVisual";
            glassVisual.transform.SetParent(itemPrefab.transform, false);
            glassVisual.transform.localPosition = Vector3.zero;
            glassVisual.transform.localRotation = Quaternion.identity;
            glassVisual.transform.localScale = Vector3.one;

            Transform high = glassVisual.transform.Find("High");
            Transform low = glassVisual.transform.Find("Low");

            if (high != null)
            {
                high.localScale = new Vector3(
                    GlassItemSize,
                    GlassItemSize,
                    GlassItemThickness);

                ApplyGlassMaterial(
                    high,
                    GlassItemSize,
                    GlassItemSize);
            }
            else
            {
                Logger.LogWarning("High object not found on Glass item visual");
            }

            if (low != null)
            {
                RemoveLowLodMesh(low, "Glass item");
            }

            Logger.LogInfo("Replaced Glass item model");
        }

        private void HideChild(GameObject parent, string childName)
        {
            Transform child = parent.transform.Find(childName);

            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private void RegisterGlassMaterial()
        {
            ItemConfig glassConfig = new ItemConfig
            {
                Name = "Glass",
                Description = "Crystal, honed smooth.",
                CraftingStation = CraftingStations.ArtisanTable,
                Amount = 1
            };

            glassConfig.AddRequirement("Crystal", 1, 1);
            glassConfig.AddRequirement("Resin", 1, 1);

            CustomItem glassItem = new CustomItem(
                GlassItemPrefabName,
                "Crystal",
                glassConfig);

            ReplaceGlassItemModel(glassItem.ItemPrefab);

            ItemDrop itemDrop = glassItem.ItemPrefab.GetComponent<ItemDrop>();

            if (itemDrop != null)
            {
                itemDrop.m_itemData.m_shared.m_icons = new Sprite[]
                {
                    LoadEmbeddedIcon("VanillaGlass.Assets.glass.png")
                };
            }

            ItemManager.Instance.AddItem(glassItem);
            Logger.LogInfo("Registered Glass material");
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
                CraftingStation = CraftingStations.ArtisanTable,
                Category = "BuildingStonecutter"
            };

            pieceConfig.AddRequirement(
                GlassItemPrefabName,
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

            RegisterGlassMaterial();

            RegisterGlassPiece(
                "piece_glass_window_1x2",
                "Glass Window 1x2",
                "VanillaGlass.Assets.glass_window_1x2.png",
                1f,
                2f);

            RegisterGlassPiece(
                "piece_glass_roof_26_1x2",
                "Glass Roof 26° 1x2",
                "VanillaGlass.Assets.glass_roof_26_1x2.png",
                1f,
                Mathf.Sqrt(5f),
                GlassPieceType.Roof26);

            RegisterGlassPiece(
                "piece_glass_roof_45_1x2",
                "Glass Roof 45° 1x2",
                "VanillaGlass.Assets.glass_roof_45_1x2.png",
                1f,
                Mathf.Sqrt(8f),
                GlassPieceType.Roof45);

            RegisterGlassPiece(
                "piece_glass_skylight_1x2",
                "Glass Skylight 1x2",
                "VanillaGlass.Assets.glass_skylight_1x2.png",
                1f,
                2f,
                GlassPieceType.Skylight);
        }
    }
}