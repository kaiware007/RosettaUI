using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosettaUI.Test
{
    [RequireComponent(typeof(RosettaUIRoot))]
    public class TestElementCreator : MonoBehaviour
    {
        #region Type Define
        
        private class MyClass : IElementCreator
        {
            public int intValue;
            public string stringValue;
            
            public Element CreateElement(LabelElement label)
            {
                return UI.Fold(label,
                    UI.Row(
                        UI.Field(() => intValue),
                        UI.Button("+", () => intValue++),
                        UI.Button("-", () => intValue--)
                    ),
                    UI.Field(() => stringValue)
                ).Open();
            }
        }

        private struct MyStruct : IElementCreator
        {
            public int intValue;


            public Element CreateElement(LabelElement label)
            {
                return UI.Fold(label,
                    UI.Row(
                        UI.FieldReadOnly("IntValue", GetIntValue)
                    )
                ).Open();
            }

            private int GetIntValue() => intValue;
        }

        #endregion


        private MyClass _myClass = new();
        private MyStruct _myStruct = new() { intValue = 1 };

        private List<MyClass> _myClassList = new()
        {
            new MyClass() { intValue = 1, stringValue = "1" },
            new MyClass() { intValue = 2, stringValue = "2" }
        };

        
        private void Start()
        {
            var root = GetComponent<RosettaUIRoot>();
            root.Build(CreateElement());
        }

        private Element CreateElement()
        {
            return UI.Window(
                UI.Page(
                    UI.Field(() => _myClass),
                    UI.Row(
                        UI.Button("Set null", () => _myClass = null),
                        UI.Button("Set new", () => _myClass = new MyClass())
                    ),
                    // TODO: need IElementCreator.CreateElement(LabelElement, IBinder, FieldOption)
                    // IElementCreator.CreateElement()だと参照の変更に対応できない
                    /*
                    UI.Field(() => _myStruct),
                    UI.Button("Set new", () => _myStruct.intValue++),
                    */
                    UI.List(() => _myClassList),
                    UI.Row(
                        UI.Button("Set null", () => _myClassList = null),
                        UI.Button("Set new", () => _myClassList = new List<MyClass>())
                    ),
                    UI.Row(
                        UI.Button("Set [0] null", () => _myClassList[0] = null),
                        UI.Button("Set [0] new", () => _myClassList[0] = new MyClass())
                    )
                )
            );
        }
    }
}