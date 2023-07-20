using System;
using RosettaUI.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace RosettaUI.UIToolkit.UnityInternalAccess
{
    public class AnimationCurveField : BaseField<AnimationCurve>
    {
        public new static readonly string ussClassName = "rosettaui-animationcurve-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";
        
        protected AnimationCurveInput animationCurveInput => (AnimationCurveInput)visualInput;

        public event Action<Vector2, AnimationCurveField> showAnimationCurvePickerFunc;
        
        private bool _valueNull;

        readonly Background m_DefaultBackground = new Background();

        /// <summary>
        /// Constructor.
        /// </summary>
        public AnimationCurveField() : this(null) { }

        public AnimationCurveField(string label) : base(label, new AnimationCurveInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            
            visualInput.AddToClassList(inputUssClassName);
            visualInput.RegisterCallback<ClickEvent>(OnClickInput);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }
        
        void ShowAnimationCurvePicker(Vector2 position)
        {
            showAnimationCurvePickerFunc?.Invoke(position, this);
        }
        
         internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            UpdateAnimationCurveTexture();
        }

        void UpdateAnimationCurveTexture()
        {
            if (_valueNull || showMixedValue)
            {
                visualInput.style.backgroundImage = m_DefaultBackground;
            }
            else
            {
                Texture2D gradientTexture = AnimationCurvePickerHelper.GenerateAnimationCurveTexture(value, resolvedStyle.backgroundImage.texture);
                visualInput.style.backgroundImage = gradientTexture;

                IncrementVersion(VersionChangeType.Repaint); // since the Texture2D object can be reused, force dirty because the backgroundImage change will only trigger the Dirty if the Texture2D objects are different.
            }
        }
        
        private void OnClickInput(ClickEvent evt)
        {
            ShowAnimationCurvePicker(evt.position);
            
            evt.StopPropagation();
        }
        
        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            var mousePosition = Input.mousePosition;
            var position = new Vector2(
                mousePosition.x,
                Screen.height - mousePosition.y
            );

            var screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
            if (!screenRect.Contains(position))
            {
                position = worldBound.center;
            }
            
            ShowAnimationCurvePicker(position);
            
            evt.StopPropagation();
        }

        public override void SetValueWithoutNotify(AnimationCurve newValue)
        {
            base.SetValueWithoutNotify(newValue);
            
            _valueNull = newValue == null;
            if (newValue != null)
            {
                value.keys = newValue.keys;
            }
            else // restore the internal gradient to the default state.
            {
                value = AnimationCurve.Constant(0,1,1);
            }

            UpdateAnimationCurveTexture();
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                visualInput.style.backgroundImage = m_DefaultBackground;
                visualInput.Add(mixedValueLabel);
            }
            else
            {
                UpdateAnimationCurveTexture();
                mixedValueLabel.RemoveFromHierarchy();
            }
        }
        
        public class AnimationCurveInput : VisualElement
        {
            
        }
    }
}