using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ZstdSharp;
using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;

namespace Next_generationSite_27.UnionP.testing
{
    public static class Compressors
    {
        public enum BitDepth
        {
            Bits1 = 1,
            Bits4 = 4,
            Bits8 = 8,
            Bits16 = 16
        }

        public static string CompressTMP(string code)
        {
            string[] replacements = {
                "#ffffff:#fff",
                "#000000:#000",
                "#ff0000:red",
                "#ffff00:yellow",
                "#00ff00:green",
                "#00ffff:cyan",
                "#0000ff:blue"
            };

            foreach (var replacement in replacements)
            {
                string[] parts = replacement.Split(':');
                code = code.Replace(parts[0], parts[1]);
            }
            return code;
        }

        public static Bitmap QuantizeBitmap(Bitmap frame)
        {
            int height = frame.Height;
            int width = frame.Width;
            Bitmap quantizedFrame = new(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixelColor = frame.GetPixel(x, y);
                    Color quantizedColor = Color.FromArgb( // Quantize colors by rounding their components to reduce the number of distinct colors
                        (pixelColor.R / 128) * 128,
                        (pixelColor.G / 128) * 128,
                        (pixelColor.B / 128) * 128
                    );
                    quantizedFrame.SetPixel(x, y, quantizedColor);
                }
            }
            return quantizedFrame;
        }

        public static async Task<Bitmap> DownscaleAsync(Bitmap original)
        {
            return await Task.Run(() => Downscale(original));
        }

        public static Bitmap Downscale(Bitmap original)
        {
            double scaleFactor = 0.7;

            int newWidth = (int)(original.Width * scaleFactor);
            int newHeight = (int)(original.Height * scaleFactor);

            Bitmap resizedImage = new(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            return resizedImage;
        }

    }
    public static class image //https://github.com/nomoemptypointer/icom_media_display/
    {
        public static Dictionary<string,(bool processing, string result)> cacheData = new();
        public static bool getFrame(string frameName, ref string tmpString)
        {
            if (cacheData.ContainsKey(frameName))
            {
                if (!cacheData[frameName].processing)
                {
                    tmpString = cacheData[frameName].result;
                    return true;
                }
                return false;
            }
            ConvertFrameAsync(frameName);
            return false;
        }
        private static async Task ConvertFrameAsync(string framePath,float fontSize = 1f,long maxSize = 46000)
        {

            cacheData[framePath] = (true,"");
            long codelen = 0;
            using (FileStream stream = new(framePath, FileMode.Open, FileAccess.Read))
            using (Bitmap frame = new(stream))
            {
                Bitmap frameToProcess = frame;
                frameToProcess = Compressors.QuantizeBitmap(frameToProcess);

                string tmpRepresentation = await ConvertToTMPCodeAsync(frameToProcess);

                while (tmpRepresentation.Length > maxSize)
                {
                    Task<Bitmap> compressorTask = Task.Run(() => Compressors.DownscaleAsync(frameToProcess));
                    frameToProcess = await compressorTask;

                    Task<string> conversionTask = Task.Run(() => ConvertToTMPCode(frameToProcess, fontSize));
                    tmpRepresentation = await conversionTask;

                    if (tmpRepresentation.Length > maxSize)
                    {
                        Log.Debug($"Frame {framePath} exceeds deadzone after downscaling, retrying until it fits. ({tmpRepresentation.Length} < {maxSize})");
                    }
                }
                cacheData[framePath] = (false,tmpRepresentation);
            }
        }

        public static async Task<string> ConvertToTMPCodeAsync(Bitmap frame)
        {
            return await Task.Run(() => ConvertToTMPCode(frame));
        }

        public static string ConvertToTMPCode(Bitmap frame,float fontSize = 2f)
        {
            int height = frame.Height;
            int width = frame.Width;
            StringBuilder codeBuilder = new();
            StringBuilder colorBlock = new();
            Color previousColor = Color.Empty;

            for (int y = 0; y < height; y++)
            {
                colorBlock.Clear(); // Clear the color block for each new row
                previousColor = Color.Empty; // Reset previous color for new row

                for (int x = 0; x < width; x++)
                {
                    Color pixelColor = frame.GetPixel(x, y);

                    if (pixelColor != previousColor)
                    {
                        if (colorBlock.Length > 0)
                        {
                            codeBuilder.Append(GetColoredBlock(colorBlock.ToString(), previousColor));
                            colorBlock.Clear();
                        }
                    }

                    colorBlock.Append("█");
                    previousColor = pixelColor;
                }
                codeBuilder.Append(GetColoredBlock(colorBlock.ToString(), previousColor)).Append("\n");
            }
            string codeStr = $"<line-height=39%><size={fontSize}>" + codeBuilder.ToString() + "</size></line-height>";
            return Compressors.CompressTMP(codeStr);
        }

        private static string GetColoredBlock(string content, Color color)
        {
            return $"<color=#{RgbToHex(color)}>{content}</color>";
        }
        public static string RgbToHex(Color color)
        {
            return color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
}
