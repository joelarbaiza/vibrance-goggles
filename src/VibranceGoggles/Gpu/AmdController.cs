using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VibranceGoggles.Gpu
{
    /// <summary>
    /// AMD Radeon graphics (integrated Vega / RDNA and discrete), through the public
    /// AMD Display Library (ADL, atiadlxx.dll) installed with the Radeon driver.
    /// </summary>
    internal sealed class AmdController : IVibranceController
    {
        private const string Dll = "atiadlxx.dll";
        private const int ADL_OK = 0;
        private const int ADL_DISPLAY_COLOR_SATURATION = 1 << 2;
        private const int ADL_DISPLAY_DISPLAYINFO_DISPLAYCONNECTED = 0x1;
        private const int ADL_DISPLAY_DISPLAYINFO_DISPLAYMAPPED = 0x2;

        private readonly List<Target> _targets;

        public string AdapterName { get; }
        public int MaxVibrance { get; }

        private struct Target
        {
            public int Adapter, Display, Default, Min, Max;
        }

        private AmdController(List<Target> targets, string adapterName)
        {
            _targets = targets;
            AdapterName = adapterName;
            Target first = targets[0];
            MaxVibrance = first.Default > 0 ? first.Max * 100 / first.Default : 200;
        }

        public static AmdController TryCreate()
        {
            try
            {
                if (ADL_Main_Control_Create(Malloc, 1) != ADL_OK) return null;

                int count = 0;
                if (ADL_Adapter_NumberOfAdapters_Get(ref count) != ADL_OK || count <= 0) return null;

                int size = Marshal.SizeOf(typeof(AdapterInfo));
                IntPtr buffer = Marshal.AllocHGlobal(size * count);
                try
                {
                    var zero = new byte[size * count];
                    Marshal.Copy(zero, 0, buffer, zero.Length);
                    if (ADL_Adapter_AdapterInfo_Get(buffer, size * count) != ADL_OK) return null;

                    var targets = new List<Target>();
                    var seen = new HashSet<string>();
                    string name = null;

                    for (int i = 0; i < count; i++)
                    {
                        var adapter = (AdapterInfo)Marshal.PtrToStructure(buffer + i * size, typeof(AdapterInfo));
                        if (adapter.Present == 0) continue;

                        foreach (DisplayInfo display in GetDisplays(adapter.AdapterIndex))
                        {
                            int flags = ADL_DISPLAY_DISPLAYINFO_DISPLAYCONNECTED | ADL_DISPLAY_DISPLAYINFO_DISPLAYMAPPED;
                            if ((display.InfoValue & flags) != flags) continue;
                            if (display.DisplayId.LogicalAdapterIndex != adapter.AdapterIndex) continue;

                            string key = display.DisplayId.LogicalAdapterIndex + ":" + display.DisplayId.LogicalIndex;
                            if (!seen.Add(key)) continue;

                            if (ADL_Display_Color_Get(adapter.AdapterIndex, display.DisplayId.LogicalIndex,
                                    ADL_DISPLAY_COLOR_SATURATION, out _, out int def, out int min, out int max, out _) != ADL_OK)
                                continue;

                            targets.Add(new Target
                            {
                                Adapter = adapter.AdapterIndex,
                                Display = display.DisplayId.LogicalIndex,
                                Default = def, Min = min, Max = max,
                            });
                            name = name ?? adapter.AdapterName?.Trim();
                        }
                    }

                    return targets.Count > 0 ? new AmdController(targets, name ?? "AMD Radeon") : null;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (DllNotFoundException)
            {
                return null; // No AMD driver installed.
            }
            catch (Exception ex)
            {
                Log.Write("AMD backend unavailable: " + ex);
                return null;
            }
        }

        public int GetVibrance()
        {
            Target t = _targets[0];
            if (ADL_Display_Color_Get(t.Adapter, t.Display, ADL_DISPLAY_COLOR_SATURATION,
                    out int current, out _, out _, out _, out _) != ADL_OK || t.Default <= 0)
                return 100;
            return current * 100 / t.Default;
        }

        public void SetVibrance(int percent)
        {
            foreach (Target t in _targets)
            {
                int value = t.Default * percent / 100;
                value = Math.Max(t.Min, Math.Min(t.Max, value));
                ADL_Display_Color_Set(t.Adapter, t.Display, ADL_DISPLAY_COLOR_SATURATION, value);
            }
        }

        private static IEnumerable<DisplayInfo> GetDisplays(int adapterIndex)
        {
            int num = 0;
            IntPtr ptr = IntPtr.Zero;
            var result = new List<DisplayInfo>();
            if (ADL_Display_DisplayInfo_Get(adapterIndex, ref num, out ptr, 0) == ADL_OK && ptr != IntPtr.Zero)
            {
                int size = Marshal.SizeOf(typeof(DisplayInfo));
                for (int i = 0; i < num; i++)
                    result.Add((DisplayInfo)Marshal.PtrToStructure(ptr + i * size, typeof(DisplayInfo)));
            }
            if (ptr != IntPtr.Zero) Marshal.FreeCoTaskMem(ptr);
            return result;
        }

        // ------------------------------------------------------------------------------- ADL interop

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr AdlMalloc(int size);

        // Kept in a static field so the garbage collector never frees the callback.
        private static readonly AdlMalloc Malloc = size => Marshal.AllocCoTaskMem(size);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct AdapterInfo
        {
            public int Size;
            public int AdapterIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string UDID;
            public int BusNumber;
            public int DeviceNumber;
            public int FunctionNumber;
            public int VendorID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string AdapterName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DisplayName;
            public int Present;
            public int Exist;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DriverPath;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DriverPathExt;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string PNPString;
            public int OSDisplayIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DisplayId
        {
            public int LogicalIndex;
            public int PhysicalIndex;
            public int LogicalAdapterIndex;
            public int PhysicalAdapterIndex;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DisplayInfo
        {
            public DisplayId DisplayId;
            public int ControllerIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string ManufacturerName;
            public int DisplayType;
            public int OutputType;
            public int Connector;
            public int InfoMask;
            public int InfoValue;
        }

        [DllImport(Dll)] private static extern int ADL_Main_Control_Create(AdlMalloc callback, int enumConnectedAdapters);
        [DllImport(Dll)] private static extern int ADL_Adapter_NumberOfAdapters_Get(ref int count);
        [DllImport(Dll)] private static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int inputSize);
        [DllImport(Dll)] private static extern int ADL_Display_DisplayInfo_Get(int adapterIndex, ref int numDisplays, out IntPtr info, int forceDetect);

        [DllImport(Dll)]
        private static extern int ADL_Display_Color_Get(int adapterIndex, int displayIndex, int colorType,
            out int current, out int @default, out int min, out int max, out int step);

        [DllImport(Dll)]
        private static extern int ADL_Display_Color_Set(int adapterIndex, int displayIndex, int colorType, int current);
    }
}
