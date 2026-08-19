using System.Collections.Generic;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
	public interface IMultipleCatalogsBuilder
	{
		List<ExternalCatalogConfig> ExternalCatalogs
		{
			get; set;
		}
	}
}
