using System;
using System.Collections.Generic;
using System.Linq;
using RosettaUI.Builder;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace RosettaUI.UIToolkit
{
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
    
    public class AnimationCurvePicker : VisualElement
    {
        #region static interface

        private static ModalWindow _window;
        private static AnimationCurvePicker _animationCurvePickerInstance;

        #endregion

        public event Action<AnimationCurve> onCurveChanged;
        
        private AnimationCurve PreviewCurve
        {
            get => _curve;
            set
            {
                _curve = value;
                _keyframes = _curve.keys;
                _customKeyFrames = _curve.keys.Select(key => (CustomKeyFrame)key).ToArray();
                if (_animationCurvePreview != null)
                {
                    Texture2D texture = AnimationCurvePickerHelper.GenerateAnimationCurveTexture(value,
                        _animationCurvePreview.style.backgroundImage.value.texture);
                    _animationCurvePreview.style.backgroundImage = texture;
                }
            }
        }


        public static void Show(Vector2 position, VisualElement target, AnimationCurve initialCurve,
            Action<AnimationCurve> onCurveChanged)
        {
            if(_window == null)
            {
                _window = new ModalWindow();
                _animationCurvePickerInstance = new AnimationCurvePicker(initialCurve);


                _window.Add(_animationCurvePickerInstance);
                
                _window.RegisterCallback<NavigationSubmitEvent>(_ => _window.Hide());
                _window.RegisterCallback<NavigationCancelEvent>(_ =>
                {
                    onCurveChanged?.Invoke(initialCurve);
                    _window.Hide();
                });
            }
            
            _window.Show(position, target);


            // はみ出し抑制
            if(!float.IsNaN(_window.resolvedStyle.width) && !float.IsNaN(_window.resolvedStyle.height))
            {
                VisualElementExtension.CheckOutOfScreen(position, _window);
            }
            
            // Show()前はPanelが設定されていないのでコールバック系はShow()後
            _animationCurvePickerInstance.PreviewCurve = initialCurve;
            _animationCurvePickerInstance.onCurveChanged += onCurveChanged;
            _animationCurvePickerInstance.RegisterCallback<DetachFromPanelEvent>(OnDetach);

            void OnDetach(DetachFromPanelEvent _)
            {
                _animationCurvePickerInstance.onCurveChanged -= onCurveChanged;
                _animationCurvePickerInstance.UnregisterCallback<DetachFromPanelEvent>(OnDetach);

                target?.Focus();
            }
        }

        private Keyframe[] _keyframes;
        private CustomKeyFrame[] _customKeyFrames;
        
        private AnimationCurve _curve;
        
        private VisualElement _animationCurvePreview;

        private Texture2D _previeTexture;
        
        private AnimationCurvePicker(AnimationCurve initialCurve)
        {
            _previeTexture = new Texture2D(256, 32, TextureFormat.RGB24,false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            //_animationCurvePreview = UIToolkitBuilder.Build(UI.Image(temp).SetHeight(100f).SetWidth(800f));
            // PreviewCurve = initialCurve;
            
            _curve = initialCurve;
            _customKeyFrames = initialCurve.keys.Select(key => (CustomKeyFrame)key).ToArray();
            _keyframes = initialCurve.keys;
            var win = UIToolkitBuilder.Build(
                UI.Page(
                    UI.Image(_previeTexture).SetHeight(100f).SetWidth(800f),
                    UI.Field("KeyFrames",
                        () => _customKeyFrames,
                        val =>
                        {
                            _customKeyFrames = val;
                            _keyframes = _customKeyFrames
                                .Select(k => (Keyframe) k).ToArray();
                            _curve.keys = _keyframes;
                            _previeTexture = AnimationCurvePickerHelper.GenerateAnimationCurveTexture(_curve,
                                _previeTexture);
                            if (_animationCurvePreview != null)
                            {
                                _animationCurvePreview.style.backgroundImage = _previeTexture;
                            }

                            OnAnimationCurveChanged();
                        }))
            );
            
            this.Add(win);
            var t = this.Q<VisualElement>(null, "unity-image");
            
            if(t != null)
            {
                _animationCurvePreview = t;
                _animationCurvePreview.style.backgroundImage = _previeTexture;
            }
            
            PreviewCurve = initialCurve;
            this.ScheduleToUseResolvedLayoutBeforeRendering(() =>
            {
                // はみ出し抑制
                VisualElementExtension.CheckOutOfScreen(_window.Position, _window);
            });
        }

        void UpdateAnimationCurve()
        {
            PreviewCurve = new AnimationCurve(_keyframes);
        }
        
        void OnAnimationCurveChanged()
        {
            UpdateAnimationCurve();
            onCurveChanged?.Invoke(PreviewCurve);
        }

    }
    
}