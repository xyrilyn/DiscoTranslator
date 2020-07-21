﻿using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DiscoTranslator
{
    public static class ImageManager
    {
        private static readonly Dictionary<string, Sprite> ImageDict = new Dictionary<string, Sprite>();

        public static Sprite Furies { get; private set; }

        public static void AddImage(string name, Sprite sprite)
        {
            ImageDict.Add(name, sprite);
        }

        public static bool TryGetImage(string name, out Sprite sprite)
        {
            return ImageDict.TryGetValue(name, out sprite);
        }

        public static void LoadImages(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var pngPath in Directory.GetFiles(path))
            {
                if (Path.GetExtension(pngPath).ToLower() != ".png")
                    continue;

                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(pngPath), true);
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

                AddImage(Path.GetFileNameWithoutExtension(pngPath), sprite);
            }

            var furiesPath = Path.Combine(path, "DialogueImages", "furies.png");
            if (File.Exists(furiesPath))
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(furiesPath), true);
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

                Furies = sprite;
            }
        }

        public static void ExportImages(string path)
        {
            Hook.EnableImageHook = false;

            Directory.CreateDirectory(path);

            var languageSources = Resources.FindObjectsOfTypeAll<I2.Loc.LanguageSourceAsset>();

            foreach (var source in languageSources)
            {
                foreach (var asset in source.mSource.Assets)
                {
                    string name = asset.name;
                    Texture2D texture;

                    if (asset is Sprite sprite)
                        texture = sprite.texture;
                    else if (asset is Texture2D)
                        texture = asset as Texture2D;
                    else
                        continue;

                    SaveTexture2D(texture, Path.Combine(path, name + ".png"));
                }
            }

            Hook.EnableImageHook = true;
        }

        public static void SaveTexture2D(Texture2D texture, string path)
        {
            // Cannot directly access Texture data, use RenderTexture to get image data
            // From https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-

            var tmp = RenderTexture.GetTemporary(
                  texture.width,
                  texture.height,
                  0,
                  RenderTextureFormat.Default,
                  RenderTextureReadWrite.Linear);

            Graphics.Blit(texture, tmp);
            var previous = RenderTexture.active;
            RenderTexture.active = tmp;
            var newTexture = new Texture2D(texture.width, texture.height);
            newTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            newTexture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            File.WriteAllBytes(path, newTexture.EncodeToPNG());
        }
    }
}
