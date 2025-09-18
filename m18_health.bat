:: RECOMMEND RUNNING m18_idle.bat BEFORE CONNECTING TO BATTERY
:: Gives simple health report of M18 batteries
:: Add "--port COM5" (or whatever your port is) to avoid
:: having to enter it everytime

@echo off
echo ***************************************************
echo ** RIGHT-CLICK AND EDIT .BAT FILE.               **
echo ** ADD "--port COM5" to command                  **
echo ** (or whatever port your serial adapter is on)  **
echo ** (you can delete this message                  **
echo ***************************************************

python.exe .\m18.py --health
cmd /k