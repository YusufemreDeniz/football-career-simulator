#!/usr/bin/env python3
"""Generate comparative simulation CSVs for SM25 vs Clean-Room Model."""

import csv
import math
from pathlib import Path

SIM_DIR = Path(__file__).resolve().parent.parent / "simulations"
SIM_DIR.mkdir(parents=True, exist_ok=True)

# ----------------- VALUATION MODELS -----------------

def sm25_value(rating, pot, age, in_decline=None, morale=10, game_time=70.0, form=7.0):
    if in_decline is None:
        in_decline = age >= 32
    a = min(age, 36)
    age_term = (37.0**2 - a**2) / 18.0
    
    # SM25 raw math (note: (rating-55)**8 in C++ can turn negative deltas positive)
    r_term = 50.0 * ((rating - 55) ** 8)
    pot_val = max(pot, rating)
    p_term = 50.0 * ((pot_val - 57) ** 8)
    
    if in_decline:
        base = (r_term * age_term) / 150_000_000.0
    else:
        base = ((0.8 * r_term + 0.2 * p_term) * age_term) / 150_000_000.0
        
    g_adj = base * (((game_time / 200.0) ** 2) - 0.15)
    f_adj = base * ((((form / 10.0) ** 2) * (game_time / 500.0)) - 0.1)
    m_mult = 0.8 + 0.02 * morale
    
    raw = (base + g_adj + f_adj) * m_mult
    return int(max(1000.0, raw))

AGE_CURVE = [
    (16, 0.78), (18, 0.92), (21, 1.10), (24, 1.16), (27, 1.08),
    (30, 0.90), (33, 0.68), (36, 0.46), (40, 0.25)
]

POT_WEIGHT_CURVE = [
    (16, 1.00), (18, 0.95), (21, 0.78), (24, 0.50), (27, 0.25),
    (30, 0.08), (33, 0.00)
]

CONTRACT_CURVE = [
    (0.0, 0.55), (0.5, 0.66), (1.0, 0.78), (2.0, 1.00),
    (3.0, 1.08), (4.0, 1.12), (5.0, 1.14)
]

def interpolate(curve, val):
    if val <= curve[0][0]:
        return curve[0][1]
    for i in range(1, len(curve)):
        if val <= curve[i][0]:
            x0, y0 = curve[i-1]
            x1, y1 = curve[i]
            return y0 + (y1 - y0) * ((val - x0) / (x1 - x0))
    return curve[-1][1]

def our_game_value(rating, pot, age, contract_years=3.0, club_strength=75):
    ability = max(40, min(99, rating))
    potential = max(ability, min(99, pot))
    norm_ability = (ability - 40) / 59.0
    
    ability_val = 250_000 + (160_000_000 - 250_000) * (norm_ability ** 4.6)
    
    pot_gap = (potential - ability) / 59.0
    pot_weight = interpolate(POT_WEIGHT_CURVE, age)
    pot_mult = 1.0 + pot_gap * pot_weight
    
    age_mult = interpolate(AGE_CURVE, age)
    contract_mult = interpolate(CONTRACT_CURVE, contract_years)
    
    club_mult = 0.90 + ((club_strength - 40) / 59.0) * (1.12 - 0.90)
    
    raw = ability_val * pot_mult * age_mult * contract_mult * club_mult
    clamped = max(250_000, min(160_000_000, raw))
    return int(round(clamped / 50_000.0) * 50_000)

# ----------------- WAGE MODELS -----------------

def sm25_basic_wage(rating, pot, is_peaked):
    r_factor = (rating / 99.0) ** 20.0
    if is_peaked:
        return 750_000.0 * r_factor
    p_factor = (pot / 99.0) ** 20.0
    return 750_000.0 * (0.9 * r_factor + 0.1 * p_factor)

def sm25_round_wage(num):
    if num < 250.0: step = 10.0
    elif num < 1000.0: step = 25.0
    elif num < 5000.0: step = 50.0
    elif num < 10000.0: step = 100.0
    elif num < 50000.0: step = 500.0
    elif num < 100000.0: step = 1000.0
    elif num < 5000000.0: step = 5000.0
    elif num < 10000000.0: step = 100000.0
    elif num < 50000000.0: step = 500000.0
    else: step = 1000000.0
    return int(round(num / step) * step)

def sm25_wage(rating, pot, club_rating, league_rating, contract_years, is_peaked=False):
    base = sm25_basic_wage(rating, pot, is_peaked)
    team_fit = 0.5 + 0.5 * (rating / float(club_rating))
    team_tier = club_rating / 94.0
    league_tier = league_rating / 89.0
    contract_mult = 1.0 + (contract_years - 1) / 40.0
    raw = base * team_fit * team_tier * league_tier * contract_mult
    clamped = max(50.0, raw)
    return sm25_round_wage(clamped)

def our_game_wage(rating, pot, club_rating, league_rating, contract_years, is_peaked=False):
    norm_ab = (rating - 40) / 59.0
    norm_pot = (pot - 40) / 59.0
    ab_factor = norm_ab ** 10.0
    pot_factor = norm_pot ** 10.0
    
    comp_factor = ab_factor if is_peaked else (0.85 * ab_factor + 0.15 * pot_factor)
    base = 100.0 + (650_000.0 - 100.0) * comp_factor
    
    team_fit = 0.50 + 0.50 * (rating / float(club_rating))
    club_tier = max(0.40, min(1.30, club_rating / 92.0))
    league_tier = max(0.40, min(1.30, league_rating / 88.0))
    contract_mult = 1.0 + (contract_years - 1) * 0.025
    
    raw = base * team_fit * club_tier * league_tier * contract_mult
    clamped = max(100.0, min(1_000_000.0, raw))
    
    if clamped < 1_000: step = 25
    elif clamped < 10_000: step = 100
    elif clamped < 50_000: step = 500
    else: step = 1000
    
    return max(100, int(round(clamped / step) * step))

# ----------------- SIMULATION GENERATION -----------------

def run_valuation_sim():
    ratings = [50, 60, 70, 75, 80, 85, 90, 92, 95, 97, 99]
    ages = [16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36]
    
    out_path = SIM_DIR / "comparative_player_valuation.csv"
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow([
            "Rating", "Potential", "Age",
            "SM25_Val_Morale5", "SM25_Val_Morale10",
            "Our_Game_Value", "Diff_Morale10_Pct"
        ])
        
        for r in ratings:
            pot_options = [r, min(99, r + 5), min(99, r + 10), 95, 99]
            pot_options = sorted(list(set([p for p in pot_options if p >= r])))
            for p in pot_options:
                for a in ages:
                    sm_m5 = sm25_value(r, p, a, morale=5)
                    sm_m10 = sm25_value(r, p, a, morale=10)
                    our_val = our_game_value(r, p, a)
                    diff_pct = round(((our_val - sm_m10) / float(sm_m10)) * 100.0, 1)
                    writer.writerow([r, p, a, sm_m5, sm_m10, our_val, diff_pct])
                    
    print(f"Generated {out_path}")

def run_wage_sim():
    ratings = [60, 70, 80, 85, 90, 95, 99]
    tiers = [
        ("Low", 65, 65),
        ("Medium", 75, 75),
        ("High", 85, 85),
        ("Elite", 92, 90)
    ]
    contracts = [1, 3, 5]
    
    out_path = SIM_DIR / "comparative_wage_matrix.csv"
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow([
            "Rating", "Potential", "ClubTier", "ClubRating", "LeagueRating",
            "ContractYears", "SM25_WeeklyWage", "Our_WeeklyWage", "Diff_Pct"
        ])
        
        for r in ratings:
            p = min(99, r + 5)
            for tier_name, club_r, league_r in tiers:
                for c in contracts:
                    sm_w = sm25_wage(r, p, club_r, league_r, c)
                    our_w = our_game_wage(r, p, club_r, league_r, c)
                    diff_pct = round(((our_w - sm_w) / float(sm_w)) * 100.0, 1)
                    writer.writerow([
                        r, p, tier_name, club_r, league_r, c,
                        sm_w, our_w, diff_pct
                    ])
                    
    print(f"Generated {out_path}")

if __name__ == "__main__":
    run_valuation_sim()
    run_wage_sim()
