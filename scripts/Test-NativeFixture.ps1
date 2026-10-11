# Windows PowerShell 5.1; HTTP collection is opt-in and restricted to registered reads.
[CmdletBinding(DefaultParameterSetName = 'Collect')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Collect')][string]$Fixture,
    [Parameter(Mandatory = $true, ParameterSetName = 'Collect')][string]$EvidenceDir,
    [Parameter(ParameterSetName = 'Collect')][string]$BaseUrl = 'http://127.0.0.1:8642',
    [Parameter(ParameterSetName = 'Collect')][switch]$Offline,
    [Parameter(ParameterSetName = 'Collect')][switch]$AllowMutation,
    [Parameter(ParameterSetName = 'Collect')][string]$EvidenceRoot,
    [Parameter(Mandatory = $true, ParameterSetName = 'Validate')][string]$ValidateEvidence
)
$ErrorActionPreference = 'Stop'
$RepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$LocalRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot '.local'))
if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) { $EvidenceRoot = $LocalRoot }

function Require($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Has($Object, [string]$Name) {
    return $null -ne $Object -and $null -ne $Object.PSObject.Properties[$Name]
}
function RequireFields($Object, $Fields) {
    foreach ($Field in $Fields) { Require (Has $Object $Field) "Missing field: $Field" }
}
function Nonempty($Value) { return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value) }
function ArrayValue($Value) { return $Value -is [Array] }
function JsonEqual($Left, $Right) {
    if ($null -eq $Left -or $null -eq $Right) { return $null -eq $Left -and $null -eq $Right }
    if ($Left -is [Array] -or $Right -is [Array]) {
        if ($Left -isnot [Array] -or $Right -isnot [Array] -or $Left.Count -ne $Right.Count) { return $false }
        for ($Index = 0; $Index -lt $Left.Count; $Index++) {
            if (-not (JsonEqual $Left[$Index] $Right[$Index])) { return $false }
        }
        return $true
    }
    if ($Left -is [pscustomobject] -or $Right -is [pscustomobject]) {
        if ($Left -isnot [pscustomobject] -or $Right -isnot [pscustomobject]) { return $false }
        $LeftKeys = @($Left.PSObject.Properties.Name)
        $RightKeys = @($Right.PSObject.Properties.Name)
        if ($LeftKeys.Count -ne $RightKeys.Count) { return $false }
        foreach ($Key in $LeftKeys) {
            if (@($RightKeys | Where-Object { $_ -ceq $Key }).Count -ne 1) { return $false }
            if (-not (JsonEqual $Left.$Key $Right.$Key)) { return $false }
        }
        return $true
    }
    # JSON numbers share one type, even when ConvertFrom-Json chooses int/long/double.
    $LeftNumber = $Left -is [int] -or $Left -is [long] -or $Left -is [double] -or $Left -is [decimal]
    $RightNumber = $Right -is [int] -or $Right -is [long] -or $Right -is [double] -or $Right -is [decimal]
    if ($LeftNumber -or $RightNumber) { return $LeftNumber -and $RightNumber -and $Left -ceq $Right }
    if ($Left -is [string] -or $Right -is [string]) { return $Left -is [string] -and $Right -is [string] -and $Left -ceq $Right }
    if ($Left -is [bool] -or $Right -is [bool]) { return $Left -is [bool] -and $Right -is [bool] -and $Left -ceq $Right }
    return $false
}
function Stamp($Value) {
    Require ($Value -is [string] -and $Value -cmatch '^\d{4}-\d{2}-\d{2}T.*Z$') 'UTC timestamp required'
    $Parsed = [DateTimeOffset]::MinValue
    Require ([DateTimeOffset]::TryParse($Value, [ref]$Parsed)) 'Invalid timestamp'
    return $Parsed
}
function ConfinedPath([string]$Value, [string]$Root) {
    Require ((Nonempty $Value) -and $Value.Length -le 240) 'Path missing or exceeds 240 characters'
    Require ($Value -notmatch '(^|[\\/])\.\.([\\/]|$)') 'Path traversal rejected'
    $Absolute = [IO.Path]::GetFullPath($Value).TrimEnd('\', '/')
    Require ($Absolute.Length -le 240) 'Resolved path exceeds 240 characters'
    Require ($Absolute.Equals($Root, [StringComparison]::OrdinalIgnoreCase) -or $Absolute.StartsWith($Root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) 'Path outside supplied workspace evidence root'
    # Lexical normalization alone would allow junction/symlink escape. Refuse existing reparse ancestors.
    $Current = $Absolute
    while ($Current.Length -ge $RepoRoot.Length) {
        if (Test-Path -LiteralPath $Current) {
            Require (((Get-Item -Force -LiteralPath $Current).Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) 'Evidence reparse point refused'
        }
        if ($Current.Equals($RepoRoot, [StringComparison]::OrdinalIgnoreCase)) { break }
        $Current = [IO.Path]::GetDirectoryName($Current)
    }
    return $Absolute
}
function ArtifactPath($Value, [string]$Directory, [switch]$MustExist) {
    Require (Nonempty $Value) 'Artifact path must be a string'
    Require ($Value.Length -le 240) 'Artifact path exceeds 240 characters'
    Require ($Value -notmatch '(^|[\\/])\.\.([\\/]|$)') 'Artifact traversal rejected'
    $Absolute = if ([IO.Path]::IsPathRooted($Value)) { [IO.Path]::GetFullPath($Value) } else { [IO.Path]::GetFullPath((Join-Path $Directory $Value)) }
    Require ($Absolute.Length -le 240) 'Resolved artifact path exceeds 240 characters'
    Require ($Absolute.StartsWith($Directory.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) 'Artifact outside report directory'
    $null = ConfinedPath $Absolute $EvidenceRoot
    if ($MustExist) { Require (Test-Path -LiteralPath $Absolute -PathType Leaf) 'Missing evidence artifact' }
    return $Absolute
}
function Definition([string]$ID) {
    Require ($ID -ceq 'contracts' -or $ID -ceq 'lifecycle') 'Unknown registered fixture'
    $Data = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot ('tests/native/' + $ID + '.json')) | ConvertFrom-Json
    Require ($Data.schemaVersion -ceq 1 -and $Data.fixture -ceq $ID -and $Data.fixtureVersion -ceq 1 -and $Data.access -ceq 'read') 'Invalid registered fixture'
    Require ((ArrayValue $Data.operations) -and $Data.operations.Count -gt 0) 'Empty fixture operations'
    Require ((ArrayValue $Data.requiredChecks) -and $Data.requiredChecks.Count -gt 0) 'Empty required checks'
    Require (@($Data.requiredChecks | Select-Object -Unique).Count -eq $Data.requiredChecks.Count) 'Duplicate required checks'
    if ($ID -ceq 'lifecycle') {
        Require ($Data.requiresMutation -is [bool] -and $Data.requiresMutation) 'Lifecycle must require mutation'
        Require ((ArrayValue $Data.syntheticSetupInstructions) -and $Data.syntheticSetupInstructions.Count -gt 0 -and (ArrayValue $Data.steps) -and $Data.steps.Count -eq 5) 'Missing bounded lifecycle setup/steps'
        Require ((ArrayValue $Data.requiredCapabilities) -and $Data.requiredCapabilities.Count -eq $Data.steps.Count) 'Invalid probe capabilities'
        foreach ($Step in $Data.steps) {
            Require ((Nonempty $Step.id) -and @($Data.requiredChecks | Where-Object { $_ -ceq $Step.id }).Count -eq 1 -and @($Data.requiredCapabilities | Where-Object { $_ -ceq $Step.id }).Count -eq 1) 'Invalid lifecycle step ID'
            Require ($Step.requiresMutation -is [bool] -and $Step.requiresMutation -and $Step.maxObservations -is [int] -and $Step.maxObservations -gt 0 -and $Step.maxObservations -le 32) 'Unbounded lifecycle step'
            Require ((ArrayValue $Step.expectedAssertions) -and $Step.expectedAssertions.Count -gt 0) 'Missing expected assertions'
        }
    }
    $Source = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'CS2MCP.Bridge/EndpointCatalog.cs')
    $Pattern = 'new EndpointDescriptor\("([^"\r\n]+)", "([^"\r\n]+)", "([^"\r\n]+)", "([^"\r\n]+)"\)'
    $Matches = [regex]::Matches($Source, $Pattern)
    Require ($Matches.Count -gt 0 -and $Matches.Count -eq [regex]::Matches($Source, 'new EndpointDescriptor\(').Count) 'Catalog extraction failed closed'
    Require ($Source.Contains('legacyHttpVerbs { get; } = Array.AsReadOnly(new[] { "GET" })')) 'Catalog verb definition changed'
    $Catalog = @{}
    foreach ($Match in $Matches) {
        $Path = $Match.Groups[1].Value
        Require (-not $Catalog.ContainsKey($Path)) 'Duplicate catalog path'
        $Catalog[$Path] = @{ path = $Path; access = $Match.Groups[2].Value; capability = $Match.Groups[3].Value; completionMechanism = $Match.Groups[4].Value; legacyHttpVerbs = @('GET') }
    }
    $Result = @{ fixture = $Data; catalog = $Catalog; catalogSha256 = (FileHash (Join-Path $RepoRoot 'CS2MCP.Bridge/EndpointCatalog.cs')) }
    ValidateOperations $Data.operations $Result
    return $Result
}
function ValidateOperations($Operations, $Definition) {
    Require ((ArrayValue $Operations) -and $Operations.Count -eq $Definition.fixture.operations.Count) 'Fixture operation count mismatch'
    for ($Index = 0; $Index -lt $Operations.Count; $Index++) {
        $Operation = $Operations[$Index]
        RequireFields $Operation @('path', 'method')
        Require ($Operation.path -is [string] -and $Operation.path -cmatch '^/[a-z/-]+$' -and $Operation.method -ceq 'GET') 'Invalid fixture operation'
        Require (@($Operation.PSObject.Properties).Count -eq 2) 'Operation must contain only method and path'
        Require ($Definition.catalog.ContainsKey($Operation.path) -and $Definition.catalog[$Operation.path].access -ceq 'read') 'Non-read operation refused'
        Require ($Operation.path -ceq $Definition.fixture.operations[$Index].path -and $Operation.method -ceq $Definition.fixture.operations[$Index].method) 'Operation differs from registered fixture'
    }
}
function ValidateEndpoints($Endpoints, $Definition) {
    Require ((ArrayValue $Endpoints) -and $Endpoints.Count -eq $Definition.catalog.Count) 'Endpoint catalog count mismatch'
    $Seen = @{}
    foreach ($Endpoint in $Endpoints) {
        RequireFields $Endpoint @('path','access','capability','completionMechanism','legacyHttpVerbs')
        Require ((Nonempty $Endpoint.path) -and $Definition.catalog.ContainsKey($Endpoint.path) -and -not $Seen.ContainsKey($Endpoint.path)) 'Unknown or duplicate endpoint'
        $Seen[$Endpoint.path] = $true
        $Source = $Definition.catalog[$Endpoint.path]
        foreach ($Field in @('path','access','capability','completionMechanism')) {
            Require ((Nonempty $Endpoint.$Field) -and $Endpoint.$Field -ceq $Source[$Field]) 'Endpoint contradicts source catalog'
        }
        Require ((ArrayValue $Endpoint.legacyHttpVerbs) -and $Endpoint.legacyHttpVerbs.Count -eq 1 -and $Endpoint.legacyHttpVerbs[0] -is [string] -and $Endpoint.legacyHttpVerbs[0] -ceq 'GET') 'Endpoint verb definition contradicts source'
    }
    ValidateOperations $Definition.fixture.operations $Definition
}
function ValidateCatalogObservation($Observation, $Definition) {
    RequireFields $Observation @('operations','endpoints')
    ValidateOperations $Observation.operations $Definition
    ValidateEndpoints $Observation.endpoints $Definition
}
function ValidateManifestMetadata($Manifest, $Definition) {
    RequireFields $Manifest @('protocolVersion','schemaVersion','endpoints')
    Require ($Manifest.protocolVersion -is [int] -and $Manifest.protocolVersion -eq 1 -and $Manifest.schemaVersion -is [int] -and $Manifest.schemaVersion -eq 1) 'Unsupported or malformed protocol/schema version'
    ValidateEndpoints $Manifest.endpoints $Definition
}
function ValidateProbeMetadata($Metadata, $Definition) {
    RequireFields $Metadata @('enabled','dispatchAvailable','reason','probes')
    Require ($Metadata.enabled -is [bool] -and $Metadata.dispatchAvailable -is [bool] -and -not $Metadata.dispatchAvailable -and $Metadata.reason -ceq 'operationGuardsNotImplemented') 'Probe metadata cannot grant dispatch'
    Require ((ArrayValue $Metadata.probes) -and $Metadata.probes.Count -eq $Definition.fixture.steps.Count) 'Probe metadata count mismatch'
    $Seen = @{}
    foreach ($Probe in $Metadata.probes) {
        RequireFields $Probe @('id','fixture','validationOnly','support','dispatchAvailable')
        Require ((Nonempty $Probe.id) -and -not $Seen.ContainsKey($Probe.id) -and @($Definition.fixture.requiredCapabilities | Where-Object { $_ -ceq $Probe.id }).Count -eq 1) 'Unknown/duplicate probe metadata'
        $Seen[$Probe.id] = $true
        Require ($Probe.fixture -ceq 'lifecycle' -and $Probe.validationOnly -is [bool] -and $Probe.validationOnly -and $Probe.support -ceq 'unverified' -and $Probe.dispatchAvailable -is [bool] -and -not $Probe.dispatchAvailable) 'Probe metadata falsely claims authority/support'
    }
}
function ValidateManifest($Manifest, $Definition, $Observed) {
    ValidateManifestMetadata $Manifest $Definition
    RequireFields $Manifest @('identity','runtime')
    RequireFields $Manifest.runtime @('ageMilliseconds','observedAtUtc')
    $Age = $Manifest.runtime.ageMilliseconds
    Require (($Age -is [int] -or $Age -is [long] -or $Age -is [double] -or $Age -is [decimal]) -and $Age -ge 0 -and $Age -le 5000) 'Runtime snapshot unavailable, malformed or stale'
    $Snapshot = Stamp $Manifest.runtime.observedAtUtc
    $Elapsed = ($Observed - $Snapshot).TotalMilliseconds
    Require ($Elapsed -ge 0 -and $Elapsed -le 5000) 'Runtime timestamp stale at check observation'
}
function ValidateBinding($Binding, $Report, [switch]$NativeIdentity) {
    RequireFields $Binding @('fixture','buildIdentity','bootId','epoch')
    Require ((Nonempty $Binding.fixture) -and $Binding.fixture -ceq $Report.fixture) 'Fixture binding mismatch'
    foreach ($Field in @('buildIdentity','bootId')) {
        $Claimed = if ($Field -ceq 'buildIdentity') { $Report.build.identity.buildIdentity } else { $Report.world.bootId }
        Require (($null -eq $Binding.$Field -and $null -eq $Claimed) -or ((Nonempty $Binding.$Field) -and $Binding.$Field -ceq $Claimed)) 'Build/world binding mismatch'
        if ($NativeIdentity) { Require (Nonempty $Binding.$Field) 'Native identity binding unavailable' }
    }
    Require (($null -eq $Binding.epoch -and $null -eq $Report.world.epoch) -or (($Binding.epoch -is [int] -or $Binding.epoch -is [long]) -and $Binding.epoch -ceq $Report.world.epoch)) 'Epoch binding mismatch'
    if ($NativeIdentity) { Require ($null -ne $Binding.epoch) 'Native epoch binding unavailable' }
}
function ValidateIdentityObservation($Identity, $Claimed) {
    foreach ($Field in @('assemblyVersion','modVersion','buildIdentity','sourceRevision','gameVersion')) {
        Require (Has $Identity $Field) 'Missing observed identity field'
        Require (($null -eq $Identity.$Field -and $null -eq $Claimed.$Field) -or ((Nonempty $Identity.$Field) -and $Identity.$Field -ceq $Claimed.$Field)) 'Observed identity differs from report'
    }
}
function ValidateBuildObservation($Observation, $Build) {
    RequireFields $Observation @('identity','loadedModuleSha256')
    ValidateIdentityObservation $Observation.identity $Build.identity
    Require ((Nonempty $Observation.loadedModuleSha256) -and $Observation.loadedModuleSha256 -ceq $Build.loadedModuleSha256) 'Observed loaded hash differs from report'
}
function ValidateWorldObservation($Observation, $World) {
    RequireFields $Observation @('bootId','epoch')
    Require ((Nonempty $Observation.bootId) -and $Observation.bootId -ceq $World.bootId -and ($Observation.epoch -is [int] -or $Observation.epoch -is [long]) -and $Observation.epoch -ceq $World.epoch) 'Observed world differs from report'
}
function ReportBinding($Report) {
    return @{ fixture = $Report.fixture; buildIdentity = $Report.build.identity.buildIdentity; bootId = $Report.world.bootId; epoch = $Report.world.epoch }
}
function ValidateReport($Report, [string]$Directory, $Definition) {
    RequireFields $Report @('schemaVersion','fixture','fixtureVersion','provenance','startedAtUtc','finishedAtUtc','build','world','checks','limitations','artifacts','validated','compatibilityVerified')
    Require ($Report.schemaVersion -is [int] -and $Report.schemaVersion -eq 1 -and $Report.fixture -ceq $Definition.fixture.fixture -and $Report.fixtureVersion -is [int] -and $Report.fixtureVersion -eq $Definition.fixture.fixtureVersion) 'Invalid report version or fixture'
    Require ($Report.provenance -ceq 'native' -or $Report.provenance -ceq 'offline') 'Invalid report provenance'
    Require ($Report.validated -is [bool] -and $Report.compatibilityVerified -is [bool]) 'Flags must be booleans'
    $Start = Stamp $Report.startedAtUtc
    $End = Stamp $Report.finishedAtUtc
    Require ($End -ge $Start) 'Impossible report timestamps'
    Require ((ArrayValue $Report.limitations) -and (ArrayValue $Report.artifacts) -and (ArrayValue $Report.checks)) 'Arrays required'
    foreach ($Limitation in $Report.limitations) { Require (Nonempty $Limitation) 'Invalid limitation' }
    foreach ($Artifact in $Report.artifacts) { $null = ArtifactPath $Artifact $Directory -MustExist }
    if ($Report.fixture -ceq 'lifecycle') { ValidateLifecycleReport $Report $Directory $Definition }
    $Build = $Report.build
    RequireFields $Build @('identity','identityReasons','sourceRevision','sourceDirty','installedSha256','candidateSha256','loadedModuleSha256','hashReasons')
    Require ($null -eq $Build.sourceDirty -or $Build.sourceDirty -is [bool]) 'Invalid source dirty flag'
    Require ($null -eq $Build.sourceRevision -or ($Build.sourceRevision -is [string] -and $Build.sourceRevision -cmatch '^([a-fA-F0-9]{40}|[a-fA-F0-9]{64})$')) 'Invalid source revision'
    $KnownBuild = $true
    foreach ($Field in @('assemblyVersion','modVersion','buildIdentity','sourceRevision','gameVersion')) {
        Require (Has $Build.identity $Field) 'Missing build identity field'
        if ($null -eq $Build.identity.$Field) { Require (Nonempty $Build.identityReasons.$Field) 'Unknown identity needs reason'; $KnownBuild = $false }
        else { Require (Nonempty $Build.identity.$Field) 'Invalid identity value' }
    }
    if ($null -ne $Build.identity.sourceRevision) { Require ($Build.identity.sourceRevision -cmatch '^([a-fA-F0-9]{40}|[a-fA-F0-9]{64})$') 'Invalid embedded revision' }
    foreach ($Field in @('installedSha256','candidateSha256','loadedModuleSha256')) {
        if ($null -eq $Build.$Field) { Require (Nonempty $Build.hashReasons.$Field) 'Unknown hash needs reason'; if ($Field -ceq 'loadedModuleSha256') { $KnownBuild = $false } }
        else { Require ($Build.$Field -is [string] -and $Build.$Field -cmatch '^[a-fA-F0-9]{64}$') 'Invalid SHA256' }
    }
    $World = $Report.world
    RequireFields $World @('bootId','epoch','reason','startContext','endContext')
    $KnownWorld = (Nonempty $World.bootId) -and ($World.epoch -is [int] -or $World.epoch -is [long]) -and $World.epoch -ge 0
    if (-not $KnownWorld) { Require ($null -eq $World.bootId -and $null -eq $World.epoch -and (Nonempty $World.reason)) 'Unknown world needs null identity and reason' }
    $IDs = @($Definition.fixture.requiredChecks)
    Require ($Report.checks.Count -eq $IDs.Count) 'Missing or extra checks'
    $Seen = @{}
    $AllPassed = $true
    foreach ($Check in $Report.checks) {
        RequireFields $Check @('id','status','expected','actual','observedAtUtc','provenance','binding','artifacts','reason')
        Require ($Check.id -is [string] -and @($IDs | Where-Object { $_ -ceq $Check.id }).Count -eq 1 -and -not $Seen.ContainsKey($Check.id)) 'Unknown or duplicate check'
        $Seen[$Check.id] = $true
        Require ($Check.status -ceq 'passed' -or $Check.status -ceq 'failed' -or $Check.status -ceq 'notRun') 'Invalid check status'
        Require ($Check.provenance -ceq 'source' -or $Check.provenance -ceq 'native' -or $Check.provenance -ceq 'offline' -or $Check.provenance -ceq 'manualPanel') 'Invalid check provenance'
        $Observed = Stamp $Check.observedAtUtc
        Require ($Observed -ge $Start -and $Observed -le $End) 'Observation outside report interval'
        Require (ArrayValue $Check.artifacts) 'Check artifacts must be array'
        foreach ($Artifact in $Check.artifacts) {
            $null = ArtifactPath $Artifact $Directory -MustExist
            Require (@($Report.artifacts | Where-Object { $_ -ceq $Artifact }).Count -eq 1) 'Unregistered check artifact'
        }
        if ($Check.status -cne 'passed') { $AllPassed = $false; Require (Nonempty $Check.reason) 'Incomplete check needs reason'; continue }
        Require ($null -ne $Check.expected -and $null -ne $Check.actual -and $Check.artifacts.Count -gt 0) 'Passed check needs values and artifacts'
        if ($Check.id -ceq 'catalogReadOnly') {
            Require ($Check.provenance -ceq 'source') 'Catalog observation must have source provenance'
            RequireFields $Check.binding @('fixture','fixtureVersion','catalogSha256')
            Require ($Check.binding.fixture -is [string] -and $Check.binding.fixture -ceq $Report.fixture -and $Check.binding.fixtureVersion -is [int] -and $Check.binding.fixtureVersion -eq $Report.fixtureVersion -and $Check.binding.catalogSha256 -is [string] -and $Check.binding.catalogSha256 -ceq $Definition.catalogSha256) 'Stale or mismatched source catalog binding'
            ValidateCatalogObservation $Check.expected $Definition
            ValidateCatalogObservation $Check.actual $Definition
        }
        if ($Check.id -ceq 'capabilityExposure') {
            Require ($Check.provenance -ceq $Report.provenance) 'Capability observation provenance differs from report'
            ValidateBinding $Check.binding $Report
            ValidateManifestMetadata $Check.expected $Definition
            ValidateManifest $Check.actual $Definition $Observed
            ValidateIdentityObservation $Check.actual.identity $Build.identity
            if ($Report.fixture -ceq 'lifecycle' -and (Has $Check.actual 'nativeValidation')) { ValidateProbeMetadata $Check.actual.nativeValidation $Definition }
        }
        if ($Check.id -ceq 'buildIdentity') {
            Require ($KnownBuild -and $Report.provenance -ceq 'native' -and $Check.provenance -ceq 'native') 'Unknown or offline build cannot pass'
            ValidateBinding $Check.binding $Report -NativeIdentity
            ValidateBuildObservation $Check.expected $Build
            ValidateBuildObservation $Check.actual $Build
        }
        if ($Check.id -ceq 'worldIdentity') {
            Require ($KnownWorld -and $Report.provenance -ceq 'native' -and $Check.provenance -ceq 'native') 'Unknown or offline world cannot pass'
            ValidateBinding $Check.binding $Report -NativeIdentity
            ValidateWorldObservation $Check.expected $World
            ValidateWorldObservation $Check.actual $World
        }
        if (Has $Definition.fixture.panelProbes $Check.id) {
            Require ($Report.provenance -ceq 'native' -and $Check.provenance -ceq 'manualPanel' -and (Nonempty $Build.identity.buildIdentity) -and $null -ne $Build.loadedModuleSha256 -and $KnownWorld) 'Panel needs native build/world identity'
            RequireFields $Check.binding @('fixture','buildIdentity','bootId','epoch')
            Require ((Nonempty $Check.binding.fixture) -and (Nonempty $Check.binding.buildIdentity) -and (Nonempty $Check.binding.bootId) -and ($Check.binding.epoch -is [int] -or $Check.binding.epoch -is [long])) 'Invalid panel binding types'
            Require ($Check.binding.fixture -ceq $Report.fixture -and $Check.binding.buildIdentity -ceq $Build.identity.buildIdentity -and $Check.binding.bootId -ceq $World.bootId -and $Check.binding.epoch -ceq $World.epoch) 'Panel binding mismatch'
            foreach ($Probe in $Definition.fixture.panelProbes.($Check.id)) {
                Require ((Has $Check.expected $Probe) -and (Has $Check.actual $Probe) -and $null -ne $Check.expected.$Probe -and $null -ne $Check.actual.$Probe) 'Missing panel probe'
            }
            Require (JsonEqual $Check.expected $Check.actual) 'Passed panel values differ'
        }
    }
    Require ($Report.validated -ceq $AllPassed) 'Contradictory validated flag'
    $Compatibility = $AllPassed -and $KnownBuild -and $KnownWorld -and $Report.provenance -ceq 'native'
    Require ($Report.compatibilityVerified -ceq $Compatibility) 'Contradictory compatibility flag'
    if ($AllPassed) { return 0 }; return 2
}
function ValidateLifecycleReport($Report, [string]$Directory, $Definition) {
    RequireFields $Report @('nativeOutcome','preparation','fixtureContext','contextArtifact','receipts')
    Require ($Report.nativeOutcome -ceq 'notRun') 'O1 cannot certify a native outcome'
    Require ((ArrayValue $Report.receipts) -and $Report.receipts.Count -eq 0) 'O1 has no probe receipts; never invent or discard native outcomes'
    RequireFields $Report.preparation @('status','allowMutation','dispatchAvailable','refusalReasons','readFailure')
    Require ($Report.preparation.status -ceq 'failed' -and $Report.preparation.allowMutation -is [bool] -and $Report.preparation.dispatchAvailable -is [bool] -and -not $Report.preparation.dispatchAvailable) 'Preparation cannot enable native dispatch'
    $Refusals = $Report.preparation.refusalReasons
    Require ((ArrayValue $Refusals) -and $Refusals.Count -gt 0 -and @($Refusals | Select-Object -Unique).Count -eq $Refusals.Count) 'Refusals missing or duplicated'
    foreach ($Reason in $Refusals) { Require (Nonempty $Reason) 'Invalid refusal reason' }
    Require (@($Refusals | Where-Object { $_ -ceq 'operationGuardsNotImplemented' }).Count -eq 1 -and @($Refusals | Where-Object { $_ -ceq 'freshWorldLeaseCheckpointEvidenceUnavailable' }).Count -eq 1) 'O1 producer dependency refusal missing'
    if (-not $Report.preparation.allowMutation) { Require (@($Refusals | Where-Object { $_ -ceq 'mutationOptInRequired' }).Count -eq 1) 'Missing opt-in refusal' }
    if ($null -ne $Report.preparation.readFailure) {
        Require (Nonempty $Report.preparation.readFailure) 'Invalid read failure'
        Require (@($Refusals | Where-Object { $_ -ceq 'readOutcomeUnknown' }).Count -eq 1) 'Read failure cannot imply cancellation'
    }
    $RefusalPath = ArtifactPath 'refusal.json' $Directory -MustExist
    Require (@($Report.artifacts | Where-Object { $_ -ceq $RefusalPath }).Count -eq 1) 'Refusal artifact not registered'
    $Recorded = Get-Content -Raw -LiteralPath $RefusalPath | ConvertFrom-Json
    Require (JsonEqual $Recorded $Report.preparation) 'Refusal artifact contradicts report'
    if ($null -eq $Report.contextArtifact) { Require ($null -eq $Report.fixtureContext) 'Context has no snapshot' }
    else {
        $ContextPath = ArtifactPath $Report.contextArtifact $Directory -MustExist
        Require (@($Report.artifacts | Where-Object { $_ -ceq $Report.contextArtifact }).Count -eq 1) 'Context artifact not registered'
        $RecordedContext = Get-Content -Raw -LiteralPath $ContextPath | ConvertFrom-Json
        Require (JsonEqual $RecordedContext $Report.fixtureContext) 'Context snapshot contradicts report'
    }
    foreach ($Step in $Definition.fixture.steps) {
        $Check = @($Report.checks | Where-Object { $_.id -ceq $Step.id })
        Require ($Check.Count -eq 1 -and $Check[0].status -ceq 'notRun' -and $Check[0].reason -ceq 'operationGuardsNotImplemented') 'O1 native probe cannot be passed or failed without execution'
        Require ($null -eq $Check[0].actual -and (ArrayValue $Check[0].artifacts) -and $Check[0].artifacts.Count -eq 0 -and $Check[0].provenance -ceq $Report.provenance) 'Unexecuted probe cannot claim observations'
        Require (JsonEqual $Check[0].expected $Step.expectedAssertions) 'Probe expectations differ from registered fixture'
    }
}
function ContextRefusals($Context, $State) {
    $Reasons = @()
    if ($null -eq $Context) { return @('fixtureContextMissing') }
    if ($Context.schemaVersion -isnot [int] -or $Context.schemaVersion -ne 1 -or $Context.fixture -cne 'lifecycle') { $Reasons += 'fixtureContextMalformed' }
    if ($Context.disposableCityConfirmed -isnot [bool] -or -not $Context.disposableCityConfirmed) { $Reasons += 'disposableCityNotConfirmed' }
    if (-not (ArrayValue $Context.permittedFixtureScope) -or @($Context.permittedFixtureScope | Where-Object { $_ -ceq 'lifecycle' }).Count -ne 1) { $Reasons += 'fixtureScopeMismatch' }
    if (-not (Nonempty $Context.world.bootId) -or ($Context.world.epoch -isnot [int] -and $Context.world.epoch -isnot [long]) -or $Context.world.epoch -lt 0 -or -not (Nonempty $Context.city.identity)) { $Reasons += 'fixtureContextIdentityMissing' }
    # Supplied claims and optional read observations detect contradictions only. Neither grants authority.
    if ($null -ne $State) {
        if ($null -eq $State.world -or $null -eq $State.city) { $Reasons += 'fixtureStateIdentityUnavailable' }
        else {
            if (-not (JsonEqual $Context.world $State.world)) { $Reasons += 'fixtureWorldMismatch' }
            if (-not (JsonEqual $Context.city $State.city)) { $Reasons += 'fixtureCityMismatch' }
        }
    }
    if ($Context.checkpoint.verified -isnot [bool] -or -not $Context.checkpoint.verified -or -not (Nonempty $Context.checkpoint.assetIdentity)) { $Reasons += 'checkpointNotVerified' }
    if ($Context.checkpoint.separatelyNamed -isnot [bool] -or -not $Context.checkpoint.separatelyNamed -or -not (Nonempty $Context.checkpoint.name)) { $Reasons += 'checkpointNotSeparatelyNamed' }
    if ($Context.checkpoint.cityIdentity -cne $Context.city.identity -or -not (JsonEqual $Context.checkpoint.world $Context.world)) { $Reasons += 'checkpointContextMismatch' }
    return $Reasons
}
function ReadJson([string]$Url) {
    $Request = [Net.HttpWebRequest]::Create($Url)
    $Request.Method = 'GET'; $Request.AllowAutoRedirect = $false
    $Request.Proxy = $null; $Request.Timeout = 10000; $Request.ReadWriteTimeout = 10000
    $Response = $null; $Reader = $null
    try {
        $Response = $Request.GetResponse()
        Require ([int]$Response.StatusCode -eq 200) 'HTTP response must be 200; redirects refused'
        $Reader = New-Object IO.StreamReader($Response.GetResponseStream())
        return ($Reader.ReadToEnd() | ConvertFrom-Json)
    } finally { if ($null -ne $Reader) { $Reader.Dispose() }; if ($null -ne $Response) { $Response.Dispose() } }
}
function FileHash([string]$Path) {
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        $Stream = [IO.File]::OpenRead($Path)
        $Hasher = [Security.Cryptography.SHA256]::Create()
        try { return [BitConverter]::ToString($Hasher.ComputeHash($Stream)).Replace('-', '').ToLowerInvariant() }
        finally { $Stream.Dispose(); $Hasher.Dispose() }
    }
    return $null
}
function WriteJson($Value, [string]$Path) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 60), (New-Object Text.UTF8Encoding($false)))
}

try {
    $EvidenceRoot = ConfinedPath $EvidenceRoot $LocalRoot
    if ($PSCmdlet.ParameterSetName -ceq 'Validate') {
        $ReportPath = ConfinedPath $ValidateEvidence $LocalRoot
        $Report = Get-Content -Raw -LiteralPath $ReportPath | ConvertFrom-Json
        $Registered = Definition $Report.fixture
        $Code = ValidateReport $Report ([IO.Path]::GetDirectoryName($ReportPath)) $Registered
        Write-Output "Evidence structurally valid; validated=$($Report.validated); compatibilityVerified=$($Report.compatibilityVerified)"
        exit $Code
    }
    $Registered = Definition $Fixture
    $Origin = $null
    Require ([Uri]::TryCreate($BaseUrl, [UriKind]::Absolute, [ref]$Origin)) 'Invalid BaseUrl'
    $Address = $null
    Require ($Origin.Scheme -ceq 'http' -and [Net.IPAddress]::TryParse($Origin.Host.Trim('[', ']'), [ref]$Address) -and [Net.IPAddress]::IsLoopback($Address)) 'BaseUrl must use a loopback HTTP IP literal'
    Require ($Origin.UserInfo -eq '' -and $Origin.Query -eq '' -and $Origin.Fragment -eq '' -and $Origin.AbsolutePath -eq '/') 'BaseUrl must be an origin without credentials or path'
    $Directory = ConfinedPath $EvidenceDir $EvidenceRoot
    $Names = @('report.json','catalog.json') + @(0..($Registered.fixture.operations.Count - 1) | ForEach-Object { 'read-{0:d2}.json' -f $_ })
    if ($Fixture -ceq 'lifecycle') {
        $Names += @('fixture-context.json','fixture-context-snapshot.json','refusal.json','run-start.json')
        if (Test-Path -LiteralPath $Directory) {
            $Existing = @(Get-ChildItem -Force -LiteralPath $Directory)
            Require (@($Existing | Where-Object { $_.Name -cne 'fixture-context.json' -or $_.PSIsContainer }).Count -eq 0) 'Lifecycle collection requires a fresh EvidenceDir; existing evidence must be preserved'
        }
    }
    foreach ($Name in $Names) { $null = ArtifactPath $Name $Directory }
    $null = New-Item -ItemType Directory -Force -Path $Directory
    $Started = [DateTime]::UtcNow.ToString('o')
    $Provenance = if ($Offline) { 'offline' } else { 'native' }
    $Artifacts = @()
    if ($Fixture -ceq 'lifecycle') {
        # CreateNew also prevents two collectors racing after the initial freshness check.
        $RunStart = Join-Path $Directory 'run-start.json'
        $Marker = [IO.File]::Open($RunStart, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        $Marker.Dispose()
        WriteJson @{ fixture = $Fixture; startedAtUtc = $Started; provenance = $Provenance } $RunStart
        $Artifacts += $RunStart
    }
    $Context = $null; $ContextArtifact = $null
    $Preparation = @{ status = 'failed'; allowMutation = [bool]$AllowMutation; dispatchAvailable = $false; refusalReasons = @(); readFailure = $null }
    if ($Fixture -ceq 'lifecycle') {
        $Preparation.refusalReasons = @('operationGuardsNotImplemented','freshWorldLeaseCheckpointEvidenceUnavailable')
        if (-not $AllowMutation) { $Preparation.refusalReasons += 'mutationOptInRequired' }
        $InputPath = Join-Path $Directory 'fixture-context.json'
        if (Test-Path -LiteralPath $InputPath -PathType Leaf) {
            # Preserve input verbatim; no in-place reset or overwrite of operator context.
            $ContextArtifact = Join-Path $Directory 'fixture-context-snapshot.json'
            [IO.File]::Copy($InputPath, $ContextArtifact, $false)
            $Artifacts += @($InputPath, $ContextArtifact)
            try { $Context = Get-Content -Raw -LiteralPath $ContextArtifact | ConvertFrom-Json }
            catch { $Preparation.refusalReasons += 'fixtureContextMalformed'; $ContextArtifact = $null }
        }
    }
    $CatalogPath = Join-Path $Directory 'catalog.json'
    $CatalogObservation = @{ operations = $Registered.fixture.operations; endpoints = @($Registered.catalog.Values) }
    WriteJson $CatalogObservation $CatalogPath
    $Artifacts += $CatalogPath
    $Checks = @(); $CheckByID = @{}
    foreach ($ID in $Registered.fixture.requiredChecks) {
        $CheckByID[$ID] = [pscustomobject]@{ id = $ID; status = 'notRun'; expected = @{ fixture = $Fixture; check = $ID }; actual = $null; observedAtUtc = $Started; provenance = $Provenance; binding = $null; artifacts = @(); reason = 'nativeObservationNotRun' }
        $Checks += $CheckByID[$ID]
    }
    $CheckByID.catalogReadOnly.status = 'passed'; $CheckByID.catalogReadOnly.provenance = 'source'; $CheckByID.catalogReadOnly.expected = $CatalogObservation; $CheckByID.catalogReadOnly.actual = $CatalogObservation; $CheckByID.catalogReadOnly.artifacts = @($CatalogPath); $CheckByID.catalogReadOnly.reason = $null
    $CheckByID.catalogReadOnly.binding = @{ fixture = $Fixture; fixtureVersion = 1; catalogSha256 = $Registered.catalogSha256 }
    $Manifest = $null; $StartContext = $null; $EndContext = $null
    try {
        $Manifest = ReadJson ($Origin.GetLeftPart([UriPartial]::Authority) + '/capabilities')
        $ManifestPath = Join-Path $Directory 'read-00.json'
        WriteJson $Manifest $ManifestPath
        $Artifacts += $ManifestPath
        $CheckByID.capabilityExposure.observedAtUtc = [DateTime]::UtcNow.ToString('o')
        ValidateManifest $Manifest $Registered (Stamp $CheckByID.capabilityExposure.observedAtUtc)
        if ($Fixture -ceq 'lifecycle') {
            if (Has $Manifest 'nativeValidation') {
                ValidateProbeMetadata $Manifest.nativeValidation $Registered
                if (-not $Manifest.nativeValidation.enabled) { $Preparation.refusalReasons += 'validationModeDisabled' }
            } else { $Preparation.refusalReasons += 'validationModeUnavailable' }
        }
        $CheckByID.capabilityExposure.expected = @{ protocolVersion = 1; schemaVersion = 1; endpoints = @($Registered.catalog.Values) }
        $CheckByID.capabilityExposure.status = 'passed'; $CheckByID.capabilityExposure.actual = $Manifest; $CheckByID.capabilityExposure.artifacts = @($ManifestPath); $CheckByID.capabilityExposure.reason = $null
        for ($Index = 1; $Index -lt $Registered.fixture.operations.Count; $Index++) {
            $Operation = $Registered.fixture.operations[$Index]
            $Value = ReadJson ($Origin.GetLeftPart([UriPartial]::Authority) + $Operation.path)
            $Path = Join-Path $Directory ('read-{0:d2}.json' -f $Index)
            WriteJson $Value $Path
            $Artifacts += $Path
            if ($Operation.path -ceq '/state') { if ($null -eq $StartContext) { $StartContext = $Value }; $EndContext = $Value }
        }
    } catch {
        $CheckByID.capabilityExposure.status = 'failed'; $CheckByID.capabilityExposure.reason = $_.Exception.Message
        if ($Fixture -ceq 'lifecycle') {
            $Preparation.readFailure = $_.Exception.Message
            $Preparation.refusalReasons += 'readOutcomeUnknown'
        }
    }
    $Identity = @{ assemblyVersion = $null; modVersion = $null; buildIdentity = $null; sourceRevision = $null; gameVersion = $null }
    $Reasons = @{}
    foreach ($Field in @('assemblyVersion','modVersion','buildIdentity','sourceRevision','gameVersion')) {
        if ($null -ne $Manifest -and (Has $Manifest.identity $Field) -and (Nonempty $Manifest.identity.$Field)) { $Identity[$Field] = $Manifest.identity.$Field }
        else { $Reasons[$Field] = 'notObserved' }
    }
    $Installed = FileHash (Join-Path $env:USERPROFILE 'AppData/LocalLow/Colossal Order/Cities Skylines II/Mods/CS2MCP/CS2MCP.dll')
    $Candidate = FileHash (Join-Path $RepoRoot 'CS2MCP.Bridge/bin/Release/CS2MCP.dll')
    $HashReasons = @{ loadedModuleSha256 = 'loadedModuleHashNotExposed' }
    if ($null -eq $Installed) { $HashReasons.installedSha256 = 'installedFileAbsent' }
    if ($null -eq $Candidate) { $HashReasons.candidateSha256 = 'candidateFileAbsent' }
    $Revision = (& git -C $RepoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { $Revision = $null }
    $Dirty = (& git -C $RepoRoot status --porcelain 2>$null | Out-String).Trim().Length -gt 0
    $CheckByID.buildIdentity.reason = 'loadedModuleHashNotExposed; gameVersion may be unknown; installed file is not loaded identity'
    $CheckByID.worldIdentity.reason = 'WorldRef bootId/epoch not exposed by current bridge'
    $Finished = [DateTime]::UtcNow.ToString('o')
    $Report = [pscustomobject]@{
        schemaVersion = 1; fixture = $Fixture; fixtureVersion = 1; provenance = $Provenance
        startedAtUtc = $Started; finishedAtUtc = $Finished
        build = @{ identity = $Identity; identityReasons = $Reasons; sourceRevision = $Revision; sourceDirty = $Dirty; installedSha256 = $Installed; candidateSha256 = $Candidate; loadedModuleSha256 = $null; hashReasons = $HashReasons }
        world = @{ bootId = $null; epoch = $null; reason = 'worldRefNotExposed'; startContext = $StartContext; endContext = $EndContext }
        checks = $Checks; artifacts = $Artifacts; validated = $false; compatibilityVerified = $false
        limitations = @('Read sequence is not atomic; city name/frame is not WorldRef', 'Capability runtime snapshot freshness is bounded to 5000ms at observation, not the whole collection', 'No panels observed automatically; native parity remains notRun', 'Installed and candidate file hashes do not identify the loaded module', 'Setter/deferred completion and cargo units require later fixtures')
    }
    if ($Fixture -ceq 'lifecycle') {
        if ($AllowMutation -or $null -ne $Context) { $Preparation.refusalReasons += @(ContextRefusals $Context $EndContext) }
        $Preparation.refusalReasons = @($Preparation.refusalReasons | Select-Object -Unique)
        foreach ($Step in $Registered.fixture.steps) {
            $Check = $Checks | Where-Object { $_.id -ceq $Step.id }
            $Check.expected = $Step.expectedAssertions; $Check.reason = 'operationGuardsNotImplemented'
        }
        $RefusalPath = Join-Path $Directory 'refusal.json'
        WriteJson $Preparation $RefusalPath
        $Artifacts += $RefusalPath
        $Report.artifacts = $Artifacts
        $Report | Add-Member -NotePropertyName nativeOutcome -NotePropertyValue 'notRun'
        $Report | Add-Member -NotePropertyName preparation -NotePropertyValue $Preparation
        $Report | Add-Member -NotePropertyName fixtureContext -NotePropertyValue $Context
        $Report | Add-Member -NotePropertyName contextArtifact -NotePropertyValue $ContextArtifact
        $Report | Add-Member -NotePropertyName receipts -NotePropertyValue @()
        $Report.limitations += @('O1 collects only read-only preparation/refusal evidence; no native probes dispatched', 'Fixture context is operator evidence, never fresh world/lease/checkpoint authority', 'O2/O3/O5 must supply normal guards and probe producers before any mutation group can run')
    }
    $CheckByID.capabilityExposure.binding = ReportBinding $Report
    $ReportPath = Join-Path $Directory 'report.json'
    WriteJson $Report $ReportPath
    $Code = ValidateReport ($Report | ConvertTo-Json -Depth 60 | ConvertFrom-Json) $Directory $Registered
    Write-Output "Report: $ReportPath; validated=false; compatibilityVerified=false"
    exit $Code
} catch {
    [Console]::Error.WriteLine("Invalid evidence or fixture: " + $_.Exception.Message)
    exit 1
}
