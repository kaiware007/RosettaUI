using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosettaUI.Builder
{
    public static class AnimationCurvePickerHelper
    {
        public static Texture2D GenerateAnimationCurveTexture(AnimationCurve curve, Texture2D texture)
        {
            int stepNum = 256;
            int heightNum = 32;
            if (texture != null)
            {
                stepNum = texture.width;
                heightNum = texture.height;
                Object.Destroy(texture);
            }
            
            texture = new Texture2D(stepNum, heightNum, TextureFormat.RGB24, false, true)
            {
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };

            for (int i = 0; i < stepNum; i++)
            {
                for (int j = 0; j < stepNum; j++)
                {
                    texture.SetPixel(i, j, Color.black);
                }
            }

            Vector2 graphTextureTexelSize = new Vector2(1f / stepNum, 1f / heightNum);
            float maxValue = 1f;
            float minValue = 0f;
            float[] values = new float[stepNum];
            for (int i = 0; i < stepNum; i++)
            {
                float time = graphTextureTexelSize.x * i;
                time += graphTextureTexelSize.x * 0.5f;
                float value = curve.Evaluate(time);
                if(value > maxValue) maxValue = value;
                if(value < minValue) minValue = value;
                values[i] = value;
            }
            var hRatio = heightNum / (maxValue - minValue);     // テクスチャの高さとカーブの高さの比率
            var yZero= Mathf.FloorToInt(-minValue * hRatio);    // Y0
            var yOne = yZero + Mathf.FloorToInt(hRatio);         // y1
            
            for (int i = 0; i < stepNum; i++)
            {
                var v = values[i] - minValue;
                var y = Mathf.FloorToInt(v * hRatio); // (int)(values[i] * hpow)
                if (yZero != y && yZero < heightNum && yZero >= 0)
                {
                    texture.SetPixel(i, yZero, Color.gray);
                }

                if (yOne != y && yOne < heightNum && yOne >= 0)
                {
                    texture.SetPixel(i, yOne, Color.gray);
                }

                texture.SetPixel(i, y, Color.red);
            }
            texture.Apply();
            return texture;
        }
    }
}