using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace M18BatteryInfo
{
    /// <summary>
    /// Hosts the m18.py process and provides asynchronous helpers for sending commands, capturing
    /// stdout/stderr, and detecting the Python REPL prompt. The class is intentionally thin and
    /// delegates all protocol behavior to the Python script.
    /// </summary>
    public sealed class PythonProcessController : IDisposable
    {
        private readonly object _syncRoot = new();
        private Process? _process;
        private TaskCompletionSource<bool>? _promptTcs;
        private CancellationTokenSource? _shutdownCts;

        public string PythonExecutablePath { get; set; }
        public string ScriptPath { get; set; }
        public bool IsRunning => _process != null && !_process.HasExited;

        public event Action<string>? OutputReceived;
        public event Action<string>? ErrorReceived;
        public event Action? PromptDetected;
        public event Action<int?>? Exited;

        public PythonProcessController(string pythonExecutablePath, string scriptPath)
        {
            PythonExecutablePath = pythonExecutablePath;
            ScriptPath = scriptPath;
        }

        /// <summary>
        /// Launch m18.py as a child process. Throws if the process is already running or if the
        /// underlying executable/script cannot be started.
        /// </summary>
        public Task StartAsync(string? portArgument, CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Python process is already running.");
            }

            _shutdownCts?.Dispose();
            _shutdownCts = new CancellationTokenSource();
            _process?.Dispose();
            _process = null;

            var startInfo = BuildStartInfo(portArgument);
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data != null)
                {
                    HandleOutputLine(args.Data);
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null)
                {
                    ErrorReceived?.Invoke(args.Data);
                }
            };

            process.Exited += (_, _) =>
            {
                Exited?.Invoke(process.ExitCode);
                _shutdownCts?.Cancel();
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start python process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _process = process;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Send a command string to the Python REPL and wait for the next prompt. Returns false if a
        /// timeout occurs; the process is left running so callers can decide how to recover.
        /// </summary>
        public async Task<bool> SendCommandAsync(string command, TimeSpan timeout)
        {
            if (!IsRunning || _process?.StandardInput == null)
            {
                throw new InvalidOperationException("Python process is not running.");
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_syncRoot)
            {
                _promptTcs = tcs;
            }

            await _process.StandardInput.WriteLineAsync(command);
            await _process.StandardInput.FlushAsync();

            var delayTask = Task.Delay(timeout, _shutdownCts?.Token ?? CancellationToken.None);
            var completed = await Task.WhenAny(tcs.Task, delayTask);

            if (completed == tcs.Task && tcs.Task.IsCompleted)
            {
                return true;
            }

            lock (_syncRoot)
            {
                if (ReferenceEquals(_promptTcs, tcs))
                {
                    _promptTcs = null;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempt a graceful shutdown by sending exit() to stdin and waiting for the process to
        /// exit. If the process does not exit within the timeout, it will be killed.
        /// </summary>
        public async Task StopAsync(TimeSpan timeout)
        {
            if (!IsRunning || _process?.StandardInput == null)
            {
                return;
            }

            try
            {
                await _process.StandardInput.WriteLineAsync("exit()");
                await _process.StandardInput.FlushAsync();
            }
            catch
            {
                // Ignore input failures; we'll fall back to kill below.
            }

            var exitedTask = WaitForExitAsync();
            var completed = await Task.WhenAny(exitedTask, Task.Delay(timeout));

            if (completed != exitedTask && IsRunning)
            {
                try
                {
                    _process.Kill(true);
                }
                catch
                {
                    // Swallow kill failures to avoid throwing on shutdown.
                }
            }
        }

        public void Dispose()
        {
            _shutdownCts?.Cancel();
            _shutdownCts?.Dispose();
            _process?.Dispose();
        }

        private Task WaitForExitAsync()
        {
            if (_process == null)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => tcs.TrySetResult(true);

            if (_process.HasExited)
            {
                tcs.TrySetResult(true);
            }

            return tcs.Task;
        }

        private ProcessStartInfo BuildStartInfo(string? portArgument)
        {
            var workingDirectory = Path.GetDirectoryName(ScriptPath);
            var arguments = $"\"{ScriptPath}\"";
            if (!string.IsNullOrWhiteSpace(portArgument))
            {
                arguments += $" --port {portArgument}";
            }

            return new ProcessStartInfo
            {
                FileName = PythonExecutablePath,
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        private void HandleOutputLine(string line)
        {
            OutputReceived?.Invoke(line);

            if (IsPrompt(line))
            {
                lock (_syncRoot)
                {
                    _promptTcs?.TrySetResult(true);
                    _promptTcs = null;
                }

                PromptDetected?.Invoke();
            }
        }

        private static bool IsPrompt(string line)
        {
            var trimmed = line.Trim();
            return trimmed == ">>>" || trimmed.EndsWith(">>>", StringComparison.Ordinal);
        }
    }
}
