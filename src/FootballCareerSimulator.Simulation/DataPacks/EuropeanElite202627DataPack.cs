using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Simulation.DataPacks;

public sealed record EuropeanEliteClubData(
    long ClubId,
    string OfficialName,
    string ShortCode,
    string Country,
    string LeagueName,
    int SportiveStrength,
    int TransferBudgetLimit,
    IReadOnlyList<MvpSquadPlayerProfile> Players,
    string PrimaryColorHex = "#00F59B",
    string SecondaryColorHex = "#FFB800",
    string KitPattern = "Solid",
    string? CrestResourcePath = null,
    string? HomeKitResourcePath = null,
    string? AwayKitResourcePath = null,
    string? ThirdKitResourcePath = null);

public sealed record GlobalFreeAgentProfile(
    long PlayerId,
    string DisplayName,
    MvpSquadPositionRole PositionRole,
    int CurrentAbility,
    int PotentialAbility,
    int Age,
    string Nationality);

public static class EuropeanElite202627DataPack
{
    public const string SnapshotDate = "2026-09-01";

    public static readonly IReadOnlyList<string> MajorFirstDivisionLeagues =
    [
        "Premier League",
        "La Liga",
        "Bundesliga",
    ];

    private static readonly IReadOnlyDictionary<long, EuropeanEliteClubData> Clubs =
        new Dictionary<long, EuropeanEliteClubData>
        {
            // =========================================================================
            // İSPANYA — LA LIGA (1. LİG — 20 KULÜP)
            // =========================================================================
            [101] = new(
                ClubId: 101,
                OfficialName: "REAL MADRID CF",
                ShortCode: "RMA",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 89,
                TransferBudgetLimit: 110_000_000,
                Players:
                [
                    new("Kylian Mbappé", MvpSquadPositionRole.Striker, 91, 94, 27),
                    new("Vinícius Jr.", MvpSquadPositionRole.LeftWinger, 90, 93, 26),
                    new("Jude Bellingham", MvpSquadPositionRole.AttackingMidfielder, 90, 94, 23),
                    new("Federico Valverde", MvpSquadPositionRole.CentralMidfielder, 88, 90, 28),
                    new("Eduardo Camavinga", MvpSquadPositionRole.DefensiveMidfielder, 83, 89, 23),
                    new("Rodrygo", MvpSquadPositionRole.RightWinger, 86, 90, 25),
                    new("Arda Güler", MvpSquadPositionRole.AttackingMidfielder, 80, 89, 21),
                    new("Endrick", MvpSquadPositionRole.Striker, 77, 88, 20),
                    new("Éder Militão", MvpSquadPositionRole.CentreBack, 85, 87, 28),
                    new("Antonio Rüdiger", MvpSquadPositionRole.CentreBack, 88, 88, 33),
                    new("Thibaut Courtois", MvpSquadPositionRole.Goalkeeper, 89, 89, 34),
                    new("Aurélien Tchouaméni", MvpSquadPositionRole.DefensiveMidfielder, 85, 88, 26),
                    new("Dani Carvajal", MvpSquadPositionRole.RightBack, 86, 86, 34),
                    new("Ferland Mendy", MvpSquadPositionRole.LeftBack, 83, 83, 31),
                    new("Brahim Díaz", MvpSquadPositionRole.AttackingMidfielder, 82, 84, 27),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#FEBE10",
                KitPattern: "Solid"),

            [105] = new(
                ClubId: 105,
                OfficialName: "FC BARCELONA",
                ShortCode: "BAR",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 87,
                TransferBudgetLimit: 70_000_000,
                Players:
                [
                    new("Lamine Yamal", MvpSquadPositionRole.RightWinger, 84, 93, 19),
                    new("Pedri", MvpSquadPositionRole.CentralMidfielder, 86, 91, 23),
                    new("Gavi", MvpSquadPositionRole.CentralMidfielder, 83, 89, 22),
                    new("Raphinha", MvpSquadPositionRole.LeftWinger, 85, 85, 29),
                    new("Robert Lewandowski", MvpSquadPositionRole.Striker, 88, 88, 38),
                    new("Jules Koundé", MvpSquadPositionRole.RightBack, 85, 87, 27),
                    new("Pau Cubarsí", MvpSquadPositionRole.CentreBack, 78, 90, 19),
                    new("Marc Casadó", MvpSquadPositionRole.DefensiveMidfielder, 77, 85, 22),
                    new("Alejandro Balde", MvpSquadPositionRole.LeftBack, 81, 87, 22),
                    new("Marc-André ter Stegen", MvpSquadPositionRole.Goalkeeper, 88, 88, 34),
                    new("Dani Olmo", MvpSquadPositionRole.AttackingMidfielder, 84, 86, 28),
                    new("Frenkie de Jong", MvpSquadPositionRole.CentralMidfielder, 86, 87, 29),
                    new("Ronald Araújo", MvpSquadPositionRole.CentreBack, 85, 88, 27),
                    new("Fermín López", MvpSquadPositionRole.AttackingMidfielder, 78, 86, 23),
                ],
                PrimaryColorHex: "#A50044",
                SecondaryColorHex: "#004D98",
                KitPattern: "Halves"),

            [112] = new(
                ClubId: 112,
                OfficialName: "ATLÉTICO DE MADRID",
                ShortCode: "ATM",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 85,
                TransferBudgetLimit: 80_000_000,
                Players:
                [
                    new("Antoine Griezmann", MvpSquadPositionRole.Striker, 88, 88, 35),
                    new("Julián Álvarez", MvpSquadPositionRole.Striker, 84, 89, 26),
                    new("Rodrigo De Paul", MvpSquadPositionRole.CentralMidfielder, 84, 84, 32),
                    new("Conor Gallagher", MvpSquadPositionRole.CentralMidfielder, 82, 85, 26),
                    new("Robin Le Normand", MvpSquadPositionRole.CentreBack, 83, 84, 29),
                    new("José María Giménez", MvpSquadPositionRole.CentreBack, 83, 83, 31),
                    new("Marcos Llorente", MvpSquadPositionRole.RightBack, 82, 82, 31),
                    new("Samuel Lino", MvpSquadPositionRole.LeftWinger, 80, 84, 26),
                    new("Pablo Barrios", MvpSquadPositionRole.CentralMidfielder, 78, 85, 23),
                    new("Jan Oblak", MvpSquadPositionRole.Goalkeeper, 88, 88, 33),
                    new("Alexander Sørloth", MvpSquadPositionRole.Striker, 82, 82, 30),
                    new("Koke", MvpSquadPositionRole.DefensiveMidfielder, 82, 82, 34),
                ],
                PrimaryColorHex: "#CB3524",
                SecondaryColorHex: "#272E61",
                KitPattern: "Stripes"),

            [201] = new(
                ClubId: 201,
                OfficialName: "ATHLETIC CLUB BILBAO",
                ShortCode: "ATH",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 82,
                TransferBudgetLimit: 48_000_000,
                Players:
                [
                    new("Nico Williams", MvpSquadPositionRole.LeftWinger, 85, 89, 24),
                    new("Iñaki Williams", MvpSquadPositionRole.RightWinger, 83, 83, 32),
                    new("Oihan Sancet", MvpSquadPositionRole.AttackingMidfielder, 82, 86, 26),
                    new("Dani Vivian", MvpSquadPositionRole.CentreBack, 83, 86, 27),
                    new("Unai Simón", MvpSquadPositionRole.Goalkeeper, 85, 86, 29),
                    new("Mikel Jauregizar", MvpSquadPositionRole.CentralMidfielder, 76, 84, 22),
                    new("Beñat Prados", MvpSquadPositionRole.DefensiveMidfielder, 77, 83, 25),
                    new("Aitor Paredes", MvpSquadPositionRole.CentreBack, 78, 83, 26),
                    new("Yuri Berchiche", MvpSquadPositionRole.LeftBack, 78, 78, 36),
                    new("Gorka Guruzeta", MvpSquadPositionRole.Striker, 79, 80, 29),
                ],
                PrimaryColorHex: "#EE2523",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [202] = new(
                ClubId: 202,
                OfficialName: "REAL SOCIEDAD",
                ShortCode: "RSO",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 81,
                TransferBudgetLimit: 45_000_000,
                Players:
                [
                    new("Martín Zubimendi", MvpSquadPositionRole.DefensiveMidfielder, 84, 87, 27),
                    new("Takefusa Kubo", MvpSquadPositionRole.RightWinger, 82, 86, 25),
                    new("Mikel Oyarzabal", MvpSquadPositionRole.Striker, 82, 83, 29),
                    new("Brais Méndez", MvpSquadPositionRole.AttackingMidfielder, 81, 82, 29),
                    new("Álex Remiro", MvpSquadPositionRole.Goalkeeper, 84, 84, 31),
                    new("Luka Sučić", MvpSquadPositionRole.CentralMidfielder, 77, 84, 23),
                    new("Igor Zubeldia", MvpSquadPositionRole.CentreBack, 80, 81, 29),
                    new("Nayef Aguerd", MvpSquadPositionRole.CentreBack, 79, 80, 30),
                    new("Jon Aramburu", MvpSquadPositionRole.RightBack, 76, 82, 24),
                    new("Sergio Gómez", MvpSquadPositionRole.LeftBack, 77, 82, 25),
                ],
                PrimaryColorHex: "#0067B1",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [203] = new(
                ClubId: 203,
                OfficialName: "VILLARREAL CF",
                ShortCode: "VIL",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 81,
                TransferBudgetLimit: 44_000_000,
                Players:
                [
                    new("Álex Baena", MvpSquadPositionRole.AttackingMidfielder, 83, 87, 25),
                    new("Ayoze Pérez", MvpSquadPositionRole.Striker, 81, 81, 33),
                    new("Yeremy Pino", MvpSquadPositionRole.RightWinger, 80, 85, 23),
                    new("Gerard Moreno", MvpSquadPositionRole.Striker, 82, 82, 34),
                    new("Dani Parejo", MvpSquadPositionRole.CentralMidfielder, 81, 81, 37),
                    new("Thierno Barry", MvpSquadPositionRole.Striker, 77, 84, 23),
                    new("Logan Costa", MvpSquadPositionRole.CentreBack, 78, 83, 25),
                    new("Sergi Cardona", MvpSquadPositionRole.LeftBack, 78, 81, 27),
                    new("Pape Gueye", MvpSquadPositionRole.DefensiveMidfielder, 78, 80, 27),
                    new("Diego Conde", MvpSquadPositionRole.Goalkeeper, 79, 82, 27),
                ],
                PrimaryColorHex: "#FFE667",
                SecondaryColorHex: "#005187",
                KitPattern: "Solid"),

            [204] = new(
                ClubId: 204,
                OfficialName: "REAL BETIS BALOMPIÉ",
                ShortCode: "BET",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 80,
                TransferBudgetLimit: 40_000_000,
                Players:
                [
                    new("Isco", MvpSquadPositionRole.AttackingMidfielder, 83, 83, 34),
                    new("Giovani Lo Celso", MvpSquadPositionRole.AttackingMidfielder, 82, 82, 30),
                    new("Johnny Cardoso", MvpSquadPositionRole.DefensiveMidfielder, 79, 85, 24),
                    new("Abde Ezzalzouli", MvpSquadPositionRole.LeftWinger, 78, 83, 24),
                    new("Vitor Roque", MvpSquadPositionRole.Striker, 77, 85, 21),
                    new("Diego Llorente", MvpSquadPositionRole.CentreBack, 79, 79, 33),
                    new("Marc Bartra", MvpSquadPositionRole.CentreBack, 78, 78, 35),
                    new("Pablo Fornals", MvpSquadPositionRole.RightWinger, 79, 79, 30),
                    new("Rui Silva", MvpSquadPositionRole.Goalkeeper, 80, 80, 32),
                ],
                PrimaryColorHex: "#0BB363",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [205] = new(
                ClubId: 205,
                OfficialName: "GIRONA FC",
                ShortCode: "GIR",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 79,
                TransferBudgetLimit: 38_000_000,
                Players:
                [
                    new("Miguel Gutiérrez", MvpSquadPositionRole.LeftBack, 82, 86, 25),
                    new("Viktor Tsygankov", MvpSquadPositionRole.RightWinger, 81, 83, 28),
                    new("Yangel Herrera", MvpSquadPositionRole.CentralMidfielder, 80, 82, 28),
                    new("Bryan Gil", MvpSquadPositionRole.LeftWinger, 78, 82, 25),
                    new("Iván Martín", MvpSquadPositionRole.AttackingMidfielder, 79, 81, 27),
                    new("Ladislav Krejčí", MvpSquadPositionRole.CentreBack, 78, 82, 27),
                    new("Daley Blind", MvpSquadPositionRole.CentreBack, 79, 79, 36),
                    new("Abel Ruiz", MvpSquadPositionRole.Striker, 77, 81, 26),
                    new("Paulo Gazzaniga", MvpSquadPositionRole.Goalkeeper, 79, 79, 34),
                ],
                PrimaryColorHex: "#CE1126",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [206] = new(
                ClubId: 206,
                OfficialName: "SEVILLA FC",
                ShortCode: "SEV",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 78,
                TransferBudgetLimit: 35_000_000,
                Players:
                [
                    new("Dodi Lukébakio", MvpSquadPositionRole.RightWinger, 80, 81, 28),
                    new("Loïc Badé", MvpSquadPositionRole.CentreBack, 80, 84, 26),
                    new("Saúl Ñíguez", MvpSquadPositionRole.CentralMidfielder, 78, 78, 31),
                    new("Juanlu Sánchez", MvpSquadPositionRole.RightBack, 76, 83, 23),
                    new("Isaac Romero", MvpSquadPositionRole.Striker, 76, 80, 26),
                    new("Adrià Pedrosa", MvpSquadPositionRole.LeftBack, 77, 79, 28),
                    new("Djibril Sow", MvpSquadPositionRole.CentralMidfielder, 77, 78, 29),
                    new("Ørjan Nyland", MvpSquadPositionRole.Goalkeeper, 77, 77, 35),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#D4001F",
                KitPattern: "Solid"),

            [207] = new(
                ClubId: 207,
                OfficialName: "VALENCIA CF",
                ShortCode: "VAL",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 77,
                TransferBudgetLimit: 30_000_000,
                Players:
                [
                    new("Giorgi Mamardashvili", MvpSquadPositionRole.Goalkeeper, 85, 89, 25),
                    new("Pepelu", MvpSquadPositionRole.DefensiveMidfielder, 80, 83, 28),
                    new("Hugo Duro", MvpSquadPositionRole.Striker, 79, 81, 26),
                    new("Javi Guerra", MvpSquadPositionRole.CentralMidfielder, 78, 85, 23),
                    new("Cristhian Mosquera", MvpSquadPositionRole.CentreBack, 78, 86, 22),
                    new("José Gayà", MvpSquadPositionRole.LeftBack, 81, 81, 31),
                    new("Diego López", MvpSquadPositionRole.RightWinger, 77, 82, 24),
                    new("Luis Rioja", MvpSquadPositionRole.LeftWinger, 76, 76, 32),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#111111",
                KitPattern: "Solid"),

            [208] = new(
                ClubId: 208,
                OfficialName: "RC CELTA DE VIGO",
                ShortCode: "CEL",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 77,
                TransferBudgetLimit: 28_000_000,
                Players:
                [
                    new("Iago Aspas", MvpSquadPositionRole.Striker, 82, 82, 39),
                    new("Óscar Mingueza", MvpSquadPositionRole.RightBack, 79, 82, 27),
                    new("Williot Swedberg", MvpSquadPositionRole.LeftWinger, 76, 84, 22),
                    new("Hugo Álvarez", MvpSquadPositionRole.LeftMidfielder, 75, 83, 23),
                    new("Fran Beltrán", MvpSquadPositionRole.CentralMidfielder, 77, 79, 27),
                    new("Carl Starfelt", MvpSquadPositionRole.CentreBack, 77, 78, 31),
                    new("Marcos Alonso", MvpSquadPositionRole.CentreBack, 77, 77, 35),
                    new("Vicente Guaita", MvpSquadPositionRole.Goalkeeper, 77, 77, 39),
                ],
                PrimaryColorHex: "#8AC3EE",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [209] = new(
                ClubId: 209,
                OfficialName: "CA OSASUNA",
                ShortCode: "OSA",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 77,
                TransferBudgetLimit: 26_000_000,
                Players:
                [
                    new("Ante Budimir", MvpSquadPositionRole.Striker, 81, 81, 35),
                    new("Bryan Zaragoza", MvpSquadPositionRole.LeftWinger, 79, 84, 25),
                    new("Aimar Oroz", MvpSquadPositionRole.AttackingMidfielder, 79, 84, 24),
                    new("Enzo Boyomo", MvpSquadPositionRole.CentreBack, 77, 83, 24),
                    new("Lucas Torró", MvpSquadPositionRole.DefensiveMidfielder, 77, 77, 32),
                    new("Jon Moncayola", MvpSquadPositionRole.CentralMidfielder, 78, 80, 28),
                    new("Jesús Areso", MvpSquadPositionRole.RightBack, 77, 80, 27),
                    new("Sergio Herrera", MvpSquadPositionRole.Goalkeeper, 79, 79, 33),
                ],
                PrimaryColorHex: "#D91A21",
                SecondaryColorHex: "#0A1E40",
                KitPattern: "Solid"),

            [210] = new(
                ClubId: 210,
                OfficialName: "RCD MALLORCA",
                ShortCode: "MLL",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 77,
                TransferBudgetLimit: 25_000_000,
                Players:
                [
                    new("Samu Costa", MvpSquadPositionRole.DefensiveMidfielder, 79, 83, 25),
                    new("Sergi Darder", MvpSquadPositionRole.CentralMidfielder, 79, 79, 32),
                    new("Vedat Muriqi", MvpSquadPositionRole.Striker, 80, 80, 32),
                    new("Cyle Larin", MvpSquadPositionRole.Striker, 76, 76, 31),
                    new("Antonio Raíllo", MvpSquadPositionRole.CentreBack, 79, 79, 34),
                    new("Martin Valjent", MvpSquadPositionRole.CentreBack, 77, 78, 30),
                    new("Johan Mojica", MvpSquadPositionRole.LeftBack, 77, 77, 34),
                    new("Dominik Greif", MvpSquadPositionRole.Goalkeeper, 77, 79, 29),
                ],
                PrimaryColorHex: "#E20613",
                SecondaryColorHex: "#111111",
                KitPattern: "Solid"),

            [211] = new(
                ClubId: 211,
                OfficialName: "RAYO VALLECANO",
                ShortCode: "RAY",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 76,
                TransferBudgetLimit: 22_000_000,
                Players:
                [
                    new("Isi Palazón", MvpSquadPositionRole.RightWinger, 79, 79, 31),
                    new("Andrei Rațiu", MvpSquadPositionRole.RightBack, 78, 80, 28),
                    new("Álvaro García", MvpSquadPositionRole.LeftWinger, 78, 78, 33),
                    new("Abdul Mumin", MvpSquadPositionRole.CentreBack, 77, 81, 28),
                    new("Florian Lejeune", MvpSquadPositionRole.CentreBack, 78, 78, 35),
                    new("Óscar Valentín", MvpSquadPositionRole.DefensiveMidfielder, 76, 76, 32),
                    new("Jorge de Frutos", MvpSquadPositionRole.RightWinger, 76, 77, 29),
                    new("Augusto Batalla", MvpSquadPositionRole.Goalkeeper, 77, 78, 30),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#E53027",
                KitPattern: "Sash"),

            [212] = new(
                ClubId: 212,
                OfficialName: "GETAFE CF",
                ShortCode: "GET",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 76,
                TransferBudgetLimit: 24_000_000,
                Players:
                [
                    new("Mauro Arambarri", MvpSquadPositionRole.CentralMidfielder, 79, 79, 30),
                    new("Luis Milla", MvpSquadPositionRole.CentralMidfielder, 79, 79, 31),
                    new("Borja Mayoral", MvpSquadPositionRole.Striker, 79, 80, 29),
                    new("Christantus Uche", MvpSquadPositionRole.AttackingMidfielder, 75, 82, 23),
                    new("Djené Dakonam", MvpSquadPositionRole.CentreBack, 78, 78, 34),
                    new("Omar Alderete", MvpSquadPositionRole.CentreBack, 77, 78, 29),
                    new("Diego Rico", MvpSquadPositionRole.LeftBack, 76, 76, 33),
                    new("David Soria", MvpSquadPositionRole.Goalkeeper, 79, 79, 33),
                ],
                PrimaryColorHex: "#005999",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [213] = new(
                ClubId: 213,
                OfficialName: "DEPORTIVO ALAVÉS",
                ShortCode: "ALA",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 75,
                TransferBudgetLimit: 20_000_000,
                Players:
                [
                    new("Carlos Vicente", MvpSquadPositionRole.RightWinger, 77, 80, 27),
                    new("Antonio Blanco", MvpSquadPositionRole.DefensiveMidfielder, 76, 80, 26),
                    new("Jon Guridi", MvpSquadPositionRole.AttackingMidfielder, 76, 77, 31),
                    new("Abdel Abqar", MvpSquadPositionRole.CentreBack, 75, 79, 27),
                    new("Nahuel Tenaglia", MvpSquadPositionRole.RightBack, 75, 77, 30),
                    new("Kike García", MvpSquadPositionRole.Striker, 75, 75, 36),
                    new("Manu Sánchez", MvpSquadPositionRole.LeftBack, 75, 78, 26),
                    new("Antonio Sivera", MvpSquadPositionRole.Goalkeeper, 77, 78, 30),
                ],
                PrimaryColorHex: "#0057A3",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [214] = new(
                ClubId: 214,
                OfficialName: "RCD ESPANYOL",
                ShortCode: "ESP",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 75,
                TransferBudgetLimit: 20_000_000,
                Players:
                [
                    new("Joan García", MvpSquadPositionRole.Goalkeeper, 79, 86, 25),
                    new("Javi Puado", MvpSquadPositionRole.LeftWinger, 77, 79, 28),
                    new("Omar El Hilali", MvpSquadPositionRole.RightBack, 75, 82, 23),
                    new("Marash Kumbulla", MvpSquadPositionRole.CentreBack, 76, 79, 26),
                    new("Alex Král", MvpSquadPositionRole.CentralMidfielder, 76, 78, 28),
                    new("Carlos Romero", MvpSquadPositionRole.LeftBack, 74, 80, 24),
                    new("Leandro Cabrera", MvpSquadPositionRole.CentreBack, 76, 76, 35),
                    new("Pol Lozano", MvpSquadPositionRole.DefensiveMidfielder, 74, 77, 26),
                ],
                PrimaryColorHex: "#007FC8",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [215] = new(
                ClubId: 215,
                OfficialName: "UD LAS PALMAS",
                ShortCode: "LPA",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 75,
                TransferBudgetLimit: 21_000_000,
                Players:
                [
                    new("Alberto Moleiro", MvpSquadPositionRole.AttackingMidfielder, 79, 87, 22),
                    new("Kirian Rodríguez", MvpSquadPositionRole.CentralMidfielder, 79, 80, 30),
                    new("Fábio Silva", MvpSquadPositionRole.Striker, 76, 82, 24),
                    new("Dário Essugo", MvpSquadPositionRole.DefensiveMidfielder, 75, 85, 21),
                    new("Mika Mármol", MvpSquadPositionRole.CentreBack, 77, 82, 25),
                    new("Sandro Ramírez", MvpSquadPositionRole.RightWinger, 76, 76, 31),
                    new("Scott McKenna", MvpSquadPositionRole.CentreBack, 75, 76, 29),
                    new("Jasper Cillessen", MvpSquadPositionRole.Goalkeeper, 77, 77, 37),
                ],
                PrimaryColorHex: "#FFE600",
                SecondaryColorHex: "#0055A5",
                KitPattern: "Solid"),

            [216] = new(
                ClubId: 216,
                OfficialName: "CD LEGANÉS",
                ShortCode: "LEG",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 74,
                TransferBudgetLimit: 18_000_000,
                Players:
                [
                    new("Juan Cruz", MvpSquadPositionRole.RightWinger, 76, 79, 26),
                    new("Renato Tapia", MvpSquadPositionRole.DefensiveMidfielder, 77, 77, 31),
                    new("Dani Raba", MvpSquadPositionRole.AttackingMidfielder, 75, 75, 30),
                    new("Valentin Rosier", MvpSquadPositionRole.RightBack, 76, 77, 30),
                    new("Sergio González", MvpSquadPositionRole.CentreBack, 75, 75, 34),
                    new("Seydouba Cissé", MvpSquadPositionRole.CentralMidfielder, 73, 78, 25),
                    new("Miguel de la Fuente", MvpSquadPositionRole.Striker, 74, 77, 27),
                    new("Marko Dmitrović", MvpSquadPositionRole.Goalkeeper, 77, 77, 34),
                ],
                PrimaryColorHex: "#0055A4",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [217] = new(
                ClubId: 217,
                OfficialName: "REAL VALLADOLID CF",
                ShortCode: "VLL",
                Country: "İspanya",
                LeagueName: "La Liga",
                SportiveStrength: 73,
                TransferBudgetLimit: 16_000_000,
                Players:
                [
                    new("Raúl Moro", MvpSquadPositionRole.LeftWinger, 76, 83, 23),
                    new("Mario Martín", MvpSquadPositionRole.DefensiveMidfielder, 73, 81, 22),
                    new("Karl Hein", MvpSquadPositionRole.Goalkeeper, 75, 82, 24),
                    new("Juma Bah", MvpSquadPositionRole.CentreBack, 74, 83, 20),
                    new("Lucas Rosa", MvpSquadPositionRole.LeftBack, 74, 78, 26),
                    new("Selim Amallah", MvpSquadPositionRole.AttackingMidfielder, 75, 76, 29),
                    new("Eray Cömert", MvpSquadPositionRole.CentreBack, 75, 76, 28),
                    new("Mamadou Sylla", MvpSquadPositionRole.Striker, 73, 73, 32),
                ],
                PrimaryColorHex: "#5B2B82",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            // =========================================================================
            // İNGİLTERE — PREMIER LEAGUE (1. LİG — 20 KULÜP)
            // =========================================================================
            [102] = new(
                ClubId: 102,
                OfficialName: "MANCHESTER CITY FC",
                ShortCode: "MCI",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 90,
                TransferBudgetLimit: 130_000_000,
                Players:
                [
                    new("Erling Haaland", MvpSquadPositionRole.Striker, 91, 94, 26),
                    new("Rodri", MvpSquadPositionRole.DefensiveMidfielder, 91, 92, 30),
                    new("Kevin De Bruyne", MvpSquadPositionRole.AttackingMidfielder, 90, 90, 35),
                    new("Phil Foden", MvpSquadPositionRole.RightWinger, 88, 91, 26),
                    new("Bernardo Silva", MvpSquadPositionRole.CentralMidfielder, 88, 88, 32),
                    new("Rúben Dias", MvpSquadPositionRole.CentreBack, 88, 89, 29),
                    new("Joško Gvardiol", MvpSquadPositionRole.LeftBack, 83, 88, 24),
                    new("Jérémy Doku", MvpSquadPositionRole.LeftWinger, 81, 87, 24),
                    new("Savinho", MvpSquadPositionRole.RightWinger, 82, 88, 22),
                    new("Ederson", MvpSquadPositionRole.Goalkeeper, 88, 88, 33),
                    new("Omar Marmoush", MvpSquadPositionRole.Striker, 84, 87, 27),
                    new("Manuel Akanji", MvpSquadPositionRole.CentreBack, 84, 84, 31),
                    new("Rico Lewis", MvpSquadPositionRole.RightBack, 79, 86, 21),
                ],
                PrimaryColorHex: "#6CABDD",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [104] = new(
                ClubId: 104,
                OfficialName: "ARSENAL FC",
                ShortCode: "ARS",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 87,
                TransferBudgetLimit: 85_000_000,
                Players:
                [
                    new("Bukayo Saka", MvpSquadPositionRole.RightWinger, 87, 90, 25),
                    new("Martin Ødegaard", MvpSquadPositionRole.AttackingMidfielder, 89, 90, 27),
                    new("William Saliba", MvpSquadPositionRole.CentreBack, 87, 91, 25),
                    new("Declan Rice", MvpSquadPositionRole.DefensiveMidfielder, 87, 89, 27),
                    new("Gabriel Magalhães", MvpSquadPositionRole.CentreBack, 86, 88, 28),
                    new("Gabriel Martinelli", MvpSquadPositionRole.LeftWinger, 83, 87, 25),
                    new("Kai Havertz", MvpSquadPositionRole.Striker, 83, 86, 27),
                    new("Jurriën Timber", MvpSquadPositionRole.RightBack, 81, 86, 25),
                    new("Ethan Nwaneri", MvpSquadPositionRole.AttackingMidfielder, 75, 88, 19),
                    new("David Raya", MvpSquadPositionRole.Goalkeeper, 84, 85, 31),
                    new("Mikel Merino", MvpSquadPositionRole.CentralMidfielder, 84, 84, 30),
                    new("Riccardo Calafiori", MvpSquadPositionRole.LeftBack, 80, 86, 24),
                ],
                PrimaryColorHex: "#EF0107",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Sleeves"),

            [106] = new(
                ClubId: 106,
                OfficialName: "LIVERPOOL FC",
                ShortCode: "LIV",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 88,
                TransferBudgetLimit: 90_000_000,
                Players:
                [
                    new("Mohamed Salah", MvpSquadPositionRole.RightWinger, 89, 89, 34),
                    new("Virgil van Dijk", MvpSquadPositionRole.CentreBack, 89, 89, 35),
                    new("Alexis Mac Allister", MvpSquadPositionRole.CentralMidfielder, 86, 88, 27),
                    new("Trent Alexander-Arnold", MvpSquadPositionRole.RightBack, 86, 88, 27),
                    new("Dominik Szoboszlai", MvpSquadPositionRole.CentralMidfielder, 82, 87, 25),
                    new("Luis Díaz", MvpSquadPositionRole.LeftWinger, 84, 85, 29),
                    new("Ryan Gravenberch", MvpSquadPositionRole.DefensiveMidfielder, 81, 86, 24),
                    new("Darwin Núñez", MvpSquadPositionRole.Striker, 82, 85, 27),
                    new("Ibrahima Konaté", MvpSquadPositionRole.CentreBack, 83, 87, 27),
                    new("Alisson Becker", MvpSquadPositionRole.Goalkeeper, 89, 89, 33),
                    new("Cody Gakpo", MvpSquadPositionRole.LeftWinger, 83, 85, 27),
                    new("Andrew Robertson", MvpSquadPositionRole.LeftBack, 84, 84, 32),
                ],
                PrimaryColorHex: "#C8102E",
                SecondaryColorHex: "#F6EB61",
                KitPattern: "Solid"),

            [114] = new(
                ClubId: 114,
                OfficialName: "CHELSEA FC",
                ShortCode: "CHE",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 84,
                TransferBudgetLimit: 100_000_000,
                Players:
                [
                    new("Cole Palmer", MvpSquadPositionRole.AttackingMidfielder, 85, 90, 24),
                    new("Moisés Caicedo", MvpSquadPositionRole.DefensiveMidfielder, 82, 87, 24),
                    new("Enzo Fernández", MvpSquadPositionRole.CentralMidfielder, 82, 87, 25),
                    new("Nicolas Jackson", MvpSquadPositionRole.Striker, 80, 85, 25),
                    new("Christopher Nkunku", MvpSquadPositionRole.Striker, 84, 85, 28),
                    new("Pedro Neto", MvpSquadPositionRole.LeftWinger, 80, 84, 26),
                    new("Levi Colwill", MvpSquadPositionRole.CentreBack, 80, 86, 23),
                    new("Malo Gusto", MvpSquadPositionRole.RightBack, 79, 85, 23),
                    new("Roméo Lavia", MvpSquadPositionRole.DefensiveMidfielder, 76, 84, 22),
                    new("Robert Sánchez", MvpSquadPositionRole.Goalkeeper, 79, 82, 28),
                    new("Noni Madueke", MvpSquadPositionRole.RightWinger, 79, 84, 24),
                    new("Marc Cucurella", MvpSquadPositionRole.LeftBack, 81, 82, 28),
                ],
                PrimaryColorHex: "#034694",
                SecondaryColorHex: "#DBA111",
                KitPattern: "Solid"),

            [118] = new(
                ClubId: 118,
                OfficialName: "ASTON VILLA FC",
                ShortCode: "AVL",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 82,
                TransferBudgetLimit: 60_000_000,
                Players:
                [
                    new("Ollie Watkins", MvpSquadPositionRole.Striker, 85, 85, 30),
                    new("Emiliano Martínez", MvpSquadPositionRole.Goalkeeper, 87, 87, 34),
                    new("Morgan Rogers", MvpSquadPositionRole.AttackingMidfielder, 78, 85, 24),
                    new("Amadou Onana", MvpSquadPositionRole.DefensiveMidfielder, 81, 86, 25),
                    new("Youri Tielemans", MvpSquadPositionRole.CentralMidfielder, 81, 82, 29),
                    new("Leon Bailey", MvpSquadPositionRole.RightWinger, 82, 82, 29),
                    new("Ezri Konsa", MvpSquadPositionRole.CentreBack, 81, 83, 28),
                    new("Pau Torres", MvpSquadPositionRole.CentreBack, 82, 83, 29),
                    new("Jhon Durán", MvpSquadPositionRole.Striker, 78, 86, 22),
                    new("Lucas Digne", MvpSquadPositionRole.LeftBack, 80, 80, 33),
                ],
                PrimaryColorHex: "#670E36",
                SecondaryColorHex: "#95BFE5",
                KitPattern: "Sleeves"),

            [119] = new(
                ClubId: 119,
                OfficialName: "MANCHESTER UNITED FC",
                ShortCode: "MUN",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 83,
                TransferBudgetLimit: 95_000_000,
                Players:
                [
                    new("Bruno Fernandes", MvpSquadPositionRole.AttackingMidfielder, 87, 87, 31),
                    new("Kobbie Mainoo", MvpSquadPositionRole.CentralMidfielder, 80, 88, 21),
                    new("Amad Diallo", MvpSquadPositionRole.RightWinger, 80, 86, 24),
                    new("Alejandro Garnacho", MvpSquadPositionRole.LeftWinger, 80, 87, 22),
                    new("Rasmus Højlund", MvpSquadPositionRole.Striker, 79, 86, 23),
                    new("Lisandro Martínez", MvpSquadPositionRole.CentreBack, 84, 85, 28),
                    new("Matthijs de Ligt", MvpSquadPositionRole.CentreBack, 83, 86, 27),
                    new("Leny Yoro", MvpSquadPositionRole.CentreBack, 78, 88, 20),
                    new("Noussair Mazraoui", MvpSquadPositionRole.RightBack, 81, 82, 28),
                    new("Manuel Ugarte", MvpSquadPositionRole.DefensiveMidfielder, 82, 85, 25),
                    new("André Onana", MvpSquadPositionRole.Goalkeeper, 83, 84, 30),
                ],
                PrimaryColorHex: "#DA291C",
                SecondaryColorHex: "#FBE122",
                KitPattern: "Solid"),

            [120] = new(
                ClubId: 120,
                OfficialName: "TOTTENHAM HOTSPUR FC",
                ShortCode: "TOT",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 83,
                TransferBudgetLimit: 78_000_000,
                Players:
                [
                    new("Son Heung-min", MvpSquadPositionRole.LeftWinger, 86, 86, 34),
                    new("James Maddison", MvpSquadPositionRole.AttackingMidfielder, 84, 85, 29),
                    new("Dejan Kulusevski", MvpSquadPositionRole.RightWinger, 83, 86, 26),
                    new("Dominic Solanke", MvpSquadPositionRole.Striker, 81, 83, 28),
                    new("Cristian Romero", MvpSquadPositionRole.CentreBack, 85, 87, 28),
                    new("Micky van de Ven", MvpSquadPositionRole.CentreBack, 83, 87, 25),
                    new("Pedro Porro", MvpSquadPositionRole.RightBack, 83, 85, 27),
                    new("Destiny Udogie", MvpSquadPositionRole.LeftBack, 81, 86, 23),
                    new("Lucas Bergvall", MvpSquadPositionRole.CentralMidfielder, 75, 87, 20),
                    new("Archie Gray", MvpSquadPositionRole.DefensiveMidfielder, 75, 86, 20),
                    new("Guglielmo Vicario", MvpSquadPositionRole.Goalkeeper, 84, 85, 29),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#132257",
                KitPattern: "Solid"),

            [121] = new(
                ClubId: 121,
                OfficialName: "NEWCASTLE UNITED FC",
                ShortCode: "NEW",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 83,
                TransferBudgetLimit: 80_000_000,
                Players:
                [
                    new("Alexander Isak", MvpSquadPositionRole.Striker, 86, 89, 26),
                    new("Bruno Guimarães", MvpSquadPositionRole.DefensiveMidfielder, 86, 87, 28),
                    new("Anthony Gordon", MvpSquadPositionRole.LeftWinger, 83, 86, 25),
                    new("Sandro Tonali", MvpSquadPositionRole.CentralMidfielder, 84, 86, 26),
                    new("Joelinton", MvpSquadPositionRole.CentralMidfielder, 82, 82, 30),
                    new("Sven Botman", MvpSquadPositionRole.CentreBack, 83, 86, 26),
                    new("Fabian Schär", MvpSquadPositionRole.CentreBack, 82, 82, 34),
                    new("Tino Livramento", MvpSquadPositionRole.RightBack, 79, 85, 23),
                    new("Lewis Hall", MvpSquadPositionRole.LeftBack, 78, 85, 21),
                    new("Nick Pope", MvpSquadPositionRole.Goalkeeper, 83, 83, 34),
                ],
                PrimaryColorHex: "#241F20",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [122] = new(
                ClubId: 122,
                OfficialName: "BRIGHTON & HOVE ALBION",
                ShortCode: "BHA",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 80,
                TransferBudgetLimit: 55_000_000,
                Players:
                [
                    new("Kaoru Mitoma", MvpSquadPositionRole.LeftWinger, 82, 83, 29),
                    new("João Pedro", MvpSquadPositionRole.Striker, 81, 86, 24),
                    new("Carlos Baleba", MvpSquadPositionRole.DefensiveMidfielder, 79, 87, 22),
                    new("Ferdi Kadıoğlu", MvpSquadPositionRole.LeftBack, 80, 84, 26),
                    new("Yankuba Minteh", MvpSquadPositionRole.RightWinger, 77, 85, 22),
                    new("Georginio Rutter", MvpSquadPositionRole.AttackingMidfielder, 78, 84, 24),
                    new("Jan Paul van Hecke", MvpSquadPositionRole.CentreBack, 79, 83, 26),
                    new("Pervis Estupiñán", MvpSquadPositionRole.LeftBack, 80, 81, 28),
                    new("Bart Verbruggen", MvpSquadPositionRole.Goalkeeper, 79, 85, 24),
                ],
                PrimaryColorHex: "#0057B8",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [123] = new(
                ClubId: 123,
                OfficialName: "WEST HAM UNITED FC",
                ShortCode: "WHU",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 79,
                TransferBudgetLimit: 50_000_000,
                Players:
                [
                    new("Jarrod Bowen", MvpSquadPositionRole.RightWinger, 83, 84, 29),
                    new("Mohammed Kudus", MvpSquadPositionRole.AttackingMidfielder, 83, 86, 26),
                    new("Lucas Paquetá", MvpSquadPositionRole.CentralMidfielder, 83, 84, 29),
                    new("Crysencio Summerville", MvpSquadPositionRole.LeftWinger, 78, 83, 24),
                    new("Tomáš Souček", MvpSquadPositionRole.DefensiveMidfielder, 79, 79, 31),
                    new("Max Kilman", MvpSquadPositionRole.CentreBack, 79, 81, 29),
                    new("Jean-Clair Todibo", MvpSquadPositionRole.CentreBack, 80, 83, 26),
                    new("Aaron Wan-Bissaka", MvpSquadPositionRole.RightBack, 79, 80, 28),
                    new("Alphonse Areola", MvpSquadPositionRole.Goalkeeper, 80, 80, 33),
                ],
                PrimaryColorHex: "#7A263A",
                SecondaryColorHex: "#1BB1E7",
                KitPattern: "Sleeves"),

            [124] = new(
                ClubId: 124,
                OfficialName: "CRYSTAL PALACE FC",
                ShortCode: "CRY",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 79,
                TransferBudgetLimit: 45_000_000,
                Players:
                [
                    new("Eberechi Eze", MvpSquadPositionRole.AttackingMidfielder, 83, 85, 28),
                    new("Adam Wharton", MvpSquadPositionRole.DefensiveMidfielder, 79, 87, 22),
                    new("Marc Guéhi", MvpSquadPositionRole.CentreBack, 82, 85, 26),
                    new("Jean-Philippe Mateta", MvpSquadPositionRole.Striker, 80, 81, 29),
                    new("Daniel Muñoz", MvpSquadPositionRole.RightBack, 80, 80, 30),
                    new("Ismaïla Sarr", MvpSquadPositionRole.RightWinger, 78, 80, 28),
                    new("Maxence Lacroix", MvpSquadPositionRole.CentreBack, 79, 82, 26),
                    new("Tyrick Mitchell", MvpSquadPositionRole.LeftBack, 78, 81, 27),
                    new("Dean Henderson", MvpSquadPositionRole.Goalkeeper, 79, 81, 29),
                ],
                PrimaryColorHex: "#1B458F",
                SecondaryColorHex: "#C4122E",
                KitPattern: "Stripes"),

            [125] = new(
                ClubId: 125,
                OfficialName: "FULHAM FC",
                ShortCode: "FUL",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 78,
                TransferBudgetLimit: 42_000_000,
                Players:
                [
                    new("Antonee Robinson", MvpSquadPositionRole.LeftBack, 82, 83, 29),
                    new("Emile Smith Rowe", MvpSquadPositionRole.AttackingMidfielder, 79, 83, 26),
                    new("Alex Iwobi", MvpSquadPositionRole.LeftWinger, 79, 80, 30),
                    new("Andreas Pereira", MvpSquadPositionRole.AttackingMidfielder, 79, 79, 30),
                    new("Sander Berge", MvpSquadPositionRole.DefensiveMidfielder, 78, 80, 28),
                    new("Joachim Andersen", MvpSquadPositionRole.CentreBack, 80, 80, 30),
                    new("Calvin Bassey", MvpSquadPositionRole.CentreBack, 78, 81, 26),
                    new("Rodrigo Muniz", MvpSquadPositionRole.Striker, 77, 81, 25),
                    new("Bernd Leno", MvpSquadPositionRole.Goalkeeper, 81, 81, 34),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#111111",
                KitPattern: "Halves"),

            [126] = new(
                ClubId: 126,
                OfficialName: "AFC BOURNEMOUTH",
                ShortCode: "BOU",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 79,
                TransferBudgetLimit: 44_000_000,
                Players:
                [
                    new("Antoine Semenyo", MvpSquadPositionRole.RightWinger, 80, 83, 26),
                    new("Milos Kerkez", MvpSquadPositionRole.LeftBack, 80, 86, 22),
                    new("Illia Zabarnyi", MvpSquadPositionRole.CentreBack, 80, 86, 24),
                    new("Dean Huijsen", MvpSquadPositionRole.CentreBack, 77, 87, 21),
                    new("Justin Kluivert", MvpSquadPositionRole.AttackingMidfielder, 79, 82, 27),
                    new("Evanilson", MvpSquadPositionRole.Striker, 79, 82, 26),
                    new("Ryan Christie", MvpSquadPositionRole.CentralMidfielder, 78, 78, 31),
                    new("Lewis Cook", MvpSquadPositionRole.DefensiveMidfielder, 78, 79, 29),
                    new("Kepa Arrizabalaga", MvpSquadPositionRole.Goalkeeper, 79, 79, 31),
                ],
                PrimaryColorHex: "#DA291C",
                SecondaryColorHex: "#111111",
                KitPattern: "Stripes"),

            [127] = new(
                ClubId: 127,
                OfficialName: "BRENTFORD FC",
                ShortCode: "BRE",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 78,
                TransferBudgetLimit: 40_000_000,
                Players:
                [
                    new("Bryan Mbeumo", MvpSquadPositionRole.RightWinger, 83, 85, 27),
                    new("Yoane Wissa", MvpSquadPositionRole.Striker, 80, 80, 30),
                    new("Mikkel Damsgaard", MvpSquadPositionRole.AttackingMidfielder, 79, 82, 26),
                    new("Nathan Collins", MvpSquadPositionRole.CentreBack, 79, 83, 25),
                    new("Ethan Pinnock", MvpSquadPositionRole.CentreBack, 78, 78, 33),
                    new("Christian Nørgaard", MvpSquadPositionRole.DefensiveMidfielder, 79, 79, 32),
                    new("Vitaly Janelt", MvpSquadPositionRole.CentralMidfielder, 78, 80, 28),
                    new("Kevin Schade", MvpSquadPositionRole.LeftWinger, 76, 82, 24),
                    new("Mark Flekken", MvpSquadPositionRole.Goalkeeper, 78, 78, 33),
                ],
                PrimaryColorHex: "#E30613",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [128] = new(
                ClubId: 128,
                OfficialName: "NOTTINGHAM FOREST FC",
                ShortCode: "NFO",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 80,
                TransferBudgetLimit: 48_000_000,
                Players:
                [
                    new("Morgan Gibbs-White", MvpSquadPositionRole.AttackingMidfielder, 82, 85, 26),
                    new("Murillo", MvpSquadPositionRole.CentreBack, 81, 87, 24),
                    new("Nikola Milenković", MvpSquadPositionRole.CentreBack, 81, 82, 28),
                    new("Callum Hudson-Odoi", MvpSquadPositionRole.LeftWinger, 79, 82, 25),
                    new("Anthony Elanga", MvpSquadPositionRole.RightWinger, 79, 83, 24),
                    new("Chris Wood", MvpSquadPositionRole.Striker, 80, 80, 34),
                    new("Elliot Anderson", MvpSquadPositionRole.CentralMidfielder, 77, 84, 23),
                    new("Ola Aina", MvpSquadPositionRole.RightBack, 79, 79, 29),
                    new("Matz Sels", MvpSquadPositionRole.Goalkeeper, 80, 80, 34),
                ],
                PrimaryColorHex: "#DD0000",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [129] = new(
                ClubId: 129,
                OfficialName: "EVERTON FC",
                ShortCode: "EVE",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 77,
                TransferBudgetLimit: 35_000_000,
                Players:
                [
                    new("Jarrad Branthwaite", MvpSquadPositionRole.CentreBack, 81, 87, 24),
                    new("Jordan Pickford", MvpSquadPositionRole.Goalkeeper, 83, 83, 32),
                    new("Iliman Ndiaye", MvpSquadPositionRole.LeftWinger, 79, 82, 26),
                    new("Dwight McNeil", MvpSquadPositionRole.AttackingMidfielder, 78, 81, 26),
                    new("James Tarkowski", MvpSquadPositionRole.CentreBack, 79, 79, 33),
                    new("Idrissa Gueye", MvpSquadPositionRole.DefensiveMidfielder, 78, 78, 36),
                    new("Dominic Calvert-Lewin", MvpSquadPositionRole.Striker, 77, 78, 29),
                    new("Vitalii Mykolenko", MvpSquadPositionRole.LeftBack, 77, 79, 27),
                ],
                PrimaryColorHex: "#003399",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [130] = new(
                ClubId: 130,
                OfficialName: "WOLVERHAMPTON WANDERERS",
                ShortCode: "WOL",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 77,
                TransferBudgetLimit: 38_000_000,
                Players:
                [
                    new("Matheus Cunha", MvpSquadPositionRole.Striker, 83, 85, 27),
                    new("João Gomes", MvpSquadPositionRole.DefensiveMidfielder, 80, 85, 25),
                    new("André", MvpSquadPositionRole.DefensiveMidfielder, 79, 84, 25),
                    new("Rayan Aït-Nouri", MvpSquadPositionRole.LeftBack, 80, 84, 25),
                    new("Jørgen Strand Larsen", MvpSquadPositionRole.Striker, 78, 82, 26),
                    new("Toti Gomes", MvpSquadPositionRole.CentreBack, 77, 80, 27),
                    new("Rodrigo Gomes", MvpSquadPositionRole.RightWinger, 75, 83, 23),
                    new("José Sá", MvpSquadPositionRole.Goalkeeper, 79, 79, 33),
                ],
                PrimaryColorHex: "#FDB913",
                SecondaryColorHex: "#231F20",
                KitPattern: "Solid"),

            [131] = new(
                ClubId: 131,
                OfficialName: "LEICESTER CITY FC",
                ShortCode: "LEI",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 75,
                TransferBudgetLimit: 28_000_000,
                Players:
                [
                    new("Bilal El Khannouss", MvpSquadPositionRole.AttackingMidfielder, 76, 86, 22),
                    new("Abdul Fatawu", MvpSquadPositionRole.RightWinger, 76, 85, 22),
                    new("Facundo Buonanotte", MvpSquadPositionRole.AttackingMidfielder, 76, 84, 21),
                    new("Jamie Vardy", MvpSquadPositionRole.Striker, 76, 76, 39),
                    new("Harry Winks", MvpSquadPositionRole.CentralMidfielder, 77, 77, 30),
                    new("Wout Faes", MvpSquadPositionRole.CentreBack, 76, 78, 28),
                    new("Victor Kristiansen", MvpSquadPositionRole.LeftBack, 75, 80, 23),
                    new("Mads Hermansen", MvpSquadPositionRole.Goalkeeper, 78, 83, 26),
                ],
                PrimaryColorHex: "#003090",
                SecondaryColorHex: "#FDBE11",
                KitPattern: "Solid"),

            [132] = new(
                ClubId: 132,
                OfficialName: "IPSWICH TOWN FC",
                ShortCode: "IPS",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 74,
                TransferBudgetLimit: 25_000_000,
                Players:
                [
                    new("Liam Delap", MvpSquadPositionRole.Striker, 77, 85, 23),
                    new("Omari Hutchinson", MvpSquadPositionRole.RightWinger, 76, 84, 22),
                    new("Leif Davis", MvpSquadPositionRole.LeftBack, 77, 80, 26),
                    new("Julio Enciso", MvpSquadPositionRole.AttackingMidfielder, 76, 84, 22),
                    new("Sam Morsy", MvpSquadPositionRole.DefensiveMidfielder, 74, 74, 34),
                    new("Jacob Greaves", MvpSquadPositionRole.CentreBack, 75, 80, 25),
                    new("Dara O'Shea", MvpSquadPositionRole.CentreBack, 75, 78, 27),
                    new("Arijanet Muric", MvpSquadPositionRole.Goalkeeper, 75, 78, 27),
                ],
                PrimaryColorHex: "#3A64A3",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [133] = new(
                ClubId: 133,
                OfficialName: "SOUTHAMPTON FC",
                ShortCode: "SOU",
                Country: "İngiltere",
                LeagueName: "Premier League",
                SportiveStrength: 74,
                TransferBudgetLimit: 24_000_000,
                Players:
                [
                    new("Tyler Dibling", MvpSquadPositionRole.RightWinger, 75, 87, 20),
                    new("Mateus Fernandes", MvpSquadPositionRole.CentralMidfielder, 76, 85, 22),
                    new("Taylor Harwood-Bellis", MvpSquadPositionRole.CentreBack, 76, 83, 24),
                    new("Kyle Walker-Peters", MvpSquadPositionRole.RightBack, 77, 78, 29),
                    new("Yukinari Sugawara", MvpSquadPositionRole.RightBack, 76, 79, 26),
                    new("Cameron Archer", MvpSquadPositionRole.Striker, 74, 80, 24),
                    new("Flynn Downes", MvpSquadPositionRole.DefensiveMidfielder, 75, 78, 27),
                    new("Aaron Ramsdale", MvpSquadPositionRole.Goalkeeper, 80, 82, 28),
                ],
                PrimaryColorHex: "#D71920",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            // =========================================================================
            // ALMANYA — BUNDESLIGA (1. LİG — 18 KULÜP)
            // =========================================================================
            [103] = new(
                ClubId: 103,
                OfficialName: "FC BAYERN MÜNCHEN",
                ShortCode: "BAY",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 88,
                TransferBudgetLimit: 95_000_000,
                Players:
                [
                    new("Harry Kane", MvpSquadPositionRole.Striker, 90, 90, 33),
                    new("Jamal Musiala", MvpSquadPositionRole.AttackingMidfielder, 88, 93, 23),
                    new("Joshua Kimmich", MvpSquadPositionRole.CentralMidfielder, 87, 87, 31),
                    new("Alphonso Davies", MvpSquadPositionRole.LeftBack, 83, 87, 25),
                    new("Dayot Upamecano", MvpSquadPositionRole.CentreBack, 83, 86, 27),
                    new("Kim Min-jae", MvpSquadPositionRole.CentreBack, 83, 85, 29),
                    new("Michael Olise", MvpSquadPositionRole.RightWinger, 83, 88, 24),
                    new("Mathys Tel", MvpSquadPositionRole.Striker, 77, 87, 21),
                    new("Aleksandar Pavlović", MvpSquadPositionRole.DefensiveMidfielder, 78, 87, 22),
                    new("Manuel Neuer", MvpSquadPositionRole.Goalkeeper, 86, 86, 40),
                    new("Kingsley Coman", MvpSquadPositionRole.LeftWinger, 83, 83, 30),
                    new("João Palhinha", MvpSquadPositionRole.DefensiveMidfielder, 84, 84, 31),
                ],
                PrimaryColorHex: "#DC052D",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [109] = new(
                ClubId: 109,
                OfficialName: "BAYER 04 LEVERKUSEN",
                ShortCode: "B04",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 86,
                TransferBudgetLimit: 75_000_000,
                Players:
                [
                    new("Florian Wirtz", MvpSquadPositionRole.AttackingMidfielder, 88, 92, 23),
                    new("Alejandro Grimaldo", MvpSquadPositionRole.LeftBack, 86, 86, 30),
                    new("Jeremie Frimpong", MvpSquadPositionRole.RightBack, 84, 87, 25),
                    new("Granit Xhaka", MvpSquadPositionRole.DefensiveMidfielder, 86, 86, 33),
                    new("Victor Boniface", MvpSquadPositionRole.Striker, 82, 86, 25),
                    new("Piero Hincapié", MvpSquadPositionRole.CentreBack, 81, 86, 24),
                    new("Edmond Tapsoba", MvpSquadPositionRole.CentreBack, 83, 86, 27),
                    new("Exequiel Palacios", MvpSquadPositionRole.CentralMidfielder, 84, 85, 27),
                    new("Amine Adli", MvpSquadPositionRole.LeftWinger, 79, 84, 26),
                    new("Lukáš Hrádecký", MvpSquadPositionRole.Goalkeeper, 83, 83, 36),
                    new("Patrik Schick", MvpSquadPositionRole.Striker, 82, 82, 30),
                    new("Jonathan Tah", MvpSquadPositionRole.CentreBack, 85, 85, 30),
                ],
                PrimaryColorHex: "#E32221",
                SecondaryColorHex: "#111111",
                KitPattern: "Solid"),

            [111] = new(
                ClubId: 111,
                OfficialName: "BORUSSIA DORTMUND",
                ShortCode: "BVB",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 83,
                TransferBudgetLimit: 65_000_000,
                Players:
                [
                    new("Serhou Guirassy", MvpSquadPositionRole.Striker, 82, 82, 30),
                    new("Julian Brandt", MvpSquadPositionRole.AttackingMidfielder, 84, 84, 30),
                    new("Nico Schlotterbeck", MvpSquadPositionRole.CentreBack, 84, 87, 26),
                    new("Karim Adeyemi", MvpSquadPositionRole.LeftWinger, 80, 85, 24),
                    new("Jamie Bynoe-Gittens", MvpSquadPositionRole.RightWinger, 78, 86, 22),
                    new("Marcel Sabitzer", MvpSquadPositionRole.CentralMidfielder, 82, 82, 32),
                    new("Felix Nmecha", MvpSquadPositionRole.CentralMidfielder, 78, 83, 25),
                    new("Waldemar Anton", MvpSquadPositionRole.CentreBack, 82, 83, 30),
                    new("Yan Couto", MvpSquadPositionRole.RightBack, 78, 84, 24),
                    new("Gregor Kobel", MvpSquadPositionRole.Goalkeeper, 87, 89, 28),
                    new("Maximilian Beier", MvpSquadPositionRole.Striker, 78, 85, 23),
                ],
                PrimaryColorHex: "#FDE100",
                SecondaryColorHex: "#111111",
                KitPattern: "Solid"),

            [301] = new(
                ClubId: 301,
                OfficialName: "RB LEIPZIG",
                ShortCode: "RBL",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 84,
                TransferBudgetLimit: 68_000_000,
                Players:
                [
                    new("Xavi Simons", MvpSquadPositionRole.AttackingMidfielder, 85, 90, 23),
                    new("Benjamin Šeško", MvpSquadPositionRole.Striker, 81, 88, 23),
                    new("Loïs Openda", MvpSquadPositionRole.Striker, 84, 87, 26),
                    new("Castello Lukeba", MvpSquadPositionRole.CentreBack, 81, 87, 23),
                    new("David Raum", MvpSquadPositionRole.LeftBack, 82, 83, 28),
                    new("Willi Orbán", MvpSquadPositionRole.CentreBack, 82, 82, 33),
                    new("Antonio Nusa", MvpSquadPositionRole.LeftWinger, 76, 86, 21),
                    new("Arthur Vermeeren", MvpSquadPositionRole.DefensiveMidfielder, 76, 85, 21),
                    new("Lutsharel Geertruida", MvpSquadPositionRole.RightBack, 81, 84, 26),
                    new("Péter Gulácsi", MvpSquadPositionRole.Goalkeeper, 82, 82, 36),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#DD0741",
                KitPattern: "Sleeves"),

            [302] = new(
                ClubId: 302,
                OfficialName: "EINTRACHT FRANKFURT",
                ShortCode: "SGE",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 81,
                TransferBudgetLimit: 48_000_000,
                Players:
                [
                    new("Hugo Ekitiké", MvpSquadPositionRole.Striker, 80, 86, 24),
                    new("Can Uzun", MvpSquadPositionRole.AttackingMidfielder, 75, 86, 20),
                    new("Hugo Larsson", MvpSquadPositionRole.CentralMidfielder, 79, 86, 22),
                    new("Robin Koch", MvpSquadPositionRole.CentreBack, 80, 81, 30),
                    new("Arthur Theate", MvpSquadPositionRole.CentreBack, 80, 83, 26),
                    new("Mario Götze", MvpSquadPositionRole.AttackingMidfielder, 80, 80, 34),
                    new("Ansgar Knauff", MvpSquadPositionRole.RightWinger, 77, 81, 24),
                    new("Rasmus Kristensen", MvpSquadPositionRole.RightBack, 77, 78, 29),
                    new("Kevin Trapp", MvpSquadPositionRole.Goalkeeper, 81, 81, 36),
                ],
                PrimaryColorHex: "#E1000F",
                SecondaryColorHex: "#111111",
                KitPattern: "Stripes"),

            [303] = new(
                ClubId: 303,
                OfficialName: "VFB STUTTGART",
                ShortCode: "VFB",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 81,
                TransferBudgetLimit: 45_000_000,
                Players:
                [
                    new("Angelo Stiller", MvpSquadPositionRole.DefensiveMidfielder, 82, 86, 25),
                    new("Enzo Millot", MvpSquadPositionRole.AttackingMidfielder, 80, 86, 24),
                    new("Deniz Undav", MvpSquadPositionRole.Striker, 82, 82, 30),
                    new("Ermedin Demirović", MvpSquadPositionRole.Striker, 80, 82, 28),
                    new("Maximilian Mittelstädt", MvpSquadPositionRole.LeftBack, 81, 82, 29),
                    new("Chris Führich", MvpSquadPositionRole.LeftWinger, 80, 82, 28),
                    new("Jamie Leweling", MvpSquadPositionRole.RightWinger, 78, 82, 25),
                    new("Atakan Karazor", MvpSquadPositionRole.DefensiveMidfielder, 79, 79, 29),
                    new("Jeff Chabot", MvpSquadPositionRole.CentreBack, 79, 80, 28),
                    new("Alexander Nübel", MvpSquadPositionRole.Goalkeeper, 81, 82, 29),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#E32219",
                KitPattern: "Hoops"),

            [304] = new(
                ClubId: 304,
                OfficialName: "SC FREIBURG",
                ShortCode: "SCF",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 79,
                TransferBudgetLimit: 36_000_000,
                Players:
                [
                    new("Vincenzo Grifo", MvpSquadPositionRole.LeftWinger, 81, 81, 33),
                    new("Ritsu Dōan", MvpSquadPositionRole.RightWinger, 80, 82, 28),
                    new("Matthias Ginter", MvpSquadPositionRole.CentreBack, 81, 81, 32),
                    new("Philipp Lienhart", MvpSquadPositionRole.CentreBack, 80, 80, 30),
                    new("Christian Günter", MvpSquadPositionRole.LeftBack, 79, 79, 33),
                    new("Maximilian Eggestein", MvpSquadPositionRole.CentralMidfielder, 78, 78, 29),
                    new("Eren Dinkçi", MvpSquadPositionRole.RightWinger, 76, 81, 24),
                    new("Noah Atubolu", MvpSquadPositionRole.Goalkeeper, 77, 84, 24),
                ],
                PrimaryColorHex: "#E30613",
                SecondaryColorHex: "#111111",
                KitPattern: "Stripes"),

            [305] = new(
                ClubId: 305,
                OfficialName: "SV WERDER BREMEN",
                ShortCode: "SVW",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 77,
                TransferBudgetLimit: 28_000_000,
                Players:
                [
                    new("Marvin Ducksch", MvpSquadPositionRole.Striker, 78, 78, 32),
                    new("Mitchell Weiser", MvpSquadPositionRole.RightBack, 79, 79, 32),
                    new("Romano Schmid", MvpSquadPositionRole.AttackingMidfielder, 78, 81, 26),
                    new("Jens Stage", MvpSquadPositionRole.CentralMidfielder, 78, 79, 29),
                    new("Marco Friedl", MvpSquadPositionRole.CentreBack, 77, 79, 28),
                    new("Niklas Stark", MvpSquadPositionRole.CentreBack, 77, 77, 31),
                    new("Felix Agu", MvpSquadPositionRole.LeftBack, 75, 79, 26),
                    new("Michael Zetterer", MvpSquadPositionRole.Goalkeeper, 77, 78, 31),
                ],
                PrimaryColorHex: "#1D9053",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [306] = new(
                ClubId: 306,
                OfficialName: "BORUSSIA MÖNCHENGLADBACH",
                ShortCode: "BMG",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 78,
                TransferBudgetLimit: 32_000_000,
                Players:
                [
                    new("Tim Kleindienst", MvpSquadPositionRole.Striker, 79, 79, 31),
                    new("Alassane Pléa", MvpSquadPositionRole.AttackingMidfielder, 79, 79, 33),
                    new("Franck Honorat", MvpSquadPositionRole.RightWinger, 78, 79, 30),
                    new("Rocco Reitz", MvpSquadPositionRole.CentralMidfielder, 77, 84, 24),
                    new("Julian Weigl", MvpSquadPositionRole.DefensiveMidfielder, 78, 78, 30),
                    new("Ko Itakura", MvpSquadPositionRole.CentreBack, 79, 80, 29),
                    new("Nico Elvedi", MvpSquadPositionRole.CentreBack, 78, 78, 29),
                    new("Moritz Nicolas", MvpSquadPositionRole.Goalkeeper, 76, 79, 28),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#111111",
                KitPattern: "Solid"),

            [307] = new(
                ClubId: 307,
                OfficialName: "VFL WOLFSBURG",
                ShortCode: "WOB",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 78,
                TransferBudgetLimit: 38_000_000,
                Players:
                [
                    new("Mohamed Amoura", MvpSquadPositionRole.Striker, 79, 83, 26),
                    new("Maximilian Arnold", MvpSquadPositionRole.DefensiveMidfielder, 80, 80, 32),
                    new("Lovro Majer", MvpSquadPositionRole.AttackingMidfielder, 79, 82, 28),
                    new("Jonas Wind", MvpSquadPositionRole.Striker, 78, 80, 27),
                    new("Konstantinos Koulierakis", MvpSquadPositionRole.CentreBack, 76, 84, 22),
                    new("Denis Vavro", MvpSquadPositionRole.CentreBack, 77, 78, 30),
                    new("Joakim Mæhle", MvpSquadPositionRole.LeftBack, 78, 79, 29),
                    new("Kamil Grabara", MvpSquadPositionRole.Goalkeeper, 79, 82, 27),
                ],
                PrimaryColorHex: "#65B32E",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [308] = new(
                ClubId: 308,
                OfficialName: "1. FSV MAINZ 05",
                ShortCode: "M05",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 77,
                TransferBudgetLimit: 26_000_000,
                Players:
                [
                    new("Jonathan Burkardt", MvpSquadPositionRole.Striker, 79, 83, 26),
                    new("Nadiem Amiri", MvpSquadPositionRole.CentralMidfielder, 79, 79, 29),
                    new("Kaishu Sano", MvpSquadPositionRole.DefensiveMidfielder, 77, 81, 25),
                    new("Lee Jae-sung", MvpSquadPositionRole.AttackingMidfielder, 77, 77, 34),
                    new("Anthony Caci", MvpSquadPositionRole.RightBack, 77, 79, 29),
                    new("Dominik Kohr", MvpSquadPositionRole.CentreBack, 76, 76, 32),
                    new("Phillipp Mwene", MvpSquadPositionRole.LeftBack, 75, 75, 32),
                    new("Robin Zentner", MvpSquadPositionRole.Goalkeeper, 78, 78, 31),
                ],
                PrimaryColorHex: "#C3141E",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Halves"),

            [309] = new(
                ClubId: 309,
                OfficialName: "TSG 1899 HOFFENHEIM",
                ShortCode: "TSG",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 77,
                TransferBudgetLimit: 30_000_000,
                Players:
                [
                    new("Andrej Kramarić", MvpSquadPositionRole.AttackingMidfielder, 81, 81, 35),
                    new("Adam Hložek", MvpSquadPositionRole.Striker, 77, 83, 24),
                    new("Tom Bischof", MvpSquadPositionRole.CentralMidfielder, 76, 86, 21),
                    new("Anton Stach", MvpSquadPositionRole.DefensiveMidfielder, 79, 81, 27),
                    new("Pavel Kadeřábek", MvpSquadPositionRole.RightBack, 76, 76, 34),
                    new("Kevin Akpoguma", MvpSquadPositionRole.CentreBack, 75, 75, 31),
                    new("Marius Bülter", MvpSquadPositionRole.LeftWinger, 75, 75, 33),
                    new("Oliver Baumann", MvpSquadPositionRole.Goalkeeper, 81, 81, 36),
                ],
                PrimaryColorHex: "#1C63B7",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [310] = new(
                ClubId: 310,
                OfficialName: "1. FC UNION BERLIN",
                ShortCode: "FCU",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 76,
                TransferBudgetLimit: 26_000_000,
                Players:
                [
                    new("Danilho Doekhi", MvpSquadPositionRole.CentreBack, 79, 81, 28),
                    new("Diogo Leite", MvpSquadPositionRole.CentreBack, 79, 82, 27),
                    new("Rani Khedira", MvpSquadPositionRole.DefensiveMidfielder, 77, 77, 32),
                    new("Benedict Hollerbach", MvpSquadPositionRole.Striker, 76, 80, 25),
                    new("Jeong Woo-yeong", MvpSquadPositionRole.AttackingMidfielder, 76, 79, 26),
                    new("Tom Rothe", MvpSquadPositionRole.LeftBack, 75, 83, 21),
                    new("Christopher Trimmel", MvpSquadPositionRole.RightBack, 74, 74, 39),
                    new("Frederik Rønnow", MvpSquadPositionRole.Goalkeeper, 80, 80, 34),
                ],
                PrimaryColorHex: "#D4011D",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [311] = new(
                ClubId: 311,
                OfficialName: "FC AUGSBURG",
                ShortCode: "FCA",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 75,
                TransferBudgetLimit: 24_000_000,
                Players:
                [
                    new("Alexis Claude-Maurice", MvpSquadPositionRole.AttackingMidfielder, 77, 79, 28),
                    new("Kristijan Jakić", MvpSquadPositionRole.DefensiveMidfielder, 77, 78, 29),
                    new("Jeffrey Gouweleeuw", MvpSquadPositionRole.CentreBack, 77, 77, 35),
                    new("Chrislain Matsima", MvpSquadPositionRole.CentreBack, 75, 82, 24),
                    new("Dimitrios Giannoulis", MvpSquadPositionRole.LeftBack, 76, 76, 30),
                    new("Phillip Tietz", MvpSquadPositionRole.Striker, 76, 77, 29),
                    new("Mert Kömür", MvpSquadPositionRole.AttackingMidfielder, 71, 83, 21),
                    new("Nediljko Labrović", MvpSquadPositionRole.Goalkeeper, 76, 79, 26),
                ],
                PrimaryColorHex: "#BA3733",
                SecondaryColorHex: "#006837",
                KitPattern: "Stripes"),

            [312] = new(
                ClubId: 312,
                OfficialName: "1. FC HEIDENHEIM 1846",
                ShortCode: "HDH",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 74,
                TransferBudgetLimit: 20_000_000,
                Players:
                [
                    new("Paul Wanner", MvpSquadPositionRole.AttackingMidfielder, 75, 87, 20),
                    new("Patrick Mainka", MvpSquadPositionRole.CentreBack, 77, 77, 31),
                    new("Jan Schöppner", MvpSquadPositionRole.DefensiveMidfielder, 75, 77, 27),
                    new("Marvin Pieringer", MvpSquadPositionRole.Striker, 75, 77, 26),
                    new("Leo Scienza", MvpSquadPositionRole.LeftWinger, 74, 76, 27),
                    new("Omar Traoré", MvpSquadPositionRole.RightBack, 74, 77, 28),
                    new("Jonas Föhrenbach", MvpSquadPositionRole.LeftBack, 74, 74, 30),
                    new("Kevin Müller", MvpSquadPositionRole.Goalkeeper, 77, 77, 35),
                ],
                PrimaryColorHex: "#E2001A",
                SecondaryColorHex: "#003D7C",
                KitPattern: "Stripes"),

            [313] = new(
                ClubId: 313,
                OfficialName: "FC ST. PAULI",
                ShortCode: "STP",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 74,
                TransferBudgetLimit: 18_000_000,
                Players:
                [
                    new("Jackson Irvine", MvpSquadPositionRole.CentralMidfielder, 76, 76, 33),
                    new("Eric Smith", MvpSquadPositionRole.CentreBack, 76, 77, 29),
                    new("Morgan Guilavogui", MvpSquadPositionRole.RightWinger, 75, 78, 28),
                    new("Karol Mets", MvpSquadPositionRole.CentreBack, 74, 74, 33),
                    new("Philipp Treu", MvpSquadPositionRole.LeftBack, 74, 79, 25),
                    new("Johannes Eggestein", MvpSquadPositionRole.Striker, 74, 75, 28),
                    new("Manolis Saliakas", MvpSquadPositionRole.RightBack, 74, 75, 29),
                    new("Nikola Vasilj", MvpSquadPositionRole.Goalkeeper, 76, 77, 30),
                ],
                PrimaryColorHex: "#624839",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Stripes"),

            [314] = new(
                ClubId: 314,
                OfficialName: "VFL BOCHUM 1848",
                ShortCode: "BOC",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 73,
                TransferBudgetLimit: 17_000_000,
                Players:
                [
                    new("Matúš Bero", MvpSquadPositionRole.CentralMidfielder, 75, 75, 30),
                    new("Ibrahima Sissoko", MvpSquadPositionRole.DefensiveMidfielder, 75, 77, 28),
                    new("Myron Boadu", MvpSquadPositionRole.Striker, 75, 79, 25),
                    new("Maximilian Wittek", MvpSquadPositionRole.LeftBack, 74, 74, 31),
                    new("Ivan Ordets", MvpSquadPositionRole.CentreBack, 75, 75, 34),
                    new("Felix Passlack", MvpSquadPositionRole.RightBack, 73, 74, 28),
                    new("Philipp Hofmann", MvpSquadPositionRole.Striker, 74, 74, 33),
                    new("Timo Horn", MvpSquadPositionRole.Goalkeeper, 75, 75, 33),
                ],
                PrimaryColorHex: "#005CA9",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [315] = new(
                ClubId: 315,
                OfficialName: "HOLSTEIN KIEL",
                ShortCode: "KSV",
                Country: "Almanya",
                LeagueName: "Bundesliga",
                SportiveStrength: 72,
                TransferBudgetLimit: 15_000_000,
                Players:
                [
                    new("Shuto Machino", MvpSquadPositionRole.Striker, 74, 77, 26),
                    new("Finn Porath", MvpSquadPositionRole.RightMidfielder, 73, 74, 29),
                    new("Armin Gigović", MvpSquadPositionRole.CentralMidfielder, 74, 79, 24),
                    new("Steven Skrzybski", MvpSquadPositionRole.AttackingMidfielder, 73, 73, 33),
                    new("Timo Becker", MvpSquadPositionRole.CentreBack, 73, 75, 29),
                    new("Patrick Erras", MvpSquadPositionRole.CentreBack, 72, 72, 31),
                    new("Nicolai Remberg", MvpSquadPositionRole.DefensiveMidfielder, 73, 77, 26),
                    new("Timon Weiner", MvpSquadPositionRole.Goalkeeper, 73, 75, 27),
                ],
                PrimaryColorHex: "#005CA9",
                SecondaryColorHex: "#E30613",
                KitPattern: "Sleeves"),

            // =========================================================================
            // DİĞER AVRUPA ŞAMPİYONLAR KUPASI ELİT KULÜPLERİ
            // =========================================================================
            [107] = new(
                ClubId: 107,
                OfficialName: "PARIS SAINT-GERMAIN FC",
                ShortCode: "PSG",
                Country: "Fransa",
                LeagueName: "Ligue 1",
                SportiveStrength: 86,
                TransferBudgetLimit: 110_000_000,
                Players:
                [
                    new("Ousmane Dembélé", MvpSquadPositionRole.RightWinger, 86, 86, 29),
                    new("Bradley Barcola", MvpSquadPositionRole.LeftWinger, 81, 88, 24),
                    new("Vitinha", MvpSquadPositionRole.CentralMidfielder, 85, 88, 26),
                    new("Achraf Hakimi", MvpSquadPositionRole.RightBack, 84, 86, 27),
                    new("Warren Zaïre-Emery", MvpSquadPositionRole.CentralMidfielder, 81, 90, 20),
                    new("João Neves", MvpSquadPositionRole.DefensiveMidfielder, 81, 89, 21),
                    new("Marquinhos", MvpSquadPositionRole.CentreBack, 86, 86, 32),
                    new("Willian Pacho", MvpSquadPositionRole.CentreBack, 80, 85, 24),
                    new("Nuno Mendes", MvpSquadPositionRole.LeftBack, 83, 87, 24),
                    new("Gianluigi Donnarumma", MvpSquadPositionRole.Goalkeeper, 88, 90, 27),
                    new("Khvicha Kvaratskhelia", MvpSquadPositionRole.LeftWinger, 86, 89, 25),
                ],
                PrimaryColorHex: "#004170",
                SecondaryColorHex: "#DA291C",
                KitPattern: "Stripes"),

            [108] = new(
                ClubId: 108,
                OfficialName: "FC INTERNAZIONALE MILANO",
                ShortCode: "INT",
                Country: "İtalya",
                LeagueName: "Serie A",
                SportiveStrength: 86,
                TransferBudgetLimit: 65_000_000,
                Players:
                [
                    new("Lautaro Martínez", MvpSquadPositionRole.Striker, 89, 90, 29),
                    new("Nicolò Barella", MvpSquadPositionRole.CentralMidfielder, 87, 88, 29),
                    new("Hakan Çalhanoğlu", MvpSquadPositionRole.DefensiveMidfielder, 86, 86, 32),
                    new("Alessandro Bastoni", MvpSquadPositionRole.CentreBack, 87, 90, 27),
                    new("Marcus Thuram", MvpSquadPositionRole.Striker, 84, 85, 29),
                    new("Federico Dimarco", MvpSquadPositionRole.LeftBack, 84, 85, 28),
                    new("Denzel Dumfries", MvpSquadPositionRole.RightBack, 82, 83, 30),
                    new("Benjamin Pavard", MvpSquadPositionRole.CentreBack, 84, 84, 30),
                    new("Davide Frattesi", MvpSquadPositionRole.CentralMidfielder, 81, 84, 26),
                    new("Yann Sommer", MvpSquadPositionRole.Goalkeeper, 86, 86, 37),
                ],
                PrimaryColorHex: "#010E80",
                SecondaryColorHex: "#111111",
                KitPattern: "Stripes"),

            [110] = new(
                ClubId: 110,
                OfficialName: "JUVENTUS FC",
                ShortCode: "JUV",
                Country: "İtalya",
                LeagueName: "Serie A",
                SportiveStrength: 84,
                TransferBudgetLimit: 70_000_000,
                Players:
                [
                    new("Dušan Vlahović", MvpSquadPositionRole.Striker, 84, 87, 26),
                    new("Kenan Yıldız", MvpSquadPositionRole.AttackingMidfielder, 79, 89, 21),
                    new("Teun Koopmeiners", MvpSquadPositionRole.AttackingMidfielder, 83, 85, 28),
                    new("Bremer", MvpSquadPositionRole.CentreBack, 86, 87, 29),
                    new("Douglas Luiz", MvpSquadPositionRole.CentralMidfielder, 83, 84, 28),
                    new("Andrea Cambiaso", MvpSquadPositionRole.LeftBack, 80, 85, 26),
                    new("Khéphren Thuram", MvpSquadPositionRole.DefensiveMidfielder, 80, 85, 25),
                    new("Francisco Conceição", MvpSquadPositionRole.RightWinger, 79, 85, 23),
                    new("Federico Gatti", MvpSquadPositionRole.CentreBack, 79, 82, 28),
                    new("Michele Di Gregorio", MvpSquadPositionRole.Goalkeeper, 82, 84, 29),
                ],
                PrimaryColorHex: "#FFFFFF",
                SecondaryColorHex: "#111111",
                KitPattern: "Stripes"),

            [113] = new(
                ClubId: 113,
                OfficialName: "AC MILAN",
                ShortCode: "ACM",
                Country: "İtalya",
                LeagueName: "Serie A",
                SportiveStrength: 83,
                TransferBudgetLimit: 60_000_000,
                Players:
                [
                    new("Rafael Leão", MvpSquadPositionRole.LeftWinger, 86, 89, 27),
                    new("Theo Hernández", MvpSquadPositionRole.LeftBack, 87, 88, 28),
                    new("Christian Pulisic", MvpSquadPositionRole.RightWinger, 83, 84, 27),
                    new("Tijjani Reijnders", MvpSquadPositionRole.CentralMidfielder, 82, 85, 28),
                    new("Youssouf Fofana", MvpSquadPositionRole.DefensiveMidfielder, 81, 84, 27),
                    new("Fikayo Tomori", MvpSquadPositionRole.CentreBack, 83, 85, 28),
                    new("Strahinja Pavlović", MvpSquadPositionRole.CentreBack, 79, 84, 25),
                    new("Álvaro Morata", MvpSquadPositionRole.Striker, 83, 83, 33),
                    new("Yunus Musah", MvpSquadPositionRole.CentralMidfielder, 76, 82, 23),
                    new("Mike Maignan", MvpSquadPositionRole.Goalkeeper, 87, 89, 31),
                ],
                PrimaryColorHex: "#FB090B",
                SecondaryColorHex: "#111111",
                KitPattern: "Stripes"),

            [115] = new(
                ClubId: 115,
                OfficialName: "SPORTING CP",
                ShortCode: "SCP",
                Country: "Portekiz",
                LeagueName: "Liga Portugal",
                SportiveStrength: 82,
                TransferBudgetLimit: 50_000_000,
                Players:
                [
                    new("Viktor Gyökeres", MvpSquadPositionRole.Striker, 85, 87, 28),
                    new("Morten Hjulmand", MvpSquadPositionRole.DefensiveMidfielder, 81, 85, 27),
                    new("Gonçalo Inácio", MvpSquadPositionRole.CentreBack, 80, 86, 25),
                    new("Ousmane Diomande", MvpSquadPositionRole.CentreBack, 79, 87, 22),
                    new("Pedro Gonçalves", MvpSquadPositionRole.AttackingMidfielder, 82, 83, 28),
                    new("Geovany Quenda", MvpSquadPositionRole.RightWinger, 74, 87, 19),
                    new("Geny Catamo", MvpSquadPositionRole.RightBack, 77, 81, 25),
                    new("Francisco Trincão", MvpSquadPositionRole.RightWinger, 79, 82, 26),
                    new("Hidemasa Morita", MvpSquadPositionRole.CentralMidfielder, 80, 80, 31),
                    new("Franco Israel", MvpSquadPositionRole.Goalkeeper, 76, 81, 26),
                ],
                PrimaryColorHex: "#008057",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Hoops"),

            [116] = new(
                ClubId: 116,
                OfficialName: "SL BENFICA",
                ShortCode: "SLB",
                Country: "Portekiz",
                LeagueName: "Liga Portugal",
                SportiveStrength: 82,
                TransferBudgetLimit: 55_000_000,
                Players:
                [
                    new("Kerem Aktürkoğlu", MvpSquadPositionRole.LeftWinger, 80, 81, 27),
                    new("Orkun Kökçü", MvpSquadPositionRole.CentralMidfielder, 82, 85, 25),
                    new("Vangelis Pavlidis", MvpSquadPositionRole.Striker, 80, 82, 27),
                    new("Ángel Di María", MvpSquadPositionRole.RightWinger, 83, 83, 38),
                    new("António Silva", MvpSquadPositionRole.CentreBack, 79, 86, 22),
                    new("Florentino Luís", MvpSquadPositionRole.DefensiveMidfielder, 79, 83, 27),
                    new("Tomás Araújo", MvpSquadPositionRole.CentreBack, 77, 84, 24),
                    new("Álvaro Carreras", MvpSquadPositionRole.LeftBack, 77, 84, 23),
                    new("Gianluca Prestianni", MvpSquadPositionRole.RightWinger, 72, 85, 20),
                    new("Anatoliy Trubin", MvpSquadPositionRole.Goalkeeper, 81, 86, 25),
                ],
                PrimaryColorHex: "#E83030",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Solid"),

            [117] = new(
                ClubId: 117,
                OfficialName: "AFC AJAX",
                ShortCode: "AJX",
                Country: "Hollanda",
                LeagueName: "Eredivisie",
                SportiveStrength: 80,
                TransferBudgetLimit: 45_000_000,
                Players:
                [
                    new("Jorrel Hato", MvpSquadPositionRole.CentreBack, 77, 88, 20),
                    new("Kenneth Taylor", MvpSquadPositionRole.CentralMidfielder, 78, 83, 24),
                    new("Brian Brobbey", MvpSquadPositionRole.Striker, 79, 84, 24),
                    new("Bertrand Traoré", MvpSquadPositionRole.RightWinger, 78, 78, 30),
                    new("Jordan Henderson", MvpSquadPositionRole.CentralMidfielder, 79, 79, 36),
                    new("Mika Godts", MvpSquadPositionRole.LeftWinger, 74, 84, 21),
                    new("Josip Šutalo", MvpSquadPositionRole.CentreBack, 78, 82, 26),
                    new("Devyne Rensch", MvpSquadPositionRole.RightBack, 77, 82, 23),
                    new("Youri Baas", MvpSquadPositionRole.LeftBack, 74, 80, 23),
                    new("Remko Pasveer", MvpSquadPositionRole.Goalkeeper, 76, 76, 42),
                ],
                PrimaryColorHex: "#D2122E",
                SecondaryColorHex: "#FFFFFF",
                KitPattern: "Sleeves"),
        };

    public static readonly IReadOnlyList<GlobalFreeAgentProfile> FreeAgents =
    [
        new(9001, "Memphis Depay", MvpSquadPositionRole.Striker, 81, 81, 32, "Hollanda"),
        new(9002, "Sergio Ramos", MvpSquadPositionRole.CentreBack, 81, 81, 40, "İspanya"),
        new(9003, "Wissam Ben Yedder", MvpSquadPositionRole.Striker, 81, 81, 36, "Fransa"),
        new(9004, "Anthony Martial", MvpSquadPositionRole.Striker, 79, 79, 30, "Fransa"),
        new(9005, "Adrien Rabiot", MvpSquadPositionRole.CentralMidfielder, 82, 82, 31, "Fransa"),
        new(9006, "Keylor Navas", MvpSquadPositionRole.Goalkeeper, 80, 80, 39, "Kosta Rika"),
        new(9007, "Joel Matip", MvpSquadPositionRole.CentreBack, 79, 79, 35, "Kamerun"),
        new(9008, "Miralem Pjanić", MvpSquadPositionRole.CentralMidfielder, 77, 77, 36, "Bosna-Hersek"),
    ];

    public static IReadOnlyCollection<EuropeanEliteClubData> AllClubs => Clubs.Values.ToArray();

    public static IReadOnlyList<EuropeanEliteClubData> GetClubsByLeague(string leagueName) =>
        Clubs.Values
            .Where(c => string.Equals(c.LeagueName, leagueName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.SportiveStrength)
            .ThenBy(c => c.OfficialName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static EuropeanEliteClubData GetClub(long clubId) => Clubs[clubId];

    public static bool TryGetClub(long clubId, out EuropeanEliteClubData club) =>
        Clubs.TryGetValue(clubId, out club!);

    public static bool TryFindByName(string clubName, out EuropeanEliteClubData club)
    {
        club = null!;
        if (string.IsNullOrWhiteSpace(clubName))
        {
            return false;
        }

        var normalized = clubName.Trim();
        var match = Clubs.Values.FirstOrDefault(c =>
            string.Equals(c.OfficialName, normalized, StringComparison.OrdinalIgnoreCase)
            || c.OfficialName.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(c.OfficialName, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        club = match;
        return true;
    }

    public static bool IsEuropeanClub(long clubId) => Clubs.ContainsKey(clubId);

    /// <summary>
    /// Uluslararası oyuncu ID'si oluşturur ((clubId * 1000L) + slot + 1).
    /// Standard PlayerId.FromClubSlot sözleşmesiyle 100% uyumludur.
    /// </summary>
    public static long MakePlayerId(long clubId, int slot) => (clubId * 1000L) + slot + 1;

    public static bool TryResolvePlayer(long playerId, out string playerName, out string clubName, out EuropeanEliteClubData? clubData, out MvpSquadPlayerProfile? playerProfile)
    {
        if (playerId >= 100000)
        {
            var clubId = (playerId - 1) / 1000L;
            var slot = (int)((playerId - 1) % 1000L);
            if (Clubs.TryGetValue(clubId, out var eliteClub) && slot < eliteClub.Players.Count)
            {
                var profile = eliteClub.Players[slot];
                playerName = profile.DisplayName;
                clubName = eliteClub.OfficialName;
                clubData = eliteClub;
                playerProfile = profile;
                return true;
            }
        }

        var freeAgent = FreeAgents.FirstOrDefault(fa => fa.PlayerId == playerId);
        if (freeAgent is not null)
        {
            playerName = freeAgent.DisplayName;
            clubName = "Serbest Oyuncu";
            clubData = null;
            playerProfile = new MvpSquadPlayerProfile(freeAgent.DisplayName, freeAgent.PositionRole, freeAgent.CurrentAbility, freeAgent.PotentialAbility, freeAgent.Age);
            return true;
        }

        playerName = string.Empty;
        clubName = string.Empty;
        clubData = null;
        playerProfile = null;
        return false;
    }
}
