using System;
using System.Linq;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using Il2CppInterop.Common.Extensions;
using Il2CppInterop.Common.XrefScans;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.Class;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.FieldInfo;
using Il2CppInterop.Runtime.Startup;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace Il2CppInterop.Runtime.Injection.Hooks
{
    internal unsafe class Class_GetFieldDefaultValue_Hook : Hook<Class_GetFieldDefaultValue_Hook.MethodDelegate>
    {
        public override string TargetMethodName => "Class::GetDefaultFieldValue";
        public override MethodDelegate GetDetour() => Hook;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate byte* MethodDelegate(Il2CppFieldInfo* field, out Il2CppTypeStruct* type);

        private byte* Hook(Il2CppFieldInfo* field, out Il2CppTypeStruct* type)
        {
            if (EnumInjector.GetDefaultValueOverride(field, out IntPtr newDefaultPtr))
            {
                INativeFieldInfoStruct wrappedField = UnityVersionHandler.Wrap(field);
                INativeClassStruct wrappedParent = UnityVersionHandler.Wrap(wrappedField.Parent);
                INativeClassStruct wrappedElementClass = UnityVersionHandler.Wrap(wrappedParent.ElementClass);
                type = wrappedElementClass.ByValArg.TypePointer;
                return (byte*)newDefaultPtr;
            }
            return Original(field, out type);
        }

        private static readonly MemoryUtils.SignatureDefinition[] s_Signatures =
        {
            // Test Game - Unity 2021.3.4 (x64)
            new MemoryUtils.SignatureDefinition
            {
                pattern = "\x48\x89\x5C\x24\x08\x48\x89\x74\x24\x10\x57\x48\x83\xEC\x20\x48\x8B\x79\x10\x48\x8B\xD9\x48\x8B\xF2\x48\x2B\x9F",
                mask = "xxxxxxxxxxxxxx?xxxxxxxxxxxxx",
                xref = false
            },
            
            // V Rising - Unity 2022.3.23 (x64)
            new MemoryUtils.SignatureDefinition
            {
                pattern = "\x48\x89\x5C\x24\x08\x48\x89\x74\x24\x10\x57\x48\x83\xEC\x40\x48\x8B\x41\x10",
                mask = "xxxxxxxxxxxxxxxxxxx",
                xref = false
            },
            // GTFO - Unity 2019.4.21 (x64)
            new MemoryUtils.SignatureDefinition
            {
                pattern = "\x48\x89\x5C\x24\x08\x57\x48\x83\xEC\x20\x48\x8B\x41\x10\x48\x8B\xD9\x48\x8B",
                mask = "xxxxxxxxxxxxxxxxxxx",
                xref = false
            },
            // Idle Slayer - Unity 2021.3.17 (x64)
            new MemoryUtils.SignatureDefinition
            {
                pattern = "\x40\x53\x48\x83\xEC\x20\x48\x8B\xDA\xE8\x00\x00\x00\x00\x4C\x8B\xC8\x48\x85\xC0",
                mask = "xxxxxxxxxx????xxxxxx",
                xref = false
            },
            // Evony - Unity 2018.4.0 (x86)
            new MemoryUtils.SignatureDefinition
            {
                pattern = "\x55\x8B\xEC\x56\xFF\x75\x08\xE8\x00\x00\x00\x00\x8B\xF0\x83\xC4\x04\x85\xF6",
                mask = "xxxxxxxx????xxxxxxx",
                xref = false
            },
        };

        private static nint FindClassGetFieldDefaultValueXref(bool forceICallMethod = false)
        {
            nint classGetDefaultFieldValue = 0;
 
            long GameAssemblyBase = 0;
 
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
                if (module.ModuleName == "GameAssembly.dll")
                {
                    GameAssemblyBase = (long)module.BaseAddress;
                    break;
                }
 
            if (GameAssemblyBase != 0)
            {
                Logger.Instance.LogWarning("test3");
                // Offset is 7B930 + C00
                classGetDefaultFieldValue = (IntPtr)GameAssemblyBase + 0x7B930 + 0xC00;
                //Logger.Instance.LogTrace("Class::GetFieldDefaultValue: 0x{GetFieldDefaultValueAddress}", classGetDefaultFieldValue.ToString("X2"));
                return classGetDefaultFieldValue;
            }
            Logger.Instance.LogTrace("test2");
            return classGetDefaultFieldValue;
        }

        public override IntPtr FindTargetMethod()
        {
            // NOTE: In some cases this pointer will be MetadataCache::GetFieldDefaultValueForField due to Field::GetDefaultFieldValue being
            // inlined but we'll treat it the same even though it doesn't receive the type parameter the RDX register
            // doesn't get cleared so we still get the same parameters
            var classGetDefaultFieldValue = s_Signatures
                .Select(s => MemoryUtils.FindSignatureInModule(InjectorHelpers.Il2CppModule, s))
                .FirstOrDefault(p => p != 0);

            if (classGetDefaultFieldValue == 0)
            {
                Logger.Instance.LogTrace("Couldn't fetch Class::GetDefaultFieldValue with signatures, using method traversal");
                classGetDefaultFieldValue = FindClassGetFieldDefaultValueXref();
            }

            return classGetDefaultFieldValue;
        }
    }
}
