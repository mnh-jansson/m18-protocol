// *************************************************************************************************
// M18AnalyzerMain.Designer.cs
// ---------------------------
// Auto-generated WinForms designer file that declares and initializes every UI control used by
// frmMain. The code positions buttons, text boxes, labels, tabs, and rich text boxes that the logic
// in M18AnalyzerMain.cs interacts with. Although the designer typically omits comments, we include
// them here to help beginners understand how InitializeComponent wires up control properties (size,
// docking, events) and how the UI layout maps to protocol actions such as toggling TX lines and
// reading health reports.
// *************************************************************************************************
namespace M18BatteryInfo
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            tabControlM18Main = new TabControl();
            tabSimple = new TabPage();
            btnTestFT232 = new Button();
            grpOutput = new GroupBox();
            btnCopyOutput = new Button();
            btnReset = new Button();
            btnHealthReport = new Button();
            btnActive = new Button();
            rtbOutput = new RichTextBox();
            btnIdle = new Button();
            btnDisconnect = new Button();
            btnConnect = new Button();
            btnRefresh = new Button();
            lblSerialPort = new Label();
            cmbBxSerialPort = new ComboBox();
            tabAdvanced = new TabPage();
            btnRestoreTxRxState = new Button();
            grpTryCmd = new GroupBox();
            btnTryCommand = new Button();
            txtTryCmdLength = new TextBox();
            txtAddressLow = new TextBox();
            txtAddressHigh = new TextBox();
            lblTryCmdLength = new Label();
            lblAddressLow = new Label();
            lblAddrHigh = new Label();
            lblCommand = new Label();
            txtCommand = new TextBox();
            grpbxDebugCmd = new GroupBox();
            btnDebugCommand = new Button();
            txtResponseLength = new TextBox();
            txtDebugCmdLength = new TextBox();
            txtAddrLSB = new TextBox();
            lblResponseLength = new Label();
            lblDebugCmdLength = new Label();
            lblLSB = new Label();
            lblMSB = new Label();
            txtAddrMSB = new TextBox();
            btnFullBrute = new Button();
            btnBruteAddr = new Button();
            btnSaveTxRxState = new Button();
            chkboxRxLog = new CheckBox();
            chkbxTXLog = new CheckBox();
            txtWriteMessage = new TextBox();
            btnWriteMessage = new Button();
            btnReadIDLabelled = new Button();
            btnReadIDRaw = new Button();
            btnReadAllSpreadsheet = new Button();
            btnReadAllRegisters = new Button();
            rtbAdvOutput = new RichTextBox();
            tabD2xx = new TabPage();
            rtbD2xxLog = new RichTextBox();
            tabSimulation = new TabPage();
            grpBxSimCustomProfile = new GroupBox();
            lblCutoffRaw = new Label();
            lblMaxCurrAmps = new Label();
            txtMaxCurrAmps = new TextBox();
            lblMaxCurrRaw = new Label();
            txtCutoffAmps = new TextBox();
            lblCutoffAmps = new Label();
            txtMaxCurrRaw = new TextBox();
            txtCutoffRaw = new TextBox();
            txtSimDuration = new TextBox();
            btnStopSim = new Button();
            btnStartSim = new Button();
            lblSimProfile = new Label();
            lblBaudRate = new Label();
            lblDuration = new Label();
            cmbBxChgProfile = new ComboBox();
            cmbBxBaudRate = new ComboBox();
            tabDiagnostics = new TabPage();
            btnCollectDiagnostics = new Button();
            btnClearDiagForm = new Button();
            grpboxDiagOutput = new GroupBox();
            rtbSubmitDiagReadOnly = new RichTextBox();
            lblType = new Label();
            lblStickerInfo = new Label();
            lblToolSerialNum = new Label();
            lblSubmitDiagDate = new Label();
            lblOneKeyID = new Label();
            lblSubmitDiagCapacity = new Label();
            txtSubmitDiagDate = new TextBox();
            txtSubmitDiagSerial = new TextBox();
            txtSubmitDiagSticker = new TextBox();
            txtSubmitDiagType = new TextBox();
            txtOneKeyID = new TextBox();
            txtSubmitDiagCapacity = new TextBox();
            btnSubmitDiagForm = new Button();
            tabAbout = new TabPage();
            linkLabelKillaVolt = new LinkLabel();
            lblKillaVoltAbout = new Label();
            linkLabelMartin = new LinkLabel();
            lblMartin = new Label();
            lblAboutTitle = new Label();
            toolTipSimpleTab = new ToolTip(components);
            rtbDebugOutput = new RichTextBox();
            tabControlM18Main.SuspendLayout();
            tabSimple.SuspendLayout();
            grpOutput.SuspendLayout();
            tabAdvanced.SuspendLayout();
            grpTryCmd.SuspendLayout();
            grpbxDebugCmd.SuspendLayout();
            tabD2xx.SuspendLayout();
            tabSimulation.SuspendLayout();
            grpBxSimCustomProfile.SuspendLayout();
            tabDiagnostics.SuspendLayout();
            grpboxDiagOutput.SuspendLayout();
            tabAbout.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlM18Main
            // 
            tabControlM18Main.Controls.Add(tabSimple);
            tabControlM18Main.Controls.Add(tabAdvanced);
            tabControlM18Main.Controls.Add(tabD2xx);
            tabControlM18Main.Controls.Add(tabSimulation);
            tabControlM18Main.Controls.Add(tabDiagnostics);
            tabControlM18Main.Controls.Add(tabAbout);
            tabControlM18Main.Location = new Point(6, 6);
            tabControlM18Main.Margin = new Padding(2);
            tabControlM18Main.Name = "tabControlM18Main";
            tabControlM18Main.SelectedIndex = 0;
            tabControlM18Main.Size = new Size(1098, 394);
            tabControlM18Main.TabIndex = 0;
            // 
            // tabSimple
            // 
            tabSimple.Controls.Add(btnTestFT232);
            tabSimple.Controls.Add(grpOutput);
            tabSimple.Controls.Add(btnDisconnect);
            tabSimple.Controls.Add(btnConnect);
            tabSimple.Controls.Add(btnRefresh);
            tabSimple.Controls.Add(lblSerialPort);
            tabSimple.Controls.Add(cmbBxSerialPort);
            tabSimple.Location = new Point(4, 29);
            tabSimple.Margin = new Padding(2);
            tabSimple.Name = "tabSimple";
            tabSimple.Padding = new Padding(2);
            tabSimple.Size = new Size(1090, 361);
            tabSimple.TabIndex = 0;
            tabSimple.Text = "Simple";
            tabSimple.UseVisualStyleBackColor = true;
            // 
            // btnTestFT232
            // 
            btnTestFT232.Location = new Point(274, 38);
            btnTestFT232.Margin = new Padding(2);
            btnTestFT232.Name = "btnTestFT232";
            btnTestFT232.Size = new Size(90, 27);
            btnTestFT232.TabIndex = 7;
            btnTestFT232.Text = "Test Device";
            btnTestFT232.UseVisualStyleBackColor = true;
            // 
            // grpOutput
            // 
            grpOutput.Controls.Add(btnCopyOutput);
            grpOutput.Controls.Add(btnReset);
            grpOutput.Controls.Add(btnHealthReport);
            grpOutput.Controls.Add(btnActive);
            grpOutput.Controls.Add(rtbOutput);
            grpOutput.Controls.Add(btnIdle);
            grpOutput.Location = new Point(5, 70);
            grpOutput.Margin = new Padding(2);
            grpOutput.Name = "grpOutput";
            grpOutput.Padding = new Padding(2);
            grpOutput.Size = new Size(971, 282);
            grpOutput.TabIndex = 6;
            grpOutput.TabStop = false;
            grpOutput.Text = "Output";
            // 
            // btnCopyOutput
            // 
            btnCopyOutput.Location = new Point(832, 120);
            btnCopyOutput.Margin = new Padding(2);
            btnCopyOutput.Name = "btnCopyOutput";
            btnCopyOutput.Size = new Size(118, 27);
            btnCopyOutput.TabIndex = 11;
            btnCopyOutput.Text = "Copy Output";
            toolTipSimpleTab.SetToolTip(btnCopyOutput, "Send 0xAA to battery. \r\nReturn true if battery replies wih 0xAA");
            btnCopyOutput.UseVisualStyleBackColor = true;
            // 
            // btnReset
            // 
            btnReset.Location = new Point(832, 248);
            btnReset.Margin = new Padding(2);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(118, 27);
            btnReset.TabIndex = 10;
            btnReset.Text = "Reset";
            toolTipSimpleTab.SetToolTip(btnReset, "Send 0xAA to battery. \r\nReturn true if battery replies wih 0xAA");
            btnReset.UseVisualStyleBackColor = true;
            // 
            // btnHealthReport
            // 
            btnHealthReport.Location = new Point(832, 88);
            btnHealthReport.Margin = new Padding(2);
            btnHealthReport.Name = "btnHealthReport";
            btnHealthReport.Size = new Size(118, 27);
            btnHealthReport.TabIndex = 9;
            btnHealthReport.Text = "Health Report";
            toolTipSimpleTab.SetToolTip(btnHealthReport, "Print simple health report on the connected battery.");
            btnHealthReport.UseVisualStyleBackColor = true;
            // 
            // btnActive
            // 
            btnActive.Location = new Point(832, 56);
            btnActive.Margin = new Padding(2);
            btnActive.Name = "btnActive";
            btnActive.Size = new Size(118, 27);
            btnActive.TabIndex = 8;
            btnActive.Text = "Active (Tx High)";
            toolTipSimpleTab.SetToolTip(btnActive, "Charger simulation active.\r\nMay increase battery charge counter.\r\nNot safe to connect or disconnect battery during this state.");
            btnActive.UseVisualStyleBackColor = true;
            // 
            // rtbOutput
            // 
            rtbOutput.BackColor = Color.Black;
            rtbOutput.ForeColor = Color.Lime;
            rtbOutput.Location = new Point(5, 24);
            rtbOutput.Margin = new Padding(2);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.Size = new Size(811, 252);
            rtbOutput.TabIndex = 0;
            rtbOutput.Text = resources.GetString("rtbOutput.Text");
            rtbOutput.TextChanged += rtbOutput_TextChanged;
            // 
            // btnIdle
            // 
            btnIdle.Location = new Point(832, 24);
            btnIdle.Margin = new Padding(2);
            btnIdle.Name = "btnIdle";
            btnIdle.Size = new Size(118, 27);
            btnIdle.TabIndex = 7;
            btnIdle.Text = "Idle (Tx Low)";
            toolTipSimpleTab.SetToolTip(btnIdle, "Does not increase charge count.\r\nSafe to connect or disconnect battery.");
            btnIdle.UseVisualStyleBackColor = true;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Location = new Point(371, 38);
            btnDisconnect.Margin = new Padding(2);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(90, 27);
            btnDisconnect.TabIndex = 5;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(179, 38);
            btnConnect.Margin = new Padding(2);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(90, 27);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Connect";
            toolTipSimpleTab.SetToolTip(btnConnect, "Connect to the selected serial port.");
            btnConnect.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(83, 38);
            btnRefresh.Margin = new Padding(2);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(90, 27);
            btnRefresh.TabIndex = 3;
            btnRefresh.Text = "Refresh";
            toolTipSimpleTab.SetToolTip(btnRefresh, "Refresh the serial port list.");
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // lblSerialPort
            // 
            lblSerialPort.AutoSize = true;
            lblSerialPort.Location = new Point(5, 11);
            lblSerialPort.Margin = new Padding(2, 0, 2, 0);
            lblSerialPort.Name = "lblSerialPort";
            lblSerialPort.Size = new Size(79, 20);
            lblSerialPort.TabIndex = 2;
            lblSerialPort.Text = "Serial Port:";
            // 
            // cmbBxSerialPort
            // 
            cmbBxSerialPort.FormattingEnabled = true;
            cmbBxSerialPort.Location = new Point(86, 9);
            cmbBxSerialPort.Margin = new Padding(2);
            cmbBxSerialPort.Name = "cmbBxSerialPort";
            cmbBxSerialPort.Size = new Size(626, 28);
            cmbBxSerialPort.TabIndex = 1;
            cmbBxSerialPort.SelectedIndexChanged += cmbBxSerialPort_SelectedIndexChanged_1;
            // 
            // tabAdvanced
            // 
            tabAdvanced.Controls.Add(btnRestoreTxRxState);
            tabAdvanced.Controls.Add(grpTryCmd);
            tabAdvanced.Controls.Add(grpbxDebugCmd);
            tabAdvanced.Controls.Add(btnFullBrute);
            tabAdvanced.Controls.Add(btnBruteAddr);
            tabAdvanced.Controls.Add(btnSaveTxRxState);
            tabAdvanced.Controls.Add(chkboxRxLog);
            tabAdvanced.Controls.Add(chkbxTXLog);
            tabAdvanced.Controls.Add(txtWriteMessage);
            tabAdvanced.Controls.Add(btnWriteMessage);
            tabAdvanced.Controls.Add(btnReadIDLabelled);
            tabAdvanced.Controls.Add(btnReadIDRaw);
            tabAdvanced.Controls.Add(btnReadAllSpreadsheet);
            tabAdvanced.Controls.Add(btnReadAllRegisters);
            tabAdvanced.Controls.Add(rtbAdvOutput);
            tabAdvanced.Location = new Point(4, 29);
            tabAdvanced.Margin = new Padding(2);
            tabAdvanced.Name = "tabAdvanced";
            tabAdvanced.Padding = new Padding(2);
            tabAdvanced.Size = new Size(1090, 361);
            tabAdvanced.TabIndex = 1;
            tabAdvanced.Text = "Advanced";
            tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // btnRestoreTxRxState
            // 
            btnRestoreTxRxState.Location = new Point(6, 198);
            btnRestoreTxRxState.Margin = new Padding(2);
            btnRestoreTxRxState.Name = "btnRestoreTxRxState";
            btnRestoreTxRxState.Size = new Size(166, 27);
            btnRestoreTxRxState.TabIndex = 15;
            btnRestoreTxRxState.Text = "Restore TxRx State";
            btnRestoreTxRxState.UseVisualStyleBackColor = true;
            // 
            // grpTryCmd
            // 
            grpTryCmd.Controls.Add(btnTryCommand);
            grpTryCmd.Controls.Add(txtTryCmdLength);
            grpTryCmd.Controls.Add(txtAddressLow);
            grpTryCmd.Controls.Add(txtAddressHigh);
            grpTryCmd.Controls.Add(lblTryCmdLength);
            grpTryCmd.Controls.Add(lblAddressLow);
            grpTryCmd.Controls.Add(lblAddrHigh);
            grpTryCmd.Controls.Add(lblCommand);
            grpTryCmd.Controls.Add(txtCommand);
            grpTryCmd.Location = new Point(448, 6);
            grpTryCmd.Margin = new Padding(2);
            grpTryCmd.Name = "grpTryCmd";
            grpTryCmd.Padding = new Padding(2);
            grpTryCmd.Size = new Size(262, 179);
            grpTryCmd.TabIndex = 14;
            grpTryCmd.TabStop = false;
            grpTryCmd.Text = "Try Command";
            // 
            // btnTryCommand
            // 
            btnTryCommand.Location = new Point(128, 147);
            btnTryCommand.Margin = new Padding(2);
            btnTryCommand.Name = "btnTryCommand";
            btnTryCommand.Size = new Size(129, 27);
            btnTryCommand.TabIndex = 15;
            btnTryCommand.Text = "Execute";
            btnTryCommand.UseVisualStyleBackColor = true;
            // 
            // txtTryCmdLength
            // 
            txtTryCmdLength.Location = new Point(128, 115);
            txtTryCmdLength.Margin = new Padding(2);
            txtTryCmdLength.Name = "txtTryCmdLength";
            txtTryCmdLength.Size = new Size(129, 27);
            txtTryCmdLength.TabIndex = 14;
            // 
            // txtAddressLow
            // 
            txtAddressLow.Location = new Point(128, 83);
            txtAddressLow.Margin = new Padding(2);
            txtAddressLow.Name = "txtAddressLow";
            txtAddressLow.Size = new Size(129, 27);
            txtAddressLow.TabIndex = 13;
            // 
            // txtAddressHigh
            // 
            txtAddressHigh.Location = new Point(128, 51);
            txtAddressHigh.Margin = new Padding(2);
            txtAddressHigh.Name = "txtAddressHigh";
            txtAddressHigh.Size = new Size(129, 27);
            txtAddressHigh.TabIndex = 12;
            // 
            // lblTryCmdLength
            // 
            lblTryCmdLength.AutoSize = true;
            lblTryCmdLength.Location = new Point(6, 122);
            lblTryCmdLength.Margin = new Padding(2, 0, 2, 0);
            lblTryCmdLength.Name = "lblTryCmdLength";
            lblTryCmdLength.Size = new Size(54, 20);
            lblTryCmdLength.TabIndex = 11;
            lblTryCmdLength.Text = "Length";
            // 
            // lblAddressLow
            // 
            lblAddressLow.AutoSize = true;
            lblAddressLow.Location = new Point(6, 90);
            lblAddressLow.Margin = new Padding(2, 0, 2, 0);
            lblAddressLow.Name = "lblAddressLow";
            lblAddressLow.Size = new Size(93, 20);
            lblAddressLow.TabIndex = 10;
            lblAddressLow.Text = "Address Low";
            // 
            // lblAddrHigh
            // 
            lblAddrHigh.AutoSize = true;
            lblAddrHigh.Location = new Point(6, 58);
            lblAddrHigh.Margin = new Padding(2, 0, 2, 0);
            lblAddrHigh.Name = "lblAddrHigh";
            lblAddrHigh.Size = new Size(98, 20);
            lblAddrHigh.TabIndex = 9;
            lblAddrHigh.Text = "Address High";
            // 
            // lblCommand
            // 
            lblCommand.AutoSize = true;
            lblCommand.Location = new Point(6, 26);
            lblCommand.Margin = new Padding(2, 0, 2, 0);
            lblCommand.Name = "lblCommand";
            lblCommand.Size = new Size(78, 20);
            lblCommand.TabIndex = 8;
            lblCommand.Text = "Command";
            // 
            // txtCommand
            // 
            txtCommand.Location = new Point(128, 19);
            txtCommand.Margin = new Padding(2);
            txtCommand.Name = "txtCommand";
            txtCommand.Size = new Size(129, 27);
            txtCommand.TabIndex = 7;
            // 
            // grpbxDebugCmd
            // 
            grpbxDebugCmd.Controls.Add(btnDebugCommand);
            grpbxDebugCmd.Controls.Add(txtResponseLength);
            grpbxDebugCmd.Controls.Add(txtDebugCmdLength);
            grpbxDebugCmd.Controls.Add(txtAddrLSB);
            grpbxDebugCmd.Controls.Add(lblResponseLength);
            grpbxDebugCmd.Controls.Add(lblDebugCmdLength);
            grpbxDebugCmd.Controls.Add(lblLSB);
            grpbxDebugCmd.Controls.Add(lblMSB);
            grpbxDebugCmd.Controls.Add(txtAddrMSB);
            grpbxDebugCmd.Location = new Point(179, 6);
            grpbxDebugCmd.Margin = new Padding(2);
            grpbxDebugCmd.Name = "grpbxDebugCmd";
            grpbxDebugCmd.Padding = new Padding(2);
            grpbxDebugCmd.Size = new Size(262, 179);
            grpbxDebugCmd.TabIndex = 13;
            grpbxDebugCmd.TabStop = false;
            grpbxDebugCmd.Text = "Debug Command";
            // 
            // btnDebugCommand
            // 
            btnDebugCommand.Location = new Point(128, 147);
            btnDebugCommand.Margin = new Padding(2);
            btnDebugCommand.Name = "btnDebugCommand";
            btnDebugCommand.Size = new Size(129, 27);
            btnDebugCommand.TabIndex = 15;
            btnDebugCommand.Text = "Execute";
            btnDebugCommand.UseVisualStyleBackColor = true;
            // 
            // txtResponseLength
            // 
            txtResponseLength.Location = new Point(128, 115);
            txtResponseLength.Margin = new Padding(2);
            txtResponseLength.Name = "txtResponseLength";
            txtResponseLength.Size = new Size(129, 27);
            txtResponseLength.TabIndex = 14;
            // 
            // txtDebugCmdLength
            // 
            txtDebugCmdLength.Location = new Point(128, 83);
            txtDebugCmdLength.Margin = new Padding(2);
            txtDebugCmdLength.Name = "txtDebugCmdLength";
            txtDebugCmdLength.Size = new Size(129, 27);
            txtDebugCmdLength.TabIndex = 13;
            // 
            // txtAddrLSB
            // 
            txtAddrLSB.Location = new Point(128, 51);
            txtAddrLSB.Margin = new Padding(2);
            txtAddrLSB.Name = "txtAddrLSB";
            txtAddrLSB.Size = new Size(129, 27);
            txtAddrLSB.TabIndex = 12;
            // 
            // lblResponseLength
            // 
            lblResponseLength.AutoSize = true;
            lblResponseLength.Location = new Point(6, 115);
            lblResponseLength.Margin = new Padding(2, 0, 2, 0);
            lblResponseLength.Name = "lblResponseLength";
            lblResponseLength.Size = new Size(121, 20);
            lblResponseLength.TabIndex = 11;
            lblResponseLength.Text = "Response Length";
            lblResponseLength.Click += lblResponseLength_Click;
            // 
            // lblDebugCmdLength
            // 
            lblDebugCmdLength.AutoSize = true;
            lblDebugCmdLength.Location = new Point(6, 90);
            lblDebugCmdLength.Margin = new Padding(2, 0, 2, 0);
            lblDebugCmdLength.Name = "lblDebugCmdLength";
            lblDebugCmdLength.Size = new Size(54, 20);
            lblDebugCmdLength.TabIndex = 10;
            lblDebugCmdLength.Text = "Length";
            // 
            // lblLSB
            // 
            lblLSB.AutoSize = true;
            lblLSB.Location = new Point(6, 58);
            lblLSB.Margin = new Padding(2, 0, 2, 0);
            lblLSB.Name = "lblLSB";
            lblLSB.Size = new Size(90, 20);
            lblLSB.TabIndex = 9;
            lblLSB.Text = "LSB Address";
            // 
            // lblMSB
            // 
            lblMSB.AutoSize = true;
            lblMSB.Location = new Point(6, 26);
            lblMSB.Margin = new Padding(2, 0, 2, 0);
            lblMSB.Name = "lblMSB";
            lblMSB.Size = new Size(96, 20);
            lblMSB.TabIndex = 8;
            lblMSB.Text = "MSB Address";
            // 
            // txtAddrMSB
            // 
            txtAddrMSB.Location = new Point(128, 19);
            txtAddrMSB.Margin = new Padding(2);
            txtAddrMSB.Name = "txtAddrMSB";
            txtAddrMSB.Size = new Size(129, 27);
            txtAddrMSB.TabIndex = 7;
            // 
            // btnFullBrute
            // 
            btnFullBrute.Location = new Point(6, 282);
            btnFullBrute.Margin = new Padding(2);
            btnFullBrute.Name = "btnFullBrute";
            btnFullBrute.Size = new Size(166, 27);
            btnFullBrute.TabIndex = 12;
            btnFullBrute.Text = "Brute Force All";
            btnFullBrute.UseVisualStyleBackColor = true;
            // 
            // btnBruteAddr
            // 
            btnBruteAddr.Location = new Point(6, 250);
            btnBruteAddr.Margin = new Padding(2);
            btnBruteAddr.Name = "btnBruteAddr";
            btnBruteAddr.Size = new Size(166, 27);
            btnBruteAddr.TabIndex = 11;
            btnBruteAddr.Text = "Brute Force Addresses";
            btnBruteAddr.UseVisualStyleBackColor = true;
            // 
            // btnSaveTxRxState
            // 
            btnSaveTxRxState.Location = new Point(6, 166);
            btnSaveTxRxState.Margin = new Padding(2);
            btnSaveTxRxState.Name = "btnSaveTxRxState";
            btnSaveTxRxState.Size = new Size(166, 27);
            btnSaveTxRxState.TabIndex = 10;
            btnSaveTxRxState.Text = "Save && Set TxRx State";
            btnSaveTxRxState.UseVisualStyleBackColor = true;
            // 
            // chkboxRxLog
            // 
            chkboxRxLog.AutoSize = true;
            chkboxRxLog.Checked = true;
            chkboxRxLog.CheckState = CheckState.Checked;
            chkboxRxLog.Location = new Point(589, 301);
            chkboxRxLog.Margin = new Padding(2);
            chkboxRxLog.Name = "chkboxRxLog";
            chkboxRxLog.Size = new Size(125, 24);
            chkboxRxLog.TabIndex = 8;
            chkboxRxLog.Text = "Enable Rx Log";
            chkboxRxLog.UseVisualStyleBackColor = true;
            // 
            // chkbxTXLog
            // 
            chkbxTXLog.AutoSize = true;
            chkbxTXLog.Checked = true;
            chkbxTXLog.CheckState = CheckState.Checked;
            chkbxTXLog.Location = new Point(461, 301);
            chkbxTXLog.Margin = new Padding(2);
            chkbxTXLog.Name = "chkbxTXLog";
            chkbxTXLog.Size = new Size(126, 24);
            chkbxTXLog.TabIndex = 7;
            chkbxTXLog.Text = "Enable TX Log";
            chkbxTXLog.UseVisualStyleBackColor = true;
            // 
            // txtWriteMessage
            // 
            txtWriteMessage.Location = new Point(179, 326);
            txtWriteMessage.Margin = new Padding(2);
            txtWriteMessage.Name = "txtWriteMessage";
            txtWriteMessage.Size = new Size(532, 27);
            txtWriteMessage.TabIndex = 6;
            txtWriteMessage.Text = "Write ASCII To Register. 20 Characters Max.";
            // 
            // btnWriteMessage
            // 
            btnWriteMessage.Location = new Point(6, 326);
            btnWriteMessage.Margin = new Padding(2);
            btnWriteMessage.Name = "btnWriteMessage";
            btnWriteMessage.Size = new Size(166, 27);
            btnWriteMessage.TabIndex = 5;
            btnWriteMessage.Text = "Write Message";
            btnWriteMessage.UseVisualStyleBackColor = true;
            // 
            // btnReadIDLabelled
            // 
            btnReadIDLabelled.Location = new Point(6, 115);
            btnReadIDLabelled.Margin = new Padding(2);
            btnReadIDLabelled.Name = "btnReadIDLabelled";
            btnReadIDLabelled.Size = new Size(166, 27);
            btnReadIDLabelled.TabIndex = 4;
            btnReadIDLabelled.Text = "Read ID (Labelled)";
            btnReadIDLabelled.UseVisualStyleBackColor = true;
            // 
            // btnReadIDRaw
            // 
            btnReadIDRaw.Location = new Point(6, 83);
            btnReadIDRaw.Margin = new Padding(2);
            btnReadIDRaw.Name = "btnReadIDRaw";
            btnReadIDRaw.Size = new Size(166, 27);
            btnReadIDRaw.TabIndex = 3;
            btnReadIDRaw.Text = "Read ID (Raw)";
            btnReadIDRaw.UseVisualStyleBackColor = true;
            // 
            // btnReadAllSpreadsheet
            // 
            btnReadAllSpreadsheet.Location = new Point(6, 51);
            btnReadAllSpreadsheet.Margin = new Padding(2);
            btnReadAllSpreadsheet.Name = "btnReadAllSpreadsheet";
            btnReadAllSpreadsheet.Size = new Size(166, 27);
            btnReadAllSpreadsheet.TabIndex = 2;
            btnReadAllSpreadsheet.Text = "Read All Spreadsheet";
            btnReadAllSpreadsheet.UseVisualStyleBackColor = true;
            // 
            // btnReadAllRegisters
            // 
            btnReadAllRegisters.Location = new Point(6, 19);
            btnReadAllRegisters.Margin = new Padding(2);
            btnReadAllRegisters.Name = "btnReadAllRegisters";
            btnReadAllRegisters.Size = new Size(166, 27);
            btnReadAllRegisters.TabIndex = 1;
            btnReadAllRegisters.Text = "Read All Registers";
            btnReadAllRegisters.UseVisualStyleBackColor = true;
            // 
            // rtbAdvOutput
            // 
            rtbAdvOutput.Location = new Point(179, 192);
            rtbAdvOutput.Margin = new Padding(2);
            rtbAdvOutput.Name = "rtbAdvOutput";
            rtbAdvOutput.Size = new Size(532, 110);
            rtbAdvOutput.TabIndex = 0;
            rtbAdvOutput.Text = "";
            // 
            // tabD2xx
            // 
            tabD2xx.Controls.Add(rtbD2xxLog);
            tabD2xx.Location = new Point(4, 29);
            tabD2xx.Margin = new Padding(2);
            tabD2xx.Name = "tabD2xx";
            tabD2xx.Padding = new Padding(2);
            tabD2xx.Size = new Size(1090, 361);
            tabD2xx.TabIndex = 2;
            tabD2xx.Text = "Raw D2XX Log";
            tabD2xx.UseVisualStyleBackColor = true;
            // 
            // rtbD2xxLog
            // 
            rtbD2xxLog.Dock = DockStyle.Fill;
            rtbD2xxLog.Location = new Point(2, 2);
            rtbD2xxLog.Margin = new Padding(2);
            rtbD2xxLog.Name = "rtbD2xxLog";
            rtbD2xxLog.Size = new Size(1086, 357);
            rtbD2xxLog.TabIndex = 0;
            rtbD2xxLog.Text = "";
            // 
            // tabSimulation
            // 
            tabSimulation.Controls.Add(grpBxSimCustomProfile);
            tabSimulation.Controls.Add(txtSimDuration);
            tabSimulation.Controls.Add(btnStopSim);
            tabSimulation.Controls.Add(btnStartSim);
            tabSimulation.Controls.Add(lblSimProfile);
            tabSimulation.Controls.Add(lblBaudRate);
            tabSimulation.Controls.Add(lblDuration);
            tabSimulation.Controls.Add(cmbBxChgProfile);
            tabSimulation.Controls.Add(cmbBxBaudRate);
            tabSimulation.Location = new Point(4, 29);
            tabSimulation.Margin = new Padding(2);
            tabSimulation.Name = "tabSimulation";
            tabSimulation.Padding = new Padding(2);
            tabSimulation.Size = new Size(1090, 361);
            tabSimulation.TabIndex = 3;
            tabSimulation.Text = "Simulation";
            tabSimulation.UseVisualStyleBackColor = true;
            // 
            // grpBxSimCustomProfile
            // 
            grpBxSimCustomProfile.Controls.Add(lblCutoffRaw);
            grpBxSimCustomProfile.Controls.Add(lblMaxCurrAmps);
            grpBxSimCustomProfile.Controls.Add(txtMaxCurrAmps);
            grpBxSimCustomProfile.Controls.Add(lblMaxCurrRaw);
            grpBxSimCustomProfile.Controls.Add(txtCutoffAmps);
            grpBxSimCustomProfile.Controls.Add(lblCutoffAmps);
            grpBxSimCustomProfile.Controls.Add(txtMaxCurrRaw);
            grpBxSimCustomProfile.Controls.Add(txtCutoffRaw);
            grpBxSimCustomProfile.Location = new Point(237, 6);
            grpBxSimCustomProfile.Margin = new Padding(2);
            grpBxSimCustomProfile.Name = "grpBxSimCustomProfile";
            grpBxSimCustomProfile.Padding = new Padding(2);
            grpBxSimCustomProfile.Size = new Size(269, 160);
            grpBxSimCustomProfile.TabIndex = 16;
            grpBxSimCustomProfile.TabStop = false;
            grpBxSimCustomProfile.Text = "Custom Charger Profile";
            grpBxSimCustomProfile.Enter += grpBxSimCustomProfile_Enter;
            // 
            // lblCutoffRaw
            // 
            lblCutoffRaw.AutoSize = true;
            lblCutoffRaw.Location = new Point(6, 26);
            lblCutoffRaw.Margin = new Padding(2, 0, 2, 0);
            lblCutoffRaw.Name = "lblCutoffRaw";
            lblCutoffRaw.Size = new Size(92, 20);
            lblCutoffRaw.TabIndex = 12;
            lblCutoffRaw.Text = "Cutoff (Raw)";
            // 
            // lblMaxCurrAmps
            // 
            lblMaxCurrAmps.AutoSize = true;
            lblMaxCurrAmps.Location = new Point(6, 122);
            lblMaxCurrAmps.Margin = new Padding(2, 0, 2, 0);
            lblMaxCurrAmps.Name = "lblMaxCurrAmps";
            lblMaxCurrAmps.Size = new Size(144, 20);
            lblMaxCurrAmps.TabIndex = 15;
            lblMaxCurrAmps.Text = "Max. Current (Amps)";
            // 
            // txtMaxCurrAmps
            // 
            txtMaxCurrAmps.Location = new Point(147, 122);
            txtMaxCurrAmps.Margin = new Padding(2);
            txtMaxCurrAmps.Name = "txtMaxCurrAmps";
            txtMaxCurrAmps.Size = new Size(108, 27);
            txtMaxCurrAmps.TabIndex = 8;
            // 
            // lblMaxCurrRaw
            // 
            lblMaxCurrRaw.AutoSize = true;
            lblMaxCurrRaw.Location = new Point(6, 90);
            lblMaxCurrRaw.Margin = new Padding(2, 0, 2, 0);
            lblMaxCurrRaw.Name = "lblMaxCurrRaw";
            lblMaxCurrRaw.Size = new Size(134, 20);
            lblMaxCurrRaw.TabIndex = 14;
            lblMaxCurrRaw.Text = "Max. Current (Raw)";
            // 
            // txtCutoffAmps
            // 
            txtCutoffAmps.Location = new Point(147, 58);
            txtCutoffAmps.Margin = new Padding(2);
            txtCutoffAmps.Name = "txtCutoffAmps";
            txtCutoffAmps.Size = new Size(108, 27);
            txtCutoffAmps.TabIndex = 9;
            // 
            // lblCutoffAmps
            // 
            lblCutoffAmps.AutoSize = true;
            lblCutoffAmps.Location = new Point(6, 58);
            lblCutoffAmps.Margin = new Padding(2, 0, 2, 0);
            lblCutoffAmps.Name = "lblCutoffAmps";
            lblCutoffAmps.Size = new Size(102, 20);
            lblCutoffAmps.TabIndex = 13;
            lblCutoffAmps.Text = "Cutoff (Amps)";
            // 
            // txtMaxCurrRaw
            // 
            txtMaxCurrRaw.Location = new Point(147, 90);
            txtMaxCurrRaw.Margin = new Padding(2);
            txtMaxCurrRaw.Name = "txtMaxCurrRaw";
            txtMaxCurrRaw.Size = new Size(108, 27);
            txtMaxCurrRaw.TabIndex = 10;
            // 
            // txtCutoffRaw
            // 
            txtCutoffRaw.Location = new Point(147, 26);
            txtCutoffRaw.Margin = new Padding(2);
            txtCutoffRaw.Name = "txtCutoffRaw";
            txtCutoffRaw.Size = new Size(108, 27);
            txtCutoffRaw.TabIndex = 11;
            // 
            // txtSimDuration
            // 
            txtSimDuration.Location = new Point(134, 6);
            txtSimDuration.Margin = new Padding(2);
            txtSimDuration.Name = "txtSimDuration";
            txtSimDuration.Size = new Size(97, 27);
            txtSimDuration.TabIndex = 7;
            // 
            // btnStopSim
            // 
            btnStopSim.Location = new Point(6, 102);
            btnStopSim.Margin = new Padding(2);
            btnStopSim.Name = "btnStopSim";
            btnStopSim.Size = new Size(90, 27);
            btnStopSim.TabIndex = 6;
            btnStopSim.Text = "Stop";
            btnStopSim.UseVisualStyleBackColor = true;
            // 
            // btnStartSim
            // 
            btnStartSim.Location = new Point(141, 102);
            btnStartSim.Margin = new Padding(2);
            btnStartSim.Name = "btnStartSim";
            btnStartSim.Size = new Size(90, 27);
            btnStartSim.TabIndex = 1;
            btnStartSim.Text = "Start";
            btnStartSim.UseVisualStyleBackColor = true;
            // 
            // lblSimProfile
            // 
            lblSimProfile.AutoSize = true;
            lblSimProfile.Location = new Point(6, 70);
            lblSimProfile.Margin = new Padding(2, 0, 2, 0);
            lblSimProfile.Name = "lblSimProfile";
            lblSimProfile.Size = new Size(108, 20);
            lblSimProfile.TabIndex = 5;
            lblSimProfile.Text = "Charger Profile";
            // 
            // lblBaudRate
            // 
            lblBaudRate.AutoSize = true;
            lblBaudRate.Location = new Point(6, 38);
            lblBaudRate.Margin = new Padding(2, 0, 2, 0);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(77, 20);
            lblBaudRate.TabIndex = 4;
            lblBaudRate.Text = "Baud Rate";
            // 
            // lblDuration
            // 
            lblDuration.AutoSize = true;
            lblDuration.Location = new Point(6, 6);
            lblDuration.Margin = new Padding(2, 0, 2, 0);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new Size(136, 20);
            lblDuration.TabIndex = 3;
            lblDuration.Text = "Duration (Seconds)";
            // 
            // cmbBxChgProfile
            // 
            cmbBxChgProfile.FormattingEnabled = true;
            cmbBxChgProfile.Location = new Point(134, 70);
            cmbBxChgProfile.Margin = new Padding(2);
            cmbBxChgProfile.Name = "cmbBxChgProfile";
            cmbBxChgProfile.Size = new Size(97, 28);
            cmbBxChgProfile.TabIndex = 2;
            // 
            // cmbBxBaudRate
            // 
            cmbBxBaudRate.FormattingEnabled = true;
            cmbBxBaudRate.Location = new Point(134, 38);
            cmbBxBaudRate.Margin = new Padding(2);
            cmbBxBaudRate.Name = "cmbBxBaudRate";
            cmbBxBaudRate.Size = new Size(97, 28);
            cmbBxBaudRate.TabIndex = 1;
            // 
            // tabDiagnostics
            // 
            tabDiagnostics.Controls.Add(btnCollectDiagnostics);
            tabDiagnostics.Controls.Add(btnClearDiagForm);
            tabDiagnostics.Controls.Add(grpboxDiagOutput);
            tabDiagnostics.Controls.Add(lblType);
            tabDiagnostics.Controls.Add(lblStickerInfo);
            tabDiagnostics.Controls.Add(lblToolSerialNum);
            tabDiagnostics.Controls.Add(lblSubmitDiagDate);
            tabDiagnostics.Controls.Add(lblOneKeyID);
            tabDiagnostics.Controls.Add(lblSubmitDiagCapacity);
            tabDiagnostics.Controls.Add(txtSubmitDiagDate);
            tabDiagnostics.Controls.Add(txtSubmitDiagSerial);
            tabDiagnostics.Controls.Add(txtSubmitDiagSticker);
            tabDiagnostics.Controls.Add(txtSubmitDiagType);
            tabDiagnostics.Controls.Add(txtOneKeyID);
            tabDiagnostics.Controls.Add(txtSubmitDiagCapacity);
            tabDiagnostics.Controls.Add(btnSubmitDiagForm);
            tabDiagnostics.Location = new Point(4, 29);
            tabDiagnostics.Margin = new Padding(2);
            tabDiagnostics.Name = "tabDiagnostics";
            tabDiagnostics.Padding = new Padding(2);
            tabDiagnostics.Size = new Size(1090, 361);
            tabDiagnostics.TabIndex = 4;
            tabDiagnostics.Text = "Submit Diagnostics";
            tabDiagnostics.UseVisualStyleBackColor = true;
            // 
            // btnCollectDiagnostics
            // 
            btnCollectDiagnostics.Location = new Point(237, 173);
            btnCollectDiagnostics.Margin = new Padding(2);
            btnCollectDiagnostics.Name = "btnCollectDiagnostics";
            btnCollectDiagnostics.Size = new Size(218, 27);
            btnCollectDiagnostics.TabIndex = 26;
            btnCollectDiagnostics.Text = "Collect Diagnostics";
            btnCollectDiagnostics.UseVisualStyleBackColor = true;
            // 
            // btnClearDiagForm
            // 
            btnClearDiagForm.Location = new Point(461, 205);
            btnClearDiagForm.Margin = new Padding(2);
            btnClearDiagForm.Name = "btnClearDiagForm";
            btnClearDiagForm.Size = new Size(122, 27);
            btnClearDiagForm.TabIndex = 23;
            btnClearDiagForm.Text = "Clear Form";
            btnClearDiagForm.UseVisualStyleBackColor = true;
            // 
            // grpboxDiagOutput
            // 
            grpboxDiagOutput.Controls.Add(rtbSubmitDiagReadOnly);
            grpboxDiagOutput.Location = new Point(237, 6);
            grpboxDiagOutput.Margin = new Padding(2);
            grpboxDiagOutput.Name = "grpboxDiagOutput";
            grpboxDiagOutput.Padding = new Padding(2);
            grpboxDiagOutput.Size = new Size(474, 160);
            grpboxDiagOutput.TabIndex = 22;
            grpboxDiagOutput.TabStop = false;
            grpboxDiagOutput.Text = "Diagnostic Output";
            // 
            // rtbSubmitDiagReadOnly
            // 
            rtbSubmitDiagReadOnly.Location = new Point(6, 26);
            rtbSubmitDiagReadOnly.Margin = new Padding(2);
            rtbSubmitDiagReadOnly.Name = "rtbSubmitDiagReadOnly";
            rtbSubmitDiagReadOnly.Size = new Size(462, 129);
            rtbSubmitDiagReadOnly.TabIndex = 16;
            rtbSubmitDiagReadOnly.Text = "";
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Location = new Point(13, 141);
            lblType.Margin = new Padding(2, 0, 2, 0);
            lblType.Name = "lblType";
            lblType.Size = new Size(40, 20);
            lblType.TabIndex = 21;
            lblType.Text = "Type";
            // 
            // lblStickerInfo
            // 
            lblStickerInfo.AutoSize = true;
            lblStickerInfo.Location = new Point(13, 109);
            lblStickerInfo.Margin = new Padding(2, 0, 2, 0);
            lblStickerInfo.Name = "lblStickerInfo";
            lblStickerInfo.Size = new Size(53, 20);
            lblStickerInfo.TabIndex = 20;
            lblStickerInfo.Text = "Sticker";
            // 
            // lblToolSerialNum
            // 
            lblToolSerialNum.AutoSize = true;
            lblToolSerialNum.Location = new Point(13, 77);
            lblToolSerialNum.Margin = new Padding(2, 0, 2, 0);
            lblToolSerialNum.Name = "lblToolSerialNum";
            lblToolSerialNum.Size = new Size(59, 20);
            lblToolSerialNum.TabIndex = 19;
            lblToolSerialNum.Text = "Serial #";
            // 
            // lblSubmitDiagDate
            // 
            lblSubmitDiagDate.AutoSize = true;
            lblSubmitDiagDate.Location = new Point(13, 45);
            lblSubmitDiagDate.Margin = new Padding(2, 0, 2, 0);
            lblSubmitDiagDate.Name = "lblSubmitDiagDate";
            lblSubmitDiagDate.Size = new Size(41, 20);
            lblSubmitDiagDate.TabIndex = 18;
            lblSubmitDiagDate.Text = "Date";
            // 
            // lblOneKeyID
            // 
            lblOneKeyID.AutoSize = true;
            lblOneKeyID.Location = new Point(13, 13);
            lblOneKeyID.Margin = new Padding(2, 0, 2, 0);
            lblOneKeyID.Name = "lblOneKeyID";
            lblOneKeyID.Size = new Size(85, 20);
            lblOneKeyID.TabIndex = 17;
            lblOneKeyID.Text = "One-Key ID";
            // 
            // lblSubmitDiagCapacity
            // 
            lblSubmitDiagCapacity.AutoSize = true;
            lblSubmitDiagCapacity.Location = new Point(13, 173);
            lblSubmitDiagCapacity.Margin = new Padding(2, 0, 2, 0);
            lblSubmitDiagCapacity.Name = "lblSubmitDiagCapacity";
            lblSubmitDiagCapacity.Size = new Size(66, 20);
            lblSubmitDiagCapacity.TabIndex = 25;
            lblSubmitDiagCapacity.Text = "Capacity";
            // 
            // txtSubmitDiagDate
            // 
            txtSubmitDiagDate.Location = new Point(96, 45);
            txtSubmitDiagDate.Margin = new Padding(2);
            txtSubmitDiagDate.Name = "txtSubmitDiagDate";
            txtSubmitDiagDate.Size = new Size(121, 27);
            txtSubmitDiagDate.TabIndex = 15;
            // 
            // txtSubmitDiagSerial
            // 
            txtSubmitDiagSerial.Location = new Point(96, 77);
            txtSubmitDiagSerial.Margin = new Padding(2);
            txtSubmitDiagSerial.Name = "txtSubmitDiagSerial";
            txtSubmitDiagSerial.Size = new Size(121, 27);
            txtSubmitDiagSerial.TabIndex = 14;
            // 
            // txtSubmitDiagSticker
            // 
            txtSubmitDiagSticker.Location = new Point(96, 109);
            txtSubmitDiagSticker.Margin = new Padding(2);
            txtSubmitDiagSticker.Name = "txtSubmitDiagSticker";
            txtSubmitDiagSticker.Size = new Size(121, 27);
            txtSubmitDiagSticker.TabIndex = 13;
            // 
            // txtSubmitDiagType
            // 
            txtSubmitDiagType.Location = new Point(96, 141);
            txtSubmitDiagType.Margin = new Padding(2);
            txtSubmitDiagType.Name = "txtSubmitDiagType";
            txtSubmitDiagType.Size = new Size(121, 27);
            txtSubmitDiagType.TabIndex = 11;
            // 
            // txtOneKeyID
            // 
            txtOneKeyID.Location = new Point(96, 13);
            txtOneKeyID.Margin = new Padding(2);
            txtOneKeyID.Name = "txtOneKeyID";
            txtOneKeyID.Size = new Size(121, 27);
            txtOneKeyID.TabIndex = 10;
            // 
            // txtSubmitDiagCapacity
            // 
            txtSubmitDiagCapacity.Location = new Point(96, 173);
            txtSubmitDiagCapacity.Margin = new Padding(2);
            txtSubmitDiagCapacity.Name = "txtSubmitDiagCapacity";
            txtSubmitDiagCapacity.Size = new Size(121, 27);
            txtSubmitDiagCapacity.TabIndex = 24;
            // 
            // btnSubmitDiagForm
            // 
            btnSubmitDiagForm.Location = new Point(589, 205);
            btnSubmitDiagForm.Margin = new Padding(2);
            btnSubmitDiagForm.Name = "btnSubmitDiagForm";
            btnSubmitDiagForm.Size = new Size(122, 27);
            btnSubmitDiagForm.TabIndex = 9;
            btnSubmitDiagForm.Text = "Submit Form";
            btnSubmitDiagForm.UseVisualStyleBackColor = true;
            // 
            // tabAbout
            // 
            tabAbout.Controls.Add(linkLabelKillaVolt);
            tabAbout.Controls.Add(lblKillaVoltAbout);
            tabAbout.Controls.Add(linkLabelMartin);
            tabAbout.Controls.Add(lblMartin);
            tabAbout.Controls.Add(lblAboutTitle);
            tabAbout.Location = new Point(4, 29);
            tabAbout.Margin = new Padding(2);
            tabAbout.Name = "tabAbout";
            tabAbout.Padding = new Padding(2);
            tabAbout.Size = new Size(1090, 361);
            tabAbout.TabIndex = 5;
            tabAbout.Text = "About";
            tabAbout.UseVisualStyleBackColor = true;
            // 
            // linkLabelKillaVolt
            // 
            linkLabelKillaVolt.AutoSize = true;
            linkLabelKillaVolt.LinkArea = new LinkArea(13, 60);
            linkLabelKillaVolt.Location = new Point(218, 141);
            linkLabelKillaVolt.Margin = new Padding(2, 0, 2, 0);
            linkLabelKillaVolt.Name = "linkLabelKillaVolt";
            linkLabelKillaVolt.Size = new Size(289, 25);
            linkLabelKillaVolt.TabIndex = 4;
            linkLabelKillaVolt.TabStop = true;
            linkLabelKillaVolt.Text = "GitHub Repo: https://github.com/KillaVolt";
            linkLabelKillaVolt.UseCompatibleTextRendering = true;
            // 
            // lblKillaVoltAbout
            // 
            lblKillaVoltAbout.AutoSize = true;
            lblKillaVoltAbout.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblKillaVoltAbout.Location = new Point(256, 115);
            lblKillaVoltAbout.Margin = new Padding(2, 0, 2, 0);
            lblKillaVoltAbout.Name = "lblKillaVoltAbout";
            lblKillaVoltAbout.Size = new Size(219, 23);
            lblKillaVoltAbout.TabIndex = 3;
            lblKillaVoltAbout.Text = "GUI designed By: KillaVolt";
            // 
            // linkLabelMartin
            // 
            linkLabelMartin.AutoSize = true;
            linkLabelMartin.LinkArea = new LinkArea(13, 60);
            linkLabelMartin.Location = new Point(160, 90);
            linkLabelMartin.Margin = new Padding(2, 0, 2, 0);
            linkLabelMartin.Name = "linkLabelMartin";
            linkLabelMartin.Size = new Size(429, 25);
            linkLabelMartin.TabIndex = 2;
            linkLabelMartin.TabStop = true;
            linkLabelMartin.Text = "GitHub Repo: https://github.com/mnh-jansson/m18-protocol/";
            linkLabelMartin.UseCompatibleTextRendering = true;
            linkLabelMartin.LinkClicked += linkLabel1_LinkClicked;
            // 
            // lblMartin
            // 
            lblMartin.AutoSize = true;
            lblMartin.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMartin.Location = new Point(179, 64);
            lblMartin.Margin = new Padding(2, 0, 2, 0);
            lblMartin.Name = "lblMartin";
            lblMartin.Size = new Size(386, 23);
            lblMartin.TabIndex = 1;
            lblMartin.Text = "Original M18 Protocol code by: Martin Jansson";
            // 
            // lblAboutTitle
            // 
            lblAboutTitle.Font = new Font("Segoe UI Black", 26F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lblAboutTitle.Image = (Image)resources.GetObject("lblAboutTitle.Image");
            lblAboutTitle.ImageAlign = ContentAlignment.MiddleLeft;
            lblAboutTitle.Location = new Point(115, 6);
            lblAboutTitle.Margin = new Padding(2, 0, 2, 0);
            lblAboutTitle.Name = "lblAboutTitle";
            lblAboutTitle.Size = new Size(506, 64);
            lblAboutTitle.TabIndex = 0;
            lblAboutTitle.Text = "         Pack Analyzer GUI";
            lblAboutTitle.TextAlign = ContentAlignment.TopCenter;
            // 
            // rtbDebugOutput
            // 
            rtbDebugOutput.Location = new Point(8, 400);
            rtbDebugOutput.Margin = new Padding(2);
            rtbDebugOutput.Name = "rtbDebugOutput";
            rtbDebugOutput.Size = new Size(1136, 224);
            rtbDebugOutput.TabIndex = 1;
            rtbDebugOutput.Text = "";
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1155, 637);
            Controls.Add(rtbDebugOutput);
            Controls.Add(tabControlM18Main);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            Name = "frmMain";
            ShowIcon = false;
            Text = "M18 Pack Analyzer";
            Load += frmMain_Load;
            tabControlM18Main.ResumeLayout(false);
            tabSimple.ResumeLayout(false);
            tabSimple.PerformLayout();
            grpOutput.ResumeLayout(false);
            tabAdvanced.ResumeLayout(false);
            tabAdvanced.PerformLayout();
            grpTryCmd.ResumeLayout(false);
            grpTryCmd.PerformLayout();
            grpbxDebugCmd.ResumeLayout(false);
            grpbxDebugCmd.PerformLayout();
            tabD2xx.ResumeLayout(false);
            tabSimulation.ResumeLayout(false);
            tabSimulation.PerformLayout();
            grpBxSimCustomProfile.ResumeLayout(false);
            grpBxSimCustomProfile.PerformLayout();
            tabDiagnostics.ResumeLayout(false);
            tabDiagnostics.PerformLayout();
            grpboxDiagOutput.ResumeLayout(false);
            tabAbout.ResumeLayout(false);
            tabAbout.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControlM18Main;
        private TabPage tabSimple;
        private TabPage tabAdvanced;
        private TabPage tabD2xx;
        private ComboBox cmbBxSerialPort;
        private RichTextBox rtbOutput;
        private TabPage tabSimulation;
        private TabPage tabDiagnostics;
        private Button btnConnect;
        private Button btnRefresh;
        private Label lblSerialPort;
        private GroupBox grpOutput;
        private Button btnDisconnect;
        private Button btnIdle;
        private Button btnActive;
        private ToolTip toolTipSimpleTab;
        private Button btnHealthReport;
        private Button btnReset;
        private Button btnCopyOutput;
        private Button btnReadAllSpreadsheet;
        private Button btnReadAllRegisters;
        private RichTextBox rtbAdvOutput;
        private RichTextBox rtbD2xxLog;
        private TextBox txtWriteMessage;
        private Button btnWriteMessage;
        private Button btnReadIDLabelled;
        private Button btnReadIDRaw;
        private TabPage tabAbout;
        private CheckBox chkboxRxLog;
        private CheckBox chkbxTXLog;
        private Button btnSaveTxRxState;
        private Button btnBruteAddr;
        private Button btnFullBrute;
        private GroupBox grpbxDebugCmd;
        private TextBox txtAddrMSB;
        private Label lblResponseLength;
        private Label lblDebugCmdLength;
        private Label lblLSB;
        private Label lblMSB;
        private TextBox txtResponseLength;
        private TextBox txtDebugCmdLength;
        private TextBox txtAddrLSB;
        private GroupBox grpTryCmd;
        private TextBox txtTryCmdLength;
        private TextBox txtAddressLow;
        private TextBox txtAddressHigh;
        private Label lblTryCmdLength;
        private Label lblAddressLow;
        private Label lblAddrHigh;
        private Label lblCommand;
        private TextBox txtCommand;
        private ComboBox cmbBxChgProfile;
        private ComboBox cmbBxBaudRate;
        private Button btnRestoreTxRxState;
        private Button btnTryCommand;
        private Button btnDebugCommand;
        private Button btnSubmitDiagForm;
        private Button btnCollectDiagnostics;
        private Label lblType;
        private Label lblStickerInfo;
        private Label lblToolSerialNum;
        private Label lblSubmitDiagDate;
        private Label lblOneKeyID;
        private Label lblSubmitDiagCapacity;
        private RichTextBox rtbSubmitDiagReadOnly;
        private TextBox txtSubmitDiagDate;
        private TextBox txtSubmitDiagSerial;
        private TextBox txtSubmitDiagSticker;
        private TextBox txtSubmitDiagType;
        private TextBox txtOneKeyID;
        private TextBox txtSubmitDiagCapacity;
        private Button btnStartSim;
        private Label lblSimProfile;
        private Label lblBaudRate;
        private Label lblDuration;
        private Button btnStopSim;
        private TextBox txtSimDuration;
        private TextBox txtCutoffRaw;
        private TextBox txtMaxCurrRaw;
        private TextBox txtCutoffAmps;
        private TextBox txtMaxCurrAmps;
        private Label lblMaxCurrAmps;
        private Label lblMaxCurrRaw;
        private Label lblCutoffAmps;
        private Label lblCutoffRaw;
        private GroupBox grpboxDiagOutput;
        private Button btnClearDiagForm;
        private GroupBox grpBxSimCustomProfile;
        private Label lblAboutTitle;
        private LinkLabel linkLabelMartin;
        private Label lblMartin;
        private LinkLabel linkLabelKillaVolt;
        private Label lblKillaVoltAbout;
        private RichTextBox rtbDebugOutput;
        private Button btnTestFT232;
    }
}
