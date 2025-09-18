@echo off

echo ***************************************************
echo ** Runs m18.py with spreadsheet output and       **
echo ** copies to clipboard. You will not see output, **
echo ** just a completion message. Then you can       **
echo ** 'ctrl+v' into a spreadsheet                   **
echo **                                               **
echo ** THIS WILL TAKE ~10 SECONDS                    **
echo **                                               **
echo ** RIGHT-CLICK AND EDIT .BAT FILE.               **
echo ** CHANGE "--port COM5" to where your adapter is **
echo ** (you can delete this message)                 **
echo ***************************************************

python.exe .\m18.py --ss --port COM5 | clip.exe

echo:  
echo ***************************************************
echo ** Finished. Now use 'ctrl+v' to paste           **
echo ** diagnostics output into spreadsheet           **
echo **                                               **
echo ** If you get errors, you must edit this batch   **
echo ** file to have the correct port. You may also   **
echo ** have hardware issues. See github              **
echo ***************************************************

cmd /k