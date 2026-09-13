// *************************************************************************************************
// PythonScriptRunner.cs
// ---------------------
// Provides a tiny helper to execute the companion Python script (m18.py) from within the C# GUI.
// This allows reuse of the original Python implementation when experimenting or comparing results.
// Comments explain how ProcessStartInfo is configured to capture stdout/stderr without opening a
// console window, which is useful when embedding scripting in desktop applications.
// *************************************************************************************************

using System; // Access to AppDomain for locating the application base directory.
using System.Diagnostics; // Process and ProcessStartInfo for launching external interpreters.
using System.IO; // Path.Combine to build script path safely across OSes.
using System.Text; // StringBuilder for efficient concatenation of stdout/stderr text.

namespace M18BatteryInfo
{
    /// <summary>
    /// Utility class for launching the Python reference implementation (m18.py) with arbitrary
    /// arguments. Captures standard output and error streams for display in the WinForms UI.
    /// </summary>
    public static class PythonScriptRunner
    {
        /// <summary>
        /// Executes m18.py with the provided argument string. The method starts a child process
        /// configured to hide its console window, captures both stdout and stderr, waits for the
        /// process to exit, and returns the combined output. This is helpful for comparing Python
        /// protocol behavior to the C# port without leaving the GUI.
        /// </summary>
        /// <param name="arguments">Arguments passed directly to m18.py (for example, "health").</param>
        /// <returns>Combined standard output and error text from the Python process.</returns>
        public static string RunPythonScript(string arguments)
        {
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "m18.py"); // Build absolute path to bundled Python script located next to executable.

            var startInfo = new ProcessStartInfo
            {
                FileName = "python", // Use system Python interpreter (must be on PATH).
                Arguments = $"\"{scriptPath}\" {arguments}".Trim(), // Quote script path to handle spaces; append user arguments.
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory, // Run from app directory so relative imports/resources resolve.
                RedirectStandardOutput = true, // Capture stdout so UI can display results.
                RedirectStandardError = true, // Capture stderr for troubleshooting Python exceptions.
                UseShellExecute = false, // Required when redirecting streams; avoids shell involvement.
                CreateNoWindow = true // Prevent console window from flashing when launched from GUI.
            };

            using var process = new Process { StartInfo = startInfo }; // Create Process with configured start info inside using to ensure disposal.
            var outputBuilder = new StringBuilder(); // Collects both stdout and stderr text.

            process.Start(); // Launch python process; OS loads interpreter and script.

            outputBuilder.Append(process.StandardOutput.ReadToEnd()); // Read stdout to completion (blocking until stream closes).
            outputBuilder.Append(process.StandardError.ReadToEnd()); // Read stderr to completion; concatenated for holistic log.

            process.WaitForExit(); // Ensure process has exited before returning to caller.

            return outputBuilder.ToString(); // Return combined output for display/logging.
        }
    }
}
