[CmdletBinding()]
param(
    [string]$WorkspaceRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$SkipAssets,
    [switch]$CheckOnly,
    [switch]$AsJson
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$headers = @{ 'User-Agent' = 'Mozilla/5.0' }
$seasonAssetRoot = Join-Path $WorkspaceRoot 'src/FootballCareerSimulator.Presentation/assets/clubs/turkey/super-lig-2026-27'
$generatedFile = Join-Path $WorkspaceRoot 'src/FootballCareerSimulator.Simulation/DataPacks/TurkeySuperLig202627DataPack.Generated.cs'
$snapshotDate = (Get-Date).ToString('yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)

$clubs = @(
    [pscustomobject]@{ Id = 1;  Name = 'GALATASARAY A.Ş.';              Slug = 'galatasaray';          TffClubId = 3604; StrengthSeed = 95; Supplements = @() },
    [pscustomobject]@{ Id = 2;  Name = 'FENERBAHÇE A.Ş.';              Slug = 'fenerbahce';            TffClubId = 3592; StrengthSeed = 93; Supplements = @() },
    [pscustomobject]@{ Id = 3;  Name = 'BEŞİKTAŞ A.Ş.';                Slug = 'besiktas';               TffClubId = 3590; StrengthSeed = 87; Supplements = @() },
    [pscustomobject]@{ Id = 4;  Name = 'TRABZONSPOR A.Ş.';             Slug = 'trabzonspor';            TffClubId = 3596; StrengthSeed = 85; Supplements = @() },
    [pscustomobject]@{ Id = 5;  Name = 'İSTANBUL BAŞAKŞEHİR FK';       Slug = 'istanbul-basaksehir';    TffClubId = 3665; StrengthSeed = 79; Supplements = @() },
    [pscustomobject]@{ Id = 6;  Name = 'GÖZTEPE A.Ş.';                 Slug = 'goztepe';                TffClubId = 3688; StrengthSeed = 78; Supplements = @() },
    [pscustomobject]@{ Id = 7;  Name = 'SAMSUNSPOR A.Ş.';              Slug = 'samsunspor';              TffClubId = 3597; StrengthSeed = 77; Supplements = @() },
    [pscustomobject]@{ Id = 8;  Name = 'ÇAYKUR RİZESPOR A.Ş.';         Slug = 'rizespor';               TffClubId = 3631; StrengthSeed = 73; Supplements = @('Habil Özbakır') },
    [pscustomobject]@{ Id = 9;  Name = 'CORENDON ALANYASPOR';          Slug = 'alanyaspor';              TffClubId = 51;   StrengthSeed = 72; Supplements = @() },
    [pscustomobject]@{ Id = 10; Name = 'KONYASPOR';                    Slug = 'konyaspor';               TffClubId = 3600; StrengthSeed = 71; Supplements = @('Da Mata', 'Esat Tunahan Şahin', 'Yağız Arpacı', 'Ata Yanık', 'Ahmet Tırpancı') },
    [pscustomobject]@{ Id = 11; Name = 'KASIMPAŞA A.Ş.';               Slug = 'kasimpasa';               TffClubId = 39;   StrengthSeed = 69; Supplements = @() },
    [pscustomobject]@{ Id = 12; Name = 'GAZİANTEP FUTBOL KULÜBÜ A.Ş.'; Slug = 'gaziantep-fk';           TffClubId = 3672; StrengthSeed = 70; Supplements = @() },
    [pscustomobject]@{ Id = 13; Name = 'KOCAELİSPOR';                  Slug = 'kocaelispor';             TffClubId = 132;  StrengthSeed = 68; Supplements = @('Umut Can Aslan', 'Arda Özyar') },
    [pscustomobject]@{ Id = 14; Name = 'GENÇLERBİRLİĞİ';               Slug = 'genclerbirligi';          TffClubId = 3606; StrengthSeed = 66; Supplements = @() },
    [pscustomobject]@{ Id = 15; Name = 'EYÜPSPOR';                     Slug = 'eyupspor';                TffClubId = 3610; StrengthSeed = 67; Supplements = @('Umut Keseci', 'Diabel Ndoye', 'Berhan Kutlay Şatlı', 'Arda Yavuz', 'Mustafa Eren Damar', 'David Costa') },
    [pscustomobject]@{ Id = 16; Name = 'ERZURUMSPOR FK';               Slug = 'bb-erzurumspor';          TffClubId = 4123; StrengthSeed = 62; Supplements = @() },
    [pscustomobject]@{ Id = 17; Name = 'AMED SPORTİF FAALİYETLER';     Slug = 'amed-sk';                 TffClubId = 3678; StrengthSeed = 64; Supplements = @() },
    [pscustomobject]@{ Id = 18; Name = 'ÇORUM FK';                     Slug = 'corum-fk';                TffClubId = 3199; StrengthSeed = 61; Supplements = @() }
)

$leagueSource = 'https://www.fussballeuropa.com/liga/super-lig'
$abilitySource = 'https://www.ea.com/en/games/ea-sports-fc/ratings/leagues-ratings/trendyol-super-lig/68'
$leagueHtml = (Invoke-WebRequest -Uri $leagueSource -Headers $headers -UseBasicParsing -TimeoutSec 30).Content
$liveTeamSlugs = @(
    [regex]::Matches($leagueHtml, 'href="/team/(?<slug>[^"/?]+)', 'IgnoreCase') |
        ForEach-Object { $_.Groups['slug'].Value } |
        Sort-Object -Unique
)
$expectedLiveTeamSlugs = @($clubs | ForEach-Object {
    if ($_.Slug -eq 'corum-fk') { 'corum-belediyespor' } else { $_.Slug }
})
$newTeams = @($liveTeamSlugs | Where-Object { $expectedLiveTeamSlugs -notcontains $_ })
$missingTeams = @($expectedLiveTeamSlugs | Where-Object { $liveTeamSlugs -notcontains $_ })

$supplementPositions = @{
    'Habil Ozbakir' = 'Defender'
    'Da Mata' = 'Defender'
    'Esat Tunahan Sahin' = 'Goalkeeper'
    'Yagiz Arpaci' = 'Defender'
    'Ata Yanik' = 'Defender'
    'Ahmet Tirpanci' = 'Defender'
    'Umut Can Aslan' = 'Defender'
    'Arda Ozyar' = 'Forward'
    'Umut Keseci' = 'Goalkeeper'
    'Diabel Ndoye' = 'Defender'
    'Berhan Kutlay Satli' = 'Defender'
    'Arda Yavuz' = 'Defender'
    'Mustafa Eren Damar' = 'Defender'
    'David Costa' = 'Midfielder'
}

$corumRoster = @(
    'Arif Şimşir', 'Ibrahim Sehic', 'Hrvoje Smolcic', 'Serdar Saatçı', 'Arda Şengül',
    'Taha İbrahim Rençber', 'Sinan Osmanoğlu', 'Berkay Arı', 'Cemali Sertel', 'Erkan Kaş',
    'Gökhan Sazdağı', 'Ylber Ramadani', 'Hasan Emre Yeşilyurt', 'Ferhat Yazgan', 'Atakan Akkaynak',
    'Ahmed Ildız', 'Pedrinho', 'Fredy', 'Danijel Aleksic', 'Kenan Fakılı',
    'Emircan Gürlük', 'Serdar Gürler', 'Braian Samudio', 'Geraldo', 'Mame Thiam'
)

$corumPositions = @{}
foreach ($name in $corumRoster[0..1]) { $corumPositions[$name] = 'Goalkeeper' }
foreach ($name in $corumRoster[2..10]) { $corumPositions[$name] = 'Defender' }
foreach ($name in $corumRoster[11..19]) { $corumPositions[$name] = 'Midfielder' }
foreach ($name in $corumRoster[20..24]) { $corumPositions[$name] = 'Forward' }

function Get-CSharpString([string]$Value) {
    return $Value.Replace('\', '\\').Replace('"', '\"')
}

function Get-FirstUrl([string]$Html, [string]$Pattern) {
    $match = [regex]::Match($Html, $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        return $null
    }

    return $match.Value.Replace('\', '/')
}

function Save-RemoteAsset([string]$Url, [string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Url)) {
        throw "Asset URL is missing for $Path"
    }

    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    Invoke-WebRequest -Uri $Url -Headers $headers -UseBasicParsing -OutFile $Path -TimeoutSec 30
}

function ConvertTo-PositionRole([string]$PositionLabel) {
    switch ($PositionLabel) {
        'Torwart' { return 'Goalkeeper' }
        'Innenverteidigung' { return 'CentreBack' }
        'Rechter Verteidiger' { return 'RightBack' }
        'Linker Verteidiger' { return 'LeftBack' }
        'Defensives Mittelfeld' { return 'DefensiveMidfielder' }
        'Zentrales Mittelfeld' { return 'CentralMidfielder' }
        'Offensives Mittelfeld' { return 'AttackingMidfielder' }
        'Rechtes Mittelfeld' { return 'RightMidfielder' }
        'Linkes Mittelfeld' { return 'LeftMidfielder' }
        'Rechter Flügel' { return 'RightWinger' }
        'Linker Flügel' { return 'LeftWinger' }
        'Mittelstürmer' { return 'Striker' }
        default { throw "Unsupported player position: $PositionLabel" }
    }
}

function ConvertTo-PositionGroup([string]$PositionRole) {
    switch ($PositionRole) {
        'Goalkeeper' { return 'Goalkeeper' }
        { $_ -in @('CentreBack', 'RightBack', 'LeftBack') } { return 'Defender' }
        { $_ -in @('DefensiveMidfielder', 'CentralMidfielder', 'AttackingMidfielder', 'RightMidfielder', 'LeftMidfielder') } { return 'Midfielder' }
        { $_ -in @('RightWinger', 'LeftWinger', 'Striker') } { return 'Forward' }
        default { throw "Unsupported player role: $PositionRole" }
    }
}

function Get-PositionKey([string]$Name) {
    $normalized = $Name.Normalize([Text.NormalizationForm]::FormD)
    $ascii = -join ($normalized.ToCharArray() | Where-Object {
        [Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne
            [Globalization.UnicodeCategory]::NonSpacingMark
    })
    return $ascii.Replace('ı', 'i').Replace('İ', 'I').Replace('ş', 's').Replace('Ş', 'S')
}

function Get-PlayerAbility([object]$Player, [int]$SlotIndex, [int]$StrengthSeed) {
    if ($null -ne $Player.EaRating) {
        $currentAbility = [int]$Player.EaRating
    }
    else {
        # EA veri tabanında bulunmayan yeni/genç oyuncular için kontrollü yedek model.
        $baseAbility = [Math]::Round(35 + ($StrengthSeed * 0.45))
        $squadRoleOffset = if ($SlotIndex -lt 11) { 1 } elseif ($SlotIndex -lt 16) { -2 } elseif ($SlotIndex -lt 21) { -5 } else { -8 }
        $nameHash = 17
        foreach ($character in $Player.Name.ToCharArray()) {
            $nameHash = (($nameHash * 31) + [int]$character) -band 0x7fffffff
        }
        $individualOffset = ($nameHash % 3) - 1
        $currentAbility = [Math]::Clamp(
            [int]($baseAbility + $squadRoleOffset + $individualOffset),
            45,
            82)
    }
    $potentialGain = if ($Player.Age -le 19) { 9 } elseif ($Player.Age -le 21) { 7 } elseif ($Player.Age -le 23) { 5 } elseif ($Player.Age -le 26) { 3 } elseif ($Player.Age -le 29) { 1 } else { 0 }
    $potentialAbility = [Math]::Min(99, $currentAbility + $potentialGain)

    return [pscustomobject]@{ Current = $currentAbility; Potential = $potentialAbility }
}

function Get-EaPlayerName([object]$Player) {
    if (-not [string]::IsNullOrWhiteSpace($Player.commonName)) {
        return $Player.commonName.Trim()
    }

    return "$($Player.firstName) $($Player.lastName)".Trim()
}

$eaPlayers = @()
$abilityPage = 1
$abilityTotal = 1
while ($eaPlayers.Count -lt $abilityTotal) {
    $pageUrl = "${abilitySource}?page=$abilityPage"
    $abilityHtml = (Invoke-WebRequest -Uri $pageUrl -Headers $headers -UseBasicParsing -TimeoutSec 30).Content
    $abilityJsonMatch = [regex]::Match(
        $abilityHtml,
        '<script id="__NEXT_DATA__" type="application/json">(?<json>.*?)</script>',
        'Singleline')
    if (-not $abilityJsonMatch.Success) {
        throw "EA FC ratings payload could not be found on page $abilityPage."
    }

    $abilityPayload = $abilityJsonMatch.Groups['json'].Value | ConvertFrom-Json
    $entries = $abilityPayload.props.pageProps.ratingsEntries
    $abilityTotal = [int]$entries.totalItems
    $pagePlayers = @($entries.items)
    if ($pagePlayers.Count -eq 0) {
        throw "EA FC ratings page $abilityPage is empty before reaching $abilityTotal players."
    }

    $eaPlayers += $pagePlayers
    $abilityPage++
}
if ($eaPlayers.Count -lt 300) {
    throw "EA FC ratings payload has only $($eaPlayers.Count) Süper Lig players."
}

$eaRatingsByName = @{}
foreach ($eaPlayer in $eaPlayers) {
    $key = Get-PositionKey (Get-EaPlayerName $eaPlayer)
    if (-not $eaRatingsByName.ContainsKey($key) -or
        $eaRatingsByName[$key].Overall -lt $eaPlayer.overallRating) {
        $eaRatingsByName[$key] = [pscustomobject]@{
            Overall = [int]$eaPlayer.overallRating
            Birthdate = $eaPlayer.birthdate
        }
    }
}

function Select-BalancedSquad([object[]]$Players, [string]$ClubName) {
    $targets = [ordered]@{
        Goalkeeper = 3
        Defender = 8
        Midfielder = 8
        Forward = 6
    }

    $slotSpecs = @(
        [pscustomobject]@{ Roles = @('Goalkeeper'); Group = 'Goalkeeper' },
        [pscustomobject]@{ Roles = @('RightBack'); Group = 'Defender' },
        [pscustomobject]@{ Roles = @('CentreBack'); Group = 'Defender' },
        [pscustomobject]@{ Roles = @('CentreBack'); Group = 'Defender' },
        [pscustomobject]@{ Roles = @('LeftBack', 'RightBack'); Group = 'Defender' },
        [pscustomobject]@{ Roles = @('RightMidfielder'); Group = 'Midfielder' },
        [pscustomobject]@{ Roles = @('DefensiveMidfielder', 'CentralMidfielder', 'AttackingMidfielder'); Group = 'Midfielder' },
        [pscustomobject]@{ Roles = @('CentralMidfielder', 'DefensiveMidfielder', 'AttackingMidfielder'); Group = 'Midfielder' },
        [pscustomobject]@{ Roles = @('LeftMidfielder'); Group = 'Midfielder' },
        [pscustomobject]@{ Roles = @('Striker'); Group = 'Forward' },
        [pscustomobject]@{ Roles = @('Striker'); Group = 'Forward' }
    )

    $defaultXi = @()
    foreach ($slot in $slotSpecs) {
        $usedNames = @($defaultXi | ForEach-Object Name)
        $candidate = @(
            $Players |
                Where-Object { $usedNames -notcontains $_.Name -and $_.Role -in $slot.Roles } |
                Sort-Object @{ Expression = { if ($null -ne $_.EaRating) { $_.EaRating } else { 0 } }; Descending = $true }, Name |
                Select-Object -First 1
        )
        if ($candidate.Count -eq 0) {
            $candidate = @(
                $Players |
                    Where-Object { $usedNames -notcontains $_.Name -and $_.Group -eq $slot.Group } |
                    Sort-Object @{ Expression = { if ($null -ne $_.EaRating) { $_.EaRating } else { 0 } }; Descending = $true }, Name |
                    Select-Object -First 1
            )
        }
        if ($candidate.Count -eq 0) {
            throw "$ClubName cannot fill the $($slot.Group) starting slot."
        }

        $defaultXi += $candidate[0]
    }

    $selected = @($defaultXi)
    foreach ($group in $targets.Keys) {
        $groupCount = @($selected | Where-Object Group -eq $group).Count
        $required = $targets[$group] - $groupCount
        if ($required -le 0) {
            continue
        }

        $selectedNames = @($selected | ForEach-Object Name)
        $selected += @(
            $Players |
                Where-Object { $selectedNames -notcontains $_.Name -and $_.Group -eq $group } |
                Select-Object -First $required
        )
    }

    $selectedNames = @($selected | ForEach-Object Name)
    if ($selected.Count -lt 25) {
        $selected += @(
            $Players |
                Where-Object { $selectedNames -notcontains $_.Name } |
                Select-Object -First (25 - $selected.Count)
        )
    }

    if ($selected.Count -lt 25) {
        throw "$ClubName has only $($selected.Count) usable player profiles."
    }

    if ($defaultXi.Count -ne 11) {
        throw "$ClubName cannot provide a natural 4-4-2 starting XI."
    }

    $defaultNames = @($defaultXi | ForEach-Object Name)
    return @($defaultXi + @($selected | Where-Object { $defaultNames -notcontains $_.Name } | Select-Object -First 14))
}

[System.IO.Directory]::CreateDirectory((Split-Path -Parent $generatedFile)) | Out-Null
[System.IO.Directory]::CreateDirectory($seasonAssetRoot) | Out-Null

$generatedClubs = @()
foreach ($club in $clubs) {
    $rosterSource = "https://www.fussballeuropa.com/team/$($club.Slug)/kader"
    if ($club.Id -eq 18) {
        $players = @($corumRoster | ForEach-Object {
            [pscustomobject]@{ Name = $_; Group = $corumPositions[$_]; Role = $null; Age = 24; EaRating = $null }
        })
        $verifiedOn = $snapshotDate
        $rosterSource = 'https://www.transfermarkt.co.uk/corum-fk/kader/verein/37951/saison_id/2026'
    }
    else {
        $rosterHtml = (Invoke-WebRequest -Uri $rosterSource -Headers $headers -UseBasicParsing -TimeoutSec 30).Content
        $players = @(
            [regex]::Matches(
                $rosterHtml,
                '<a href="/spieler/[^"?]+" class="kader-row-link">(?<row>(?:(?!</a>).)*)</a>',
                'IgnoreCase,Singleline') |
                ForEach-Object {
                    $row = $_.Groups['row'].Value
                    $nameMatch = [regex]::Match($row, '<div class="ts-name">(?<name>[^<]+)</div>', 'IgnoreCase')
                    $metaMatch = [regex]::Match($row, '<div class="ts-teamname">(?<meta>[^<]+)</div>', 'IgnoreCase')
                    if ($nameMatch.Success -and $metaMatch.Success) {
                        $name = [System.Net.WebUtility]::HtmlDecode($nameMatch.Groups['name'].Value.Trim())
                        $meta = [System.Net.WebUtility]::HtmlDecode($metaMatch.Groups['meta'].Value.Trim())
                        $position = ($meta -split ',', 2)[-1].Trim()
                        $ageMatch = [regex]::Match($meta, '^(?<age>\d{1,2})\s+Jahre')
                        $role = ConvertTo-PositionRole $position
                        [pscustomobject]@{
                            Name = $name
                            Group = ConvertTo-PositionGroup $role
                            Role = $role
                            Age = if ($ageMatch.Success) { [int]$ageMatch.Groups['age'].Value } else { 24 }
                            EaRating = $null
                        }
                    }
                } |
                Group-Object Name |
                ForEach-Object { $_.Group[0] }
        )
        $verifiedOn = $snapshotDate
    }

    foreach ($supplement in $club.Supplements) {
        if ($players.Name -notcontains $supplement) {
            $positionKey = Get-PositionKey $supplement
            if (-not $supplementPositions.ContainsKey($positionKey)) {
                throw "Missing position for supplement player $supplement."
            }

            $players += [pscustomobject]@{
                Name = $supplement
                Group = $supplementPositions[$positionKey]
                Role = $null
                Age = 21
                EaRating = $null
            }
        }
    }

    if ($players.Count -lt 25) {
        throw "$($club.Name) roster has only $($players.Count) unique players."
    }

    foreach ($player in $players) {
        $ratingKey = Get-PositionKey $player.Name
        if ($eaRatingsByName.ContainsKey($ratingKey)) {
            $player.EaRating = $eaRatingsByName[$ratingKey].Overall
        }
    }

    $players = @(Select-BalancedSquad $players $club.Name)

    $tffSource = "https://www.tff.org/Default.aspx?kulupId=$($club.TffClubId)&pageID=28"
    $tffHtml = (Invoke-WebRequest -Uri $tffSource -Headers $headers -UseBasicParsing -TimeoutSec 30).Content
    $crestUrl = Get-FirstUrl $tffHtml 'https://fys\.tff\.org/TFFUploadFolder/KulupLogolari[/\\][^"'']+?\.png'
    $homeKitUrl = Get-FirstUrl $tffHtml 'https://fys\.tff\.org/TFFUploadFolder/KulupForma/26/[^"'']+?_I_1_F_1500\.png'
    $awayKitUrl = Get-FirstUrl $tffHtml 'https://fys\.tff\.org/TFFUploadFolder/KulupForma/26/[^"'']+?_D_1_F_1500\.png'
    $thirdKitUrl = Get-FirstUrl $tffHtml 'https://fys\.tff\.org/TFFUploadFolder/KulupForma/26/[^"'']+?_Y_1_F_1500\.png'
    if ([string]::IsNullOrWhiteSpace($thirdKitUrl)) {
        $thirdKitUrl = Get-FirstUrl $tffHtml 'https://fys\.tff\.org/TFFUploadFolder/KulupForma/26/[^"'']+?_(?:I|D)_2_F_1500\.png'
    }
    if ([string]::IsNullOrWhiteSpace($thirdKitUrl)) {
        $thirdKitUrl = $awayKitUrl
    }

    $clubAssetDirectory = Join-Path $seasonAssetRoot $club.Slug
    if (-not $SkipAssets -and -not $CheckOnly) {
        Save-RemoteAsset $crestUrl (Join-Path $clubAssetDirectory 'crest.png')
        Save-RemoteAsset $homeKitUrl (Join-Path $clubAssetDirectory 'kit-home.png')
        Save-RemoteAsset $awayKitUrl (Join-Path $clubAssetDirectory 'kit-away.png')
        Save-RemoteAsset $thirdKitUrl (Join-Path $clubAssetDirectory 'kit-third.png')
    }

    $resourceRoot = "res://assets/clubs/turkey/super-lig-2026-27/$($club.Slug)"
    $generatedClubs += [pscustomobject]@{
        Club = $club
        Players = $players
        VerifiedOn = $verifiedOn
        RosterSource = $rosterSource
        AbilitySource = $abilitySource
        TffSource = $tffSource
        CrestPath = "$resourceRoot/crest.png"
        HomeKitPath = "$resourceRoot/kit-home.png"
        AwayKitPath = "$resourceRoot/kit-away.png"
        ThirdKitPath = "$resourceRoot/kit-third.png"
    }
}

$builder = [System.Text.StringBuilder]::new()
[void]$builder.AppendLine('// <auto-generated />')
[void]$builder.AppendLine('using FootballCareerSimulator.Domain.Shared;')
[void]$builder.AppendLine('using FootballCareerSimulator.Simulation.TeamPreparation;')
[void]$builder.AppendLine()
[void]$builder.AppendLine('namespace FootballCareerSimulator.Simulation.DataPacks;')
[void]$builder.AppendLine()
[void]$builder.AppendLine('public static class TurkeySuperLig202627DataPack')
[void]$builder.AppendLine('{')
[void]$builder.AppendLine('    public const string CompetitionName = "Trendyol Süper Lig";')
[void]$builder.AppendLine('    public const string SeasonName = "2026-2027";')
[void]$builder.AppendLine("    public const string SnapshotDate = `"$snapshotDate`";")
[void]$builder.AppendLine()
[void]$builder.AppendLine('    private static readonly IReadOnlyDictionary<long, TurkeySuperLigClubData> Clubs =')
[void]$builder.AppendLine('        new Dictionary<long, TurkeySuperLigClubData>')
[void]$builder.AppendLine('        {')
foreach ($entry in $generatedClubs) {
    $club = $entry.Club
    [void]$builder.AppendLine("            [$($club.Id)] = new(")
    [void]$builder.AppendLine("                ClubId: $($club.Id),")
    [void]$builder.AppendLine("                OfficialName: `"$(Get-CSharpString $club.Name)`",")
    [void]$builder.AppendLine("                RosterVerifiedOn: `"$($entry.VerifiedOn)`",")
    [void]$builder.AppendLine("                RosterSourceUrl: `"$(Get-CSharpString $entry.RosterSource)`",")
    [void]$builder.AppendLine("                AbilitySourceUrl: `"$(Get-CSharpString $entry.AbilitySource)`",")
    [void]$builder.AppendLine("                BrandingSourceUrl: `"$(Get-CSharpString $entry.TffSource)`",")
    [void]$builder.AppendLine("                CrestResourcePath: `"$($entry.CrestPath)`",")
    [void]$builder.AppendLine("                HomeKitResourcePath: `"$($entry.HomeKitPath)`",")
    [void]$builder.AppendLine("                AwayKitResourcePath: `"$($entry.AwayKitPath)`",")
    [void]$builder.AppendLine("                ThirdKitResourcePath: `"$($entry.ThirdKitPath)`",")
    [void]$builder.AppendLine('                Players:')
    [void]$builder.AppendLine('                [')
    for ($slotIndex = 0; $slotIndex -lt $entry.Players.Count; $slotIndex++) {
        $player = $entry.Players[$slotIndex]
        $ability = Get-PlayerAbility $player $slotIndex $club.StrengthSeed
        if ($null -ne $player.Role) {
            [void]$builder.AppendLine("                    new(`"$(Get-CSharpString $player.Name)`", MvpSquadPositionRole.$($player.Role), $($ability.Current), $($ability.Potential), $($player.Age)),")
        }
        else {
            [void]$builder.AppendLine("                    new(`"$(Get-CSharpString $player.Name)`", MvpSquadPositionGroup.$($player.Group), null, $($ability.Current), $($ability.Potential), $($player.Age)),")
        }
    }
    [void]$builder.AppendLine('                ]),')
}
[void]$builder.AppendLine('        };')
[void]$builder.AppendLine()
[void]$builder.AppendLine('    public static IReadOnlyCollection<TurkeySuperLigClubData> AllClubs => Clubs.Values.ToArray();')
[void]$builder.AppendLine()
[void]$builder.AppendLine('    public static TurkeySuperLigClubData GetClub(ClubId clubId) =>')
[void]$builder.AppendLine('        Clubs.TryGetValue(clubId.Value, out var club)')
[void]$builder.AppendLine('            ? club')
[void]$builder.AppendLine('            : throw new ArgumentOutOfRangeException(nameof(clubId), clubId.Value, "Club is not in the 2026-27 Süper Lig data pack.");')
[void]$builder.AppendLine()
[void]$builder.AppendLine('    public static bool TryGetClub(ClubId clubId, out TurkeySuperLigClubData club) =>')
[void]$builder.AppendLine('        Clubs.TryGetValue(clubId.Value, out club!);')
[void]$builder.AppendLine('}')
[void]$builder.AppendLine()
[void]$builder.AppendLine('public sealed record TurkeySuperLigClubData(')
[void]$builder.AppendLine('    long ClubId,')
[void]$builder.AppendLine('    string OfficialName,')
[void]$builder.AppendLine('    string RosterVerifiedOn,')
[void]$builder.AppendLine('    string RosterSourceUrl,')
[void]$builder.AppendLine('    string AbilitySourceUrl,')
[void]$builder.AppendLine('    string BrandingSourceUrl,')
[void]$builder.AppendLine('    string CrestResourcePath,')
[void]$builder.AppendLine('    string HomeKitResourcePath,')
[void]$builder.AppendLine('    string AwayKitResourcePath,')
[void]$builder.AppendLine('    string ThirdKitResourcePath,')
[void]$builder.AppendLine('    IReadOnlyList<MvpSquadPlayerProfile> Players)')
[void]$builder.AppendLine('{')
[void]$builder.AppendLine('    public IReadOnlyList<string> PlayerNames => Players.Select(player => player.DisplayName).ToArray();')
[void]$builder.AppendLine('    public int SquadStrength => (int)Math.Round(Players.Take(11).Average(player => player.CurrentAbility ?? 65), MidpointRounding.AwayFromZero);')
[void]$builder.AppendLine('}')

$generatedContent = $builder.ToString()
$existingContent = if (Test-Path -LiteralPath $generatedFile) {
    [System.IO.File]::ReadAllText($generatedFile)
}
else {
    ''
}

function Get-ComparableDataPackContent([string]$Content) {
    return [regex]::Replace(
        [regex]::Replace($Content, 'SnapshotDate = "[^"]+"', 'SnapshotDate = "<date>"'),
        'RosterVerifiedOn: "[^"]+"',
        'RosterVerifiedOn: "<date>"')
}

$existingPlayers = @(
    [regex]::Matches($existingContent, 'new\("(?<name>(?:\\.|[^"])*)", MvpSquadPosition') |
        ForEach-Object { $_.Groups['name'].Value.Replace('\"', '"').Replace('\\', '\') } |
        Sort-Object -Unique
)
$livePlayers = @(
    [regex]::Matches($generatedContent, 'new\("(?<name>(?:\\.|[^"])*)", MvpSquadPosition') |
        ForEach-Object { $_.Groups['name'].Value.Replace('\"', '"').Replace('\\', '\') } |
        Sort-Object -Unique
)
$newPlayers = @($livePlayers | Where-Object { $existingPlayers -notcontains $_ })
$removedPlayers = @($existingPlayers | Where-Object { $livePlayers -notcontains $_ })
$rosterUpdateRequired = (Get-ComparableDataPackContent $existingContent) -cne
    (Get-ComparableDataPackContent $generatedContent)
$updateRequired = $rosterUpdateRequired -or $newTeams.Count -gt 0 -or $missingTeams.Count -gt 0

$status = [ordered]@{
    Competition = 'Trendyol Süper Lig'
    Season = '2026-2027'
    CheckedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    CurrentSnapshotDate = if ($existingContent -match 'SnapshotDate = "(?<date>[^"]+)"') { $Matches['date'] } else { $null }
    LiveSnapshotDate = $snapshotDate
    ClubCount = $generatedClubs.Count
    LiveTeamCount = $liveTeamSlugs.Count
    PlayerCount = $livePlayers.Count
    EaRatingMatchCount = @($generatedClubs.Players | ForEach-Object { $_ } | Where-Object { $null -ne $_.EaRating }).Count
    UpdateRequired = $updateRequired
    NewTeams = $newTeams
    MissingTeams = $missingTeams
    NewPlayers = $newPlayers
    RemovedPlayers = $removedPlayers
    Message = if ($updateRequired) {
        'Canlı kadrolar değişti; veri paketi güncellenmeli.'
    }
    else {
        'Veri paketi canlı kadrolarla güncel.'
    }
    UpdateCommand = 'powershell -ExecutionPolicy Bypass -File tools/Update-TurkeySuperLig202627DataPack.ps1 -SkipAssets'
}

if (-not $CheckOnly) {
    [System.IO.File]::WriteAllText(
        $generatedFile,
        $generatedContent,
        [System.Text.UTF8Encoding]::new($false))
}

if ($AsJson) {
    $status | ConvertTo-Json -Depth 4
    return
}

if ($CheckOnly) {
    Write-Output $status.Message
    Write-Output "UpdateRequired=$($status.UpdateRequired); NewPlayers=$($newPlayers.Count); RemovedPlayers=$($removedPlayers.Count)"
    return
}

Write-Output "Generated $generatedFile"
if ($SkipAssets) {
    Write-Output 'Existing club assets were kept.'
}
else {
    Write-Output "Downloaded $($clubs.Count) crests and $($clubs.Count * 3) official kit images to $seasonAssetRoot"
}
