// *************************************************************************************************
// SerialPortConnection.cs
// -----------------------
// Wraps RJCP.IO.Ports.SerialPortStream with helper methods that mirror the subset of operations
// needed by M18Protocol: opening the port with specific framing, toggling BREAK/DTR, purging
// buffers, and reading/writing exact byte counts. The wrapper also supports an optional raw logger
// to surface low-level serial actions to the UI without exposing SerialPortStream directly. RJCP
// provides .NET 10 support and granular control-line handling that matches the Python reference
// script, including manual Break/DTR and millisecond timeouts.
// *************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using RJCP.IO.Ports;

namespace M18BatteryInfo;

public sealed class SerialPortConnection : IDisposable
{
    private readonly SerialPortStream _serialPort;
    private Action<string>? _rawLogger;

    public SerialPortConnection(SerialPortDisplay device, Action<string>? rawLogger)
    {
        Device = device;
        _rawLogger = rawLogger;

        _serialPort = new SerialPortStream(device.PortName, 4800, 8, Parity.None, StopBits.Two)
        {
            ReadTimeout = 800,
            WriteTimeout = 800,
            DtrEnable = false,
            RtsEnable = false
        };

        Log($"Opening {device.PortName} at 4800 8N2 using RJCP.SerialPortStream");
        _serialPort.Open();
        PurgeAll();
    }

    public SerialPortDisplay Device { get; }

    public bool IsOpen => _serialPort.IsOpen;

    public void SetBreak(bool enable)
    {
        _serialPort.BreakState = enable;
        Log($"SerialPort.BreakState={(enable ? "On" : "Off")}");
    }

    public void SetDtr(bool enable)
    {
        _serialPort.DtrEnable = enable;
        Log($"SerialPort.DtrEnable={(enable ? "On" : "Off")}");
    }

    public void PurgeAll()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
        Log("SerialPort.DiscardInBuffer/DiscardOutBuffer");
    }

    public void PurgeRx()
    {
        _serialPort.DiscardInBuffer();
        Log("SerialPort.DiscardInBuffer");
    }

    public void WriteBytes(byte[] data)
    {
        _serialPort.Write(data, 0, data.Length);
        _serialPort.Flush(); // Ensure immediate transmission; mirrors Python's synchronous writes.
        Log($"SerialPort.Write {FormatHex(data)}");
    }

    public byte[] ReadBytes(int count)
    {
        var buffer = new byte[count];
        var totalRead = 0;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            while (totalRead < count)
            {
                var bytesRead = _serialPort.Read(buffer, totalRead, count - totalRead);
                totalRead += bytesRead;
            }
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException($"SerialPort.Read timed out after reading {totalRead} of {count} byte(s) over {stopwatch.ElapsedMilliseconds} ms", ex);
        }

        stopwatch.Stop();
        Log($"SerialPort.Read count={count}, bytesRead={totalRead}, elapsed={stopwatch.ElapsedMilliseconds}ms, data={FormatHex(buffer.Take(totalRead))}");

        if (totalRead < count)
        {
            throw new InvalidOperationException($"SerialPort.Read returned {totalRead} of {count} byte(s)");
        }

        return buffer;
    }

    public void Close()
    {
        if (!_serialPort.IsOpen)
        {
            return;
        }

        Log("SerialPort.Close");
        _serialPort.Close();
    }

    public void Dispose()
    {
        Close();
        _serialPort.Dispose();
    }

    public void UpdateLogger(Action<string>? logger)
    {
        _rawLogger = logger;
    }

    private void Log(string message)
    {
        _rawLogger?.Invoke(message);
    }

    private static string FormatHex(IEnumerable<byte> data)
    {
        return string.Join(" ", data.Select(b => $"{b:X2}"));
    }
}
