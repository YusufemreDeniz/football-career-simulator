#!/usr/bin/env python3
"""SM25 Demanded Wage Matrix simülasyon üreticisi."""

from __future__ import annotations

import csv
import math
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
OUTPUT_CSV = SCRIPT_DIR.parent / "simulations" / "demanded_wage_matrix.csv"


def next_double(seed: int, minimum: float, maximum: float) -> float:
    value = (seed + 0x9E3779B9) & 0xFFFFFFFF
    value = ((value ^ (value >> 16)) * 0x85EBCA6B) & 0xFFFFFFFF
    value = ((value ^ (value >> 13)) * 0xC2B2AE35) & 0xFFFFFFFF
    value ^= (value >> 16)
    unit = value / (0xFFFFFFFF + 1.0)
    return minimum + (maximum - minimum) * unit


def round_monetary_value(value: float) -> int:
    if value < 250.0:
        step = 10
    elif value < 1000.0:
        step = 25
    elif value < 5000.0:
        step = 50
    elif value < 10000.0:
        step = 100
    elif value < 50000.0:
        step = 500
    elif value < 100000.0:
        step = 1000
    elif value < 5000000.0:
        step = 5000
    elif value < 10000000.0:
        step = 100000
    elif value < 50000000.0:
        step = 500000
    else:
        step = 1000000
    rounded = round(value / step) * step
    return int(rounded)


def round_to_step(value: float, step: int) -> int:
    return int(round(value / step) * step)


def calculate_length_multiplier(years: int) -> float:
    clamped = max(1, min(5, years))
    return 1.0 + (clamped - 1) / 40.0


def simulate():
    # 12 Kontrollü Oyuncu Senaryosu
    scenarios = [
        {
            "name": "Düşük Rating Genç",
            "age": 18, "rating": 55, "potential": 70, "value": 150_000,
            "current_wage": 200, "current_club": 10, "club_quality_base_wage": 350,
            "target_club_mod": 1.0, "type": "Transfer", "squad_status": "Youngster",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1001, "club_id": 20
        },
        {
            "name": "Yüksek Rating Genç",
            "age": 19, "rating": 76, "potential": 88, "value": 18_000_000,
            "current_wage": 15_000, "current_club": 15, "club_quality_base_wage": 22_000,
            "target_club_mod": 1.2, "type": "Transfer", "squad_status": "FirstTeam",
            "unwilling": False, "trait_mod": -5.0, "player_id": 1002, "club_id": 25  # HardBargain -5%
        },
        {
            "name": "Düşük Rating Peaked",
            "age": 28, "rating": 62, "potential": 62, "value": 400_000,
            "current_wage": 1_200, "current_club": 8, "club_quality_base_wage": 1_400,
            "target_club_mod": 0.9, "type": "Transfer", "squad_status": "Rotation",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1003, "club_id": 8
        },
        {
            "name": "Yüksek Rating Peaked",
            "age": 28, "rating": 88, "potential": 88, "value": 85_000_000,
            "current_wage": 180_000, "current_club": 1, "club_quality_base_wage": 210_000,
            "target_club_mod": 1.5, "type": "Transfer", "squad_status": "KeyPlayer",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1004, "club_id": 1
        },
        {
            "name": "Yüksek Potential Wonderkid",
            "age": 17, "rating": 72, "potential": 92, "value": 12_000_000,
            "current_wage": 5_000, "current_club": 12, "club_quality_base_wage": 14_000,
            "target_club_mod": 1.1, "type": "Transfer", "squad_status": "HotProspect",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1005, "club_id": 5
        },
        {
            "name": "Düşük Kulüp Hedefli Transfer",
            "age": 25, "rating": 68, "potential": 72, "value": 1_800_000,
            "current_wage": 3_500, "current_club": 14, "club_quality_base_wage": 3_200,
            "target_club_mod": 0.85, "type": "Transfer", "squad_status": "FirstTeam",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1006, "club_id": 30
        },
        {
            "name": "Elit Kulüp Hedefli Transfer",
            "age": 25, "rating": 84, "potential": 87, "value": 55_000_000,
            "current_wage": 90_000, "current_club": 6, "club_quality_base_wage": 130_000,
            "target_club_mod": 1.4, "type": "Transfer", "squad_status": "KeyPlayer",
            "unwilling": False, "trait_mod": -5.0, "player_id": 1007, "club_id": 2  # HardBargain -5%
        },
        {
            "name": "Renewal Sözleşme Yenileme",
            "age": 27, "rating": 80, "potential": 82, "value": 32_000_000,
            "current_wage": 45_000, "current_club": 1, "club_quality_base_wage": 65_000,
            "target_club_mod": 1.0, "type": "Renewal", "squad_status": "FirstTeam",
            "unwilling": False, "trait_mod": -5.0, "player_id": 1008, "club_id": 1  # ToughNegotiator -5%
        },
        {
            "name": "Free Agent Serbest Oyuncu",
            "age": 24, "rating": 74, "potential": 78, "value": 6_000_000,
            "current_wage": 0, "current_club": 0, "club_quality_base_wage": 16_000,
            "target_club_mod": 1.0, "type": "FreeAgent", "squad_status": "Rotation",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1009, "club_id": 10
        },
        {
            "name": "Unhappy / Wants-To-Leave Renewal",
            "age": 26, "rating": 82, "potential": 85, "value": 42_000_000,
            "current_wage": 60_000, "current_club": 1, "club_quality_base_wage": 85_000,
            "target_club_mod": 1.0, "type": "Renewal", "squad_status": "KeyPlayer",
            "unwilling": True, "trait_mod": 0.0, "player_id": 1010, "club_id": 1  # +50% Unhappy penalty!
        },
        {
            "name": "Farklı Squad Status (Backup)",
            "age": 29, "rating": 70, "potential": 70, "value": 2_500_000,
            "current_wage": 8_000, "current_club": 4, "club_quality_base_wage": 7_500,
            "target_club_mod": 1.0, "type": "Transfer", "squad_status": "Backup",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1011, "club_id": 4
        },
        {
            "name": "Minimum Wage Boundary / Genç Akademi",
            "age": 16, "rating": 48, "potential": 58, "value": 30_000,
            "current_wage": 0, "current_club": 0, "club_quality_base_wage": 35,
            "target_club_mod": 1.0, "type": "FreeAgent", "squad_status": "Youngster",
            "unwilling": False, "trait_mod": 0.0, "player_id": 1012, "club_id": 50  # Clamps to £50!
        }
    ]

    game_seed = 42
    contract_years_options = [1, 3, 5]
    negotiation_states = [
        ("Initial_Demand", 0),
        ("Rejected_Round_1", 1),
        ("Rejected_Round_2", 2)
    ]

    rows = []
    for sc in scenarios:
        for yrs in contract_years_options:
            length_mult = calculate_length_multiplier(yrs)
            base_wage = sc["club_quality_base_wage"]

            # Step 1: Base * LengthMultiplier
            length_wage = int(base_wage * length_mult)

            # Step 2: Trait Modifier (-5% if HardBargain/ToughNegotiator)
            if sc["trait_mod"] != 0.0:
                length_wage = int(round(length_wage * (1.0 + sc["trait_mod"] / 100.0)))

            # Step 3: Minimum £50 Floor
            base_contract_wage = max(50, length_wage)

            # Step 4: Acceptable Wage (Unhappy renewal penalty +50%)
            acceptable_wage = base_contract_wage
            if sc["type"] == "Renewal" and sc["unwilling"]:
                acceptable_wage += acceptable_wage // 2

            # Step 5: Variations for Initial Display vs Rejection
            for state_name, neg_count in negotiation_states:
                if state_name == "Initial_Demand":
                    seed = sc["player_id"] + game_seed
                    rand_mult = next_double(seed, 1.10, 1.25)
                    pre_round = base_contract_wage * rand_mult
                    final_demand = round_to_step(pre_round, 100)
                    used_factor = rand_mult
                else:
                    seed = sc["player_id"] + sc["club_id"]
                    rand_mult = next_double(seed, 1.00, 1.10)
                    pre_round = acceptable_wage * rand_mult
                    final_demand = round_monetary_value(pre_round)
                    used_factor = rand_mult

                rows.append({
                    "Scenario": sc["name"],
                    "Age": sc["age"],
                    "Rating": sc["rating"],
                    "Potential": sc["potential"],
                    "PlayerValue": sc["value"],
                    "CurrentWage": sc["current_wage"],
                    "CurrentClub": sc["current_club"],
                    "TargetClubModifier": sc["target_club_mod"],
                    "NegotiationType": sc["type"],
                    "ContractYears": yrs,
                    "LengthMultiplier": round(length_mult, 3),
                    "SquadStatus": sc["squad_status"],
                    "NegotiationState": state_name,
                    "NegotiationCount": neg_count,
                    "Seed": seed,
                    "BaseWage": base_wage,
                    "TraitModifierPct": sc["trait_mod"],
                    "UnwillingPenalty": "+50%" if (sc["type"] == "Renewal" and sc["unwilling"]) else "None",
                    "AcceptableWageThreshold": acceptable_wage,
                    "RandomFactor": round(used_factor, 4),
                    "PreRoundWage": round(pre_round, 2),
                    "FinalDemandedWage": final_demand
                })

    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with open(OUTPUT_CSV, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    print(f"Generated {len(rows)} simulation rows -> {OUTPUT_CSV}")


if __name__ == "__main__":
    simulate()
