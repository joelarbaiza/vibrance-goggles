using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;

namespace VibranceGoggles.Gpu
{
    /// <summary>
    /// Intel HD / UHD / Iris graphics, through the "DataHandler" COM server that the Intel driver installs
    /// (igfxDH.dll, used by the Intel Graphics Control Panel).
    ///
    /// Every driver release ships a slightly different type library, so instead of shipping a fixed
    /// interop assembly we generate one at runtime from the type library registered on this machine
    /// and talk to it through reflection. Nothing has to be copied by hand.
    /// </summary>
    internal sealed class IntelController : IVibranceController
    {
        private static readonly Guid DataHandlerClsid = new Guid("D5F5053A-9585-4D80-8F6F-7B6587CEFB93");

        // Intel works in "offset" units: 0 = normal, -100 = grayscale, +100 = double saturation.
        private const int Normal = 100;

        private readonly object _handler;
        private readonly MethodInfo _getConfig;
        private readonly MethodInfo _getColor;
        private readonly MethodInfo _setColor;
        private readonly Type _devicesType;
        private readonly Type _colorInfoType;
        private readonly Type _commandType;
        private readonly int _min;

        public string AdapterName { get; }
        // Values above the driver's fMax wrap around (they are stored in a signed byte), so never exceed it.
        public int MaxVibrance { get; }

        private IntelController(Assembly interop)
        {
            Type iface = interop.GetTypes().First(t => t.IsInterface && t.GetMethod("get_color") != null);
            _getConfig = iface.GetMethod("get_CurrentConfig");
            _getColor = iface.GetMethod("get_color");
            _setColor = iface.GetMethod("set_color");
            _devicesType = _getColor.GetParameters()[1].ParameterType.GetElementType();
            _colorInfoType = _devicesType.GetField("Device").FieldType.GetElementType();
            _commandType = _devicesType.GetField("Command").FieldType;

            _handler = Activator.CreateInstance(Type.GetTypeFromCLSID(DataHandlerClsid, true));

            object brand = iface.GetProperty("BrandInfo").GetValue(_handler, null);
            AdapterName = Convert.ToString(brand.GetType().GetField("wcAdapterName").GetValue(brand)).Trim();

            object sat = ReadSaturation(out _, out _);
            _min = Normal + ToInt(sat, "fMin");
            MaxVibrance = Normal + ToInt(sat, "fMax");
            if (_min < 0) _min = 0;
        }

        public static IntelController TryCreate()
        {
            try
            {
                if (Type.GetTypeFromCLSID(DataHandlerClsid, false) == null) return null;
                ITypeLib typeLib = LoadTypeLibrary();
                if (typeLib == null) return null;

                var converter = new TypeLibConverter();
                Assembly interop = converter.ConvertTypeLibToAssembly(
                    typeLib, "igfxDHLib.Runtime.dll", TypeLibImporterFlags.None,
                    new ImporterSink(), null, null, "igfxDHLib", null);

                return new IntelController(interop);
            }
            catch (Exception ex)
            {
                Log.Write("Intel backend unavailable: " + ex);
                return null;
            }
        }

        public int GetVibrance()
        {
            object sat = ReadSaturation(out _, out _);
            return Normal + ToInt(sat, "fCurrent");
        }

        public void SetVibrance(int percent)
        {
            int value = Math.Max(_min, Math.Min(MaxVibrance, percent)) - Normal;

            ReadSaturation(out object devices, out uint[] targets);
            _devicesType.GetField("Command").SetValue(devices, Enum.Parse(_commandType, "CHANGE_DEVICE_COLOR"));

            var list = (Array)_devicesType.GetField("Device").GetValue(devices);
            for (int i = 0; i < list.Length; i++)
            {
                object info = list.GetValue(i);
                FieldInfo satField = _colorInfoType.GetField("Saturation");
                object sat = satField.GetValue(info);
                FieldInfo current = sat.GetType().GetField("fCurrent");
                current.SetValue(sat, Convert.ChangeType(value, current.FieldType));
                satField.SetValue(info, sat);
                list.SetValue(info, i);
            }
            _devicesType.GetField("Device").SetValue(devices, list);

            foreach (uint target in targets.Where(t => t != 0).DefaultIfEmpty(0u))
            {
                _setColor.Invoke(_handler, new object[] { target, devices, 0u });
            }
        }

        /// <summary>Reads the color state of the active displays; returns the saturation of the first one.</summary>
        private object ReadSaturation(out object devices, out uint[] targets)
        {
            object config = Activator.CreateInstance(_getConfig.GetParameters()[0].ParameterType.GetElementType());
            var configArgs = new[] { config };
            _getConfig.Invoke(_handler, configArgs);
            config = configArgs[0];

            object topology = config.GetType().GetField("systemTopology").GetValue(config);
            var paths = (Array)topology.GetType().GetField("pathInfo").GetValue(topology);

            int count = Math.Min(3, paths.Length);
            targets = new uint[count];
            uint mask = 0;
            for (int i = 0; i < count; i++)
            {
                object path = paths.GetValue(i);
                targets[i] = Convert.ToUInt32(path.GetType().GetField("dwTargetId").GetValue(path));
                mask |= targets[i];
            }

            devices = Activator.CreateInstance(_devicesType);
            _devicesType.GetField("ulDevices").SetValue(devices, mask);
            _devicesType.GetField("Device").SetValue(devices, Array.CreateInstance(_colorInfoType, 3));
            _devicesType.GetField("Command").SetValue(devices, Enum.Parse(_commandType, "GET_COLOR"));

            var colorArgs = new[] { (object)0u, devices };
            _getColor.Invoke(_handler, colorArgs);
            devices = colorArgs[1];

            var list = (Array)_devicesType.GetField("Device").GetValue(devices);
            return _colorInfoType.GetField("Saturation").GetValue(list.GetValue(0));
        }

        private static int ToInt(object structure, string field) =>
            (int)Math.Round(Convert.ToDouble(structure.GetType().GetField(field).GetValue(structure)));

        // ----------------------------------------------------------------------------------------------

        private static ITypeLib LoadTypeLibrary()
        {
            // 1) The type library registered for the DataHandler class (normal case).
            string libId = Registry.ClassesRoot
                .OpenSubKey($@"CLSID\{{{DataHandlerClsid}}}\TypeLib")?.GetValue(null) as string;

            if (libId != null && Guid.TryParse(libId, out Guid libGuid))
            {
                using (RegistryKey versions = Registry.ClassesRoot.OpenSubKey($@"TypeLib\{libId}"))
                {
                    foreach (string version in (versions?.GetSubKeyNames() ?? new string[0]).OrderByDescending(v => v))
                    {
                        string[] parts = version.Split('.');
                        if (parts.Length == 2 &&
                            short.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out short major) &&
                            short.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out short minor) &&
                            LoadRegTypeLib(ref libGuid, major, minor, 0, out ITypeLib lib) == 0)
                        {
                            return lib;
                        }
                    }
                }
            }

            // 2) Fallback: read the type library embedded in the COM server DLL itself.
            string server = Registry.ClassesRoot
                .OpenSubKey($@"CLSID\{{{DataHandlerClsid}}}\InprocServer32")?.GetValue(null) as string;
            if (!string.IsNullOrEmpty(server) &&
                LoadTypeLibEx(Environment.ExpandEnvironmentVariables(server), 2 /* REGKIND_NONE */, out ITypeLib embedded) == 0)
            {
                return embedded;
            }

            return null;
        }

        [DllImport("oleaut32.dll")]
        private static extern int LoadRegTypeLib(ref Guid libId, short major, short minor, int lcid, out ITypeLib typeLib);

        [DllImport("oleaut32.dll", CharSet = CharSet.Unicode)]
        private static extern int LoadTypeLibEx(string file, int regKind, out ITypeLib typeLib);

        private sealed class ImporterSink : ITypeLibImporterNotifySink
        {
            public void ReportEvent(ImporterEventKind eventKind, int eventCode, string eventMsg) { }
            public Assembly ResolveRef(object typeLib) => null;
        }
    }
}
