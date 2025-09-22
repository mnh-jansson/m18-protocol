# M18 Protocol

This repository contains research about the Milwaukee M18 protocol.

First step was to fake the charger commands in order to verify that the communication works as expected. :white_check_mark: Next step was figuring out what other commands are supported. :white_check_mark:

While most of the registers and data are known, there are still some unknown. Contributions are welcome!

## Hardware

In order to simulate the charger, the following circuit is proposed:

**NOTE When using fake FT232 chips, break condition is not supported. The behaviour can be emulated by using the DTR line to pull the TX line low.**

List of [working and non-working devices](https://github.com/mnh-jansson/m18-protocol/discussions/16). Please add yours if not already listed.

The voltage of the USB to Serial adapter should be 3.3V

![hardware](docs/wiring.png)

## Requirements and Usage

To use this software, Python is required. Please read the [python](https://docs.python.org/3/) and [pip](https://pip.pypa.io/en/stable/installation/) documentation.

Install the required packages by running

```bash
pip install -r requirements.txt
```

Once the required packages are installed, run the following command. If the serial port is known, specify it using `--port` to speed things up.

```bash
python3 m18.py
```
or on Windows
```bash
python.exe m18.py
```

Alternatively, use the [uv](https://docs.astral.sh/uv/) package manager to create an isolated virtual environment, install the correct version of Python, install dependencies, and execute the project:

```bash
uv run m18.py
```


This opens an interactive shell that can be used to send different commands. Refer to the instructions provided in the shell.

## Output

* Most users will just want to use `m.health()` for a simple health report. 
* To see all registers, use `m.read_id()`
* To output all registers in a format that can be copy/pasted into a spreadsheet, use `m.read_id(output="raw")`
* To help us identify unknown registers, you can submit your diagnostics to us with `m.submit_form()`. This will prompt you for the 3 parts of the serial number, the type of battery (e.g. 3Ah high output), and other stuff that you can leave blank if you like

A spreadsheet template can be found below. Do NOT request access, go to `File -> Make a copy` or `File -> Download`

https://docs.google.com/spreadsheets/d/1rZZ3mtU2uwuo_uMv7O7hi5kyPA9AXUDU5CBsHKWMi-U/

## Windows Users
There are 4 .bat files for Windows users that are not familiar with the command line. Double-click on them to run them.
* `m18_idle.bat` will prompt you to select a serial device, then bring the TX (J2) pin low. This is recommended before connecting to the battery to avoid increasing the counter for dumb-charges
* `m18_health.bat` will print out a simple health report. The adapter must be connected to the battery or you will get errors
* `m18_interactive.bat` will put you into the interactive shell where you can call `m.health()`, `m.read_id()`, and submit your diagnostics to us with `m.submit_form()`
* `m18_clipboard.bat` will fetch all diagnostic registers and copy them to the clipboard. You must right-click on this .bat file and select `Edit`, then change `--port COM5` to whatever your port is. Once finished, you can select a cell in a spreadsheet (for example, the template provided above), and ctrl+v to copy all the registers

## Troubleshooting
Some USB serial adapters don't give/receive the correct voltages when paired with M18 batteries. To test if this is your problem, connect adapter to battery then measure voltages between B- and J2 (next to B-) and J1 (next to B+). Then use the m.idle() and m.high() commands.

You should see:
* m.idle(): J2<1V, J1<1V
* m.high(): J2>8V, J2>2V

I get:
* m.idle(): J2=0.13V, J1=0.81V
* m.high(): J2=8.8V, J1=3.3V

The [circuit mentioned here](https://github.com/mnh-jansson/m18-protocol/issues/7#issuecomment-3312151944) has fixed this issue for some people


