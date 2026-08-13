using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Output rule that renames group layouts using simple heuristics.
    /// </summary>
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(ImprovedNamesOutputRule))]
    public class ImprovedNamesOutputRule : OutputRule
    {
        #region Methods
        /// <inheritdoc />
        protected override bool DoesMatchSelectionCriteria(GroupLayout groupLayout)
        {
            return true;
        }

        /// <summary>
        /// Renames group layouts based on heuristics derived from their contents.
        /// </summary>
        public override void Refine()
        {
            HashSet<string> inputAssets = m_DataContainer.InputAssets;

            foreach (var groupLayout in m_Selection)
            {
                var currentName = groupLayout.Name;


                var groupLayoutAssets = groupLayout.Nodes.Select(node => node.AssetPath).ToHashSet();

                var intersection = new HashSet<string>(inputAssets);
                intersection.IntersectWith(groupLayoutAssets);

                if (intersection.Count == 1)
                {
                    Rename(groupLayout, $"Hierarchy of {Path.GetFileName(intersection.ToList()[0])}_{currentName}");

                    continue;
                }


                if (groupLayout.Sources.Count == 1)
                {
                    Rename(groupLayout, $"Dependencies of {groupLayout.Sources.ToList()[0].FileName}_{currentName}");

                    continue;
                }


                if (groupLayout.Nodes.Count == 1)
                {
                    Rename(groupLayout, $"{groupLayout.Nodes.ToList()[0].FileName}_{currentName}");

                    continue;
                }


                if (groupLayout.IsShared)
                {
                    var newName = $"Shared_{currentName}";

                    Rename(groupLayout, newName);

                    continue;
                }
            }
        }
        #endregion
    }
}
