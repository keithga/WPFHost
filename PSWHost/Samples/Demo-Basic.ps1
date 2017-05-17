
<#
.SYNOPSIS
Simple Demo Program

.DESCRIPTION
Some basic demos of the PSWHost framework

.NOTES
Copyright Keith Garner (KeithGa@KeithGa.com) all rights reserved.
Apache License 2.0

#>

[CmdletBinding()]
Param(
	[parameter(Mandatory=$False,HelpMessage="Source File")]
	[System.IO.FileInfo] $Path,

	[parameter(Mandatory=$False,HelpMessage="Destination Folder")]
	[System.IO.DirectoryInfo] $Destination
)

#########################################

Write-Host "Hello World!"

Write-Host "What is your Name: " -NoNewline
$Name = Read-Host
Write-Host "Hello $Name"

$Cred = Get-Credential -UserName $Name -Message "Don't Enter Real Credentials, but something!"
write-Host "Hello $($Cred.UserName)"

#########################################

function Copy-MyItem
(
	[parameter(Mandatory=$true,HelpMessage="Source File")]
	[System.IO.FileInfo] $Path,

	[parameter(Mandatory=$true,HelpMessage="Destination Folder")]
	[System.IO.DirectoryInfo] $Destination
)
{
	write-Verbose "Copy $($Path.FUllName) to $($Destination.FullName)"
	copy-Item @PSBoundParameters -confirm
}

Write-Host "Copy a File..."

Copy-MyItem @PSBoundParameters

#########################################

Write-Host "Press Any Key To Continue..."
$host.ui.RawUI.ReadKey() | out-null

