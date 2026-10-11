# Windows PowerShell 5.1; HTTP collection is opt-in and restricted to registered reads.
[CmdletBinding(DefaultParameterSetName = 'Collect')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Collect')][string]$Fixture,
    [Parameter(Mandatory = $true, ParameterSetName = 'Collect')][string]$EvidenceDir,
    [Parameter(ParameterSetName = 'Collect')][string]$BaseUrl = 'http://127.0.0.1:8642',
    [Parameter(ParameterSetName = 'Collect')][switch]$Offline,
    [Parameter(Mandatory = $true, ParameterSetName = 'Validate')][string]$ValidateEvidence
)
$ErrorActionPreference = 'Stop'
$RepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$EvidenceRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot '.local/v01'))

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
function ArtifactPath($Value, [string]$Directory, [switch]$MustExist) {
    Require (Nonempty $Value) 'Artifact path must be a string'
    Require ($Value.Length -le 240) 'Artifact path exceeds 240 characters'
    Require ($Value -notmatch '(^|[\\/])\.\.([\\/]|$)') 'Artifact traversal rejected'
    $Absolute = if ([IO.Path]::IsPathRooted($Value)) { [IO.Path]::GetFullPath($Value) } else { [IO.Path]::GetFullPath((Join-Path $Directory $Value)) }
    Require ($Absolute.Length -le 240) 'Resolved artifact path exceeds 240 characters'
    Require ($Absolute.StartsWith($Directory.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) 'Artifact outside report directory'
    Require ($Absolute.StartsWith($EvidenceRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) 'Artifact outside .local/v01'
    if ($MustExist) { Require (Test-Path -LiteralPath $Absolute -PathType Leaf) 'Missing evidence artifact' }
    return $Absolute
}
function Definition {
    $Data = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'tests/native/contracts.json') | ConvertFrom-Json
    Require ($Data.schemaVersion -ceq 1 -and $Data.fixture -ceq 'contracts' -and $Data.fixtureVersion -ceq 1 -and $Data.access -ceq 'read') 'Invalid registered fixture'
    Require ((ArrayValue $Data.operations) -and $Data.operations.Count -gt 0) 'Empty fixture operations'
    Require ((ArrayValue $Data.requiredChecks) -and $Data.requiredChecks.Count -gt 0) 'Empty required checks'
    Require (@($Data.requiredChecks | Select-Object -Unique).Count -eq $Data.requiredChecks.Count) 'Duplicate required checks'
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
    Require ($Report.schemaVersion -is [int] -and $Report.schemaVersion -eq 1 -and $Report.fixture -ceq 'contracts' -and $Report.fixtureVersion -is [int] -and $Report.fixtureVersion -eq 1) 'Invalid report version or fixture'
    Require ($Report.provenance -ceq 'native' -or $Report.provenance -ceq 'offline') 'Invalid report provenance'
    Require ($Report.validated -is [bool] -and $Report.compatibilityVerified -is [bool]) 'Flags must be booleans'
    $Start = Stamp $Report.startedAtUtc
    $End = Stamp $Report.finishedAtUtc
    Require ($End -ge $Start) 'Impossible report timestamps'
    Require ((ArrayValue $Report.limitations) -and (ArrayValue $Report.artifacts) -and (ArrayValue $Report.checks)) 'Arrays required'
    foreach ($Limitation in $Report.limitations) { Require (Nonempty $Limitation) 'Invalid limitation' }
    foreach ($Artifact in $Report.artifacts) { $null = ArtifactPath $Artifact $Directory -MustExist }
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
    $Registered = Definition
    if ($PSCmdlet.ParameterSetName -ceq 'Validate') {
        $ReportPath = [IO.Path]::GetFullPath($ValidateEvidence)
        $Report = Get-Content -Raw -LiteralPath $ReportPath | ConvertFrom-Json
        $Code = ValidateReport $Report ([IO.Path]::GetDirectoryName($ReportPath)) $Registered
        Write-Output "Evidence structurally valid; validated=$($Report.validated); compatibilityVerified=$($Report.compatibilityVerified)"
        exit $Code
    }
    Require ($Fixture -ceq 'contracts') 'Only registered read-only contracts fixture supported'
    $Origin = $null
    Require ([Uri]::TryCreate($BaseUrl, [UriKind]::Absolute, [ref]$Origin)) 'Invalid BaseUrl'
    $Address = $null
    Require ($Origin.Scheme -ceq 'http' -and [Net.IPAddress]::TryParse($Origin.Host.Trim('[', ']'), [ref]$Address) -and [Net.IPAddress]::IsLoopback($Address)) 'BaseUrl must use a loopback HTTP IP literal'
    Require ($Origin.UserInfo -eq '' -and $Origin.Query -eq '' -and $Origin.Fragment -eq '' -and $Origin.AbsolutePath -eq '/') 'BaseUrl must be an origin without credentials or path'
    $Directory = [IO.Path]::GetFullPath($EvidenceDir)
    Require ($Directory.StartsWith($EvidenceRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $Directory.Equals($EvidenceRoot, [StringComparison]::OrdinalIgnoreCase)) 'EvidenceDir must be below .local/v01'
    Require ($EvidenceDir -notmatch '(^|[\\/])\.\.([\\/]|$)') 'EvidenceDir traversal rejected'
    $Names = @('report.json','catalog.json') + @(0..($Registered.fixture.operations.Count - 1) | ForEach-Object { 'read-{0:d2}.json' -f $_ })
    foreach ($Name in $Names) { $null = ArtifactPath $Name $Directory }
    $null = New-Item -ItemType Directory -Force -Path $Directory
    $Started = [DateTime]::UtcNow.ToString('o')
    $Provenance = if ($Offline) { 'offline' } else { 'native' }
    $Artifacts = @()
    $CatalogPath = Join-Path $Directory 'catalog.json'
    $CatalogObservation = @{ operations = $Registered.fixture.operations; endpoints = @($Registered.catalog.Values) }
    WriteJson $CatalogObservation $CatalogPath
    $Artifacts += $CatalogPath
    $Checks = @()
    foreach ($ID in $Registered.fixture.requiredChecks) {
        $Checks += [pscustomobject]@{ id = $ID; status = 'notRun'; expected = @{ fixture = 'contracts'; check = $ID }; actual = $null; observedAtUtc = $Started; provenance = $Provenance; binding = $null; artifacts = @(); reason = 'nativeObservationNotRun' }
    }
    $Checks[0].status = 'passed'; $Checks[0].provenance = 'source'; $Checks[0].expected = $CatalogObservation; $Checks[0].actual = $CatalogObservation; $Checks[0].artifacts = @($CatalogPath); $Checks[0].reason = $null
    $Checks[0].binding = @{ fixture = 'contracts'; fixtureVersion = 1; catalogSha256 = $Registered.catalogSha256 }
    $Manifest = $null; $StartContext = $null; $EndContext = $null
    try {
        $Manifest = ReadJson ($Origin.GetLeftPart([UriPartial]::Authority) + '/capabilities')
        $ManifestPath = Join-Path $Directory 'read-00.json'
        WriteJson $Manifest $ManifestPath
        $Artifacts += $ManifestPath
        $Checks[1].observedAtUtc = [DateTime]::UtcNow.ToString('o')
        ValidateManifest $Manifest $Registered (Stamp $Checks[1].observedAtUtc)
        $Checks[1].expected = @{ protocolVersion = 1; schemaVersion = 1; endpoints = @($Registered.catalog.Values) }
        $Checks[1].status = 'passed'; $Checks[1].actual = $Manifest; $Checks[1].artifacts = @($ManifestPath); $Checks[1].reason = $null
        for ($Index = 1; $Index -lt $Registered.fixture.operations.Count; $Index++) {
            $Operation = $Registered.fixture.operations[$Index]
            $Value = ReadJson ($Origin.GetLeftPart([UriPartial]::Authority) + $Operation.path)
            $Path = Join-Path $Directory ('read-{0:d2}.json' -f $Index)
            WriteJson $Value $Path
            $Artifacts += $Path
            if ($Operation.path -ceq '/state') { if ($null -eq $StartContext) { $StartContext = $Value }; $EndContext = $Value }
        }
    } catch {
        $Checks[1].status = 'failed'; $Checks[1].reason = $_.Exception.Message
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
    $Checks[2].reason = 'loadedModuleHashNotExposed; gameVersion may be unknown; installed file is not loaded identity'
    $Checks[3].reason = 'WorldRef bootId/epoch not exposed by current bridge'
    $Finished = [DateTime]::UtcNow.ToString('o')
    $Report = [pscustomobject]@{
        schemaVersion = 1; fixture = 'contracts'; fixtureVersion = 1; provenance = $Provenance
        startedAtUtc = $Started; finishedAtUtc = $Finished
        build = @{ identity = $Identity; identityReasons = $Reasons; sourceRevision = $Revision; sourceDirty = $Dirty; installedSha256 = $Installed; candidateSha256 = $Candidate; loadedModuleSha256 = $null; hashReasons = $HashReasons }
        world = @{ bootId = $null; epoch = $null; reason = 'worldRefNotExposed'; startContext = $StartContext; endContext = $EndContext }
        checks = $Checks; artifacts = $Artifacts; validated = $false; compatibilityVerified = $false
        limitations = @('Read sequence is not atomic; city name/frame is not WorldRef', 'Capability runtime snapshot freshness is bounded to 5000ms at observation, not the whole collection', 'No panels observed automatically; native parity remains notRun', 'Installed and candidate file hashes do not identify the loaded module', 'Setter/deferred completion and cargo units require later fixtures')
    }
    $Checks[1].binding = ReportBinding $Report
    $ReportPath = Join-Path $Directory 'report.json'
    WriteJson $Report $ReportPath
    $Code = ValidateReport ($Report | ConvertTo-Json -Depth 60 | ConvertFrom-Json) $Directory $Registered
    Write-Output "Report: $ReportPath; validated=false; compatibilityVerified=false"
    exit $Code
} catch {
    [Console]::Error.WriteLine("Invalid evidence or fixture: " + $_.Exception.Message)
    exit 1
}
