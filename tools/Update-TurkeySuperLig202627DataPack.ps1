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

$corumRoster = @(
    'Arif Şimşir', 'Ibrahim Sehic', 'Hrvoje Smolcic', 'Serdar Saatçı', 'Arda Şengül',
    'Taha İbrahim Rençber', 'Sinan Osmanoğlu', 'Berkay Arı', 'Cemali Sertel', 'Erkan Kaş',
    'Gökhan Sazdağı', 'Ylber Ramadani', 'Hasan Emre Yeşilyurt', 'Ferhat Yazgan', 'Atakan Akkaynak',
    'Ahmed Ildız', 'Pedrinho', 'Fredy', 'Danijel Aleksic', 'Kenan Fakılı',
    'Emircan Gürlük', 'Serdar Gürler', 'Braian Samudio', 'Geraldo', 'Mame Thiam'
)

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

[System.IO.Directory]::CreateDirectory((Split-Path -Parent $generatedFile)) | Out-Null
[System.IO.Directory]::CreateDirectory($seasonAssetRoot) | Out-Null

$generatedClubs = @()
foreach ($club in $clubs) {
    $rosterSource = "https://www.fussballeuropa.com/team/$($club.Slug)/kader"
    if ($club.Id -eq 18) {
        $names = @($corumRoster)
        $verifiedOn = '2026-08-10'
        $rosterSource = 'https://www.transfermarkt.co.uk/corum-fk/kader/verein/37951/saison_id/2026'
    }
    else {
        $rosterHtml = (Invoke-WebRequest -Uri $rosterSource -Headers $headers -UseBasicParsing -TimeoutSec 30).Content
        $names = @(
            [regex]::Matches(
                $rosterHtml,
                '<a href="/spieler/[^"?]+" class="kader-row-link">(?:(?!</a>).)*?<div class="ts-name">(?<name>[^<]+)</div>',
                'IgnoreCase,Singleline') |
                ForEach-Object { [System.Net.WebUtility]::HtmlDecode($_.Groups['name'].Value.Trim()) } |
                Select-Object -Unique
        )
        $verifiedOn = '2026-08-10'
    }

    foreach ($supplement in $club.Supplements) {
        if ($names -notcontains $supplement) {
            $names += $supplement
        }
    }

    if ($names.Count -lt 25) {
        throw "$($club.Name) roster has only $($names.Count) unique players."
    }

    $names = @($names | Select-Object -First 25)

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
        Names = $names
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
    [void]$builder.AppendLine('                PlayerNames:')
    [void]$builder.AppendLine('                [')
    foreach ($name in $entry.Names) {
        [void]$builder.AppendLine("                    `"$(Get-CSharpString $name)`",")
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
[void]$builder.AppendLine('    IReadOnlyList<string> PlayerNames);')

[System.IO.File]::WriteAllText(
    $generatedFile,
    $builder.ToString(),
    [System.Text.UTF8Encoding]::new($false))

Write-Output "Generated $generatedFile"
Write-Output "Downloaded $($clubs.Count) crests and $($clubs.Count * 3) official kit images to $seasonAssetRoot"
