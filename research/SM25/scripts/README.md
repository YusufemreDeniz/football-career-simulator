# SM25 Research Scripts

Run commands from the repository root.

```powershell
python research\SM25\scripts\inventory.py
python research\SM25\scripts\elf_utils.py research\SM25\il2cpp\libil2cpp.so segments
python research\SM25\scripts\elf_utils.py research\SM25\il2cpp\libil2cpp.so va-to-raw 0x02FC3FBC
python research\SM25\scripts\read_constant.py research\SM25\il2cpp\libil2cpp.so 0x013325B8 float64 --mode va
python research\SM25\scripts\extract_method.py research\SM25\isil\IsilDump\Assembly-CSharp\PlayerData.txt GetPlayerBasicValue
python research\SM25\scripts\arm64_disassemble.py research\SM25\il2cpp\libil2cpp.so 0x02FC3FBC 0x14C
python research\SM25\scripts\verify_get_player_basic_value.py
dotnet run --project research\SM25\scripts\ValuationSimulation\ValuationSimulation.csproj --configuration Release -- research\SM25\simulations\player_valuation_matrix.csv
```

`arm64_disassemble.py` loads Capstone from `research/SM25/tools/python_deps_local` when present. Install the pinned version locally with:

```powershell
python -m pip install --target research\SM25\tools\python_deps_local capstone==5.0.6
```

The ELF and constant utilities reject addresses outside file-backed `PT_LOAD` ranges. Always state whether a constant-reader address is `--mode va` or `--mode raw`.
