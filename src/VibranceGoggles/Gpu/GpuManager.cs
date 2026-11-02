using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceGoggles.Gpu
{
    /// <summary>Detects every supported graphics backend and drives them together.</summary>
    internal sealed class GpuManager
    {
        private readonly List<IVibranceController> _controllers = new List<IVibranceController>();

        public GpuManager()
        {
            IVibranceController intel = IntelController.TryCreate();
            if (intel != null) _controllers.Add(intel);

            IVibranceController amd = AmdController.TryCreate();
            if (amd != null) _controllers.Add(amd);

            foreach (var c in _controllers) Log.Write("Detected: " + c.AdapterName);
        }

        public bool HasGpu => _controllers.Count > 0;

        public string Description => HasGpu ? string.Join(" + ", _controllers.Select(c => c.AdapterName)) : null;

        public int MaxVibrance => HasGpu ? _controllers.Min(c => c.MaxVibrance) : 200;

        public int GetVibrance()
        {
            try { return HasGpu ? _controllers[0].GetVibrance() : 100; }
            catch (Exception ex) { Log.Write("Get failed: " + ex.Message); return 100; }
        }

        public void SetVibrance(int percent)
        {
            foreach (IVibranceController c in _controllers)
            {
                try { c.SetVibrance(percent); }
                catch (Exception ex) { Log.Write($"Set {percent}% on {c.AdapterName} failed: {ex.Message}"); }
            }
        }
    }
}
