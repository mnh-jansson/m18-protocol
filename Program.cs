// *************************************************************************************************
// Program.cs
// ----------
// Entry point for the WinForms application. This file ties the executable to the UI defined in
// frmMain (the main form constructed in M18AnalyzerMain.cs / M18AnalyzerMain.Designer.cs) and uses
// ApplicationConfiguration to bootstrap high-DPI awareness and default fonts. The goal is to show
// how a C# WinForms app starts, how STAThread is required for COM-based UI components, and where the
// program transitions from the process boundary into user-interface code that eventually invokes
// serial-protocol logic in M18Protocol.cs and USB enumeration utilities in SerialPortUtil.cs. Even
// though the logic is minimal, commenting every line reveals the basic structure of a .NET desktop
// program for newcomers.
// *************************************************************************************************

namespace M18BatteryInfo
{
    /// <summary>
    /// Hosts the Main method required by .NET to start execution. The class is marked internal to
    /// keep the symbol inside the assembly while still being discoverable by the runtime. It
    /// delegates all user interaction to <see cref="frmMain"/>, which wires up event handlers to
    /// the serial protocol layer (<see cref="M18Protocol"/>) and utility helpers
    /// (<see cref="SerialPortUtil"/>) that talk to hardware.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application. The STAThread attribute is mandatory for
        ///  WinForms because UI components rely on COM apartment threading. The method enables
        ///  framework-level configuration (high DPI, default fonts) and then instantiates and shows
        ///  the primary form, which in turn registers all button click handlers that trigger
        ///  hardware traffic over UART.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Initialize WinForms settings such as DPI awareness and default font. This call lives
            // in System.Windows.Forms.ApplicationConfiguration and is automatically generated when
            // you create a modern WinForms project. Without it, UI rendering may be blurry on high
            // DPI displays. This line does not touch our custom code but configures the Win32/WinForms
            // plumbing that all later GUI code depends on.
            ApplicationConfiguration.Initialize();

            // Create and run the main window. Application.Run enters the WinForms message loop
            // (GetMessage/DispatchMessage under the hood) and will not exit until the form closes.
            // We construct frmMain, which is defined across M18AnalyzerMain.cs (logic) and
            // M18AnalyzerMain.Designer.cs (control layout). That form then interacts with
            // M18Protocol.cs to toggle UART lines and read battery data, plus SerialPortUtil.cs to
            // discover serial devices.
            Application.Run(new frmMain());
        }
    }
}
