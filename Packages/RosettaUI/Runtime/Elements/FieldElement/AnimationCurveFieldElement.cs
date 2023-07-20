using UnityEngine;

namespace RosettaUI
{
    public class AnimationCurveFieldElement : FieldBaseElement<AnimationCurve>
    {
        public AnimationCurveFieldElement(LabelElement label, IBinder<AnimationCurve> binder) : base(label, binder) { }
    }
}