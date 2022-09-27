using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Interface.UI.Components
{
    class ArrowShortcutsContainer : VisualElement
    {
        MultiSelect m_MultiSelect;

        public ArrowShortcutsContainer()
        {
            focusable = true;

            RegisterCallback<GeometryChangedEvent>(_ => OnChildrenModified());
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnChildrenModified()
        {
            if (m_MultiSelect != null)
            {
                m_MultiSelect.OnSelectionChanged -= Focus;
            }
            m_MultiSelect = this.Q<MultiSelect>();
            if (m_MultiSelect != null)
            {
                m_MultiSelect.OnSelectionChanged += Focus;
            }
        }

        void OnKeyDown(KeyDownEvent keyDownEvent)
        {
            keyDownEvent.StopPropagation();

            var isSingleSelect = !(keyDownEvent.shiftKey || keyDownEvent.commandKey);
            var isSelectPrevious = keyDownEvent.keyCode == KeyCode.UpArrow;
            var isDirectionTowardsAnchor = IsDirectionTowardsAnchor(isSelectPrevious);
            var targetSelectable = GetTargetSelectable(keyDownEvent, isSingleSelect, isDirectionTowardsAnchor, isSelectPrevious);
            ApplyInputOnTarget(targetSelectable, isSingleSelect, isDirectionTowardsAnchor);
        }

        Selectable GetTargetSelectable(KeyDownEvent keyDownEvent, bool isSingleSelect, bool isDirectionTowardsAnchor, bool isSelectPrevious)
        {
            Selectable targetSelectable = null;
            switch (keyDownEvent.keyCode)
            {
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                    targetSelectable =
                        !isSingleSelect && isDirectionTowardsAnchor
                        ? m_MultiSelect.SelectionEnd :
                        m_MultiSelect.GetNextSelectable(
                            m_MultiSelect.SelectionStart, isSelectPrevious, true, true);
                    break;
                case KeyCode.LeftArrow:
                    if (!TrySetVisibility(false))
                    {
                        targetSelectable = m_MultiSelect.GetParentItem(m_MultiSelect.SelectionStart);
                    }
                    break;
                case KeyCode.RightArrow:
                    if (!TrySetVisibility(true))
                    {
                        targetSelectable =
                            m_MultiSelect.GetNextSelectable(m_MultiSelect.SelectionStart, false, false, true)
                            ?? m_MultiSelect.GetNextSelectable(m_MultiSelect.SelectionStart, false, true, true);
                    }
                    break;
            }

            return targetSelectable;
        }

        void ApplyInputOnTarget(Selectable targetSelectable, bool isSingleSelect, bool isDirectionTowardsAnchor)
        {
            if (targetSelectable != null)
            {
                if (isSingleSelect)
                {
                    m_MultiSelect.SetSelection(targetSelectable);
                }
                else
                {
                    if (isDirectionTowardsAnchor)
                        m_MultiSelect.RemoveSelection(targetSelectable);
                    else
                        m_MultiSelect.AddSelection(targetSelectable);
                }
            }
        }

        bool IsDirectionTowardsAnchor(bool selectPrevious)
        {
            if (m_MultiSelect.SelectionEnd == m_MultiSelect.SelectionStart)
                return false;

            var isBeforeTarget =
                m_MultiSelect.IsItemBeforeTarget(m_MultiSelect.SelectionEnd, m_MultiSelect.SelectionStart);
            return selectPrevious ? !isBeforeTarget : isBeforeTarget;
        }

        bool TrySetVisibility(bool targetVisibility)
        {
            if (m_MultiSelect.SelectionStart?.parent is Collapsible collapsible
                && collapsible.IsContentVisible != targetVisibility)
            {
                collapsible.SetVisibility(targetVisibility);
                return true;
            }

            return false;
        }

        new class UxmlFactory : UxmlFactory<ArrowShortcutsContainer> {}
    }
}
