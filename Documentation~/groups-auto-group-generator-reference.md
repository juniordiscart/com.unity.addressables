# Auto Group Generator settings reference

When you use the **[Auto Group Generator](groups-auto-group-generator.md)** to automatically generate Addressable groups, by default it creates and uses the following settings files in the `AddressablesAssetData` folder of your project:

* [Auto Group Generator settings](#auto-group-generator-settings) (`DefaultAutoGroupGeneratorSettings`).
* [Asset Selection Input Rule](#input-rules) (`DefaultInputRule`).
* [Default Output Rule](#output-rules) (`DefaultOutputRule`).

You can rename these files, or create your own settings files.

## Auto Group Generator settings

When you select an Auto Group Generator settings file, the following settings are available in the **Inspector** window.

### Rules

Set the number of input and output rules the settings file uses. Input rules define which assets the **Auto Group Generator** automatically creates groups for. Output rules specify how the groups are created.

You can create additional custom rules, and Unity executes the rules sequentially. Use the **Add (+)** button to add custom rules to the list, and click and drag the handle (=) to change the order of the rules.

### Cleanup

Control how Unity discards any unused groups.

|**Setting**|**Description**|
|---|---|
|**Remove Unnecessary Entries**|Removes existing manual addressable entries that are no longer needed.|
|**Remove Empty Groups**|Deletes any empty groups.|
|**Remove Addressable Scenes From Build Pipeline**|Removes any scenes listed in the [Input rules](#input-rules) from the [Scene List](xref:um-build-profile-scene-list), which ensures that those scenes are only loaded through the Addressables system.|
|**Sort Addressable Groups**| Sorts all generated Addressable groups alphabetically by group name once processing is complete.|

### Process

Control how Unity processes groups.

|**Setting**|**Description**|
|---|---|
|**Scan For Unsupported Files**|Verifies that all assets are valid before starting the group generation process. This prevents corrupted files or assets with HideFlags from being added as Addressable entries. However, this can be a slow process because Unity has to load each asset to validate it.|
|**Last Processing Step**|Choose the point at which Auto Group Generator finishes its execution. The tool runs all steps up to and including the selected step and then skips any that comes after. Choose from the following options:<br/><ul><li>**None**</li><li>**Input Assets**: Retrieves the list of input assets defined in the [input rules](#input-rules).</li><li>**Generate Dependency Graph**: Scans all project assets to build a dependency graph, which is used in later steps to identify assets shared across different dependency hierarchies.</li><li>**Generate Sub Graphs**: Locates the dependencies of assets that are always loaded together. These sub graphs form the basis for group generation. </li><li>**Generate Group Layout**: Drafts an initial set of addressable groups based on the sub graphs.</li><li>**Generate Addressables Groups**: Creates addressable entries for assets that aren't addressable and places them in the appropriate groups. If an asset is already addressable but automatically assigned to a different group, the asset is moved to the new group without changing its address or labels.</li><li>**Cleanup**: Runs optional cleanup operations, such as removing addressable entries from the Build Profile or deleting empty addressable groups.</li><li>**All**: Runs all processing steps.</li></ul>|

### Reports

Generate and manage reporting.

|**Setting**|**Description**|
|---|---|
|**Process Report**|Generates a JSON file after the selected processing step which contains a report of the group generation process. The reports are saved in your project's `Application.persistentDataPath`.|
|**Log Level**|Select the amount and type of information logged to the Console during execution.|

## Input rules

When you select an Asset Selection Input Rule file, the following settings are available in the **Inspector** window.

### Selected Assets

Use the **Add Asset** selector to manually add individual objects to the input rule.

### JSON Input Lists

Add a `.json` [text asset](xref:um-class-text-asset) to add an array of asset paths to the input rule.

|**Setting**|**Description**|
|---|---|
|**Add JSON (TextAsset)**|Choose the JSON file to use.|
|**Include Current Addressables**|Automatically collects all assets marked as Addressable and treats them as if they're in the **Selected Assets** list. This is useful if you have groups that you've manually created, and you don't want to automatically remove any existing Addressable entries. However, Unity might move the existing entries into new automatically generated groups.|

### Add Scenes in Build Profile

Select this option to add any scenes in the Scene List to the **Selected Assets** list.

## Output rules

When you select a Default Output Rule file, you can use the **Inspector** to select the [group template](GroupTemplates.md) used.

## Additional resources

* [Automatically generate groups](groups-auto-group-generator.md)
* [Define group settings](GroupSchemas.md)
* [Create a group template](GroupTemplates.md)