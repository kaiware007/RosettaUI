using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace RosettaUI
{
    public  static partial class BinderToElement
    {
        public static Element CreateFieldElement(LabelElement label, IBinder binder, in FieldOption option)
        {
            var valueType = binder.ValueType;

            if (BinderHistory.IsCircularReference(binder))
            {
                return CreateCircularReferenceElement(label, valueType);
            }

            using var binderHistory = BinderHistory.GetScope(binder);
            var optionCaptured = option;
            
            if (!UICustomCreationScope.IsIn(valueType) && UICustom.GetElementCreationFunc(valueType) is { } creationFunc)
            {
                using var scope = new UICustomCreationScope(valueType);
                return UI.NullGuardIfNeed(label, binder, () => creationFunc(label, binder));
            }

            return binder switch
            {
                IBinder<int> ib => new IntFieldElement(label, ib, option),
                IBinder<uint> ib => new UIntFieldElement(label, ib, option),
                IBinder<float> ib => new FloatFieldElement(label, ib, option),
                IBinder<string> ib => new TextFieldElement(label, ib, option),
                IBinder<bool> ib => new ToggleElement(label, ib),
                IBinder<Color> ib => new ColorFieldElement(label, ib),
                IBinder<Gradient> ib => new GradientFieldElement(label, ib),
                IBinder<AnimationCurve> ib => CreateAnimationCurveElement(label, ib),
                _ when valueType.IsEnum => CreateEnumElement(label, binder),
                _ when TypeUtility.IsNullable(valueType) => CreateNullableFieldElement(label, binder, option),
                _ when typeof(IElementCreator).IsAssignableFrom(valueType) => CreateElementCreatorElement(label, binder),
                _ when ListBinder.IsListBinder(binder) => CreateListView(label, binder),

                _ => UI.NullGuardIfNeed(label, binder, () => CreateMemberFieldElement(label, binder, optionCaptured))
            };
        }

        private static Element CreateAnimationCurveElement(LabelElement label, IBinder<AnimationCurve> binder)
        {
            var instance = binder.Get();
             Keyframe[] keyframes = instance.keys;
                CustomKeyFrame[] customKeyFrames = keyframes.Select(k => (CustomKeyFrame) k).ToArray();
            
                Texture2D temp = new Texture2D(256, 32, TextureFormat.RGB24,false, true)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                
                static void CreateAnimationCurveTexture(ref Texture2D texture, int stepNum, int heightNum, AnimationCurve curve)
                {
                    if (texture == null)
                    {
                        texture = new Texture2D(stepNum, heightNum, TextureFormat.RGB24, false, true)
                        {
                            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                        };
                    }

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
                }
                
                CreateAnimationCurveTexture(ref temp, 256, 32, instance);

                
                return UI.Row(
                label.SetMinWidth(240f),
                UI.Space().SetWidth(10f),
                UI.Image(temp).SetHeight(50f).SetWidth(300f),
                UI.WindowLauncher("Edit Curve", 
                    UI.Window($"Edit Curve - {label.Value}",
                        UI.Image(temp).SetHeight(100f).SetWidth(800f),
                        UI.Field("KeyFrames",
                            () => customKeyFrames,
                            val =>
                            {
                                customKeyFrames = val;
                                keyframes = customKeyFrames.Select(k => (Keyframe) k).ToArray();
                                instance.keys = keyframes;
                                CreateAnimationCurveTexture(ref temp, 256, 32, instance);
                            })
                    )
                )
            );
        }
        
        [Serializable]
        public struct CustomKeyFrame
        {
            public float Time;
            public float Value;
            public float InOutTangent;

            public static implicit operator Keyframe(CustomKeyFrame value)
            {
                return new Keyframe(value.Time, value.Value, value.InOutTangent, value.InOutTangent);
            }

            public static implicit operator CustomKeyFrame(Keyframe value)
            {
                return new CustomKeyFrame() { Time = value.time, Value = value.value, InOutTangent = value.inTangent };
            }
        }
        
        private static Element CreateEnumElement(LabelElement label, IBinder binder)
        {
            var valueType = binder.ValueType;
            var enumToIdxBinder = EnumToIdxBinder.Create(binder);

            return new DropdownElement(label, enumToIdxBinder, Enum.GetNames(valueType));
        }
        
        private static Element CreateNullableFieldElement(LabelElement label, IBinder binder, FieldOption option)
        {
            var valueBinder = NullableToValueBinder.Create(binder);
            return UI.NullGuard(label, binder, () => CreateFieldElement(label, valueBinder, option));
        }

        private static Element CreateElementCreatorElement(LabelElement label, IBinder binder)
        {
            var lastObject = binder.GetObject();
            if (binder.ValueType.IsValueType)
            {
                var elementCreator = lastObject as IElementCreator;
                Assert.IsNotNull(elementCreator);
                return elementCreator?.CreateElement(label);
            }
            
            // ElementCreatorの場合、参照が変わったらUIを作り直す
            return UI.NullGuard(label, binder, () =>
                UI.DynamicElementOnTrigger
                (
                    rebuildIf: _ =>
                    {
                        var current = binder.GetObject();
                        var refChanged = !ReferenceEquals(lastObject, binder.GetObject());
                        lastObject = current;
                        return refChanged;
                    },
                    () =>
                    {
                        if (lastObject is IElementCreator elementCreator)
                        {
                            label?.DetachView();
                            return elementCreator.CreateElement(label);
                        }

                        Debug.LogWarning($"{binder.ValueType} is not {nameof(IElementCreator)}");
                        return null;
                    }
                )
            );
        }

        private static Element CreateListView(LabelElement label, IBinder binder)
        {
            var option = (binder is IPropertyOrFieldBinder pfBinder)
                ? new ListViewOption(
                    reorderable: TypeUtility.IsReorderable(pfBinder.ParentBinder.ValueType, pfBinder.PropertyOrFieldName)
                )
                : ListViewOption.Default;


            return UI.List(label, binder, null, option);
        }

        private static Element CreateMemberFieldElement(LabelElement label, IBinder binder, in FieldOption option)
        {
            var valueType = binder.ValueType;
            var optionCaptured = option;

            // UICustomCreationScopeをキャンセル
            // クラスのメンバーに同じクラスがある場合はUICustomを有効にする
            using var uiCustomScope = new UICustomCreationScope(null);

            var elements = TypeUtility.GetUITargetFieldNames(valueType).Select(fieldName =>
            {
                var fieldBinder = PropertyOrFieldBinder.Create(binder, fieldName);
                var fieldLabel = UICustom.ModifyPropertyOrFieldLabel(valueType, fieldName);

                var range = TypeUtility.GetRange(valueType, fieldName);
                if (range != null)
                {
                    var (minGetter, maxGetter) = RangeUtility.CreateGetterMinMax(range, fieldBinder.ValueType);
                    return UI.Slider(fieldLabel, fieldBinder, minGetter, maxGetter);
                }
               
                var field = UI.Field(fieldLabel, fieldBinder, optionCaptured);
                
                
                if (TypeUtility.IsMultiline(valueType, fieldName) && field is TextFieldElement textField)
                {
                    textField.IsMultiLine = true;
                }

                return field;
            });


            Element ret;
            if (TypeUtility.IsSingleLine(valueType))
                ret = new CompositeFieldElement(label, elements);
            else if (label != null)
                ret = UI.Fold(label, elements);
            else
                ret = UI.Column(elements);

            return ret;
        }
        
        private static Element CreateCircularReferenceElement(LabelElement label, Type type)
        {
            return new CompositeFieldElement(label,
                new[]
                {
                    new HelpBoxElement($"[{type}] Circular reference detected.", HelpBoxType.Error)
                }).SetInteractable(false);
        }
    }
}