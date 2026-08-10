[CmdletBinding()]
param(
    [string]$WorkspaceRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$headers = @{ 'User-Agent' = 'Mozilla/5.0' }
$seasonAssetRoot = Join-Path $WorkspaceRoot 'src/FootballCareerSimulator.Presentation/assets/clubs/turkey/super-lig-2026-27'
$generatedFile = Join-Path $WorkspaceRoot 'src/FootballCareerSimulator.Simulation/DataPacks/TurkeySuperLig202627DataPack.Generated.cs'

$clubs = @(
    [pscustomobject]@{ Id = 1;  Name = 'GALATASARAY A.Ş.';              Slug = 'galatasaray';          TffClubId = 3604; Supplements = @() },
    [pscustomobject]@{ Id = 2;  Name = 'FENERBAHÇE A.Ş.';              Slug = 'fenerbahce';            TffClubId = 3592; Supplements = @() },
    [pscustomobject]@{ Id = 3;  Name = 'BEŞİKTAŞ A.Ş.';                Slug = 'besiktas';               TffClubId = 3590; Supplements = @() },
    [pscustomobject]@{ Id = 4;  Name = 'TRABZONSPOR A.Ş.';             Slug = 'trabzonspor';            TffClubId = 3596; Supplements = @() },
    [pscustomobject]@{ Id = 5;  Name = 'İSTANBUL BAŞAKŞEHİR FK';       Slug = 'istanbul-basaksehir';    TffClubId = 3665; Supplements = @() },
    [pscustomobject]@{ Id = 6;  Name = 'GÖZTEPE A.Ş.';                 Slug = 'goztepe';                TffClubId = 3688; Supplements = @() },
    [pscustomobject]@{ Id = 7;  Name = 'SAMSUNSPOR A.Ş.';              Slug = 'samsunspor';              TffClubId = 3597; Supplements = @() },
    [pscustomobject]@{ Id = 8;  Name = 'ÇAYKUR RİZESPOR A.Ş.';         Slug = 'rizespor';               TffClubId = 3631; Supplements = @('Habil Özbakır') },
    [pscustomobject]@{ Id = 9;  Name = 'CORENDON ALANYASPOR';          Slug = 'alanyaspor';              TffClubId = 51;   Supplements = @() },
    [pscustomobject]@{ Id = 10; Name = 'KONYASPOR';                    Slug = 'konyaspor';               TffClubId = 3600; Supplements = @('Da Mata', 'Esat Tunahan Şahin', 'Yağız Arpacı', 'Ata Yanık', 'Ahmet Tırpancı') },
    [pscustomobject]@{ Id = 11; Name = 'KASIMPAŞA A.Ş.';               Slug = 'kasimpasa';               TffClubId = 39;   Supplements = @() },
    [pscustomobject]@{ Id = 12; Name = 'GAZİANTEP FUTBOL KULÜBÜ A.Ş.'; Slug = 'gaziantep-fk';           TffClubId = 3672; Supplements = @() },
    [pscustomobject]@{ Id = 13; Name = 'KOCAELİSPOR';                  Slug = 'kocaelispor';             TffClubId = 132;  Supplements = @('Umut Can Aslan', 'Arda Özyar') },
    [pscustomobject]@{ Id = 14; Name = 'GENÇLERBİRLİĞİ';               Slug = 'genclerbirligi';          TffClubId = 3606; Supplements = @() },
    [pscustomobject]@{ Id = 15; Name = 'EYÜPSPOR';                     Slug = 'eyupspor';                TffClubId = 3610; Supplements = @('Umut Keseci', 'Diabel Ndoye', 'Berhan Kutlay Şatlı', 'Arda Yavuz', 'Mustafa Eren Damar', 'David Costa') },
    [pscustomobject]@{ Id = 16; Name = 'ERZURUMSPOR FK';               Slug = 'bb-erzurumspor';          TffClubId = 4123; Supplements = @() },
    [pscustomobject]@{ Id = 17; Name = 'AMED SPORTİF FAALİYETLER';     Slug = 'amed-sk';                 TffClubId = 3678; Supplements = @() },
    [pscustomobject]@{ Id = 18; Name = 'ÇORUM FK';                     Slug = 'corum-fk';                TffClubId = 3199; Supplements = @() }
)

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

function ConvertTo-PositionGroup([string]$PositionLabel) {
    if ($PositionLabel -match 'Torwart') { return 'Goalkeeper' }
    if ($PositionLabel -match 'Verteid') { return 'Defender' }
    if ($PositionLabel -match 'Mittelfeld') { return 'Midfielder' }
    if ($PositionLabel -match 'Sturmer|Stürmer|Flugel|Flügel|Spitze') { return 'Forward' }

    throw "Unsupported player position: $PositionLabel"
}

function Get-PositionKey([string]$Name) {
    $normalized = $Name.Normalize([Text.NormalizationForm]::FormD)
    $ascii = -join ($normalized.ToCharArray() | Where-Object {
        [Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne
            [Globalization.UnicodeCategory]::NonSpacingMark
    })
    return $ascii.Replace('ı', 'i').Replace('İ', 'I').Replace('ş', 's').Replace('Ş', 'S')
}

function Select-BalancedSquad([object[]]$Players, [string]$ClubName) {
    $targets = [ordered]@{
        Goalkeeper = 3
        Defender = 8
        Midfielder = 8
        Forward = 6
    }

    $selected = @()
    foreach ($role in $targets.Keys) {
        $selected += @($Players | Where-Object Role -eq $role | Select-Object -First $targets[$role])
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

    $defaultXi = @()
    $defaultXi += @($selected | Where-Object Role -eq 'Goalkeeper' | Select-Object -First 1)
    $defaultXi += @($selected | Where-Object Role -eq 'Defender' | Select-Object -First 4)
    $defaultXi += @($selected | Where-Object Role -eq 'Midfielder' | Select-Object -First 4)
    $defaultXi += @($selected | Where-Object Role -eq 'Forward' | Select-Object -First 2)
    if ($defaultXi.Count -ne 11) {
        throw "$ClubName cannot provide a natural 4-4-2 starting XI."
    }

    $defaultNames = @($defaultXi | ForEach-Object Name)
    return @($defaultXi + @($selected | Where-Object { $defaultNames -notcontains $_.Name }))
}

[System.IO.Directory]::CreateDirectory((Split-Path -Parent $generatedFile)) | Out-Null
[System.IO.Directory]::CreateDirectory($seasonAssetRoot) | Out-Null

$generatedClubs = @()
foreach ($club in $clubs) {
    $rosterSource = "https://www.fussballeuropa.com/team/$($club.Slug)/kader"
    if ($club.Id -eq 18) {
        $players = @($corumRoster | ForEach-Object {
            [pscustomobject]@{ Name = $_; Role = $corumPositions[$_] }
        })
        $verifiedOn = '2026-08-10'
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
                        [pscustomobject]@{ Name = $name; Role = (ConvertTo-PositionGroup $position) }
                    }
                } |
                Group-Object Name |
                ForEach-Object { $_.Group[0] }
        )
        $verifiedOn = '2026-08-10'
    }

    foreach ($supplement in $club.Supplements) {
        if ($players.Name -notcontains $supplement) {
            $positionKey = Get-PositionKey $supplement
            if (-not $supplementPositions.ContainsKey($positionKey)) {
                throw "Missing position for supplement player $supplement."
            }

            $players += [pscustomobject]@{
                Name = $supplement
                Role = $supplementPositions[$positionKey]
            }
        }
    }

    if ($players.Count -lt 25) {
        throw "$($club.Name) roster has only $($players.Count) unique players."
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
    Save-RemoteAsset $crestUrl (Join-Path $clubAssetDirectory 'crest.png')
    Save-RemoteAsset $homeKitUrl (Join-Path $clubAssetDirectory 'kit-home.png')
    Save-RemoteAsset $awayKitUrl (Join-Path $clubAssetDirectory 'kit-away.png')
    Save-RemoteAsset $thirdKitUrl (Join-Path $clubAssetDirectory 'kit-third.png')

    $resourceRoot = "res://assets/clubs/turkey/super-lig-2026-27/$($club.Slug)"
    $generatedClubs += [pscustomobject]@{
        Club = $club
        Players = $players
        VerifiedOn = $verifiedOn
        RosterSource = $rosterSource
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
[void]$builder.AppendLine('    public const string SnapshotDate = "2026-08-10";')
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
    [void]$builder.AppendLine("                BrandingSourceUrl: `"$(Get-CSharpString $entry.TffSource)`",")
    [void]$builder.AppendLine("                CrestResourcePath: `"$($entry.CrestPath)`",")
    [void]$builder.AppendLine("                HomeKitResourcePath: `"$($entry.HomeKitPath)`",")
    [void]$builder.AppendLine("                AwayKitResourcePath: `"$($entry.AwayKitPath)`",")
    [void]$builder.AppendLine("                ThirdKitResourcePath: `"$($entry.ThirdKitPath)`",")
    [void]$builder.AppendLine('                Players:')
    [void]$builder.AppendLine('                [')
    foreach ($player in $entry.Players) {
        [void]$builder.AppendLine("                    new(`"$(Get-CSharpString $player.Name)`", MvpSquadPositionGroup.$($player.Role)),")
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
[void]$builder.AppendLine('    string BrandingSourceUrl,')
[void]$builder.AppendLine('    string CrestResourcePath,')
[void]$builder.AppendLine('    string HomeKitResourcePath,')
[void]$builder.AppendLine('    string AwayKitResourcePath,')
[void]$builder.AppendLine('    string ThirdKitResourcePath,')
[void]$builder.AppendLine('    IReadOnlyList<MvpSquadPlayerProfile> Players)')
[void]$builder.AppendLine('{')
[void]$builder.AppendLine('    public IReadOnlyList<string> PlayerNames => Players.Select(player => player.DisplayName).ToArray();')
[void]$builder.AppendLine('}')

[System.IO.File]::WriteAllText(
    $generatedFile,
    $builder.ToString(),
    [System.Text.UTF8Encoding]::new($false))

Write-Output "Generated $generatedFile"
Write-Output "Downloaded $($clubs.Count) crests and $($clubs.Count * 3) official kit images to $seasonAssetRoot"
