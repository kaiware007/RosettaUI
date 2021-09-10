﻿using RosettaUI.Reactive;

namespace RosettaUI
{
    /// <summary>
    /// 値を持ち外部と同期するElement
    /// </summary>
    public abstract class ReadOnlyValueElement<T> : Element
    {
        #region For Builder

        public readonly ReactiveProperty<T> valueRx;

        #endregion

        readonly IGetter<T> getter;


        public T value => valueRx.Value;
        
        public bool IsConst => getter.IsConst;


        public ReadOnlyValueElement(IGetter<T> getter)
        {
            this.getter = getter ?? new ConstGetter<T>(default);
            valueRx = new ReactiveProperty<T>(getter.Get());
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();

            if (!IsConst)
            {
                valueRx.Value = getter.Get();
            }
        }
    }
}