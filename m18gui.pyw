"""Tkinter-based GUI wrapper around the :mod:`m18` protocol module."""

import sys, subprocess, threading, io, time, traceback, tkinter as tk
from tkinter import ttk, messagebox

def ensure_module(mod_name, pip_name):
    """Import ``mod_name``, installing ``pip_name`` via pip if missing."""
    try: __import__(mod_name)
    except ImportError:
        root = tk.Tk(); root.title("Installing dependencies")
        ttk.Label(root, text=f"Installing {pip_name}…").pack(padx=6, pady=6)
        pb = ttk.Progressbar(root, mode="indeterminate"); pb.pack(fill="x", padx=6, pady=(0, 6)); pb.start(12)
        root.update_idletasks()
        subprocess.check_call([sys.executable, "-m", "pip", "install", pip_name], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        pb.stop(); root.destroy()
        try: __import__(mod_name)
        except ImportError: messagebox.showerror("Error", f"Installed {pip_name} but cannot import."); sys.exit(1)
ensure_module("serial", "pyserial")
from serial.tools import list_ports
import m18

class M18GUI(tk.Tk):
    """Simple desktop interface for performing common M18 operations."""
    def __init__(self):
        super().__init__()
        self.title("M18 Protocol GUI")
        self.geometry("610x370")
        self.resizable(False, False)
        self.m18_obj = None
        self.sim_thread = None
        self.sim_stop_event = None
        self.sim_profiles = {"Gentle": (150, 2500), "Normal": (300, 6000), "Aggressive": (450, 9000)}
        self.custom_cutoff_raw_var = tk.StringVar(value="300")
        self.custom_cutoff_amps_var = tk.StringVar(value="0.30")
        self.custom_max_raw_var = tk.StringVar(value="6000")
        self.custom_max_amps_var = tk.StringVar(value="6.00")
        self._sync_in_progress = False
        self.port_map = {}
        self.create_widgets()
        self.refresh_ports()
        self.update_profile_display()

    def create_widgets(self):
        """Build all tabs, controls, and shared output areas."""
        self.notebook = ttk.Notebook(self); self.notebook.pack(fill="both", expand=True, padx=0, pady=0)
        self.main_tab, self.console_tab, self.sim_tab, self.about_tab = (ttk.Frame(self.notebook) for _ in range(4))
        self.notebook.add(self.main_tab, text="Main Controls"); self.notebook.add(self.console_tab, text="Interactive Console")
        self.notebook.add(self.sim_tab, text="Simulation"); self.notebook.add(self.about_tab, text="About")

        # Main Controls
        top = ttk.Frame(self.main_tab); top.pack(fill="x", padx=5, pady=(4,1))
        ttk.Label(top, text="Serial port:").grid(row=0, column=0, sticky="w")
        self.port_var = tk.StringVar()
        self.port_combo = ttk.Combobox(top, textvariable=self.port_var, state="readonly", width=40); self.port_combo.grid(row=0, column=1, padx=2, sticky="w")
        ttk.Button(top, text="Refresh", width=7, command=self.refresh_ports).grid(row=0, column=2, padx=2)
        self.connect_btn = ttk.Button(top, text="Connect", width=7, command=self.connect_device); self.connect_btn.grid(row=0, column=3, padx=2)
        self.disconnect_btn = ttk.Button(top, text="Disconnect", width=9, command=self.disconnect_device, state="disabled"); self.disconnect_btn.grid(row=0, column=4, padx=2)
        self.status_var = tk.StringVar(value="Not connected")
        ttk.Label(top, textvariable=self.status_var).grid(row=1, column=0, columnspan=5, sticky="w", pady=(2,0))
        cmds = ttk.LabelFrame(self.main_tab, text="Commands"); cmds.pack(fill="x", padx=5, pady=(3,1))
        self.idle_btn = ttk.Button(cmds, text="Idle (TX low, safe to connect)", width=25, command=self.cmd_idle, state="disabled"); self.idle_btn.grid(row=0, column=0, padx=2, pady=2)
        self.health_btn = ttk.Button(cmds, text="Health report", width=15, command=self.cmd_health, state="disabled"); self.health_btn.grid(row=0, column=1, padx=2, pady=2)
        self.clipboard_btn = ttk.Button(cmds, text="Copy all registers to clipboard", width=28, command=self.cmd_clipboard, state="disabled"); self.clipboard_btn.grid(row=0, column=2, padx=2, pady=2)
        # Output (only on non-About tabs)
        self.bottom_frame = ttk.LabelFrame(self, text="Output"); self.bottom_frame.pack(fill="x", padx=5, pady=(0,4), side="bottom")
        self.output_text = tk.Text(self.bottom_frame, wrap="word", height=6, font=("Consolas", 9)); self.output_text.pack(fill="both", expand=True, padx=2, pady=2)
        scrollbar = ttk.Scrollbar(self.bottom_frame, orient="vertical", command=self.output_text.yview); scrollbar.pack(side="right", fill="y")
        self.output_text.configure(yscrollcommand=scrollbar.set)
        def on_tab_changed(event):
            tab = event.widget.tab(event.widget.select(), "text")
            if tab == "About": self.bottom_frame.pack_forget()
            else: self.bottom_frame.pack(fill="x", padx=5, pady=(0,4), side="bottom")
        self.notebook.bind("<<NotebookTabChanged>>", on_tab_changed)

        # Console Tab
        c_frame = ttk.Frame(self.console_tab); c_frame.pack(fill="both", expand=True, padx=4, pady=2)
        ttk.Label(c_frame, text="Python console. Use 'm' for M18 object.").pack(anchor="w", pady=(0,1))
        self.console_text = tk.Text(c_frame, height=4, wrap="none", font=("Consolas",9)); self.console_text.pack(fill="x", padx=1, pady=(0,1))
        c_btns = ttk.Frame(c_frame); c_btns.pack(fill="x")
        ttk.Button(c_btns, text="Execute", width=10, command=self.run_console_code).pack(side="left", padx=3)
        ttk.Button(c_btns, text="Clear", width=7, command=lambda: self.console_text.delete("1.0", tk.END)).pack(side="left", padx=3)

        # Simulation Tab
        s_frame = ttk.Frame(self.sim_tab); s_frame.pack(fill="both", padx=4, pady=1)
        ttk.Label(s_frame, text="Simulation duration (sec):").grid(row=0, column=0, sticky="w", padx=2)
        self.sim_duration_var = tk.StringVar(value="60")
        ttk.Entry(s_frame, textvariable=self.sim_duration_var, width=5).grid(row=0, column=1, sticky="w", padx=2)
        ttk.Label(s_frame, text="Sim baudrate (concept):").grid(row=1, column=0, sticky="w", padx=2)
        self.sim_baud_var = tk.StringVar(value="4800")
        ttk.Combobox(s_frame, textvariable=self.sim_baud_var, state="readonly", width=8, values=["1200","2400","4800","9600"]).grid(row=1, column=1, sticky="w", padx=2)
        ttk.Label(s_frame, text="UART to pack always runs at 4800 baud.\nNo real charging current flows from USB.").grid(row=2, column=0, columnspan=2, sticky="w", padx=2, pady=(0,6))
        ttk.Label(s_frame, text="Charger profile:").grid(row=3, column=0, sticky="w", padx=2)
        self.sim_profile_var = tk.StringVar(value="Normal")
        profs = list(self.sim_profiles.keys())+["Custom"]
        prof_box = ttk.Combobox(s_frame, textvariable=self.sim_profile_var, state="readonly", width=10, values=profs)
        prof_box.grid(row=3, column=1, sticky="w", padx=2)
        prof_box.bind("<<ComboboxSelected>>", self.on_profile_changed)
        self.profile_info_var = tk.StringVar()
        ttk.Label(s_frame, textvariable=self.profile_info_var, foreground="blue").grid(row=4, column=0, columnspan=2, sticky="w", padx=2, pady=(0,2))
        self.sim_status_var = tk.StringVar(value="Simulation idle. Connect to a battery to enable.")
        ttk.Label(s_frame, textvariable=self.sim_status_var).grid(row=5, column=0, columnspan=2, sticky="w", padx=2)
        self.sim_start_btn = ttk.Button(s_frame, text="Start simulation", width=15, command=self.start_simulation, state="disabled")
        self.sim_start_btn.grid(row=6, column=0, padx=2, pady=2)
        self.sim_stop_btn = ttk.Button(s_frame, text="Stop simulation", width=15, command=self.stop_simulation, state="disabled")
        self.sim_stop_btn.grid(row=6, column=1, padx=2, pady=2)

        # About Tab
        import webbrowser
        a_frame = ttk.Frame(self.about_tab)
        a_frame.place(relx=0.5, rely=0.09, anchor="n")
        ttk.Label(a_frame, text="M18 Protocol GUI", font=("Segoe UI", 15, "bold")).pack(pady=(0,1))
        ttk.Label(a_frame, text="Core protocol by Martin Jansson", font=("Segoe UI", 9, "italic")).pack(pady=(5,0))
        link2 = ttk.Label(a_frame, text="Original: https://github.com/mnh-jansson/m18-protocol", foreground="blue", cursor="hand2"); link2.pack()
        link2.bind("<Button-1>", lambda e: webbrowser.open("https://github.com/mnh-jansson/m18-protocol"))
        ttk.Label(a_frame, text="Developed by KillaVolt", font=("Segoe UI", 9)).pack()
        link = ttk.Label(a_frame, text="GitHub Profile: https://github.com/KillaVolt", foreground="blue", cursor="hand2"); link.pack()
        link.bind("<Button-1>", lambda e: webbrowser.open("https://github.com/KillaVolt"))

        table = ttk.Treeview(a_frame, columns=("addr", "desc"), show="headings", height=8)
        table.heading("addr", text="Hex Addr"); table.heading("desc", text="Meaning")
        table.column("addr", width=65, anchor="center"); table.column("desc", width=150, anchor="w"); table.pack()
        for addr, desc in [("F85D","1: Command"),("F85E","4: Access"),("F85F","3: Length"),("F860","Address?"),("F861","Address?"),("F862","Length?"),("F863","N/A"),("F864","Checksum")]:
            table.insert("", "end", values=(addr,desc))

    # -------- Utility / Port / Logging ----------
    def log(self, text, clear=False):
        """Append ``text`` to the shared output area, optionally clearing it."""
        self.output_text.delete("1.0", tk.END) if clear else None; self.output_text.insert(tk.END, text + "\n"); self.output_text.see(tk.END)

    def sim_log(self, text):
        """Prefix simulation messages to distinguish them in the log area."""
        self.log(f"[SIM] {text}")

    def set_status(self, text):
        """Update the status label at the top of the UI."""
        self.status_var.set(text)

    def refresh_ports(self):
        """Enumerate available serial ports and repopulate the dropdown."""
        ports = list_ports.comports(); names = []; self.port_map.clear()
        for p in ports:
            label = f"{p.device} — {p.description} ({getattr(p,'manufacturer','') or ''})".strip()
            names.append(label); self.port_map[label] = p.device
        self.port_combo["values"] = names; self.port_combo.current(0) if names else self.port_combo.set("")
        self.set_status("Ports refreshed")

    def require_connection(self):
        """Warn the user if no device is connected before running an action."""
        if self.m18_obj is None: messagebox.showwarning("Not connected", "Please connect to a port first."); return False
        return True

    def capture_output(self, func, *a, **k):
        """Run ``func`` while capturing stdout into a string."""
        buf, old = io.StringIO(), sys.stdout
        try: sys.stdout = buf; func(*a, **k)
        finally: sys.stdout = old
        return buf.getvalue()

    # --------- Connect/Disconnect -----------
    def connect_device(self):
        """Instantiate :class:`m18.M18` using the selected serial port."""
        sel = self.port_var.get()
        port = self.port_map.get(sel, sel)
        try: self.m18_obj = m18.M18(port)
        except Exception as e:
            self.m18_obj = None; messagebox.showerror("Connection error", f"Failed to connect to {port}.\n\n{e}")
            self.set_status("Connection failed"); return
        self.set_status(f"Connected to {port}"); self.log(f"Connected to {port}", clear=True)
        for b in [self.connect_btn]: b.configure(state="disabled")
        for b in [self.disconnect_btn, self.idle_btn, self.health_btn, self.clipboard_btn, self.sim_start_btn]: b.configure(state="normal")

    def disconnect_device(self):
        """Return the pack to idle, close the port, and reset UI state."""
        self.stop_simulation()
        if self.m18_obj:
            try: self.m18_obj.idle()
            except: pass
            try: self.m18_obj.port.close()
            except: pass
            self.m18_obj = None
        self.set_status("Disconnected"); self.log("Disconnected")
        for b in [self.connect_btn]: b.configure(state="normal")
        for b in [self.disconnect_btn, self.idle_btn, self.health_btn, self.clipboard_btn, self.sim_start_btn, self.sim_stop_btn]: b.configure(state="disabled")

    # ----------- Command Handlers -----------
    def cmd_idle(self):
        """Invoke ``idle`` on the connected device in a worker thread."""
        if not self.require_connection(): return
        def work():
            try:
                self.m18_obj.idle()
                self.after(0, lambda: self.log("TX now low (<1V). Safe to connect battery."))
                self.after(0, lambda: self.set_status("Idle (TX low)"))
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("Idle error", str(e)))
        threading.Thread(target=work, daemon=True).start()

    def cmd_health(self):
        """Run ``health`` and display captured output."""
        if not self.require_connection(): return
        def work():
            try:
                output = self.capture_output(self.m18_obj.health)
                def finish():
                    self.log("=== Health report ===", clear=True)
                    self.log(output)
                    self.set_status("Health report complete")
                self.after(0, finish)
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("Health error", str(e)))
        threading.Thread(target=work, daemon=True).start()

    def cmd_clipboard(self):
        """Copy all register values to the clipboard."""
        if not self.require_connection(): return
        def work():
            try:
                output = self.capture_output(self.m18_obj.read_id, None, True, "raw")
                def finish():
                    self.clipboard_clear()
                    self.clipboard_append(output)
                    self.log("Register data copied to clipboard")
                self.after(0, finish)
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("Clipboard error", str(e)))
        threading.Thread(target=work, daemon=True).start()

    # ---------- Interactive Console ----------
    def run_console_code(self):
        """Execute arbitrary Python with the M18 object available as ``m``."""
        code = self.console_text.get("1.0", tk.END).strip()
        if not code: messagebox.showinfo("No code", "Please enter some code."); return
        if not self.require_connection(): return
        def work():
            buf = io.StringIO(); old_stdout, old_stderr = sys.stdout, sys.stderr
            sys.stdout = sys.stderr = buf
            try:
                env = {"m": self.m18_obj}
                try: exec(code, env, {})
                except Exception: traceback.print_exc()
            finally: sys.stdout, sys.stderr = old_stdout, old_stderr
            output = buf.getvalue()
            def finish():
                self.log("=== Console execution ===")
                self.log(output if output.strip() else "(no output)")
            self.after(0, finish)
        threading.Thread(target=work, daemon=True).start()

    # ---------- Profile Display ----------
    def on_profile_changed(self, event=None): self.update_profile_display()
    def update_profile_display(self):
        """Show human readable info about the selected simulation profile."""
        p = self.sim_profile_var.get()
        if p in self.sim_profiles:
            c, m = self.sim_profiles[p]
            info = f"Profile '{p}': Cutoff {c} ({c/1000:.2f}A), Max {m} ({m/1000:.2f}A). Simulation only."
            self.profile_info_var.set(info)
        else: self.profile_info_var.set("Custom profile: adjust fields below.")
    def get_profile_currents(self):
        """Return cutoff and max currents based on profile or custom fields."""
        p = self.sim_profile_var.get()
        if p in self.sim_profiles: return self.sim_profiles[p]
        try:
            c = int(self.custom_cutoff_raw_var.get())
            m = int(self.custom_max_raw_var.get())
            if c < 0 or m <= 0 or c > 20000 or m > 20000: raise ValueError
        except: raise ValueError("Invalid custom values.")
        return c, m

    # ---------- Simulation ----------
    def start_simulation(self):
        """Spawn a background thread to perform the charging dialogue."""
        if not self.require_connection(): return
        if self.sim_thread and self.sim_thread.is_alive():
            messagebox.showinfo("Running", "Simulation already running."); return
        try:
            duration = float(self.sim_duration_var.get())
            if duration <= 0: raise ValueError
        except: messagebox.showwarning("Invalid duration", "Enter a positive number."); return
        sim_baud = self.sim_baud_var.get()
        interval_map = {"1200": 1.0, "2400": 0.75, "4800": 0.5, "9600": 0.25}
        keepalive_interval = interval_map.get(sim_baud, 0.5)
        try:
            cutoff, maxc = self.get_profile_currents()
        except Exception as e:
            messagebox.showwarning("Profile error", str(e))
            return

        pname = self.sim_profile_var.get()

        self.sim_stop_event = threading.Event()
        self.sim_thread = threading.Thread(
            target=self._simulation_worker,
            args=(
                duration,
                keepalive_interval,
                pname,
                cutoff,
                cutoff / 1000.0,
                maxc,
                maxc / 1000.0,
            ),
            daemon=True,
        )
        self.sim_thread.start()

        self.sim_start_btn.configure(state="disabled")
        self.sim_stop_btn.configure(state="normal")
        self.sim_status_var.set(f"Simulation running ({pname}, baud {sim_baud})")

        self.sim_log(
            f"Start {duration}s | {pname} | "
            f"cutoff={cutoff} ({cutoff/1000:.2f}A) | "
            f"max={maxc} ({maxc/1000:.2f}A) | "
            f"interval={keepalive_interval:.2f}s"
        )

    def stop_simulation(self):
        """Signal the simulation worker to exit."""
        if self.sim_stop_event:
            self.sim_stop_event.set()

    def _simulation_worker(
        self,
        duration,
        interval,
        pname,
        cutoff_raw,
        cutoff_amps,
        max_raw,
        max_amps,
    ):
        """Perform the scripted keepalive dialogue for the configured duration."""
        m = self.m18_obj
        if m is None:
            self.after(
                0,
                lambda: messagebox.showerror(
                    "Simulation error", "No active M18 connection."
                ),
            )
            return

        old_cutoff = getattr(m, "CUTOFF_CURRENT", None)
        old_max = getattr(m, "MAX_CURRENT", None)

        m.CUTOFF_CURRENT = cutoff_raw
        m.MAX_CURRENT = max_raw

        start_time = time.time()

        try:
            self.after(0, lambda: self.sim_log("Resetting and negotiating charger state..."))

            try:
                m.reset()
            except Exception as e:
                self.after(0, lambda: self.sim_log(f"reset() failed: {e}"))
                return

            try:
                m.configure(2)
                m.get_snapchat()
                time.sleep(0.6)
                m.keepalive()
                m.configure(1)
                m.get_snapchat()
            except Exception as e:
                self.after(0, lambda: self.sim_log(f"Initial negotiation failed: {e}"))
                return

            while not self.sim_stop_event.is_set():
                elapsed = time.time() - start_time
                if elapsed >= duration:
                    break

                try:
                    time.sleep(interval)
                    m.keepalive()
                    self.after(
                        0,
                        lambda e=elapsed: self.sim_log(f"Keepalive t={e:.1f}s"),
                    )
                except Exception as e:
                    self.after(0, lambda: self.sim_log(f"keepalive() failed: {e}"))
                    break

        finally:
            try:
                m.idle()
            except Exception:
                pass

            if old_cutoff is not None:
                m.CUTOFF_CURRENT = old_cutoff
            if old_max is not None:
                m.MAX_CURRENT = old_max

            def finish():
                self.sim_log("Simulation finished. Returned to idle.")
                self.sim_status_var.set("Simulation idle.")
                self.sim_start_btn.configure(state="normal")
                self.sim_stop_btn.configure(state="disabled")

            self.after(0, finish)


if __name__ == "__main__":
    app = M18GUI()
    app.mainloop()
