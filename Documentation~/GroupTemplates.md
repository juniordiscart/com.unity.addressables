---
uid: group-templates
---

# Create a group template

A group template defines which types of schema objects Unity creates for a new group. The Addressables system includes the Packed Assets template, which includes all the settings needed to build and load Addressables using the default build scripts.

If you want to create your own build scripts or utilities that need additional settings, you can define these settings in your own schema objects and create your own group templates. The following instructions describe how to do this:

1. Navigate to the desired location in your Assets folder using the Project panel.
2. Create a Blank Group Template (menu: **Assets** &gt; **Addressables** &gt; **Group Templates** &gt; **Blank Group Templates**).
3. Assign a name to the template.
4. In the Inspector window, add a description, if desired.
5. Click the **Add Schema** button and choose from the list of schemas.

Repeat these steps to add as many new schemas as needed.

> [!NOTE]
> If you use the default build script, a group must use the __Content Packing & Loading__ schema. If you use content update builds, a group must include the __Content Update Restrictions__ schema. Refer to [Builds](xref:addressables-builds) for more information.
