using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
#if UNITY_6000_5_OR_NEWER
using UnityEngine.Assemblies;
#endif

namespace UnityEngine.ResourceManagement.Util
{
    /// <summary>
    /// Cross-runtime type name normalization and resolution.
    ///
    /// Why this exists: catalogs are written in the Editor (Mono today, CoreCLR later) and
    /// read by players running Mono / IL2CPP / CoreCLR. Embedding fully versioned assembly
    /// identities (mscorlib vs System.Private.CoreLib, plus Version/Culture/PublicKeyToken)
    /// makes catalogs runtime-specific and breaks under UnityLinker stripping when type
    /// forwarding facades are removed.
    ///
    /// Writers normalize types via <see cref="NormalizeTypeName"/> + <see cref="GetSimpleAssemblyName"/>;
    /// readers resolve via <see cref="Resolve"/>. The corelib assembly is written as null —
    /// both top-level and for generic arguments — so unqualified names fall back to
    /// <c>Type.GetType</c>'s default lookup, which finds core types on every runtime.
    ///
    /// Known limitation: the corelib fallback only covers types that live in corelib on the
    /// reading runtime. BCL types that moved assemblies between runtimes (e.g. System.Uri:
    /// 'System' on Mono, 'System.Private.Uri' on CoreCLR) resolve only if an assembly with
    /// the recorded simple name survives in the player — facades do not survive UnityLinker
    /// stripping at Minimal or higher. Accepted for now: catalog types are almost always
    /// Unity object or provider types, which live in stable assemblies.
    /// </summary>
    internal static class TypeNameResolver
    {
        static readonly Assembly s_CorelibAssembly = typeof(object).Assembly;

        static readonly Dictionary<string, Assembly> s_SimpleNameToAssembly = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        static readonly Dictionary<long, Type> s_ResolveCache = new Dictionary<long, Type>();
        static readonly HashSet<string> s_WarnedCollisions = new HashSet<string>(StringComparer.Ordinal);
        static readonly object s_Lock = new object();
        static bool s_Initialized;

        /// <summary>Warms up the resolver's loaded-assembly cache. Call once, early.</summary>
        public static void Initialize()
        {
            lock (s_Lock)
            {
                if (s_Initialized)
                    return;
                s_Initialized = true;
                foreach (var asm in GetLoadedAssemblies())
                    RegisterAssembly(asm);
                AppDomain.CurrentDomain.AssemblyLoad += (_, args) => RegisterAssembly(args.LoadedAssembly);
            }
        }

        static void RegisterAssembly(Assembly asm)
        {
            if (asm == null || asm.IsDynamic)
                return;
            string name;
            try { name = asm.GetName().Name; }
            catch { return; }
            if (string.IsNullOrEmpty(name))
                return;
            lock (s_Lock)
            {
                if (s_SimpleNameToAssembly.TryGetValue(name, out var existing))
                {
                    if (!ReferenceEquals(existing, asm) && s_WarnedCollisions.Add(name))
                        Debug.LogWarning($"Multiple loaded assemblies share the simple name '{name}'. Type resolution will use the first registered. Avoid asmdef name collisions.");
                    return;
                }
                s_SimpleNameToAssembly[name] = asm;
            }
        }

        /// <summary>
        /// Returns the simple assembly name to embed for a type, or null for the core runtime
        /// assembly (so the reader can use <c>Type.GetType(typeName)</c>).
        /// </summary>
        public static string GetSimpleAssemblyName(Type t)
        {
            if (t == null || t.Assembly == s_CorelibAssembly)
                return null;
            return t.Assembly.GetName().Name;
        }

        /// <summary>
        /// Returns a type name where every embedded assembly identity (in generic argument
        /// AQNs and array element types) has been reduced to the simple name. Non-generic,
        /// non-array types pass through to <c>t.FullName</c> unchanged.
        /// </summary>
        public static string NormalizeTypeName(Type t)
        {
            if (t == null)
                return null;
            if (t.IsArray)
            {
                var elemName = NormalizeTypeName(t.GetElementType());
                int rank = t.GetArrayRank();
                if (rank == 1)
                    return elemName + "[]";
                return elemName + "[" + new string(',', rank - 1) + "]";
            }
            if (t.IsByRef)
                return NormalizeTypeName(t.GetElementType()) + "&";
            if (t.IsPointer)
                return NormalizeTypeName(t.GetElementType()) + "*";
            if (!t.IsGenericType || t.IsGenericTypeDefinition)
                return t.FullName;

            var def = t.GetGenericTypeDefinition();
            var args = t.GetGenericArguments();
            var sb = new StringBuilder(def.FullName);
            sb.Append('[');
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append('[');
                sb.Append(NormalizeTypeName(args[i]));
                var innerAsm = GetSimpleAssemblyName(args[i]);
                if (!string.IsNullOrEmpty(innerAsm))
                {
                    sb.Append(", ");
                    sb.Append(innerAsm);
                }
                sb.Append(']');
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Resolves a (possibly null assemblyName, typeName) pair into a Type, using a
        /// runtime-tolerant assembly resolver that matches by simple name and falls back
        /// to the core runtime assembly. Returns null if no match.
        /// </summary>
        public static Type Resolve(string assemblyName, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
            Initialize();

            long key = ((long)(assemblyName == null ? 0 : assemblyName.GetHashCode()) << 32) ^ (uint)typeName.GetHashCode();
            lock (s_Lock)
            {
                if (s_ResolveCache.TryGetValue(key, out var cached))
                    return cached;
            }

            Type resolved = null;
            try
            {
                if (string.IsNullOrEmpty(assemblyName))
                {
                    // The callbacks are not redundant here: generic arguments can still embed assembly names.
                    resolved = Type.GetType(typeName, ResolveAssembly, ResolveType, throwOnError: false);
                }
                else
                {
                    resolved = Type.GetType(typeName + ", " + assemblyName, ResolveAssembly, ResolveType, throwOnError: false);
                    if (resolved == null)
                        resolved = Type.GetType(typeName, throwOnError: false);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is FileNotFoundException))
                    Debug.LogException(ex);
            }

            lock (s_Lock)
            {
                s_ResolveCache[key] = resolved;
            }
            return resolved;
        }

        static Assembly ResolveAssembly(AssemblyName name)
        {
            var simple = name.Name;
            if (string.IsNullOrEmpty(simple))
                return s_CorelibAssembly;
            lock (s_Lock)
            {
                if (s_SimpleNameToAssembly.TryGetValue(simple, out var asm))
                    return asm;
            }
            try
            {
                var loaded = Assembly.Load(new AssemblyName(simple));
                if (loaded != null)
                    RegisterAssembly(loaded);
                return loaded ?? s_CorelibAssembly;
            }
            catch (FileNotFoundException)
            {
                return s_CorelibAssembly;
            }
            catch (Exception)
            {
                return s_CorelibAssembly;
            }
        }

        static Type ResolveType(Assembly asm, string typeName, bool ignoreCase)
        {
            if (asm != null)
            {
                var t = asm.GetType(typeName, throwOnError: false, ignoreCase: ignoreCase);
                if (t != null)
                    return t;
            }
            return Type.GetType(typeName, throwOnError: false, ignoreCase: ignoreCase);
        }

        static IEnumerable<Assembly> GetLoadedAssemblies()
        {
#if UNITY_6000_5_OR_NEWER
            return CurrentAssemblies.GetLoadedAssemblies();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
    }
}
