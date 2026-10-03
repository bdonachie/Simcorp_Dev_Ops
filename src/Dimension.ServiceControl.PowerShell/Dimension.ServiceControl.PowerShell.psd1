@{
    RootModule        = 'Dimension.ServiceControl.PowerShell.dll'
    ModuleVersion     = '1.0.0'
    GUID              = '6b0de76e-ae63-429f-994f-84a44eab5870'
    Author            = 'Ben Donachie'
    Description       = 'Cmdlets for querying and controlling SimCorp Dimension services through ScdSvcCtl.exe.'

    # Works in Windows PowerShell 5.1 and PowerShell 7+, since the assemblies target
    # netstandard2.0.
    PowerShellVersion = '5.1'

    # Loaded before the root module so the wrapper types resolve from the module folder
    # rather than relying on probing.
    RequiredAssemblies = @('Dimension.ServiceControl.dll')

    CmdletsToExport   = @(
        'Get-DimensionService',
        'Get-DimensionAgent',
        'Get-DimensionQueueItem',
        'Start-DimensionService',
        'Stop-DimensionService',
        'Remove-DimensionQueueItem',
        'Set-DimensionServiceControlPath',
        'Get-DimensionServiceControlPath',
        'Test-DimensionServiceControl'
    )

    FunctionsToExport = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    PrivateData = @{
        PSData = @{
            Tags       = @('SimCorp', 'Dimension', 'ScdSvcCtl', 'Operations')
            ProjectUri = 'https://github.com/bdonachie/Simcorp_Dev_Ops'
            LicenseUri = 'https://github.com/bdonachie/Simcorp_Dev_Ops/blob/main/LICENSE'
        }
    }
}
