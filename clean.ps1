# Script PowerShell para remover pastas bin e obj de forma forçada
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
