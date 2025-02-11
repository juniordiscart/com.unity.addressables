namespace AddressableAssets.DocExampleCode
{
    using UnityEditor;
    using UnityEngine.AddressableAssets;

    internal class UsingAssetRefSpriteValidateAsset
    {
        #region SAMPLE
        public bool IsPathValid()
        {
            string guid = AssetDatabase.AssetPathToGUID("Assets/Sprites/oldSprite.png");
            AssetReferenceSprite assetRef = new AssetReferenceSprite(guid);

            return assetRef.ValidateAsset("Assets/Sprites/newSprite.png");
        }
        #endregion
    }
}
