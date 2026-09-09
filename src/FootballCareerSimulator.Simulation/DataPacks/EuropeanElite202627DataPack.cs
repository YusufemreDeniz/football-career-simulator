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
    IReadOnlyList<MvpSquadPlayerProfile> Players);

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

    private static readonly IReadOnlyDictionary<long, EuropeanEliteClubData> Clubs =
        new Dictionary<long, EuropeanEliteClubData>
        {
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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
                ]),
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

    public static EuropeanEliteClubData GetClub(long clubId) => Clubs[clubId];

    public static bool TryGetClub(long clubId, out EuropeanEliteClubData club) =>
        Clubs.TryGetValue(clubId, out club!);

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
