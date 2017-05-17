
<#
# Example of how to make custom calls back to the host.
#>

######################################################################

Clear-Host 

#
# PSScriptRoot appears as a variable, but it is an Automatic Variable under the covers which presents unique challenges. 
# Use the code below to read the PSScriptRoot variable from the host and use within your scripts. 
#

$MyScriptRoot = Get-Variable PSScriptRoot | Select-Object -ExpandProperty Value

write-host "Location for EXE: $MyScriptRoot"

#########################################

Write-Host "Press Any Key To Continue..."
$host.ui.RawUI.ReadKey() | out-null

