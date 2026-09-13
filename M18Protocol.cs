// *************************************************************************************************
// M18Protocol.cs
// --------------
// Literal, instruction-for-instruction port of m18.py. Every sleep, buffer reset, control-line
// toggle, byte ordering rule, and logging string mirrors the Python implementation. The structure
// intentionally ignores .NET conventions so the execution order, timing, and side effects remain
// identical to the Python reference, including redundant operations and blocking behaviour. Serial
// I/O now routes through RJCP.IO.Ports.SerialPortStream (via SerialPortConnection) to guarantee
// .NET 10 compatibility and access to manual BREAK/DTR toggles plus buffer flushes.
// *************************************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace M18BatteryInfo
{
    public class M18Protocol
    {
        // ---------------------------
        // Constants matching m18.py
        // ---------------------------
        public const byte SYNC_BYTE = 0xAA;
        public const byte CAL_CMD = 0x55;
        public const byte CONF_CMD = 0x60;
        public const byte SNAP_CMD = 0x61;
        public const byte KEEPALIVE_CMD = 0x62;

        public const int DEFAULT_CUTOFF_CURRENT = 300;
        public const int DEFAULT_MAX_CURRENT = 6000;

        // ---------------------------
        // State fields (mirror Python names and defaults)
        // ---------------------------
        public int ACC = 4;
        public bool PRINT_TX = false;
        public bool PRINT_RX = false;
        public bool PRINT_TX_SAVE = false;
        public bool PRINT_RX_SAVE = false;
        public int CUTOFF_CURRENT = DEFAULT_CUTOFF_CURRENT;
        public int MAX_CURRENT = DEFAULT_MAX_CURRENT;
        public Action<string>? TxLogger { get; set; }
        public Action<string>? RxLogger { get; set; }
        private Action<string>? rawLogger;
        public Action<string>? RawLogger
        {
            get => rawLogger;
            set
            {
                rawLogger = value;
                port?.UpdateLogger(value);
            }
        }

        public SerialPortConnection port;

        // ---------------------------
        // Data tables copied verbatim from m18.py
        // ---------------------------
        private readonly List<(int, int, int)> data_matrix = new()
        {
            (0x00, 0x00, 0x02),
            (0x00, 0x02, 0x02),
            (0x00, 0x04, 0x05),
            (0x00, 0x0D, 0x04),
            (0x00, 0x11, 0x04),
            (0x00, 0x15, 0x04),
            (0x00, 0x19, 0x04),
            (0x00, 0x23, 0x14),
            (0x00, 0x37, 0x04),
            (0x00, 0x69, 0x02),
            (0x00, 0x7B, 0x01),
            (0x40, 0x00, 0x04),
            (0x40, 0x0A, 0x0A),
            (0x40, 0x14, 0x02),
            (0x40, 0x16, 0x02),
            (0x40, 0x19, 0x02),
            (0x40, 0x1B, 0x02),
            (0x40, 0x1D, 0x02),
            (0x40, 0x1F, 0x02),
            (0x60, 0x00, 0x02),
            (0x60, 0x02, 0x02),
            (0x60, 0x04, 0x04),
            (0x60, 0x08, 0x04),
            (0x60, 0x0C, 0x02),
            (0x90, 0x00, 0x3A),
            (0x90, 0x3A, 0x3A),
            (0x90, 0x74, 0x3A),
            (0x90, 0xAE, 0x3A),
            (0x90, 0xE8, 0x3A),
            (0x91, 0x22, 0x30),
            (0x91, 0x52, 0x00),
            (0xA0, 0x00, 0x06)
        };

        private readonly List<(int, int, string, string)> data_id = new()
        {
            (0x0000, 2,  "uint",  "Cell type"),
            (0x0002, 2,  "uint",  "Unknown (always 0)"),
            (0x0004, 5,  "sn",    "Capacity & Serial number (?)"),
            (0x000D, 4,  "uint",  "Unknown (4th code?)"),
            (0x0011, 4,  "date",  "Manufacture date"),
            (0x0015, 4,  "date",  "Date of first charge (Forge)"),
            (0x0019, 4,  "date",  "Date of last charge (Forge)"),
            (0x0023, 20, "ascii", "Note (ascii string)"),
            (0x0037, 4,  "date",  "Current date"),
            (0x0069, 2,  "uint",  "Unknown (always 2)"),
            (0x007B, 1,  "uint",  "Unknown (always 0)"),
            (0x4000, 4,  "uint",  "Unknown (Forge)"),
            (0x400A, 10, "cell_v","Cell voltages (mV)"),
            (0x4014, 2,  "adc_t", "Temperature (C) (non-Forge)"),
            (0x4016, 2,  "uint",  "Unknown (Forge)"),
            (0x4019, 2,  "uint",  "Unknown (Forge)"),
            (0x401B, 2,  "uint",  "Unknown (Forge)"),
            (0x401D, 2,  "uint",  "Unknown (Forge)"),
            (0x401F, 2,  "dec_t", "Temperature (C) (Forge)"),
            (0x6000, 2,  "uint",  "Unknown (Forge)"),
            (0x6002, 2,  "uint",  "Unknown (Forge)"),
            (0x6004, 4,  "uint",  "Unknown (Forge)"),
            (0x6008, 4,  "uint",  "Unknown (Forge)"),
            (0x600C, 2,  "uint",  "Unknown (Forge)"),
            (0x9000, 4,  "date",  "Date of first charge (rounded)"),
            (0x9004, 4,  "date",  "Date of last tool use (rounded)"),
            (0x9008, 4,  "date",  "Date of last charge (rounded)"),
            (0x900C, 4,  "date",  "Unknown date (often zero)"),
            (0x9010, 2,  "uint",  "Days since first charge"),
            (0x9012, 4,  "uint",  "Total discharge (amp-sec)"),
            (0x9016, 4,  "uint",  "Total discharge (watt-sec or joules)"),
            (0x901A, 4,  "uint",  "Total charge count"),
            (0x901E, 2,  "uint",  "Dumb charge count (J2>7.1V for >=0.48s)"),
            (0x9020, 2,  "uint",  "Redlink (UART) charge count"),
            (0x9022, 2,  "uint",  "Completed charge count (?)"),
            (0x9024, 4,  "hhmmss","Total charging time (HH:MM:SS)"),
            (0x9028, 4,  "hhmmss","Time on charger whilst full (HH:MM:SS)"),
            (0x902C, 2,  "uint",  "Unknown (another low-voltage charge counter?)"),
            (0x902E, 2,  "uint",  "Charge started with a cell < 2.5V"),
            (0x9030, 2,  "uint",  "Discharge to empty"),
            (0x9032, 2,  "uint",  "Num. overheat on tool (must be > 10A)"),
            (0x9034, 2,  "uint",  "Overcurrent?"),
            (0x9036, 2,  "uint",  "Low voltage events)"),
            (0x9038, 2,  "uint",  "Low-voltage bounce? (4 flashing LEDs)"),
            (0x903A, 2,  "uint",  "Discharge @ 10-20A (seconds)"),
            (0x903C, 2,  "uint",  "          @ 20-30A (could be watts)"),
            (0x903E, 2,  "uint",  "          @ 30-40A      "),
            (0x9040, 2,  "uint",  "          @ 40-50A      "),
            (0x9042, 2,  "uint",  "          @ 50-60A      "),
            (0x9044, 2,  "uint",  "          @ 60-70A      "),
            (0x9046, 2,  "uint",  "          @ 70-80A      "),
            (0x9048, 2,  "uint",  "          @ 80-90A      "),
            (0x904A, 2,  "uint",  "          @ 90-100A     "),
            (0x904C, 2,  "uint",  "          @ 100-110A    "),
            (0x904E, 2,  "uint",  "          @ 110-120A    "),
            (0x9050, 2,  "uint",  "          @ 120-130A    "),
            (0x9052, 2,  "uint",  "          @ 130-140A    "),
            (0x9054, 2,  "uint",  "          @ 140-150A    "),
            (0x9056, 2,  "uint",  "          @ 150-160A    "),
            (0x9058, 2,  "uint",  "          @ 160-170A    "),
            (0x905A, 2,  "uint",  "          @ 170-180A    "),
            (0x905C, 2,  "uint",  "          @ 180-190A    "),
            (0x905E, 2,  "uint",  "          @ 190-200A    "),
            (0x9060, 2,  "uint",  "          @ 200-210A    "),
            (0x9062, 2,  "uint",  "Discharge @ 5-10A (seconds)"),
            (0x9064, 2,  "uint",  "          @ 10-15A (could be watts)"),
            (0x9066, 2,  "uint",  "          @ 15-20A (histo not well understood yet)"),
            (0x9068, 2,  "uint",  "          @ 20-25A      "),
            (0x906A, 2,  "uint",  "          @ 25-30A      "),
            (0x906C, 2,  "uint",  "          @ 30-35A      "),
            (0x906E, 2,  "uint",  "          @ 35-40A      "),
            (0x9070, 2,  "uint",  "          @ 40-45A      "),
            (0x9072, 2,  "uint",  "          @ 45-50A      "),
            (0x9074, 2,  "uint",  "          @ 50-55A      "),
            (0x9076, 2,  "uint",  "          @ 55-60A      "),
            (0x9078, 2,  "uint",  "          @ 60-65A      "),
            (0x907A, 2,  "uint",  "          @ 65-70A      "),
            (0x907C, 2,  "uint",  "          @ 70-65A      "),
            (0x907E, 2,  "uint",  "          @ 75-80A      "),
            (0x9080, 2,  "uint",  "          @ 80-85A      "),
            (0x9082, 2,  "uint",  "          @ 85-90A      "),
            (0x9084, 2,  "uint",  "          @ 90-95A      "),
            (0x9086, 2,  "uint",  "          @ 95-100A     "),
            (0x9088, 2,  "uint",  "          @ 100-105A    "),
            (0x908A, 2,  "uint",  "          @ 105-110A    "),
            (0x908C, 2,  "uint",  "          @ 110-115A    "),
            (0x908E, 2,  "uint",  "          @ 115-120A    "),
            (0x9090, 2,  "uint",  "          @ 120-125A    "),
            (0x9092, 2,  "uint",  "          @ 125-130A    "),
            (0x9094, 2,  "uint",  "          @ 130-135A    "),
            (0x9096, 2,  "uint",  "          @ 135-140A    "),
            (0x9098, 2,  "uint",  "          @ 140-145A    "),
            (0x909A, 2,  "uint",  "          @ 145-150A    "),
            (0x909C, 2,  "uint",  "          @ 150-155A    "),
            (0x909E, 2,  "uint",  "          @ 155-160A    "),
            (0x90A0, 2,  "uint",  "          @ 160-165A    "),
            (0x90A2, 2,  "uint",  "          @ 165-170A    "),
            (0x90A4, 2,  "uint",  "          @ 170-175A    "),
            (0x90A6, 2,  "uint",  "          @ 175-180A    "),
            (0x90A8, 2,  "uint",  "          @ 180-185A    "),
            (0x90AA, 2,  "uint",  "          @ 185-190A    "),
            (0x90AC, 2,  "uint",  "          @ 190-195A    "),
            (0x90AE, 2,  "uint",  "          @ 195-200A    "),
            (0x90B0, 2,  "uint",  "          @ 200A+       "),
            (0x90B2, 2,  "uint",  "Charge started < 17V"),
            (0x90B4, 2,  "uint",  "Charge started 17-18V"),
            (0x90B6, 2,  "uint",  "Charge started 18-19V"),
            (0x90B8, 2,  "uint",  "Charge started 19-20V"),
            (0x90BA, 2,  "uint",  "Charge started 20V+"),
            (0x90BC, 2,  "uint",  "Charge ended < 17V"),
            (0x90BE, 2,  "uint",  "Charge ended 17-18V"),
            (0x90C0, 2,  "uint",  "Charge ended 18-19V"),
            (0x90C2, 2,  "uint",  "Charge ended 19-20V"),
            (0x90C4, 2,  "uint",  "Charge ended 20V+"),
            (0x90C6, 2,  "uint",  "Charge start temp -30C to -20C"),
            (0x90C8, 2,  "uint",  "Charge start temp -20C to -10C"),
            (0x90CA, 2,  "uint",  "Charge start temp -10C to 0C"),
            (0x90CC, 2,  "uint",  "Charge start temp 0C to +10C"),
            (0x90CE, 2,  "uint",  "Charge start temp +10C to +20C"),
            (0x90D0, 2,  "uint",  "Charge start temp +20C to +30C"),
            (0x90D2, 2,  "uint",  "Charge start temp +30C to +40C"),
            (0x90D4, 2,  "uint",  "Charge start temp +40C to +50C"),
            (0x90D6, 2,  "uint",  "Charge start temp +50C to +60C"),
            (0x90D8, 2,  "uint",  "Charge start temp +60C to +70C"),
            (0x90DA, 2,  "uint",  "Charge start temp +70C to +80C"),
            (0x90DC, 2,  "uint",  "Charge start temp +80C and over"),
            (0x90DE, 2,  "uint",  "Charge end temp -30C to -20C"),
            (0x90E0, 2,  "uint",  "Charge end temp -20C to -10C"),
            (0x90E2, 2,  "uint",  "Charge end temp -10C to 0C"),
            (0x90E4, 2,  "uint",  "Charge end temp 0C to +10C"),
            (0x90E6, 2,  "uint",  "Charge end temp +10C to +20C"),
            (0x90E8, 2,  "uint",  "Charge end temp +20C to +30C"),
            (0x90EA, 2,  "uint",  "Charge end temp +30C to +40C"),
            (0x90EC, 2,  "uint",  "Charge end temp +40C to +50C"),
            (0x90EE, 2,  "uint",  "Charge end temp +50C to +60C"),
            (0x90F0, 2,  "uint",  "Charge end temp +60C to +70C"),
            (0x90F2, 2,  "uint",  "Charge end temp +70C to +80C"),
            (0x90F4, 2,  "uint",  "Charge end temp +80C and over"),
            (0xA000, 6,  "uint",  "Unknown (Forge)")
        };

        // -------------------------------------
        // Helper exception to mirror Python usage
        // -------------------------------------
        private class ValueError : Exception
        {
            public ValueError(string message) : base(message) { }
        }

        // -------------------------------------
        // Debug byte printer (identical format)
        // -------------------------------------
        public static void print_debug_bytes(IEnumerable<byte> data)
        {
            var dataPrint = string.Join(" ", GetHex(data));
            Console.WriteLine("DEBUG: " + dataPrint);
        }

        private static IEnumerable<string> GetHex(IEnumerable<byte> bytes)
        {
            foreach (var b in bytes)
            {
                yield return $"{b:02X}";
            }
        }

        // -------------------------------------
        // Constructor mirroring __init__ in m18.py
        // -------------------------------------
        public M18Protocol(SerialPortDisplay device, Action<string>? rawLogger = null)
        {
            RawLogger = rawLogger;
            port = new SerialPortConnection(device, RawLogger);
            idle();
        }

        // -------------------------------------
        // Logging flag helpers (match Python naming and behaviour)
        // -------------------------------------
        public void txrx_print(bool enable = true)
        {
            PRINT_TX = enable;
            PRINT_RX = enable;
        }

        public void txrx_save_and_set(bool enable = true)
        {
            PRINT_TX_SAVE = PRINT_TX;
            PRINT_RX_SAVE = PRINT_RX;
            txrx_print(enable);
        }

        public void txrx_restore()
        {
            PRINT_TX = PRINT_TX_SAVE;
            PRINT_RX = PRINT_RX_SAVE;
        }

        // -------------------------------------
        // Reset: identical control-line toggles, sleeps, send, and idle logic
        // -------------------------------------
        public bool reset()
        {
            ACC = 4;
            port.SetBreak(true);
            port.SetDtr(true);
            Thread.Sleep(300);
            port.SetBreak(false);
            port.SetDtr(false);
            Thread.Sleep(300);
            send(new[] { SYNC_BYTE });
            try
            {
                var response = read_response(1);
                Thread.Sleep(10);
                if (response.Length > 0 && response[0] == SYNC_BYTE)
                {
                    return true;
                }
                Console.WriteLine($"Unexpected response: {string.Join(" ", GetHex(response))}");
                return false;
            }
            catch (ValueError)
            {
                return false;
            }
        }

        // -------------------------------------
        // Utility helpers mapped directly
        // -------------------------------------
        public void update_acc()
        {
            var accValues = new List<int> { 0x04, 0x0C, 0x1C };
            var currentIndex = accValues.IndexOf(ACC);
            var nextIndex = (currentIndex + 1) % accValues.Count;
            ACC = accValues[nextIndex];
        }

        public bool PrintTx
        {
            get => PRINT_TX;
            set => PRINT_TX = value;
        }

        public bool PrintRx
        {
            get => PRINT_RX;
            set => PRINT_RX = value;
        }

        public int reverse_bits(int value)
        {
            return Convert.ToInt32(string.Concat(Convert.ToString(value & 0xFF, 2).PadLeft(8, '0').Reverse()), 2);
        }

        public int checksum(IEnumerable<byte> payload)
        {
            var cksum = 0;
            foreach (var b in payload)
            {
                cksum += b & 0xFFFF;
            }
            return cksum;
        }

        public byte[] add_checksum(List<byte> lsbCommand)
        {
            var cksum = checksum(lsbCommand);
            lsbCommand.AddRange(BitConverter.GetBytes((ushort)cksum).Reverse());
            return lsbCommand.ToArray();
        }

        // -------------------------------------
        // Send path (input buffer reset + bit reversal + optional log)
        // -------------------------------------
        public void send(byte[] command)
        {
            port.PurgeRx();
            var logBytes = command.ToArray();
            var msb = command.Select(b => (byte)reverse_bits(b)).ToArray();

            if (PRINT_TX)
            {
                LogTx("Sending:  " + BitConverter.ToString(logBytes).Replace("-", " "));
            }

            port.WriteBytes(msb);
        }

        public void send_command(byte[] command)
        {
            send(add_checksum(new List<byte>(command)));
        }

        // -------------------------------------
        // Read path (exact ordering and timing)
        // -------------------------------------
        public byte[] read_response(int size)
        {
            var msbResponse = new List<byte>();
            var first = ReadOneByte();
            msbResponse.Add(first);
            if (reverse_bits(first) == 0x82)
            {
                msbResponse.Add(ReadOneByte());
            }
            else
            {
                for (var i = 0; i < size - 1; i++)
                {
                    msbResponse.Add(ReadOneByte());
                }
            }

            var lsbResponse = new List<byte>();
            foreach (var b in msbResponse)
            {
                lsbResponse.Add((byte)reverse_bits(b));
            }

            var debugPrint = string.Join(" ", GetHex(lsbResponse));
            if (PRINT_RX)
            {
                LogRx($"Received: {debugPrint}");
            }
            Thread.Sleep(50);
            return lsbResponse.ToArray();
        }

        private void LogTx(string message)
        {
            if (TxLogger != null)
            {
                TxLogger(message);
                return;
            }

            Console.WriteLine(message);
        }

        private void LogRx(string message)
        {
            if (RxLogger != null)
            {
                RxLogger(message);
                return;
            }

            Console.WriteLine(message);
        }

        private byte ReadOneByte()
        {
            try
            {
                var buffer = port.ReadBytes(1);
                if (buffer.Length < 1)
                {
                    throw new ValueError("Empty response");
                }

                return buffer[0];
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                throw new ValueError(ex.Message);
            }
        }

        // -------------------------------------
        // Protocol commands
        // -------------------------------------
        public byte[] configure(byte state)
        {
            ACC = 4;
            var cmd = new List<byte>();
            cmd.Add(CONF_CMD);
            cmd.Add((byte)ACC);
            cmd.Add(8);
            cmd.AddRange(BitConverter.GetBytes((ushort)CUTOFF_CURRENT).Reverse());
            cmd.AddRange(BitConverter.GetBytes((ushort)MAX_CURRENT).Reverse());
            cmd.AddRange(BitConverter.GetBytes((ushort)MAX_CURRENT).Reverse());
            cmd.Add(state);
            cmd.Add(13);
            var cmdBytes = cmd.ToArray();
            send_command(cmdBytes);
            return read_response(5);
        }

        public byte[] get_snapchat()
        {
            var cmd = new List<byte> { SNAP_CMD, (byte)ACC, 0 };
            var cmdBytes = cmd.ToArray();
            send_command(cmdBytes);
            update_acc();
            return read_response(8);
        }

        public byte[] keepalive()
        {
            var cmd = new List<byte> { KEEPALIVE_CMD, (byte)ACC, 0 };
            var cmdBytes = cmd.ToArray();
            send_command(cmdBytes);
            return read_response(9);
        }

        public byte[] calibrate()
        {
            var cmd = new List<byte> { CAL_CMD, (byte)ACC, 0 };
            var cmdBytes = cmd.ToArray();
            send_command(cmdBytes);
            update_acc();
            return read_response(8);
        }

        // -------------------------------------
        // Charger simulation routines
        // -------------------------------------
        public void simulate()
        {
            Console.WriteLine("Simulating charger communication");
            txrx_save_and_set(true);

            reset();

            configure(2);
            get_snapchat();
            Thread.Sleep(600);
            keepalive();
            configure(1);
            get_snapchat();
            try
            {
                while (true)
                {
                    Thread.Sleep(500);
                    keepalive();
                }
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("\nSimulation aborted by user. Exiting gracefully...");
            }
            finally
            {
                idle();
            }

            txrx_restore();
        }

        public void simulate_for(double duration)
        {
            Console.WriteLine($"Simulating charger communication for {duration} seconds...");
            var beginTime = DateTime.UtcNow;
            reset();
            configure(2);
            get_snapchat();
            Thread.Sleep(600);
            keepalive();
            configure(1);
            get_snapchat();
            try
            {
                while ((DateTime.UtcNow - beginTime).TotalSeconds < duration)
                {
                    Thread.Sleep(500);
                    keepalive();
                }
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("\nSimulation aborted by user. Exiting gracefully...");
            }
            finally
            {
                idle();
                Console.WriteLine("Duration: " + (DateTime.UtcNow - beginTime).TotalSeconds);
            }
        }

        // -------------------------------------
        // Debug helpers
        // -------------------------------------
        public void debug(byte a, byte b, byte c, int length)
        {
            var rxDebug = PRINT_RX;
            var txDebug = PRINT_TX;
            PRINT_TX = false;
            PRINT_RX = false;

            reset();
            PRINT_TX = txDebug;
            var data = cmd(a, b, c, length);
            var dataPrint = string.Join(" ", GetHex(data));
            Console.WriteLine($"Response from: 0x{(a * 0x100 + b):04X}: {dataPrint}");
            idle();
            PRINT_RX = rxDebug;
        }

        public void try_cmd(byte cmdByte, byte msb, byte lsb, byte length, int retLen = 0)
        {
            txrx_save_and_set(false);
            if (retLen == 0)
            {
                retLen = length + 5;
            }

            reset();
            var cmdBuf = new List<byte> { cmdByte, 0x04, 0x03, msb, lsb, length };
            var cmdBytes = cmdBuf.ToArray();
            send_command(cmdBytes);
            var data = read_response(retLen);
            var dataPrint = string.Join(" ", GetHex(data));
            Console.WriteLine($"Response from: 0x{(msb * 0x100 + lsb):04X}: {dataPrint}");
            idle();
            txrx_restore();
        }

        public byte[] cmd(int a, int b, int c, int length, byte command = 0x01)
        {
            var cmdBuf = new List<byte> { command, 0x04, 0x03, (byte)a, (byte)b, (byte)c };
            var cmdBytes = cmdBuf.ToArray();
            send_command(cmdBytes);
            return read_response(length);
        }

        public void brute(int a, int b, int length = 0xFF, byte command = 0x01)
        {
            reset();
            try
            {
                for (var i = 0; i < length; i++)
                {
                    var ret = cmd(a, b, i, i + 5, command);
                    if (ret[0] == 0x81)
                    {
                        var dataPrint = string.Join(" ", GetHex(ret));
                        Console.WriteLine($"Valid response from: 0x{(a * 0x100 + b):04X} with length: 0x{i:02X}: {dataPrint}");
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("\nSimulation aborted by user. Exiting gracefully...");
            }
            finally
            {
                idle();
            }
        }

        public void full_brute(int start = 0, int stop = 0xFFFF, int length = 0xFF)
        {
            var addr = 0;
            try
            {
                for (addr = start; addr < stop; addr++)
                {
                    var msb = (addr >> 8) & 0xFF;
                    var lsb = addr & 0xFF;
                    brute(msb, lsb, length, 0x01);
                    if ((addr % 256) == 0)
                    {
                        Console.WriteLine($"addr = 0x{addr:04X} " + DateTime.Now);
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("\nSimulation aborted by user. Exiting gracefully...");
                Console.WriteLine($"\nStopped at address: 0x{addr:04X}");
            }
            finally
            {
                idle();
            }
        }

        public byte[] wcmd(byte a, byte b, byte c, int length)
        {
            var cmdBuf = new List<byte> { 0x01, 0x05, 0x03, a, b, c };
            var cmdBytes = cmdBuf.ToArray();
            send_command(cmdBytes);
            return read_response(length);
        }

        public void write_message(string message)
        {
            try
            {
                if (message.Length > 0x14)
                {
                    Console.WriteLine("ERROR: Message too long!");
                    return;
                }
                Console.WriteLine($"Writing \"{message}\" to memory");
                reset();
                message = message.PadRight(0x14, '-');
                for (var i = 0; i < message.Length; i++)
                {
                    wcmd(0, (byte)(0x23 + i), (byte)message[i], 2);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"write_message: Failed with error: {e}");
            }
        }

        // -------------------------------------
        // Control-line helpers
        // -------------------------------------
        public void idle()
        {
            port.SetBreak(true);
            port.SetDtr(true);
        }

        public void high()
        {
            port.SetBreak(false);
            port.SetDtr(false);
        }

        public void high_for(double duration)
        {
            high();
            Thread.Sleep((int)(duration * 1000));
            idle();
        }

        // -------------------------------------
        // Calculations and conversions
        // -------------------------------------
        public double calculate_temperature(int adcValue)
        {
            var r1 = 10e3;
            var r2 = 20e3;
            var t1 = 50;
            var t2 = 35;

            var adc1 = 0x0180;
            var adc2 = 0x022E;

            var m = (t2 - t1) / (r2 - r1);
            var b = t1 - m * r1;

            var resistance = r1 + (adcValue - adc1) * (r2 - r1) / (adc2 - adc1);
            var temperature = m * resistance + b;

            return Math.Round(temperature, 2);
        }

        private ulong BigEndianToUInt(byte[] data)
        {
            ulong value = 0;
            foreach (var b in data)
            {
                value = (value << 8) + b;
            }
            return value;
        }

        public DateTime bytes2dt(byte[] timeBytes)
        {
            var epoch = (long)BigEndianToUInt(timeBytes);
            var dt = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
            return dt;
        }

        // -------------------------------------
        // Bulk read helpers
        // -------------------------------------
        public void read_all()
        {
            try
            {
                reset();
                foreach (var entry in data_matrix)
                {
                    var response = cmd(entry.Item1, entry.Item2, entry.Item3, entry.Item3 + 5);
                    if (response.Length >= 4 && response[0] == 0x81)
                    {
                        var data = new byte[entry.Item3];
                        Array.Copy(response, 3, data, 0, entry.Item3);
                        var dataPrint = string.Join(" ", GetHex(data));
                        Console.WriteLine($"Response from: 0x{(entry.Item1 * 0x100 + entry.Item2):04X}: {dataPrint}");
                    }
                    else
                    {
                        var dataPrint = string.Join(" ", GetHex(response));
                        Console.WriteLine($"Invalid response from: 0x{(entry.Item1 * 0x100 + entry.Item2):04X} Response: {dataPrint}");
                    }
                }
                idle();
            }
            catch (Exception e)
            {
                Console.WriteLine($"read_all: Failed with error: {e}");
            }
        }

        public object? read_id(IEnumerable<int>? id_array = null, bool force_refresh = true, string output = "label")
        {
            if (id_array == null || !id_array.Any())
            {
                id_array = Enumerable.Range(0, data_id.Count);
            }

            if (!((output == "label") || (output == "raw") || (output == "array") || (output == "form")))
            {
                Console.WriteLine($"Unrecognised 'output' = {output}. Please choose \"label\", \"raw\", or \"array\"");
                output = "label";
            }

            var array = new List<object?>();

            try
            {
                reset();

                if (force_refresh)
                {
                    foreach (var entry in data_matrix)
                    {
                        cmd(entry.Item1, entry.Item2, entry.Item3, entry.Item3 + 5);
                    }
                    idle();
                    Thread.Sleep(100);
                }

                var now = DateTime.Now;
                var formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                if (output == "label")
                {
                    Console.WriteLine(formattedTime);
                    Console.WriteLine("ID  ADDR   LEN TYPE       LABEL                                   VALUE");
                }
                else if (output == "raw")
                {
                    Console.WriteLine(formattedTime);
                }
                else if (output == "form")
                {
                    array.Add(formattedTime);
                }

                reset();
                foreach (var i in id_array)
                {
                    var entry = data_id[i];
                    var addr = entry.Item1;
                    var addr_h = (addr >> 8) & 0xFF;
                    var addr_l = addr & 0xFF;
                    var length = entry.Item2;
                    var data_type = entry.Item3;
                    var label = entry.Item4;

                    var response = cmd(addr_h, addr_l, length, length + 5);
                    object? array_value;
                    object? value;

                    if (response.Length >= 4 && response[0] == 0x81)
                    {
                        var data = new byte[length];
                        Array.Copy(response, 3, data, 0, length);
                        switch (data_type)
                        {
                            case "uint":
                                array_value = value = BigEndianToUInt(data);
                                break;
                            case "date":
                                var dt = bytes2dt(data);
                                array_value = dt;
                                value = dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                break;
                            case "hhmmss":
                                var dur = (int)BigEndianToUInt(data);
                                var mm = dur / 60;
                                var ss = dur % 60;
                                var hh = mm / 60;
                                mm = mm % 60;
                                array_value = value = $"{hh}:{mm:00}:{ss:00}";
                                break;
                            case "ascii":
                                var str = Encoding.UTF8.GetString(data);
                                array_value = value = $"\"{str}\"";
                                break;
                            case "sn":
                                var btype = (data[0] << 8) + data[1];
                                var serial = (data[2] << 16) + (data[3] << 8) + data[4];
                                if (output == "label" || output == "array")
                                {
                                    array_value = value = $"Type: {btype:3d}, Serial: {serial:d}";
                                }
                                else
                                {
                                    value = $"{btype}\n{serial}";
                                    array_value = value;
                                }
                                break;
                            case "adc_t":
                                var adcTemp = calculate_temperature((data[0] << 8) + data[1]);
                                array_value = value = adcTemp;
                                break;
                            case "dec_t":
                                var temp = data[0] + data[1] / 256.0;
                                array_value = value = temp.ToString("0.00", CultureInfo.InvariantCulture);
                                break;
                            case "cell_v":
                                var cv = new List<int>();
                                for (var idx = 0; idx < 10; idx += 2)
                                {
                                    cv.Add((data[idx] << 8) + data[idx + 1]);
                                }
                                array_value = cv;
                                if (output == "label")
                                {
                                    value = $"1: {cv[0]:4d}, 2: {cv[1]:4d}, 3: {cv[2]:4d}, 4: {cv[3]:4d}, 5: {cv[4]:4d}";
                                }
                                else
                                {
                                    value = $"{cv[0]:4d}\n{cv[1]:4d}\n{cv[2]:4d}\n{cv[3]:4d}\n{cv[4]:4d}";
                                }
                                break;
                            default:
                                array_value = null;
                                value = null;
                                break;
                        }
                    }
                    else
                    {
                        array_value = null;
                        value = "------";
                    }

                    if (output == "label")
                    {
                        Console.WriteLine($"{i:3d} 0x{addr:04X} {length:2d} {data_type,6}   {label,-39} {value}");
                    }
                    else if (output == "raw")
                    {
                        Console.WriteLine(value);
                    }
                    else if (output == "array")
                    {
                        array.Add(new List<object?> { i, array_value });
                    }
                    else if (output == "form")
                    {
                        array.Add(value);
                    }
                }

                if ((output == "array" || output == "form") && array.Count > 0)
                {
                    idle();
                    return array;
                }

                idle();
            }
            catch (Exception e)
            {
                Console.WriteLine($"read_id: Failed with error: {e}");
            }

            return null;
        }

        public void read_all_spreadsheet()
        {
            try
            {
                reset();
                foreach (var entry in data_matrix)
                {
                    cmd(entry.Item1, entry.Item2, entry.Item3, entry.Item3 + 5);
                }
                idle();
                Thread.Sleep(500);

                reset();
                var now = DateTime.Now;
                var formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                Console.WriteLine(formattedTime);

                foreach (var entry in data_matrix)
                {
                    var response = cmd(entry.Item1, entry.Item2, entry.Item3, entry.Item3 + 5);
                    if (response.Length >= 4 && response[0] == 0x81)
                    {
                        var data = new byte[entry.Item3];
                        Array.Copy(response, 3, data, 0, entry.Item3);
                        Console.WriteLine($"0x{(entry.Item1 * 0x100 + entry.Item2):04X}");
                        if (data.Length == 0)
                        {
                            Console.WriteLine("EMPTY");
                        }
                        else
                        {
                            var dataPrint = string.Join("\n", data);
                            Console.WriteLine(dataPrint);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"0x{(entry.Item1 * 0x100 + entry.Item2):04X}");
                        var dataPrint = string.Join(" ", GetHex(response));
                        Console.WriteLine($"INV: {dataPrint}");
                        for (var i = 1; i < entry.Item3; i++)
                        {
                            Console.WriteLine("blank");
                        }
                    }
                }

                idle();
            }
            catch (Exception e)
            {
                Console.WriteLine($"read_all_spreadsheet: Failed with error: {e}");
            }
        }

        public void health(bool force_refresh = true)
        {
            var reg_list = new List<int>
            {
                4,
                28,
                25,
                26,
                12,
                13,
                18,
                29,
                39,
                40,
                41,
                42,
                43,
                33, 32, 31,
                35,
                36,
                38
            };
            reg_list.AddRange(Enumerable.Range(44, 20));
            reg_list.AddRange(new List<int> { 8, 2 });

            txrx_save_and_set(true);

            try
            {
                Console.WriteLine("Reading battery. This will take 5-10sec\n");
                var array = read_id(reg_list, force_refresh, "array") as List<object?>;
                if (array == null)
                {
                    txrx_restore();
                    return;
                }

                var sn = array[40] as List<object?>;
                var snValue = sn?[1]?.ToString() ?? string.Empty;
                var numbers = Regex.Matches(snValue, @"\d+\.?\d*");
                var bat_type = numbers.Count > 0 ? numbers[0].Value : "";
                var e_serial = numbers.Count > 1 ? numbers[1].Value : "";
                var bat_lookup = new Dictionary<string, (double, string)>
                {
                    { "36", (1.5, "1.5Ah CP (5s1p 18650)") },
                    { "37", (2, "2Ah CP (5s1p 18650)") },
                    { "38", (3, "3Ah XC (5s2p 18650)") },
                    { "39", (4, "4Ah XC (5s2p 18650)") },
                    { "40", (5, "5Ah XC (5s2p 18650) (<= Dec 2018)") },
                    { "165", (5, "5Ah XC (5s2p 18650) (Aug 2019 - Jun 2021)") },
                    { "306", (5, "5Ah XC (5s2p 18650) (Feb 2021 - Jul 2023)") },
                    { "424", (5, "5Ah XC (5s2p 18650) (>= Sep 2023)") },
                    { "46", (6, "6Ah XC (5s2p 18650)") },
                    { "47", (9, "9Ah HD (5s3p 18650)") },
                    { "104", (3, "3Ah HO (5s1p 21700)") },
                    { "150", (6, "5.5Ah HO (5s2p 21700) (EU only)") },
                    { "106", (6, "6Ah HO (5s2p 21700)") },
                    { "107", (8, "8Ah HO (5s2p 21700)") },
                    { "108", (12, "12Ah HO (5s3p 21700)") },
                    { "383", (8, "8Ah Forge (5s2p 21700 tabless)") },
                    { "384", (12, "12Ah Forge (5s3p 21700 tabless)") }
                };
                var bat_text = bat_lookup.ContainsKey(bat_type) ? bat_lookup[bat_type] : (0, "Unknown");
                Console.WriteLine($"Type: {bat_type} [{bat_text.Item2}]");
                Console.WriteLine($"E-serial: {e_serial} (does NOT match case serial)");

                var bat_now = (array[39] as List<object?>)?[1] as DateTime? ?? DateTime.Now;

                Console.WriteLine("Manufacture date: " + ((array[0] as List<object?>)?[1] as DateTime?)?.ToString("yyyy-MM-dd"));
                Console.WriteLine("Days since 1st charge: " + ((array[1] as List<object?>)?[1] ?? ""));
                Console.WriteLine("Days since last tool use: " + (bat_now - (((array[2] as List<object?>)?[1] as DateTime?) ?? bat_now)).Days);
                Console.WriteLine("Days since last charge: " + (bat_now - (((array[3] as List<object?>)?[1] as DateTime?) ?? bat_now)).Days);

                var cvList = (array[4] as List<object?>)?[1] as List<int> ?? new List<int>();
                Console.WriteLine($"Pack voltage: {cvList.Sum() / 1000.0}");
                Console.WriteLine("Cell Voltages (mV): " + string.Join(", ", cvList));
                Console.WriteLine($"Cell Imbalance (mV): {(cvList.Count > 0 ? cvList.Max() - cvList.Min() : 0)}");
                if ((array[5] as List<object?>)?[1] != null)
                {
                    Console.WriteLine($"Temperature (deg C): {(array[5] as List<object?>)?[1]}");
                }
                if ((array[6] as List<object?>)?[1] != null)
                {
                    Console.WriteLine($"Temperature (deg C): {(array[6] as List<object?>)?[1]}");
                }

                Console.WriteLine("\nCHARGING STATS:");
                Console.WriteLine($"Charge count [Redlink, dumb, (total)]: {(array[13] as List<object?>)?[1]}, {(array[14] as List<object?>)?[1]}, ({(array[15] as List<object?>)?[1]})");
                Console.WriteLine($"Total charge time: {(array[16] as List<object?>)?[1]}");
                Console.WriteLine($"Time idling on charger: {(array[17] as List<object?>)?[1]}");
                Console.WriteLine($"Low-voltage charges (any cell <2.5V): {(array[18] as List<object?>)?[1]}");

                Console.WriteLine("\nTOOL USE STATS:");
                var totalDischarge = Convert.ToDouble((array[7] as List<object?>)?[1] ?? 0);
                Console.WriteLine("Total discharge (Ah): " + (totalDischarge / 3600).ToString("0.00", CultureInfo.InvariantCulture));
                string total_discharge_cycles;
                if (bat_text.Item1 != 0)
                {
                    total_discharge_cycles = (totalDischarge / 3600 / bat_text.Item1).ToString("0.00", CultureInfo.InvariantCulture);
                }
                else
                {
                    total_discharge_cycles = "Unknown battery type, unable to calculate";
                }
                Console.WriteLine($"Total discharge cycles: {total_discharge_cycles}");
                Console.WriteLine($"Times discharged to empty: {(array[8] as List<object?>)?[1]}");
                Console.WriteLine($"Times overheated: {(array[9] as List<object?>)?[1]}");
                Console.WriteLine($"Overcurrent events: {(array[10] as List<object?>)?[1]}");
                Console.WriteLine($"Low-voltage events: {(array[11] as List<object?>)?[1]}");
                Console.WriteLine($"Low-voltage bounce/stutter: {(array[12] as List<object?>)?[1]}");

                var tool_time = 0;
                for (var i = 19; i < 39; i++)
                {
                    tool_time += Convert.ToInt32((array[i] as List<object?>)?[1] ?? 0);
                }

                Console.WriteLine($"Total time on tool (>10A): {TimeSpan.FromSeconds(tool_time)}");

                var j = 0;
                for (var idx = 19; idx < 38; idx++)
                {
                    j = idx;
                    var amp_range = $"{(idx - 18) * 10}-{(idx - 17) * 10}A";
                    var label = $"Time @ {amp_range,8}:";
                    var t = Convert.ToInt32((array[idx] as List<object?>)?[1] ?? 0);
                    var hhmmss = TimeSpan.FromSeconds(t);
                    var pct = tool_time != 0 ? Math.Round((t / (double)tool_time) * 100) : 0;
                    var bar = new string('X', (int)Math.Round(pct));
                    Console.WriteLine($"{label} {hhmmss} {pct:00}% {bar}");
                }
                j += 1;
                var lastAmp = "> 200A";
                var lastLabel = $"Time @ {lastAmp,8}:";
                var lastT = Convert.ToInt32((array[j] as List<object?>)?[1] ?? 0);
                var lastHhmmss = TimeSpan.FromSeconds(lastT);
                var lastPct = tool_time != 0 ? Math.Round((lastT / (double)tool_time) * 100) : 0;
                var lastBar = new string('X', (int)Math.Round(lastPct));
                Console.WriteLine($"{lastLabel} {lastHhmmss} {lastPct:00}% {lastBar}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"health: Failed with error: {e}");
                Console.WriteLine("Check battery is connected and you have correct serial port");
            }

            txrx_restore();
        }

        // -------------------------------------
        // Form submission (synchronous to match Python flow)
        // -------------------------------------
        public void submit_form()
        {
            Console.WriteLine("Please provide this information. All the values can be found on the label under the battery.");
            Console.Write("Enter One-Key ID (example: H18FDCAD): ");
            var one_key_id = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Date (example: 190316): ");
            var date = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Serial number (example: 0807426): ");
            var serial_number = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Sticker (example: 4932 4512 45): ");
            var sticker = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Type (example: M18B9): ");
            var model_type = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Capacity (example: 9.0Ah): ");
            var capacity = Console.ReadLine() ?? string.Empty;

            submit_form(one_key_id, date, serial_number, sticker, model_type, capacity);
        }

        public void submit_form(string one_key_id, string date, string serial_number, string sticker, string model_type, string capacity)
        {
            const string formUrl = "https://docs.google.com/forms/d/e/1FAIpQLScvTbSDYBzSQ8S4XoF-rfgwNj97C-Pn4Px3GIixJxf0C1YJJA/formResponse";
            Console.WriteLine("Getting data from battery...");
            var output = read_id(output: "form") as List<object?>;

            if (output == null)
            {
                Console.WriteLine("submit_form: No output returned, aborting");
                return;
            }
            var s_output = string.Join("\n", output);

            var formData = new Dictionary<string, string>
            {
                { "entry.905246449", one_key_id },
                { "entry.453401884", date },
                { "entry.2131879277", serial_number },
                { "entry.337435885", sticker },
                { "entry.1496274605", model_type },
                { "entry.324224550", capacity },
                { "entry.716337020", s_output }
            };

            using var client = new HttpClient();
            var response = client.PostAsync(formUrl, new FormUrlEncodedContent(formData)).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Form submitted successfully!");
            }
            else
            {
                Console.WriteLine($"submit_form: Failed to submit form. Status code: {(int)response.StatusCode}");
            }
        }

        // -------------------------------------
        // Help text (unchanged)
        // -------------------------------------
        public void help()
        {
            Console.WriteLine("Functions: \n" +
                "DIAGNOSTICS: \n" +
                "m.health() - print simple health report on battery \n" +
                "m.read_id() - print labelled and formatted diagnostics \n" +
                "m.read_id(output=\"raw\") - print in spreadsheet format \n" +
                "m.submit_form() - prompts for manual inputs and submits battery diagnostics data \n" +
                "\n" +
                "m.help() - this message\n" +
                "m.adv_help() - advanced help\n" +
                "\n" +
                "exit() - end program\n");
        }

        public void adv_help()
        {
            Console.WriteLine("Advanced functions: \n" +
                "m.read_all() - print all known bytes in 0x01 command \n" +
                "m.read_all_spreadsheet() - print bytes in spreadsheet format \n" +
                "\n" +
                "CHARGING SIMULATION: \n" +
                "m.simulate() - simulate charging comms \n" +
                "m.simulate_for(t) - simulate for t seconds \n" +
                "m.high_for(t) - bring J2 high for t sec, then idle \n" +
                "\n" +
                "m.write_message(message) - write ascii string to 0x0023 register (20 chars)\n" +
                "\n" +
                "Debug: \n" +
                "m.PRINT_TX = True - boolean to enable TX messages \n" +
                "m.PRINT_RX = True - boolean to enable RX messages \n" +
                "m.txrx_print(bool) - set PRINT_TX & RX to bool \n" +
                "m.txrx_save_and_set(bool) - save PRINT_TX & RX state, then set both to bool \n" +
                "m.txrx_restore() - restore PRINT_TX & RX to saved values \n" +
                "m.brute(addr_msb, addr_lsb) \n" +
                "m.full_brute(start, stop, len) - check registers from 'start' to 'stop'. look for 'len' bytes \n" +
                "m.debug(addr_msb, addr_lsb, len, rsp_len) - send reset() then cmd() to battery \n" +
                "m.try_cmd(cmd, addr_h, addr_l, len) - try 'cmd' at [addr_h addr_l] with 'len' bytes \n" +
                "\n" +
                "Internal:\n" +
                "m.high() - bring J2 pin high (20V)\n" +
                "m.idle() - pull J2 pin low (0V) \n" +
                "m.reset() - send 0xAA to battery. Return true if battery replies wih 0xAA \n" +
                "m.get_snapchat() - request 'snapchat' from battery (0x61)\n" +
                "m.configure() - send 'configure' message (0x60, charger parameters)\n" +
                "m.calibrate() - calibration/interrupt command (0x55) \n" +
                "m.keepalive() - send charge current request (0x62) \n");
        }
    }
}
