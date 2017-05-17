
& $PSScriptRoot\Build.ps1 -Resources $(get-childitem $PSSCriptRoot\Samples\PSTest*) -OutputFolder $PSScriptRoot\Release\Test

& $PSScriptRoot\Build.ps1 -Resources $PSScriptRoot\Samples\Demo-Basic.ps1 -OutputFolder $PSScriptRoot\Release\Basic
& $PSScriptRoot\Build.ps1 -Resources $PSScriptRoot\Samples\Demo-Wrapper.ps1 -OutputFolder $PSScriptRoot\Release\Wrapper
& $PSScriptRoot\Build.ps1 -Resources $PSScriptRoot\Samples\Demo-Wrapper.ps1 -OutputFolder $PSScriptRoot\Release\WrapperAdmin -Admin
& $PSScriptRoot\Build.ps1 -Resources $PSScriptRoot\Samples\Demo-Wrapper.ps1 -OutputFolder $PSScriptRoot\Release\WrapperAdminx64 -Admin -CPUType x64

