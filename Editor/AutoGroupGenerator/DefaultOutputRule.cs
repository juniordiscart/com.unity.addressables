using UnityEngine;
using UnityEditor.AddressableAssets.Settings;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Output rule that accepts every group layout unchanged.
    /// </summary>
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(DefaultOutputRule))]
    [AddressablesHelpURL("groups-auto-group-generator-reference.html")]
    public class DefaultOutputRule : OutputRule
    {
        #region Methods
        /// <inheritdoc />
        protected override bool DoesMatchSelectionCriteria(GroupLayout groupLayout)
        {
            return true;
        }
        #endregion
    }
}
