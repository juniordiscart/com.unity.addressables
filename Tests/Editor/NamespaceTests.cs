using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    /*
     * This test exists to verify that all classes are correctly specified within a Unity namespace as developer IDEs
     * like to put new files into package folder namespaces like Editor.Build. If this test is failing for
     * you make sure it is in a Unity specific namespace.
     */
    public class NamespaceTests
    {
        public static Type[] defaultNamespaceTypes = new Type[]
        {
            typeof(AddressablesPlayerBuildProcessor),
            typeof(RevertUnchangedAssetsToPreviousAssetState),
        };

        [Test]
        public void AllEditorClassesAreInCorrectNamespace()
        {
            string assemblyName = "Unity.Addressables.Editor";

            // Specify the root namespace you want to check
            string rootNamespace = "UnityEditor.AddressableAssets";

            // Load the assembly
            Assembly assembly = Assembly.Load(assemblyName);

            // Get all types in the assembly
            Type[] types = assembly.GetTypes();

            // Filter for only classes (excluding interfaces, enums, etc.)
            var classes = types.Where(t => t.IsClass && t.IsPublic);

            foreach (var classType in classes)
            {
                if (defaultNamespaceTypes.Contains(classType))
                {
                    Assert.IsNull(classType.Namespace,
                        $"Class {classType.FullName} is not in the default namespace as expected.");
                    continue;
                }

                // Check if the class is in the correct namespace
                Assert.IsTrue(IsInNamespaceOrChildren(classType, rootNamespace),
                    $"Class {classType.FullName} is in {classType.Namespace}  which is not in the {rootNamespace} namespace or its children.");
            }
        }


        private bool IsInNamespaceOrChildren(Type type, string rootNamespace)
        {
            string typeNamespace = type.Namespace;

            // Check if the type's namespace starts with the root namespace
            return typeNamespace != null && typeNamespace.StartsWith(rootNamespace);
        }

    }
}