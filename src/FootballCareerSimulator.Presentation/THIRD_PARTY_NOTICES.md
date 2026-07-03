# Üçüncü Taraf Bildirimleri

Bu dosya, `docs/18_SPIKE_EXECUTION_PLAN.md` Kart 7 (Spike 5) kapsamında, dağıtılan Windows x64
paketiyle birlikte gelen üçüncü taraf bileşenlerin lisans bilgilerini içerir. Bu, projenin nihai
paketleme/lisans uyum sürecinin yerine geçmez; yalnızca "third-party license bildirimleri pakette
bulunur" başarı kriterini somut biçimde kanıtlayan bir başlangıç noktasıdır.

## Godot Engine

* **Bileşen:** Godot Engine çalışma zamanı (motor ikili dosyası ve `GodotSharp.dll`)
* **Sürüm:** 4.7-stable (mono/.NET)
* **Lisans:** MIT License
* **Kaynak:** <https://godotengine.org/license/>
* **Telif hakkı:** Copyright (c) 2014-present Godot Engine contributors, Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.

Godot Engine ayrıca kendi içine gömülü çok sayıda üçüncü taraf kütüphane (ör. Vulkan SDK bileşenleri,
FreeType, ICU, HarfBuzz, Graphite, zlib, libpng) içerir. Bunların tam listesi ve lisans metinleri
Godot Engine kaynak deposundaki `COPYRIGHT.txt` dosyasında yer alır: <https://github.com/godotengine/godot/blob/master/COPYRIGHT.txt>.

## .NET Runtime

* **Bileşen:** .NET çalışma zamanı ve temel kütüphaneler (bu pakete self-contained olarak gömülmüştür: `coreclr.dll`, `System.*.dll` ve ilgili dosyalar)
* **Lisans:** MIT License
* **Kaynak:** <https://github.com/dotnet/runtime/blob/main/LICENSE.TXT>
* **Telif hakkı:** Copyright (c) .NET Foundation and Contributors

## Football Career Simulator'ın kendi kodu

Bu proje kapsamında yazılan Domain/Simulation/Application/Presentation kodu, üçüncü taraf bir
bileşen değildir; proje sahibine aittir.

---

Kesin, otomatik üretilen ve tüm geçişli bağımlılıkları kapsayan bir üçüncü taraf bildirim süreci
(ör. `dotnet-project-licenses` gibi bir araçla) bu spike'ın kapsamı dışındadır; gerçek sürüm/dağıtım
süreci hazırlanırken ayrıca ele alınacaktır.
