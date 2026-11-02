namespace VibranceGoggles.Gpu
{
    /// <summary>
    /// A graphics driver backend that can change display saturation.
    /// Vibrance is expressed in percent: 100 = normal colors, 0 = grayscale, 200 = double saturation.
    /// </summary>
    internal interface IVibranceController
    {
        /// <summary>Human readable adapter name, e.g. "Intel(R) UHD Graphics 630".</summary>
        string AdapterName { get; }

        /// <summary>Highest vibrance the driver accepts.</summary>
        int MaxVibrance { get; }

        int GetVibrance();

        void SetVibrance(int percent);
    }
}
