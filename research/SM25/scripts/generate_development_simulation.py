#!/usr/bin/env python3
"""Generate comparative player development simulation CSV for SM25 vs Clean-Room Model."""

import csv
import math
from pathlib import Path

SIM_DIR = Path(__file__).resolve().parent.parent / "simulations"
SIM_DIR.mkdir(parents=True, exist_ok=True)

def sm25_sim_development(start_age, start_rating, potential, is_gk=False, longevity=True, seasons=1):
    # SM25 parameters
    pot_age = 24 if potential < 90 else 25
    if potential >= 95: pot_age = 26
    
    base_decline = 31 if potential >= 90 else 29
    if is_gk: base_decline += 1
    decline_age = max(base_decline, pot_age + 2, start_age)
    
    current_age = start_age + seasons
    
    # 1. Growth phase
    if current_age <= pot_age:
        # SinWave progress
        total_growth_years = max(1, pot_age - 16)
        elapsed_years = max(0, current_age - 16)
        progress = min(1.0, elapsed_years / float(total_growth_years))
        wave = math.sin(progress * 90.0 * (math.pi / 180.0))
        rating = start_rating + (potential - start_rating) * wave
    # 2. Peak plateau phase
    elif current_age < decline_age:
        rating = float(potential)
    # 3. Decline phase
    else:
        loss_rate = 1.0 if longevity else 1.5
        years_in_decline = current_age - decline_age
        rating = max(60.0, float(potential) - years_in_decline * loss_rate)
        
    return round(rating, 1)

def our_clean_room_development(start_age, start_rating, potential, is_gk=False, seasons=1):
    # Independent clean-room development curve
    peak_start = 28 if is_gk else 25
    peak_end = 34 if is_gk else 30
    
    current_age = start_age + seasons
    
    if current_age < peak_start:
        # Smooth growth
        progress = (current_age - 16.0) / (peak_start - 16.0)
        progress = max(0.0, min(1.0, progress))
        # Logistic / power blend
        growth_factor = 1.0 - math.pow(1.0 - progress, 2.2)
        rating = start_rating + (potential - start_rating) * growth_factor
    elif current_age <= peak_end:
        rating = float(potential)
    else:
        # Progressive physical decline
        decline_progress = (current_age - peak_end) / 10.0
        decline_rate = 1.2 if not is_gk else 0.8
        rating = max(55.0, potential - (math.pow(decline_progress, 1.3) * 15.0 * decline_rate))
        
    return round(rating, 1)

def run():
    out_path = SIM_DIR / "comparative_development_cohort.csv"
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow([
            "InitialAge", "StartRating", "Potential", "IsGK", "HorizonSeasons",
            "SimulatedAge", "SM25_ProjectedRating", "Our_ProjectedRating", "Diff"
        ])
        
        ages = [16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36]
        pairs = [(60, 75), (70, 85), (80, 92), (85, 95), (90, 99)]
        horizons = [1, 3, 5, 10]
        
        for a in ages:
            for r, p in pairs:
                for is_gk in [False, True]:
                    for h in horizons:
                        sm_r = sm25_sim_development(a, r, p, is_gk=is_gk, seasons=h)
                        our_r = our_clean_room_development(a, r, p, is_gk=is_gk, seasons=h)
                        diff = round(our_r - sm_r, 1)
                        writer.writerow([a, r, p, is_gk, h, a + h, sm_r, our_r, diff])
                        
    print(f"Generated {out_path}")

if __name__ == "__main__":
    run()
