
<#

Copyright Keith Garner, All rights reserved.

This is just a placeholder File. 

#>


$MyScriptRoot = Get-Variable PSScriptRoot | Select-Object -ExpandProperty Value
Write-Host "ScriptRoot: $MyScriptRoot"

& "$MyScriptRoot\..\..\Samples\PSTest-1.HelloWorld.ps1"

exit

& "$MyScriptRoot\..\..\Samples\Demo-Basic.ps1"
& "$MyScriptRoot\..\..\Samples\Demo-Wrapper.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-2.Write.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-3.Progress.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-4.Reading.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-5.PromptForChoice.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-6.Prompt.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-7.CallBacks.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-8.ErrorHandling.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-9.out-gridview.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-A.edit-keyvaluepair.ps1"
& "$MyScriptRoot\..\..\Samples\PSTest-B.show-XAML.ps1"

