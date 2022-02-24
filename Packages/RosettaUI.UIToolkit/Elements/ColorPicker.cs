using System;
using System.Linq.Expressions;
using RosettaUI.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace RosettaUI.UIToolkit
{
    public class ColorPicker : VisualElement
    {
        #region static interface

        private static ModalWindow _window;
        private static ColorPicker _colorPickerInstance;
        public static int TextDigit { get; set; } = 3;

        public static void Show(Vector2 position, VisualElement target, Color initialColor,
            Action<Color> onColorChanged)
        {
            if (_window == null)
            {
                _window = new ModalWindow();
                _colorPickerInstance = new ColorPicker();
                _window.Add(_colorPickerInstance);
            }

            _window.Show(position, target);

            // Show()前はPanelが設定されていないのでコールバック系はShow()
            _colorPickerInstance.PrevColor = initialColor;
            _colorPickerInstance.onColorChanged += onColorChanged;
            _colorPickerInstance.RegisterCallback<DetachFromPanelEvent>(OnDetach);


            void OnDetach(DetachFromPanelEvent _)
            {
                _colorPickerInstance.onColorChanged -= onColorChanged;
                _colorPickerInstance.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            }
        }

        #endregion

        #region static members

        private static VisualTreeAsset _visualTreeAsset;
        private static Texture2D _svTexture;

        #endregion

        public event Action<Color> onColorChanged;

        private VisualElement _previewPrev;
        private VisualElement _previewCurr;
        private VisualElement _hueHandler;
        private VisualElement _hueCursor;
        private VisualElement _svHandler;
        private VisualElement _svCursor;


        private readonly SliderSet _sliderSet;

        private float _hueCircleThickness;

        private Vector3 _hsv;
        private float _alpha;

        private Color PrevColor
        {
            get => _previewPrev.style.backgroundColor.value;
            set
            {
                _previewPrev.style.backgroundColor = value;
                Color = value;
            }
        }

        public Color Color
        {
            get
            {
                var hsv = Hsv;
                var rgb = Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
                return new Color(rgb.r, rgb.g, rgb.b, _alpha);
            }
            private set
            {
                Color.RGBToHSV(value, out var h, out var s, out var v);
                _hsv = new Vector3(h, s, v);
                _alpha = value.a;

                OnColorChanged();
            }
        }


        private Vector3 Hsv
        {
            get => _hsv;
            set
            {
                if (_hsv == value) return;

                _hsv = value;
                OnColorChanged();
            }
        }

        private float Alpha
        {
            get => _alpha;
            set
            {
                if (_alpha == value) return;
                _alpha = value;
                OnColorChanged();
            }
        }

        private ColorPicker()
        {
            _visualTreeAsset ??= Resources.Load<VisualTreeAsset>("RosettaUI_ColorPicker");
            _visualTreeAsset.CloneTree(this);

            InitPreview();
            InitHsvHandlers();
            _sliderSet = new SliderSet(this);
        }


        private void InitHsvHandlers()
        {
            _hueHandler = this.Q("handler-h");
            _hueCursor = _hueHandler.Q("circle");
            _svHandler = this.Q("handler-sv");
            _svCursor = _svHandler.Q("circle");

            PointerDrag.RegisterCallback(_hueHandler, OnPointerMoveOnPanel_Hue, CheckPointIsValid_Hue, true);
            PointerDrag.RegisterCallback(_svHandler, OnPointerMoveOnPanel_SV, CheckPointerIsValid_SV, true);

            
            schedule.Execute(() =>
            {
                // Hue Circle
                var hueCircleSize = _hueHandler.resolvedStyle.width;
                (_hueHandler.style.backgroundImage, _hueCircleThickness) = ColorPickerHelper.GetHueCircleTextureAndThickness(hueCircleSize);

                var cursorSize = _hueCircleThickness * 1.1f; // 画像内に影がついている関係で、画像のサイズ＝カーソルの直径ではない。ので見た目が合うように少し拡大しておく
                var cursorStyle = _hueCursor.style;
                cursorStyle.width = cursorSize;
                cursorStyle.height = cursorSize;
                cursorStyle.marginLeft = cursorSize * -0.5f;
                cursorStyle.marginTop = cursorSize * -0.5f;
                
                // SV Circle
                if (_svTexture == null)
                {
                    var size = ColorPickerHelper.defaultSvTextureSize;
                    _svTexture = ColorPickerHelper.CreateTexture(size.x, size.y);
                }

                _svHandler.style.backgroundImage = _svTexture;
                
                
                // 表示更新
                OnColorChanged();
            });
        }

        private void InitPreview()
        {
            var preview = this.Q("preview");
            _previewPrev = this.Q("preview-prev");
            _previewCurr = this.Q("preview-curr");

            preview.style.backgroundImage = ColorPickerHelper.CheckerBoardTexture;
            _previewPrev.RegisterCallback<PointerDownEvent>((_) => Color = PrevColor);
        }


        bool CheckPointIsValid_Hue(PointerDownEvent evt)
        {
            var pos = evt.localPosition;
            
            var circleSize =_hueHandler.resolvedStyle.width;
            var center = Vector2.one * circleSize * 0.5f;
            var maxRadius = circleSize * 0.5f;
            
            var distance = Vector2.Distance(pos, center);
            
            return (maxRadius - _hueCircleThickness <= distance && distance <= maxRadius);
        }
        
        private void OnPointerMoveOnPanel_Hue(PointerMoveEvent evt)
        {
            var localPos = _hueHandler.WorldToLocal(evt.position);

            var hsv = Hsv;
            hsv.x = LocalPosToHue(localPos);
            Hsv = hsv;
            
            evt.StopPropagation();
        }

        bool CheckPointerIsValid_SV(PointerDownEvent evt)
        {
            var pos = evt.localPosition;

            var circleSize = _svHandler.resolvedStyle.width;
            var radius = circleSize * 0.5f;
            var center = Vector2.one * radius;
            
            var distance = Vector2.Distance(pos, center);

            return distance <= radius;
        }
        
        private void OnPointerMoveOnPanel_SV(PointerMoveEvent evt)
        {
            var localPos = _svHandler.WorldToLocal(evt.position);
            var radius = _svHandler.resolvedStyle.width * 0.5f;
            var posOnCircle = (localPos - Vector2.one * radius) / radius;
            if (posOnCircle.sqrMagnitude >= 1f)
            {
                posOnCircle.Normalize();
            }

            var posOnSquare = ColorPickerHelper.CircleToSquare(posOnCircle);
      
            var sv = (posOnSquare + Vector2.one) * 0.5f; // map: -1~1 > 0~1
            
            var hsv = Hsv;
            hsv.y = Mathf.Clamp01(sv.x);
            hsv.z = Mathf.Clamp01(1f - sv.y);　// 数値表示が枠に収まるように端数を丸める
            Hsv = hsv;

            evt.StopPropagation();
        }

        void OnColorChanged()
        {
            _previewCurr.style.backgroundColor = Color;

            var hsv = Hsv;
            if (_svTexture != null)
            {
                ColorPickerHelper.UpdateSvCircleTexture(_svTexture, hsv.x);
            }

            UpdateSvCursor(hsv.y, hsv.z);
            UpdateHueCursor(hsv.x);

            _sliderSet.OnColorChanged();

            onColorChanged?.Invoke(Color);
        }
       

        void UpdateHueCursor(float h)
        {
            var pos = HueToLocalPos(h);
            
            var s = _hueCursor.style;
            s.top = pos.y;
            s.left = pos.x;
        }


        Vector2 HueToLocalPos(float h)
        {
            var circleSize =_hueHandler.resolvedStyle.width; 
            var radius = (circleSize * 0.5f) - (_hueCircleThickness * 0.5f);
            
            var rad = h * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * radius
                      + (Vector2.one * circleSize * 0.5f);
        }

        float LocalPosToHue(Vector2 pos)
        {
            var circleSize =_hueHandler.resolvedStyle.width;

            var p = pos - (Vector2.one * circleSize * 0.5f);

            var f = Mathf.Atan2(-p.y, p.x) / (Mathf.PI * 2f);
            if (f < 0f) f += 1f;
            return f;
        }


        void UpdateSvCursor(float s, float v)
        {
            var xy = new Vector2(s, v) * 2f - Vector2.one; //map: 0~1 > -1~1

            var uv = ColorPickerHelper.SquareToCircle(xy);

            /*
            // カーソルを円に収めるために少しだけ縮める
            var cursorSize = _svCursor.resolvedStyle.width;
            var parentSize = _svHandler.resolvedStyle.width;
            uv *= (1f - cursorSize / parentSize);
            */ 
            
            var circleXY = (uv + Vector2.one) * 0.5f; // map: -1~1 > 0~1

            var svCircleStyle = _svCursor.style;
            svCircleStyle.left = Length.Percent(circleXY.x * 100f);
            svCircleStyle.top = Length.Percent((1f - circleXY.y) * 100f);
        }

        class SliderSet
        {
            private static readonly string USSClassNameTextureSlider = "rosettaui-texture-slider";
            
            static Vector3 Round(Vector3 value, int digit)
            {
                var scale = Mathf.Pow(10f, digit);
                var v = value * scale;

                return new Vector3(
                    Mathf.Round(v.x),
                    Mathf.Round(v.y),
                    Mathf.Round(v.z)
                ) / scale;
            }

            #region Type Define

            enum SliderType
            {
                Hsv,
                Rgb
            }

            #endregion

            private bool _isInitialized;

            private readonly ColorPicker _colorPicker;

            private readonly Button _sliderTypeButton;
            private readonly Slider _slider0;
            private readonly Slider _slider1;
            private readonly Slider _slider2;
            private readonly Slider _slider3;

            private Texture2D _sliderTexture0;
            private Texture2D _sliderTexture1;
            private Texture2D _sliderTexture2;
            
            
            private SliderType _sliderType;

            private Color Color
            {
                get => _colorPicker.Color;
                set => _colorPicker.Color = value;
            }

            private float Alpha
            {
                get => _colorPicker.Alpha;
                set => _colorPicker.Alpha = value;
            }

            private Vector3 Hsv
            {
                get => _colorPicker.Hsv;
                set => _colorPicker.Hsv = value;
            }

            public SliderSet(ColorPicker colorPicker)
            {
                _colorPicker = colorPicker;

                _sliderTypeButton = colorPicker.Q<Button>("slider-type-button");
                _sliderTypeButton.clicked += ToggleSliderType;

                _slider0 = InitSlider("slider0", 0, true);
                _slider1 = InitSlider("slider1", 1, true);
                _slider2 = InitSlider("slider2", 2, true);
                _slider3 = InitSlider("slider3", 3, false);

                _colorPicker.schedule.Execute(() =>
                {
                    _sliderTexture0 = InitSliderTexture(_slider0);
                    _sliderTexture1 = InitSliderTexture(_slider1);
                    _sliderTexture2 = InitSliderTexture(_slider2);

                    _isInitialized = true;
                    
                    SetSliderType(SliderType.Hsv);
                    UpdateView();
                });

                Slider InitSlider(string name, int idx, bool isTextureSlider)
                {
                    var slider = _colorPicker.Q<Slider>(name);
                    slider.highValue = 1f;
                    slider.RegisterValueChangedCallback((evt) => OnSliderValueChanged(evt.newValue, idx));
                    
                    if (isTextureSlider)
                    {
                        slider.AddToClassList(USSClassNameTextureSlider);
                    }
                    
                    return slider;
                    
                    
                    void OnSliderValueChanged(float value, int index)
                    {
                        if (index == 3)
                        {
                            Alpha = value;
                        }
                        else
                        {
                            switch (_sliderType)
                            {
                                case SliderType.Hsv:
                                    var hsv = Hsv;
                                    hsv[index] = Mathf.Clamp01(value);
                                    Hsv = hsv;
                                    break;

                                case SliderType.Rgb:
                                    var color = Color;
                                    color[index] = Mathf.Clamp01(value);
                                    Color = color;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                }
            }

            static Texture2D InitSliderTexture(VisualElement slider)
            {
                var tracker = slider.Q("unity-tracker");
                var tex = ColorPickerHelper.CreateTexture(
                    Mathf.CeilToInt(tracker.resolvedStyle.width),
                    1,
                    TextureFormat.RGB24);

                tracker.style.backgroundImage = tex;

                return tex;
            }

            public void OnColorChanged() => UpdateView();
 
            void UpdateView()
            {
                if (!_isInitialized) return;
                
                var vec3 = _sliderType switch
                {
                    SliderType.Hsv => Hsv,
                    SliderType.Rgb => (Vector3) (Vector4) Color,
                    _ => throw new ArgumentException()
                };

                vec3 = Round(vec3, TextDigit); // 数値表示が枠に収まるように端数を丸める

                _slider0.SetValueWithoutNotify(vec3.x);
                _slider1.SetValueWithoutNotify(vec3.y);
                _slider2.SetValueWithoutNotify(vec3.z);
                _slider3.SetValueWithoutNotify(Alpha);

                switch (_sliderType)
                {
                    case SliderType.Hsv:
                        ColorPickerHelper.UpdateSSliderTexture(_sliderTexture1, Hsv.x, Hsv.z);
                        ColorPickerHelper.UpdateVSliderTexture(_sliderTexture2, Hsv.x, Hsv.y);
                        break;
                        
                    case SliderType.Rgb:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            private void ToggleSliderType()
            {
                var newType = _sliderType == SliderType.Hsv ? SliderType.Rgb : SliderType.Hsv;
                SetSliderType(newType);
                UpdateView();
            }

            void SetSliderType(SliderType sliderType)
            {
                _sliderType = sliderType;

                var buttonText = "";
                switch (sliderType)
                {
                    case SliderType.Hsv:
                        buttonText = "HSV";
                        SetSliderLabels("H", "S", "V", "A");
                        ColorPickerHelper.WriteHueSliderTexture(_sliderTexture0);
                        break;

                    case SliderType.Rgb:
                        buttonText = "RGB";
                        SetSliderLabels("R", "G", "B", "A");
                        break;
                }

                _sliderTypeButton.text = buttonText;

                void SetSliderLabels(string label0, string label1, string label2, string label3)
                {
                    _slider0.label = label0;
                    _slider1.label = label1;
                    _slider2.label = label2;
                    _slider3.label = label3;
                }
            }
        }
    }
}
