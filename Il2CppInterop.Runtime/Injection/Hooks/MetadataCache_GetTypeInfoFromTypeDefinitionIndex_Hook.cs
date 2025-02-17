using System;
using System.Linq;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using Il2CppInterop.Common.XrefScans;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Startup;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace Il2CppInterop.Runtime.Injection.Hooks
{
    internal unsafe class MetadataCache_GetTypeInfoFromTypeDefinitionIndex_Hook :
        Hook<MetadataCache_GetTypeInfoFromTypeDefinitionIndex_Hook.MethodDelegate>
    {
        public override string TargetMethodName => "MetadataCache::GetTypeInfoFromTypeDefinitionIndex";
        public override MethodDelegate GetDetour() => Hook;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate Il2CppClass* MethodDelegate(int index);

        private Il2CppClass* Hook(int index)
        {
            if (InjectorHelpers.s_InjectedClasses.TryGetValue(index, out IntPtr classPtr))
                return (Il2CppClass*)classPtr;

            return Original(index);
        }

        private IntPtr FindGetTypeInfoFromTypeDefinitionIndex(bool forceICallMethod = false)
        {
            IntPtr getTypeInfoFromTypeDefinitionIndex = IntPtr.Zero;
         
            long GameAssemblyBase = 0;
         
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
                if (module.ModuleName == "GameAssembly.dll")
                {
                    GameAssemblyBase = (long)module.BaseAddress;
                    break;
                }
         
            if (GameAssemblyBase != 0)
            {
                // Offset is 86610 + C00
                getTypeInfoFromTypeDefinitionIndex = (IntPtr)GameAssemblyBase + 0x86610 + 0xC00;
                Logger.Instance.LogWarning("Type::GetUnderlyingType: 0x{TypeGetUnderlyingTypeAddress}", getTypeInfoFromTypeDefinitionIndex.ToInt64().ToString("X2"));
                return getTypeInfoFromTypeDefinitionIndex;
            }
            Logger.Instance.LogWarning("test");
            return getTypeInfoFromTypeDefinitionIndex;
        }

        public override IntPtr FindTargetMethod()
        {
            return FindGetTypeInfoFromTypeDefinitionIndex(); 
        }
    }
}
