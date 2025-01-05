# M18 Protocol

This repository contains research about the Milwaukee M18 protocol.

First step is to fake the charger commands in order to verify that the communication works as expected.

Next step is figuring out what other commands are supported.

## Hardware

In order to simulate the charger, the following circuit is proposed:

![hardware](docs/wiring.png)

## Software

Running
```bash
python3 m18.py
```

Opens an interractive shell that can be used to send different commands. Refer to the instructions provided in the shell.