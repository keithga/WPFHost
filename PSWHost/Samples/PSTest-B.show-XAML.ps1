
<#
Example of how to display XAML controls within WPFHost

    Show-XAMLControl -XAMLString <string> [-DefaultValues <hashtable>] [-ScriptBlock <scriptblock>] [-NewStdOut <string>] [-WaitForUser] [-FullScreen] [<CommonParameters>]

#>

#region Example 1 - Dispaly a simple control and remove
####################################

Write-Host "Example 1 - Display a Graphic embedded in a UserControl"

# This control was created in Visual Studio and the *.xaml file contents were simplly copied here. 

$GraphicControl = @"
<UserControl x:Class="DelM_wizardSample.UserControl1"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:DelM_wizardSample"
             mc:Ignorable="d" Height="123.529" Width="418.235">
    <Grid>
        <Image x:Name="image" Source="https://c.s-microsoft.com/en-us/CMSImages/Microsoft-logo_rgb_c-gray.png?version=edbb485d-ca98-43e1-a006-428100e18ad7"/>
    </Grid>
</UserControl>
"@

Write-Host "Dispaly two controls and then remove"

$Graphic1 = $GraphicControl | Show-XAMLControl
$Graphic2 = $GraphicControl | Show-XAMLControl
Start-Sleep 2
$Graphic1 | Remove-XAMLControl
$Graphic2 | Remove-XAMLControl

cls

#endregion

#region Example 2 - Put a XAML overlay on the screen (fullscreen)
####################################

$WizardXAML = @"
<UserControl x:Class="DelM_wizardSample.WizardControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:DelM_wizardSample"
             mc:Ignorable="d" 
             d:DesignHeight="600" d:DesignWidth="800">
    <Grid >
        <DockPanel >
            <StackPanel DockPanel.Dock="Top" Height="80" Background="{DynamicResource {x:Static SystemColors.WindowBrushKey}}" Orientation="Horizontal" >
                <Image x:Name="image" Height="46" VerticalAlignment="Center" Source="file://C:\Users\Keith\Desktop\PSWHost\PSWHost\ico32512.ico" Margin="15"/>
                <Label x:Name="MainTitle" Content="Label" VerticalAlignment="Center" FontWeight="Bold" FontSize="16"/>
            </StackPanel>
            <StackPanel DockPanel.Dock="Left" Background="{DynamicResource {x:Static SystemColors.ControlBrushKey}}" Width="200">
                <StackPanel Margin="5" >
                    <Label x:Name="ProgressPane" />
                </StackPanel>
            </StackPanel>
            <StackPanel DockPanel.Dock="Left" Width="2" Background="White"/>
            <StackPanel DockPanel.Dock="Bottom"  Background="{DynamicResource {x:Static SystemColors.ControlBrushKey}}" Height="50"  >
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center" Height="50">
                    <Button x:Name="BackB" Content="&lt; Back" Height="27" Width="75" IsEnabled="False" />
                    <Button x:Name="NextB" Content="Next &gt;" Height="27" Width="75" IsDefault="True" Margin="0,0,10,0"/>
                    <Button x:Name="CancelB" Content="Cancel" Height="27" Width="75" IsCancel="True" Margin="0,0,10,0" />
                </StackPanel>
            </StackPanel>
            <StackPanel DockPanel.Dock="Bottom"  Height="1" Background="White"/>
                <ScrollViewer Background="{DynamicResource {x:Static SystemColors.ControlBrushKey}}" VerticalScrollBarVisibility="Auto" >
                    <DockPanel Margin="3" x:Name="WizardOut" LastChildFill="True" >
                        <!-- Hello World -->
                    </DockPanel>
                </ScrollViewer>
        </DockPanel>

    </Grid>
</UserControl>
"@


$Wizard = Show-XAMLControl -XAMLString $WizardXAML -NewStdOut "WizardOut" -fullscreen 

Write-Host "This message should appear within the window."

$Graphic1 = $GraphicControl | Show-XAMLControl

write-host
WRite-Host "Press Any Key to Continue"
$host.ui.RawUI.ReadKey() | out-null

$Graphic1 | Remove-XAMLControl

Remove-XAMLControl -InputObject $Wizard

Write-Host "DOne!"

#endregion

#region Example 2 - Display a XAML control with UI input
####################################

$MyScriptBlock = {
    function TextBox_Loaded
    {
        param ( $Sender, $Event )
        $Sender.text += "..."
    }

    function RadioButton1_checked
    {
        param ( $Sender, $Event )
        if ( $ProgressPane ) {
            $ProgressPane.Content += "."
        }
    }
}

$Defaults = @{
 ProgressPane = "Welcome`r`nEULA`r`nQuestion1`r`nQuestion2`r`nReady`r`nProgess`r`nFinished"
 MainTitle = "Some really cool control here!"
 slider= 5;
 datePicker = "05/18/1980"
 checkBox = $true;
 comboBox = 1;
 listBox = 1;
 radioButton1= $true;
 passwordBox = "123";
 textBox = "123";
}

$TestControl = @"
<Window x:Class="DelM_wizardSample.testcontrols"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:DelM_wizardSample"
        mc:Ignorable="d"
        Title="testcontrols" Height="300" Width="300">
    <Grid>
        <StackPanel>
            <Slider x:Name="slider"/>
            <DatePicker x:Name="datePicker"/>
            <CheckBox x:Name="checkBox" Content="CheckBox" />
            <ComboBox x:Name="comboBox" >
                <ComboBoxItem Content="One" />
                <ComboBoxItem Content="Two" />
                <ComboBoxItem Content="Three" />
            </ComboBox>

            <ListBox x:Name="listBox" >
                <ListBoxItem Content="One"  />
                <ListBoxItem Content="Two" />
                <ListBoxItem Content="Three" />
            </ListBox>
            <RadioButton x:Name="radioButton1" Content="RadioButton" />
            <RadioButton x:Name="radioButton2" Content="RadioButton" />
            <RadioButton x:Name="radioButton3" Content="RadioButton" />

            <PasswordBox x:Name="passwordBox"/>
            <TextBox x:Name="textBox" TextWrapping="Wrap" />

        </StackPanel>

    </Grid>
</Window>
"@

Write-Host "Display a wizard page as a control. Adding -WaitForUser will also cause OK/Cancel buttons to appear"

show-XAMLControl -XAMLString $TestControl -WaitForUser -DefaultValues $Defaults -ScriptBlock $MyScriptBlock

#endregion

#region Example 2 - 
####################################

Write-Host "This time wrap the control arround our wizard. The OK/Cancel buttons will come from the wizard template"

$Wizard = Show-XAMLControl -XAMLString $WizardXAML -NewStdOut "WizardOut" -fullscreen 

Write-Host "This message should appear within the window."

show-XAMLControl -XAMLString $TestControl -WaitForUser -DefaultValues $Defaults -ScriptBlock $MyScriptBlock 

Remove-XAMLControl -InputObject $Wizard

#endregion
