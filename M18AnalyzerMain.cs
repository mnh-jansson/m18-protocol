using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace M18BatteryInfo;

public partial class frmMain : Form
{
    private static readonly object ConsoleCaptureLock = new();

    private M18Protocol? _protocol;
    private SerialPortDisplay? _selectedDevice;
    private CancellationTokenSource? _simulationCts;
    private Task? _simulationTask;
    private bool _busy;
    private bool _closing;
    private bool _hasAppendedLog;
    private bool _hasAppendedAdvancedLog;
    private bool _hasAppendedDebugLog;
    private bool _hasAppendedSerialLog;

    public frmMain()
    {
        InitializeComponent();
        WireEvents();
        InitializeDefaults();
        RunSelfChecks();
        UpdateConnectionUi();
    }

    private void WireEvents()
    {
        btnRefresh.Click += btnRefresh_Click;
        btnConnect.Click += btnConnect_Click;
        btnDisconnect.Click += btnDisconnect_Click;
        btnIdle.Click += btnIdle_Click;
        btnActive.Click += btnActive_Click;
        btnHealthReport.Click += btnHealthReport_Click;
        btnReset.Click += btnReset_Click;
        btnCopyOutput.Click += btnCopyOutput_Click;
        btnTestFT232.Click += btnTestFT232_Click;
        cmbBxSerialPort.SelectedIndexChanged += cmbBxSerialPort_SelectedIndexChanged;
        chkbxTXLog.CheckedChanged += chkbxTXLog_CheckedChanged;
        chkboxRxLog.CheckedChanged += chkboxRxLog_CheckedChanged;

        btnReadAllRegisters.Click += async (_, _) => await ExecuteProtocolAsync("Reading all registers", p => p.read_all(), AppendAdvancedLog);
        btnReadAllSpreadsheet.Click += async (_, _) => await ExecuteProtocolAsync("Reading spreadsheet output", p => p.read_all_spreadsheet(), AppendAdvancedLog);
        btnReadIDRaw.Click += async (_, _) => await ExecuteProtocolAsync("Reading raw battery identification", p => p.read_id(output: "raw"), AppendAdvancedLog);
        btnReadIDLabelled.Click += async (_, _) => await ExecuteProtocolAsync("Reading labelled battery identification", p => p.read_id(), AppendAdvancedLog);
        btnWriteMessage.Click += btnWriteMessage_Click;
        btnSaveTxRxState.Click += btnSaveTxRxState_Click;
        btnRestoreTxRxState.Click += btnRestoreTxRxState_Click;
        btnBruteAddr.Click += btnBruteAddr_Click;
        btnDebugCommand.Click += btnDebugCommand_Click;
        btnTryCommand.Click += btnTryCommand_Click;

        btnStartSim.Click += btnStartSim_Click;
        btnStopSim.Click += btnStopSim_Click;
        cmbBxChgProfile.SelectedIndexChanged += cmbBxChgProfile_SelectedIndexChanged;
        txtCutoffRaw.TextChanged += (_, _) => UpdateCurrentDisplay(txtCutoffRaw, txtCutoffAmps);
        txtMaxCurrRaw.TextChanged += (_, _) => UpdateCurrentDisplay(txtMaxCurrRaw, txtMaxCurrAmps);

        btnCollectDiagnostics.Click += btnCollectDiagnostics_Click;
        btnSubmitDiagForm.Click += btnSubmitDiagForm_Click;
        btnClearDiagForm.Click += btnClearDiagForm_Click;
        linkLabelKillaVolt.LinkClicked += (_, _) => OpenLink("https://github.com/KillaVolt");
        FormClosing += frmMain_FormClosing;
    }

    private void InitializeDefaults()
    {
        txtAddrMSB.Text = "0x00";
        txtAddrLSB.Text = "0x04";
        txtDebugCmdLength.Text = "5";
        txtResponseLength.Text = "10";
        txtCommand.Text = "0x01";
        txtAddressHigh.Text = "0x00";
        txtAddressLow.Text = "0x04";
        txtTryCmdLength.Text = "5";
        txtWriteMessage.Text = string.Empty;

        cmbBxBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBxBaudRate.Items.Add("4800");
        cmbBxBaudRate.SelectedIndex = 0;
        cmbBxChgProfile.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBxChgProfile.Items.AddRange(["Default", "Custom"]);
        cmbBxChgProfile.SelectedIndex = 0;
        txtSimDuration.Text = "30";
        txtCutoffRaw.Text = M18Protocol.DEFAULT_CUTOFF_CURRENT.ToString(CultureInfo.InvariantCulture);
        txtMaxCurrRaw.Text = M18Protocol.DEFAULT_MAX_CURRENT.ToString(CultureInfo.InvariantCulture);
        txtCutoffAmps.ReadOnly = true;
        txtMaxCurrAmps.ReadOnly = true;

        btnFullBrute.Enabled = false;
        toolTipSimpleTab.SetToolTip(btnFullBrute, "The discarded GUI did not provide a bounded, cancellable full-address scan.");
        toolTipSimpleTab.SetToolTip(btnTestFT232, "Open the selected port and verify the battery reset handshake.");
    }

    private void frmMain_Load(object sender, EventArgs e) => RefreshSerialPorts();

    private void btnRefresh_Click(object? sender, EventArgs e) => RefreshSerialPorts();

    private void RefreshSerialPorts()
    {
        try
        {
            var devices = SerialPortUtil.EnumerateDetailedPorts(AppendRawSerialLog);
            cmbBxSerialPort.Items.Clear();
            foreach (var device in devices)
            {
                cmbBxSerialPort.Items.Add(device);
                AppendDebugLog($"Found {device.DisplayName} (source: {device.Source}).");
            }

            if (devices.Count > 0)
            {
                cmbBxSerialPort.SelectedIndex = 0;
            }
            else
            {
                _selectedDevice = null;
                AppendLog("No serial ports detected.");
            }
        }
        catch (Exception ex)
        {
            LogError("Unable to enumerate serial ports", ex);
        }
    }

    private void cmbBxSerialPort_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedDevice = cmbBxSerialPort.SelectedItem as SerialPortDisplay;
    }

    private async void btnConnect_Click(object? sender, EventArgs e)
    {
        if (_selectedDevice == null)
        {
            MessageBox.Show("Select a serial port first.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        AppendLogBoth($"Opening {_selectedDevice.DisplayName} at 4800 baud, 8N2...");
        try
        {
            var protocol = await Task.Run(() => new M18Protocol(_selectedDevice, AppendRawSerialLog));
            protocol.TxLogger = AppendProtocolLog;
            protocol.RxLogger = AppendProtocolLog;
            protocol.PRINT_TX = chkbxTXLog.Checked;
            protocol.PRINT_RX = chkboxRxLog.Checked;
            _protocol = protocol;
            AppendLogBoth($"Connected to {_selectedDevice.DisplayName}.");
        }
        catch (Exception ex)
        {
            LogError($"Unable to open {_selectedDevice.DisplayName}", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void btnDisconnect_Click(object? sender, EventArgs e) => await DisconnectAsync();

    private async Task DisconnectAsync(bool announce = true)
    {
        _simulationCts?.Cancel();
        if (_simulationTask != null)
        {
            try
            {
                await _simulationTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        var protocol = _protocol;
        _protocol = null;
        if (protocol == null)
        {
            UpdateConnectionUi();
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                try
                {
                    protocol.idle();
                }
                finally
                {
                    protocol.port.Dispose();
                }
            });
            if (announce)
            {
                AppendLogBoth("Disconnected safely in the idle state.");
            }
        }
        catch (Exception ex)
        {
            LogError("Disconnect failed", ex);
        }
        finally
        {
            UpdateConnectionUi();
        }
    }

    private async void btnIdle_Click(object? sender, EventArgs e) => await SetLineStateAsync(true);

    private async void btnActive_Click(object? sender, EventArgs e) => await SetLineStateAsync(false);

    private async Task SetLineStateAsync(bool idle)
    {
        var protocol = RequireProtocol();
        if (protocol == null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            await Task.Run(() =>
            {
                if (idle)
                {
                    protocol.idle();
                }
                else
                {
                    protocol.high();
                }
            });
            AppendLogBoth(idle ? "TX set to idle (low)." : "TX set to active (high).");
        }
        catch (Exception ex)
        {
            LogError("Unable to change TX state", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void btnHealthReport_Click(object? sender, EventArgs e) =>
        await ExecuteProtocolAsync("Collecting health report", p => p.health(), AppendLog);

    private async void btnReset_Click(object? sender, EventArgs e) =>
        await ExecuteProtocolAsync("Resetting battery interface", p => Console.WriteLine(p.reset() ? "Reset acknowledged." : "Reset was not acknowledged."), AppendLog);

    private void btnCopyOutput_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(rtbOutput.Text))
        {
            Clipboard.SetText(rtbOutput.Text);
            AppendLog("Output copied to the clipboard.");
        }
    }

    private async void btnTestFT232_Click(object? sender, EventArgs e)
    {
        if (_selectedDevice == null)
        {
            MessageBox.Show("Select a serial port first.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            var acknowledged = await Task.Run(() =>
            {
                M18Protocol? protocol = null;
                try
                {
                    protocol = new M18Protocol(_selectedDevice, AppendRawSerialLog);
                    return protocol.reset();
                }
                finally
                {
                    if (protocol != null)
                    {
                        protocol.idle();
                        protocol.port.Dispose();
                    }
                }
            });
            AppendLogBoth(acknowledged ? "Battery reset handshake succeeded." : "The port opened, but the battery did not acknowledge reset.");
        }
        catch (Exception ex)
        {
            LogError("Device test failed", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void btnWriteMessage_Click(object? sender, EventArgs e)
    {
        var message = txtWriteMessage.Text;
        if (message.Length > 20)
        {
            MessageBox.Show("The battery note is limited to 20 characters.", "Write Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await ExecuteProtocolAsync($"Writing battery note: {message}", p => p.write_message(message), AppendAdvancedLog);
    }

    private void btnSaveTxRxState_Click(object? sender, EventArgs e)
    {
        var protocol = RequireProtocol();
        if (protocol == null)
        {
            return;
        }

        protocol.txrx_save_and_set(true);
        chkbxTXLog.Checked = true;
        chkboxRxLog.Checked = true;
        AppendAdvancedLog("Saved TX/RX logging state and enabled both logs.");
    }

    private void btnRestoreTxRxState_Click(object? sender, EventArgs e)
    {
        var protocol = RequireProtocol();
        if (protocol == null)
        {
            return;
        }

        protocol.txrx_restore();
        chkbxTXLog.Checked = protocol.PRINT_TX;
        chkboxRxLog.Checked = protocol.PRINT_RX;
        AppendAdvancedLog("Restored the saved TX/RX logging state.");
    }

    private async void btnBruteAddr_Click(object? sender, EventArgs e)
    {
        if (!TryReadNumber(txtAddrMSB, "MSB address", 0, 255, out var msb) ||
            !TryReadNumber(txtAddrLSB, "LSB address", 0, 255, out var lsb) ||
            !TryReadNumber(txtDebugCmdLength, "maximum length", 1, 255, out var length))
        {
            return;
        }

        await ExecuteProtocolAsync($"Probing address 0x{msb:X2}{lsb:X2}", p => p.brute(msb, lsb, length), AppendAdvancedLog);
    }

    private async void btnDebugCommand_Click(object? sender, EventArgs e)
    {
        if (!TryReadNumber(txtAddrMSB, "MSB address", 0, 255, out var msb) ||
            !TryReadNumber(txtAddrLSB, "LSB address", 0, 255, out var lsb) ||
            !TryReadNumber(txtDebugCmdLength, "request length", 0, 255, out var length) ||
            !TryReadNumber(txtResponseLength, "response length", 1, 1024, out var responseLength))
        {
            return;
        }

        await ExecuteProtocolAsync($"Reading address 0x{msb:X2}{lsb:X2}", p => p.debug((byte)msb, (byte)lsb, (byte)length, responseLength), AppendAdvancedLog);
    }

    private async void btnTryCommand_Click(object? sender, EventArgs e)
    {
        if (!TryReadNumber(txtCommand, "command", 0, 255, out var command) ||
            !TryReadNumber(txtAddressHigh, "address high", 0, 255, out var high) ||
            !TryReadNumber(txtAddressLow, "address low", 0, 255, out var low) ||
            !TryReadNumber(txtTryCmdLength, "length", 0, 255, out var length))
        {
            return;
        }

        await ExecuteProtocolAsync($"Trying command 0x{command:X2} at 0x{high:X2}{low:X2}", p => p.try_cmd((byte)command, (byte)high, (byte)low, (byte)length), AppendAdvancedLog);
    }

    private void chkbxTXLog_CheckedChanged(object? sender, EventArgs e)
    {
        if (_protocol != null)
        {
            _protocol.PRINT_TX = chkbxTXLog.Checked;
        }
    }

    private void chkboxRxLog_CheckedChanged(object? sender, EventArgs e)
    {
        if (_protocol != null)
        {
            _protocol.PRINT_RX = chkboxRxLog.Checked;
        }
    }

    private async void btnStartSim_Click(object? sender, EventArgs e)
    {
        var protocol = RequireProtocol();
        if (protocol == null)
        {
            return;
        }

        if (!double.TryParse(txtSimDuration.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds <= 0)
        {
            MessageBox.Show("Duration must be a positive number of seconds.", "Simulation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cutoff = M18Protocol.DEFAULT_CUTOFF_CURRENT;
        var maximum = M18Protocol.DEFAULT_MAX_CURRENT;
        if (cmbBxChgProfile.SelectedIndex == 1 &&
            (!TryReadNumber(txtCutoffRaw, "cutoff current", 0, ushort.MaxValue, out cutoff) ||
             !TryReadNumber(txtMaxCurrRaw, "maximum current", 0, ushort.MaxValue, out maximum)))
        {
            return;
        }

        protocol.CUTOFF_CURRENT = cutoff;
        protocol.MAX_CURRENT = maximum;
        _simulationCts = new CancellationTokenSource();
        var token = _simulationCts.Token;
        _busy = true;
        AppendLogBoth($"Starting {seconds:0.##}-second simulation with cutoff {cutoff} and maximum current {maximum}.");

        _simulationTask = Task.Run(() => RunSimulation(protocol, TimeSpan.FromSeconds(seconds), token), token);
        UpdateConnectionUi();
        try
        {
            await _simulationTask;
            AppendLogBoth(token.IsCancellationRequested ? "Simulation stopped and returned to idle." : "Simulation completed and returned to idle.");
        }
        catch (OperationCanceledException)
        {
            AppendLogBoth("Simulation stopped and returned to idle.");
        }
        catch (Exception ex)
        {
            LogError("Simulation failed", ex);
        }
        finally
        {
            _simulationCts.Dispose();
            _simulationCts = null;
            _simulationTask = null;
            _busy = false;
            UpdateConnectionUi();
        }
    }

    private void btnStopSim_Click(object? sender, EventArgs e)
    {
        _simulationCts?.Cancel();
        AppendLogBoth("Stopping simulation...");
    }

    private static void RunSimulation(M18Protocol protocol, TimeSpan duration, CancellationToken token)
    {
        var started = Stopwatch.StartNew();
        protocol.txrx_save_and_set(true);
        try
        {
            token.ThrowIfCancellationRequested();
            if (!protocol.reset())
            {
                throw new InvalidOperationException("Battery reset was not acknowledged.");
            }

            protocol.configure(2);
            protocol.get_snapchat();
            if (token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(600)))
            {
                token.ThrowIfCancellationRequested();
            }

            protocol.keepalive();
            protocol.configure(1);
            protocol.get_snapchat();

            while (started.Elapsed < duration)
            {
                if (token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(500)))
                {
                    token.ThrowIfCancellationRequested();
                }
                protocol.keepalive();
            }
        }
        finally
        {
            protocol.idle();
            protocol.txrx_restore();
        }
    }

    private void cmbBxChgProfile_SelectedIndexChanged(object? sender, EventArgs e)
    {
        grpBxSimCustomProfile.Enabled = cmbBxChgProfile.SelectedIndex == 1;
    }

    private static void UpdateCurrentDisplay(TextBox raw, TextBox amps)
    {
        amps.Text = TryParseNumber(raw.Text, 0, ushort.MaxValue, out var value)
            ? (value / 1000d).ToString("0.###", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private async void btnCollectDiagnostics_Click(object? sender, EventArgs e) =>
        await ExecuteProtocolAsync("Collecting labelled diagnostics", p => p.read_id(), SetDiagnosticsOutput);

    private async void btnSubmitDiagForm_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSubmitDiagSerial.Text) ||
            string.IsNullOrWhiteSpace(txtSubmitDiagType.Text) ||
            string.IsNullOrWhiteSpace(txtSubmitDiagCapacity.Text))
        {
            MessageBox.Show("Serial number, type, and capacity are required.", "Submit Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await ExecuteProtocolAsync(
            "Collecting and submitting diagnostics",
            p => p.submit_form(
                txtOneKeyID.Text.Trim(),
                txtSubmitDiagDate.Text.Trim(),
                txtSubmitDiagSerial.Text.Trim(),
                txtSubmitDiagSticker.Text.Trim(),
                txtSubmitDiagType.Text.Trim(),
                txtSubmitDiagCapacity.Text.Trim()),
            SetDiagnosticsOutput);
    }

    private void btnClearDiagForm_Click(object? sender, EventArgs e)
    {
        txtOneKeyID.Clear();
        txtSubmitDiagDate.Clear();
        txtSubmitDiagSerial.Clear();
        txtSubmitDiagSticker.Clear();
        txtSubmitDiagType.Clear();
        txtSubmitDiagCapacity.Clear();
        rtbSubmitDiagReadOnly.Clear();
    }

    private async Task ExecuteProtocolAsync(string description, Action<M18Protocol> action, Action<string> output)
    {
        var protocol = RequireProtocol();
        if (protocol == null)
        {
            return;
        }

        SetBusy(true);
        AppendLogBoth(description + "...");
        try
        {
            var captured = await Task.Run(() => CaptureProtocolOutput(protocol, action));
            if (!string.IsNullOrWhiteSpace(captured))
            {
                output(captured);
            }
            AppendLogBoth(description + " complete.");
        }
        catch (Exception ex)
        {
            LogError(description + " failed", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static string CaptureProtocolOutput(M18Protocol protocol, Action<M18Protocol> action)
    {
        lock (ConsoleCaptureLock)
        {
            var original = Console.Out;
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            try
            {
                Console.SetOut(writer);
                action(protocol);
            }
            finally
            {
                try
                {
                    protocol.idle();
                }
                finally
                {
                    Console.SetOut(original);
                }
            }
            return writer.ToString().TrimEnd();
        }
    }

    private M18Protocol? RequireProtocol()
    {
        if (_protocol != null)
        {
            return _protocol;
        }

        MessageBox.Show("Connect to a battery first.", "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return null;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UpdateConnectionUi();
    }

    private void UpdateConnectionUi()
    {
        var connected = _protocol != null;
        var simulating = _simulationTask is { IsCompleted: false };
        var commandsEnabled = connected && !_busy && !simulating;

        btnConnect.Enabled = !connected && !_busy;
        btnDisconnect.Enabled = connected && (!_busy || simulating);
        btnRefresh.Enabled = !connected && !_busy;
        btnTestFT232.Enabled = !connected && !_busy;
        cmbBxSerialPort.Enabled = !connected && !_busy;

        foreach (var control in new Control[]
        {
            btnIdle, btnActive, btnHealthReport, btnReset,
            btnReadAllRegisters, btnReadAllSpreadsheet, btnReadIDRaw, btnReadIDLabelled,
            btnWriteMessage, btnSaveTxRxState, btnRestoreTxRxState, btnBruteAddr,
            btnDebugCommand, btnTryCommand, btnCollectDiagnostics, btnSubmitDiagForm
        })
        {
            control.Enabled = commandsEnabled;
        }

        btnStartSim.Enabled = commandsEnabled;
        btnStopSim.Enabled = simulating;
        btnFullBrute.Enabled = false;
    }

    private static bool TryParseNumber(string text, int minimum, int maximum, out int value)
    {
        var input = text.Trim();
        var style = NumberStyles.Integer;
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            input = input[2..];
            style = NumberStyles.AllowHexSpecifier;
        }

        return int.TryParse(input, style, CultureInfo.InvariantCulture, out value) && value >= minimum && value <= maximum;
    }

    private static bool TryReadNumber(TextBox input, string name, int minimum, int maximum, out int value)
    {
        if (TryParseNumber(input.Text, minimum, maximum, out value))
        {
            return true;
        }

        MessageBox.Show($"{name} must be between {minimum} and {maximum}; hexadecimal values may use a 0x prefix.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        input.Focus();
        return false;
    }

    [Conditional("DEBUG")]
    private static void RunSelfChecks()
    {
        Debug.Assert(TryParseNumber("0xFF", 0, 255, out var hex) && hex == 255);
        Debug.Assert(TryParseNumber("255", 0, 255, out var dec) && dec == 255);
        Debug.Assert(!TryParseNumber("256", 0, 255, out _));
    }

    private void AppendLog(string message) => AppendSimpleLog(FormatLogMessage(message));

    private void AppendLogBoth(string message)
    {
        var formatted = FormatLogMessage(message);
        AppendSimpleLog(formatted);
        AppendDebugLog(formatted);
    }

    private void AppendProtocolLog(string message)
    {
        var formatted = FormatLogMessage(message);
        AppendAdvancedLog(formatted);
        AppendDebugLog(formatted);
    }

    private string FormatLogMessage(string message) => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";

    private void AppendSimpleLog(string message)
    {
        if (rtbOutput.InvokeRequired)
        {
            rtbOutput.Invoke(new Action<string>(AppendSimpleLog), message);
            return;
        }
        AppendText(rtbOutput, ref _hasAppendedLog, message);
    }

    private void AppendAdvancedLog(string message)
    {
        if (rtbAdvOutput.InvokeRequired)
        {
            rtbAdvOutput.Invoke(new Action<string>(AppendAdvancedLog), message);
            return;
        }
        AppendText(rtbAdvOutput, ref _hasAppendedAdvancedLog, message);
    }

    private void AppendDebugLog(string message)
    {
        if (rtbDebugOutput.InvokeRequired)
        {
            rtbDebugOutput.Invoke(new Action<string>(AppendDebugLog), message);
            return;
        }
        AppendText(rtbDebugOutput, ref _hasAppendedDebugLog, message);
    }

    private void AppendRawSerialLog(string message)
    {
        if (rtbD2xxLog.InvokeRequired)
        {
            rtbD2xxLog.Invoke(new Action<string>(AppendRawSerialLog), message);
            return;
        }
        AppendText(rtbD2xxLog, ref _hasAppendedSerialLog, FormatLogMessage(message));
    }

    private static void AppendText(RichTextBox output, ref bool hasText, string message)
    {
        if (hasText)
        {
            output.AppendText(Environment.NewLine);
        }
        output.AppendText(message);
        output.SelectionStart = output.TextLength;
        output.ScrollToCaret();
        hasText = true;
    }

    private void SetDiagnosticsOutput(string text)
    {
        if (rtbSubmitDiagReadOnly.InvokeRequired)
        {
            rtbSubmitDiagReadOnly.Invoke(new Action<string>(SetDiagnosticsOutput), text);
            return;
        }
        rtbSubmitDiagReadOnly.Text = text;
    }

    private void LogError(string context, Exception exception)
    {
        var message = $"{context}: {exception.Message}";
        AppendLogBoth(message);
        MessageBox.Show(message, "M18 Battery Analyzer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void OpenLink(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private async void frmMain_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closing)
        {
            return;
        }

        e.Cancel = true;
        _closing = true;
        await DisconnectAsync(false);
        Close();
    }

    private void cmbBxSerialPort_SelectedIndexChanged_1(object sender, EventArgs e) =>
        cmbBxSerialPort_SelectedIndexChanged(sender, e);

    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
        OpenLink("https://github.com/mnh-jansson/m18-protocol/");

    private void grpBxSimCustomProfile_Enter(object sender, EventArgs e)
    {
    }

    private void lblResponseLength_Click(object sender, EventArgs e)
    {
    }

    private void rtbOutput_TextChanged(object sender, EventArgs e)
    {
    }
}
