
<#
# Example of how to make calls to Edit-KeyValuePair
#>

cls

$MyDefaults = @(
    [PSCustomObject] @{ Tag="Engagement.CustomerLabOwner.FullName"; Name="Customer Name"; Value = "John Doe"; ToolTip = "Customer's organization name"; },
    [PSCustomObject] @{ Tag="Engagement.CustomerName"; Name="Customer Contact"; Value = "Contoso"; ToolTip = "Customer contact full name"; },
    [PSCustomObject] @{ Tag="Engagement.CustomerLabOwner.EmailAddress"; Name="Customer Contact Email"; Value = "John.Doe@Contoso.com"; ToolTip = "Customer contact email address"; },
    [PSCustomObject] @{ Tag="Engagement.CustomerLabOwner.PhoneNumber"; Name="Customer Contact Phone"; Value = "425-555-1212"; ToolTip = "Customer contact phone number"; },
    [PSCustomObject] @{ Tag="Engagement.DeliveryConsultant.FullName"; Name="Consultant Name"; Value = "Frank McConsultant"; ToolTip = "Consultant full name"; },
    [PSCustomObject] @{ Tag="Engagement.DeliveryConsultant.EmailAddress"; Name="Consultant Email"; Value = "Frank.M@NorthWind.com"; ToolTip = "Consultant's email address"; },
    [PSCustomObject] @{ Tag="Engagement.DeliveryConsultant.PhoneNumber"; Name="Consultant Phone"; Value = "206-555-1212"; ToolTip = "Consultant's telphone number."; },
    [PSCustomObject] @{ Tag=""; Name=""; Value = ""; ToolTip = ""; },
    [PSCustomObject] @{ Tag="HostEnvironment.LabID"; Name="Lab Identifer"; Value = "MyEnv"; ToolTip = "Unique prefix that is added to all virtual devices and files created by hydration for this lab defintion."; },
    [PSCustomObject] @{ Tag="HostEnvironment.LabStorePath"; Name="Lab Store"; Value = "c:\foo"; ToolTip = "Folder in which the lab should be stored. A subfolder will be created and named using the lab identifer provided above."; },
    [PSCustomObject] @{ Tag="LabGuests.LabGuest.GuestRoles.GuestRole.JoinDomain"; Name="DC1.contoso.com"; Value = "SomeValue"; ToolTip = "Internal Name."; }
    [PSCustomObject] @{ Tag="LabGuests.LabGuest.GuestRoles.GuestRole.DomainNetBiosName"; Name="DC1"; Value = "SomeValue"; ToolTip = "internal Domain."; }
)

$MyFields = $MyDefaults | edit-KeyValuePair -HeaderWidths 0,-175,1,0

$MyFields | Select-Object -Property Tag,Value | Out-String | Write-host

