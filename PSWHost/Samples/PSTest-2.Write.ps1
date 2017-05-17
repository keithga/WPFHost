<#
# Example of how to write output to the host.
#>

$Host.UI.RawUI.WindowTitle = "Write"

###################################

$h = "Hello World pipeline"

# Output to the pipeline

'Hello World pipeline1!'
"Hello World pipeline2!"
$h

# Output to Write-Host

    write-host "Hello " -NoNewline
    write-host "World " -NoNewline
    write-host "!!!"

    write-host "Hello &#x0a; World"
    write-host "Hello `r`n World"

    write-host "Hello World &" -ForegroundColor Blue
    write-host "Hello World &" -ForegroundColor Red
    write-host "Hello World &" -BackgroundColor Black -ForegroundColor White
    write-host "Hello World &"
    write-host "Hello World <>"

    Start-Sleep 1

    clear-host


    write-host "Hello " -NoNewline
    write-host "World " -NoNewline
    write-host "!!!"

    write-host "Hello World &" -ForegroundColor Blue
    write-host "Hello World &" -ForegroundColor Red
    write-host "Hello World &" -BackgroundColor Black -ForegroundColor White
    write-host "Hello World &"
    write-host "Hello World <>"

    Write-Host ("1234567890" * 60)


Write-Host -NoNewline $h.split()[0]
Write-Host -NoNewline " "
Write-Host            $h.split()[-1]

# Colors

write-host "$h" -BackgroundColor White -ForegroundColor Red

foreach ( $FC in [System.Enum]::GetNames('System.ConsoleColor') )
{
    foreach ( $BC in [System.Enum]::GetNames('System.ConsoleColor') )
    {
        write-host "$h" -ForegroundColor $FC -BackgroundColor $BC
        start-sleep -Milliseconds 1
    }
}

Write-Host "That's a lot of text"

start-sleep 5

clear-host

write-host "screen should be cleared..."

#Other types of output

$SavedVerbose = $VerbosePreference
$VerbosePreference = "SilentlyContinue"
write-verbose "This message should not be written to the console $h"
$VerbosePreference = "Continue"
write-verbose $h
$VerbosePreference = $SavedVerbose


# Use $debugpreference to prevent Write-Debug from asking for user input.
$debugPreference = "COntinue"
WRite-Debug $h

Write-Warning $h

WRite-Error $h

start-sleep 2

