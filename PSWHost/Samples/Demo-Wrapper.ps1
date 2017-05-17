
<#
.SYNOPSIS
WPFHost wrapper around PowerShell

.DESCRIPTION
This script can be added to WPFHost.exe. Any script and/or command added to the WPFHost.exe commandline will be executed.

.EXAMPLE
c:> & '.\Release\WPFHost.exe' -command "copy-item c:\windows\notepad.exe c:\windows\MyEditor.exe -confirm"

.NOTES
Copyright Keith Garner (KeithGa@KeithGa.com) all rights reserved.
Apache License 2.0

#>

[cmdletbinding()]
param(
    [parameter(mandatory=$true, position=0, ValueFromRemainingArguments=$true)][string] $Command
)

Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Force -ErrorAction SilentlyContinue

invoke-expression -Command $Command
