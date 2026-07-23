using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GitDesktop.UI
{
    /// <summary>
    /// Helper class for creating searchable dropdown combos
    /// </summary>
    public class SearchableComboHelper
    {
        private string searchText = "";

        public bool BeginCombo(string label, string previewValue, string id = "")
        {
            if (ImGui.BeginCombo(id, previewValue, ImGuiComboFlags.HeightLargest))
            {
                return true;
            }

            return false;
        }

        public void EndCombo()
        {
            ImGui.EndCombo();
        }

        public void SearchInput()
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##SearchBranch", "Search branches...", ref searchText, 256);
        }

        public int SelectableList(string[] items, int currentSelectedIndex)
        {
            int selectedIndex = -1;

            if (string.IsNullOrEmpty(searchText))
            {
                // No filter - show all items
                for (int i = 0; i < items.Length; i++)
                {
                    bool isSelected = (i == currentSelectedIndex);
                    if (ImGui.Selectable(items[i], isSelected))
                    {
                        selectedIndex = i;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
            }
            else
            {
                // Filter items by search text
                string lowerSearch = searchText.ToLower();
                List<(int originalIndex, string name)> filteredItems = [];

                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].ToLower().Contains(lowerSearch))
                    {
                        filteredItems.Add((i, items[i]));
                    }
                }

                // Display filtered items
                for (int i = 0; i < filteredItems.Count; i++)
                {
                    int originalIndex = filteredItems[i].originalIndex;
                    string name = filteredItems[i].name;
                    bool isSelected = (originalIndex == currentSelectedIndex);

                    if (ImGui.Selectable(name, isSelected))
                    {
                        selectedIndex = originalIndex;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
            }

            return selectedIndex;
        }

        public void Reset()
        {
            searchText = "";
        }
    }
}
