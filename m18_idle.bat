:: Puts serial adapter TX in idle (low = <1V)
:: This prevents false charge count increases

@echo off

echo ***************************************************
echo ** RIGHT-CLICK AND EDIT .BAT FILE.               **
echo ** ADD "--port COM5" to command                  **
echo ** (or whatever port your serial adapter is on)  **
echo ** (you can delete this message                  **
echo ***************************************************

python.exe .\m18.py --idle
cmd /k