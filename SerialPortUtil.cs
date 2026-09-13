// *************************************************************************************************
// SerialPortUtil.cs
// ------------------
// Provides detailed COM-port enumeration for Windows using the SetupAPI P/Invoke layer. This mirrors
// Python's pyserial list_ports.comports() by retrieving friendly names, hardware IDs (VID/PID),
// manufacturer strings, and registry data. The WinForms UI (frmMain) uses these helpers to populate
// the serial-port combo box with human-readable entries and to supply debug metadata for hardware
// troubleshooting. Every method is heavily commented to explain Windows API interop, registry access,
// and why multiple discovery strategies are combined.
// *************************************************************************************************

using System; // Core types like IntPtr and StringComparer.
using System.Collections.Generic; // Collections used to gather port metadata.
using System.Linq; // LINQ helpers for ordering and filtering port lists.
using System.Runtime.InteropServices; // P/Invoke attributes to call SetupAPI functions.
using System.Text; // StringBuilder for marshaling strings from unmanaged memory.
using System.Text.RegularExpressions; // Regex for extracting COM port names and USB VID/PID.
using Microsoft.Win32; // Registry access to augment port data.
using Microsoft.Win32.SafeHandles; // Safe handle wrapper for registry keys opened via SetupAPI.

namespace M18BatteryInfo;

/// <summary>
    /// Provides serial port enumeration using Windows SetupAPI to capture detailed descriptions similar
    /// to Python's pyserial list_ports.comports(). The class combines multiple discovery sources (friendly
    /// names, registry keys, and GetPortNames) to produce robust metadata for UI display and debugging.
/// </summary>
internal static class SerialPortUtil
{
    // SetupAPI flags and registry property identifiers. These magic numbers come from Windows header
    // files (setupapi.h/devpkey.h) and let us request specific properties from the device tree.
    private const uint DIGCF_PRESENT = 0x00000002; // Return only devices currently present in the system (ignores removed/phantom devices).

    private const uint SPDRP_DEVICEDESC = 0x00000000; // Device description (REG_SZ) property key.
    private const uint SPDRP_HARDWAREID = 0x00000001; // Hardware IDs (REG_MULTI_SZ) property key (contains VID/PID).
    private const uint SPDRP_MFG = 0x0000000B; // Manufacturer string (REG_SZ) property key.
    private const uint SPDRP_FRIENDLYNAME = 0x0000000C; // Friendly name shown in Device Manager (REG_SZ).
    private const uint SPDRP_LOCATION_INFORMATION = 0x0000000D; // Location information string (REG_SZ) e.g., USB port path.

    private const uint DEVPROP_TYPE_STRING = 0x12; // DEVPROP_TYPE_STRING constant used with SetupDiGetDeviceProperty.
    private const uint DEVPROP_TYPE_STRING_LIST = 0x1012; // DEVPROP_TYPE_STRING | DEVPROP_TYPEMOD_LIST for multi-string values.

    private const uint DIREG_DEV = 0x00000001; // Flag to open hardware key for device.
    private const int KEY_QUERY_VALUE = 0x0001; // Registry access mask for querying values.

    private static readonly Guid PortsClassGuid = new("4d36e978-e325-11ce-bfc1-08002be10318"); // GUID for Ports (COM & LPT) device class.
    private static readonly DEVPROPKEY DEVPKEY_Device_LocationPaths = new()
    {
        fmtid = new Guid("9d7debbc-c85d-4e75-a5f2-0e0e3bfb4ffd"), // GUID for location paths property set.
        pid = 37 // Property identifier within the set representing location paths.
    };

    /// <summary>
    /// Enumerates serial ports using SetupAPI and returns metadata suitable for UI display. This helper
    /// remains to correlate FTDI serial numbers with COM port assignments for reference only; all I/O
    /// now flows through RJCP.IO.Ports.SerialPortStream.
    /// </summary>
    /// <param name="debugLogger">Optional logger for verbose debug output.</param>
    /// <returns>Ordered list of serial port metadata.</returns>
    public static List<SerialPortDisplay> EnumerateDetailedPorts(Action<string>? debugLogger = null)
    {
        var ports = new Dictionary<string, SerialPortDisplay>(StringComparer.OrdinalIgnoreCase); // Use dictionary to merge data from multiple sources case-insensitively.

        EnumerateViaSetupApi(ports, debugLogger); // Primary enumeration using SetupAPI P/Invoke for rich data.

        return ports.Values
            .OrderBy(port => port.PortName, StringComparer.OrdinalIgnoreCase) // Sort alphabetically for predictable UI ordering.
            .ToList(); // Materialize list for caller.
    }

    private static void EnumerateViaSetupApi(IDictionary<string, SerialPortDisplay> ports, Action<string>? log)
    {
        // Acquire a handle to the Ports device class (COM & LPT) containing the serial devices. The GUID restricts
        // the search to devices implementing the ports interface so we do not enumerate unrelated hardware.
        var portsClassGuid = PortsClassGuid;
        var deviceInfoSet = SetupDiGetClassDevs(ref portsClassGuid, null, IntPtr.Zero, DIGCF_PRESENT); // Calls into setupapi.dll.
        if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
        {
            log?.Invoke("SetupAPI enumeration failed to acquire device info set; no COM metadata available."); // Warn UI that we cannot fetch metadata.
            return; // Without a valid handle we cannot enumerate.
        }

        try
        {
            var devInfoData = new SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() // Populate structure size before calling into SetupAPI.
            };

            for (uint index = 0; SetupDiEnumDeviceInfo(deviceInfoSet, index, ref devInfoData); index++) // Loop through devices until function returns false.
            {
                try
                {
                    var friendlyName = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_FRIENDLYNAME); // Pull human-friendly text e.g., "USB Serial Port (COM3)".
                    var description = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_DEVICEDESC); // Generic device description.
                    var manufacturer = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_MFG); // Manufacturer string (e.g., FTDI).
                    var hardwareIds = GetMultiStringProperty(deviceInfoSet, ref devInfoData, SPDRP_HARDWAREID); // Multi-string containing USB VID/PID.
                    var locationInformation = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_LOCATION_INFORMATION); // Port location info (hub/port).
                    var locationPaths = GetDevicePropertyMultiString(deviceInfoSet, ref devInfoData, DEVPKEY_Device_LocationPaths); // More detailed location path list.
                    var deviceInstanceId = GetDeviceInstanceId(deviceInfoSet, ref devInfoData); // Unique device instance string used for registry lookup.

                    var (portName, portSource) = ExtractBestPortName(friendlyName, description, deviceInfoSet, ref devInfoData, deviceInstanceId); // Try multiple strategies to extract "COMx".

                    if (string.IsNullOrWhiteSpace(portName))
                    {
                        log?.Invoke($"SetupAPI device entry missing COM port name. Desc: '{description ?? friendlyName ?? "(none)"}'"); // Skip devices without COM port string.
                        continue;
                    }

                    ports.TryGetValue(portName!, out var existing); // Try to merge with existing record if fallback already added.
                    existing ??= SerialPortDisplay.Create(portName!); // Create base record when first seen.
                    var combinedSource = CombineSources(existing.Source, "SetupAPI (Ports class)"); // Track provenance for debugging.

                    var parsedUsb = ParseUsbIdentifiers(hardwareIds, deviceInstanceId); // Parse VID/PID/serial from hardware IDs and instance ID.
                    var locationPath = locationPaths?.FirstOrDefault() ?? locationInformation; // Prefer detailed location path but fall back to location info.

                    // Merge new data into record, preserving existing values when already populated by other sources.
                    ports[portName!] = existing with
                    {
                        Description = string.IsNullOrWhiteSpace(existing.Description) ? description : existing.Description,
                        Manufacturer = string.IsNullOrWhiteSpace(existing.Manufacturer) ? manufacturer : existing.Manufacturer,
                        FriendlyName = string.IsNullOrWhiteSpace(existing.FriendlyName) ? friendlyName : existing.FriendlyName,
                        HardwareIds = string.IsNullOrWhiteSpace(existing.HardwareIds) ? hardwareIds : existing.HardwareIds,
                        DeviceInstanceId = string.IsNullOrWhiteSpace(existing.DeviceInstanceId) ? deviceInstanceId : existing.DeviceInstanceId,
                        UsbVendorId = string.IsNullOrWhiteSpace(existing.UsbVendorId) ? parsedUsb.Vid : existing.UsbVendorId,
                        UsbProductId = string.IsNullOrWhiteSpace(existing.UsbProductId) ? parsedUsb.Pid : existing.UsbProductId,
                        UsbSerialNumber = string.IsNullOrWhiteSpace(existing.UsbSerialNumber) ? parsedUsb.SerialNumber : existing.UsbSerialNumber,
                        LocationPath = string.IsNullOrWhiteSpace(existing.LocationPath) ? locationPath : existing.LocationPath,
                        Source = combinedSource
                    };

                    // Emit verbose log of all discovered values so users can diagnose driver issues.
                    LogDevice(log, portName!, new()
                    {
                        ($"FriendlyName", friendlyName, "SetupAPI SPDRP_FRIENDLYNAME"),
                        ($"Description", description, "SetupAPI SPDRP_DEVICEDESC"),
                        ($"Manufacturer", manufacturer, "SetupAPI SPDRP_MFG"),
                        ($"HardwareIds", hardwareIds, "SetupAPI SPDRP_HARDWAREID"),
                        ($"DeviceInstanceId", deviceInstanceId, "SetupDiGetDeviceInstanceId"),
                        ($"LocationPath", locationPath, "DEVPKEY_Device_LocationPaths/SPDRP_LOCATION_INFORMATION"),
                        ($"USB VID", parsedUsb.Vid, "Parsed from hardware IDs"),
                        ($"USB PID", parsedUsb.Pid, "Parsed from hardware IDs"),
                        ($"USB Serial", parsedUsb.SerialNumber, "Parsed from device instance"),
                        ($"Port source", portSource, "FriendlyName/Description/Registry")
                    });
                }
                catch (Exception ex)
                {
                    log?.Invoke($"SetupAPI enumeration error: {ex.Message}"); // Log individual device failures without aborting enumeration.
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet); // Always release the device info set handle to avoid leaks.
        }
    }

    private static (string? Port, string Source) ExtractBestPortName(string? friendlyName, string? description, IntPtr deviceInfoSet, ref SP_DEVINFO_DATA devInfoData, string? deviceInstanceId)
    {
        var portFromFriendly = ExtractPortName(friendlyName); // Friendly name often contains "(COM3)" suffix.
        if (!string.IsNullOrWhiteSpace(portFromFriendly))
        {
            return (portFromFriendly, "Friendly name"); // Return immediately when found in friendly name.
        }

        var portFromDescription = ExtractPortName(description); // Device description sometimes includes COM port.
        if (!string.IsNullOrWhiteSpace(portFromDescription))
        {
            return (portFromDescription, "Device description"); // Use description source if found.
        }

        var registryPort = TryGetPortNameFromRegistry(deviceInfoSet, ref devInfoData); // Query device-specific registry key for PortName value.
        if (!string.IsNullOrWhiteSpace(registryPort))
        {
            return (registryPort, "Device registry PortName"); // Use value from HKLM\SYSTEM\CurrentControlSet\Enum\<device>\Device Parameters.
        }

        var enumRegistryPort = TryGetPortNameFromEnumRegistry(deviceInstanceId); // Try alternate registry path using device instance ID.
        if (!string.IsNullOrWhiteSpace(enumRegistryPort))
        {
            return (enumRegistryPort, "HKLM\\SYSTEM\\CurrentControlSet\\Enum device parameters"); // Note source for debugging.
        }

        return (null, "Unavailable"); // Signal failure to extract COM port name.
    }

    private static void LogDevice(Action<string>? log, string portName, List<(string Name, string? Value, string Source)> values)
    {
        if (log == null)
        {
            return; // No logger supplied; do nothing.
        }

        var builder = new StringBuilder();
        builder.Append($"Detected {portName} with: "); // Prefix with port name for clarity.
        foreach (var (name, value, source) in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue; // Skip empty values to avoid noise.
            }

            builder.Append($"{name}='{value}' (from {source}); "); // Append each property and where it came from for auditability.
        }

        log(builder.ToString().Trim()); // Emit constructed line to UI logger.
    }

    private static string CombineSources(string? existingSource, string newSource)
    {
        if (string.IsNullOrWhiteSpace(existingSource))
        {
            return newSource; // If no existing source, return the new one.
        }

        return existingSource.Contains(newSource, StringComparison.OrdinalIgnoreCase)
            ? existingSource // Avoid duplicating same source string.
            : $"{existingSource}; {newSource}"; // Concatenate sources with semicolon to show multiple discovery paths.
    }

    private static string? GetDeviceInstanceId(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData)
    {
        var buffer = new StringBuilder(512); // Start with moderate buffer size for instance ID.
        if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, (uint)buffer.Capacity, out var requiredSize))
        {
            return buffer.ToString(); // Return instance ID string when call succeeds.
        }

        if (Marshal.GetLastWin32Error() != 122) // ERROR_INSUFFICIENT_BUFFER
        {
            return null; // If failure not due to buffer size, give up.
        }

        buffer = new StringBuilder((int)requiredSize); // Resize buffer to required size.
        return SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, (uint)buffer.Capacity, out _)
            ? buffer.ToString() // Return string on success.
            : null; // Otherwise return null.
    }

    private static string? GetDeviceRegistryProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property)
    {
        var buffer = new byte[1024]; // Allocate buffer for UTF-16 data.

        if (!SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property, out _, buffer, (uint)buffer.Length, out var requiredSize) || requiredSize == 0)
        {
            return null; // Return null when property missing or call fails.
        }

        return Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0'); // Decode UTF-16 and trim null terminator.
    }

    private static string? GetMultiStringProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property)
    {
        var buffer = new byte[2048]; // Allocate larger buffer for REG_MULTI_SZ values.

        if (!SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property, out var regType, buffer, (uint)buffer.Length, out var requiredSize) || requiredSize == 0)
        {
            return null; // Fail fast on missing data.
        }

        // REG_MULTI_SZ is a sequence of null-terminated UTF-16 strings ending with an additional null terminator.
        if (regType != 7) // 7 = REG_MULTI_SZ
        {
            return Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0'); // For non-multi strings, decode directly.
        }

        var multiSz = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0'); // Decode concatenated strings.
        var entries = multiSz.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries); // Split into individual entries.
        return entries.Length == 0 ? null : string.Join(", ", entries); // Join with comma for display.
    }

    private static List<string>? GetDevicePropertyMultiString(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, DEVPROPKEY propertyKey)
    {
        var buffer = new byte[4096]; // Large buffer for location paths list.

        if (!SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref propertyKey, out var propertyType, buffer, (uint)buffer.Length, out var requiredSize, 0) || requiredSize == 0)
        {
            return null; // Return null when property missing or call fails.
        }

        if (propertyType != DEVPROP_TYPE_STRING_LIST && propertyType != DEVPROP_TYPE_STRING)
        {
            return null; // Ignore unsupported property types to avoid mis-parsing binary data.
        }

        var value = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0'); // Decode UTF-16.
        var parts = value.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries); // Split on null separators.
        return parts.Length == 0 ? null : new List<string>(parts); // Return list for caller convenience.
    }

    private static string? TryGetPortNameFromRegistry(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData)
    {
        var regKey = SetupDiOpenDevRegKey(deviceInfoSet, ref deviceInfoData, 0, 0, DIREG_DEV, KEY_QUERY_VALUE); // Open device-specific registry key.
        if (regKey == IntPtr.Zero || regKey == new IntPtr(-1))
        {
            return null; // Key could not be opened (permissions or missing).
        }

        using var safeHandle = new SafeRegistryHandle(regKey, true); // Wrap raw handle for safe disposal.
        using var key = RegistryKey.FromHandle(safeHandle); // Create RegistryKey from handle.
        return key.GetValue("PortName") as string; // Return PortName value (e.g., COM3) if present.
    }

    private static string? TryGetPortNameFromEnumRegistry(string? deviceInstanceId)
    {
        if (string.IsNullOrWhiteSpace(deviceInstanceId))
        {
            return null; // No instance ID means we cannot build registry path.
        }

        try
        {
            var path = $"SYSTEM\\CurrentControlSet\\Enum\\{deviceInstanceId}\\Device Parameters"; // Build registry path to device parameters.
            using var enumKey = Registry.LocalMachine.OpenSubKey(path); // Open read-only.
            return enumKey?.GetValue("PortName") as string; // Extract PortName if it exists.
        }
        catch
        {
            // Registry access can fail due to permissions; ignore and continue.
            return null; // Silence exception; other methods may still succeed.
        }
    }

    private static string? ExtractPortName(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null; // Nothing to parse.
        }

        var match = Regex.Match(source, @"(COM\d+)", RegexOptions.IgnoreCase); // Regex finds patterns like COM3.
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null; // Normalize to uppercase for consistency.
    }

    private static (string? Vid, string? Pid, string? SerialNumber) ParseUsbIdentifiers(string? hardwareIds, string? deviceInstanceId)
    {
        string? firstHardwareId = hardwareIds
            ?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(id => id.Trim())
            .FirstOrDefault(); // Hardware IDs can be comma/semicolon separated; take the first entry.

        var vidMatch = Regex.Match(firstHardwareId ?? string.Empty, "VID_([0-9A-Fa-f]{4})"); // Capture USB vendor ID.
        var pidMatch = Regex.Match(firstHardwareId ?? string.Empty, "PID_([0-9A-Fa-f]{4})"); // Capture USB product ID.

        string? serialNumber = null;
        if (!string.IsNullOrWhiteSpace(deviceInstanceId))
        {
            var segments = deviceInstanceId.Split('\\'); // Device instance ID typically "USB\\VID_xxxx&PID_xxxx\\SERIAL".
            if (segments.Length >= 3)
            {
                serialNumber = segments[2]; // Third segment often contains USB serial number or location.
            }
        }

        return (
            vidMatch.Success ? vidMatch.Groups[1].Value.ToUpperInvariant() : null, // Normalize VID to uppercase.
            pidMatch.Success ? pidMatch.Groups[1].Value.ToUpperInvariant() : null, // Normalize PID to uppercase.
            serialNumber); // Return parsed serial (may be null).
    }

    /// <summary>
    /// Mirrors the native SP_DEVINFO_DATA structure used by SetupAPI to describe a device instance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    /// <summary>Represents a DEVPROPKEY structure used with SetupDiGetDeviceProperty.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    /// <summary>Retrieves a handle to a device information set for a specified class.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);

    /// <summary>Enumerates device information elements in a device information set.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

    /// <summary>Destroys a device information set and frees associated memory.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    /// <summary>Retrieves a device property from the registry.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte[] propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    /// <summary>Retrieves a device instance identifier (e.g., USB\\VID_XXXX&PID_XXXX\\SERIAL).</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInstanceId(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        StringBuilder deviceInstanceId,
        uint deviceInstanceIdSize,
        out uint requiredSize);

    /// <summary>Retrieves device properties such as DEVPKEY_Device_LocationPaths.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceProperty(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        ref DEVPROPKEY propertyKey,
        out uint propertyType,
        byte[] propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize,
        uint flags);

    /// <summary>Opens a registry key for a device instance.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiOpenDevRegKey(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint scope,
        uint hwProfile,
        uint keyType,
        int samDesired);
}

public sealed record SerialPortDisplay(
    string PortName,
    string? Description,
    string? Manufacturer,
    string? FriendlyName,
    string? HardwareIds,
    string? DeviceInstanceId,
    string? UsbVendorId,
    string? UsbProductId,
    string? UsbSerialNumber,
    string? LocationPath,
    string Source)
{
    public static SerialPortDisplay Create(string portName) => new(portName, null, null, null, null, null, null, null, null, null, string.Empty); // Factory helper to start with empty metadata.

    public string? DeviceDescription => SanitizeDescription(FriendlyName ?? Description); // Choose friendly name or description, cleaning redundant COM text.

    public string DisplayName
    {
        get
        {
            var parts = new List<string> { PortName }; // Always start with COM port name (e.g., COM3).

            var ftdiOrUsbSegment = BuildFtdiOrUsbSegment(); // Add FTDI/USB VID/PID segment when available for quick visual identification.
            if (!string.IsNullOrWhiteSpace(ftdiOrUsbSegment))
            {
                parts.Add(ftdiOrUsbSegment!);
            }

            if (!string.IsNullOrWhiteSpace(DeviceDescription))
            {
                parts.Add(DeviceDescription!); // Append cleaned description for context (e.g., "USB Serial Device").
            }

            if (!string.IsNullOrWhiteSpace(Manufacturer))
            {
                parts.Add(Manufacturer!); // Append manufacturer such as "FTDI" or "Microsoft".
            }

            return string.Join(" - ", parts); // Join with hyphen separators to produce combo-box display text.
        }
    }

    public bool IsLikelyFtdi => new[] { Description, Manufacturer, FriendlyName }
        .Any(value => value != null && value.IndexOf("FTDI", StringComparison.OrdinalIgnoreCase) >= 0); // Heuristic: check for "FTDI" substring to highlight FT232 cables.

    private string? BuildFtdiOrUsbSegment()
    {
        if (IsLikelyFtdi)
        {
            return "FTDI"; // Explicitly flag FTDI devices because they are commonly used with this project.
        }

        var usbParts = new List<string>(); // Collect VID/PID pair for non-FTDI devices.

        if (!string.IsNullOrWhiteSpace(UsbVendorId))
        {
            usbParts.Add($"VID:{UsbVendorId}");
        }

        if (!string.IsNullOrWhiteSpace(UsbProductId))
        {
            usbParts.Add($"PID:{UsbProductId}");
        }

        return usbParts.Count > 0 ? string.Join(" ", usbParts) : null; // Return combined VID/PID or null if unknown.
    }

    private string? SanitizeDescription(string? rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return null; // No description to clean.
        }

        var cleaned = rawDescription
            .Replace($"({PortName})", string.Empty, StringComparison.OrdinalIgnoreCase) // Remove redundant "(COMx)" segments.
            .Replace(PortName, string.Empty, StringComparison.OrdinalIgnoreCase) // Remove raw COM string.
            .Trim().Trim('-', '—').Trim(); // Trim leftover punctuation and whitespace.

        return string.IsNullOrWhiteSpace(cleaned) ? rawDescription : cleaned; // If cleaning removed everything, fall back to raw description.
    }

    public override string ToString() => DisplayName; // ComboBox calls ToString(), so return DisplayName for a friendly label.
}
