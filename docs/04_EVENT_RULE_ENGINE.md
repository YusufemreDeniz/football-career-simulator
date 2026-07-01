# Olay ve Kural Motoru

**Durum:** Taslak / Henüz çalışılmadı

## Belgenin Amacı

Bu belge, oyunun "Olay, Bağlam ve Sonuç Sistemi"nin nasıl çalışacağını; olayların nasıl tanımlanacağını, bağlam değişkenlerinin nasıl değerlendirileceğini, sonuçların nasıl üretileceğini ve olay zincirlerinin nasıl yönetileceğini tanımlamak için ayrılmıştır. Bu sistem, ana oyun tasarım belgesinde vurgulanan "doğrudan tablo güncellemesi değil, tanımlı olaylar ve kurallar üzerinden sonuç üretme" ilkesinin teknik temelini oluşturur.

## Bu Belgede İleride Cevaplanacak Sorular

- Bir olayın veri yapısı (tip, zaman, aktörler, önem, bağlam) tam olarak nasıl tanımlanacak?
- Bağlam değerlendirme kuralları nasıl ifade edilecek (kural tabanlı mı, ağırlıklı puanlama mı)?
- Olay zincirleri nasıl temsil edilecek ve bir zincirin hangi aşamada dallanacağı nasıl belirlenecek?
- Bir olayın hangi sistemleri tetikleyeceği merkezi mi yoksa dağıtık mı yönetilecek?
- Motorun performansı, uzun dönem simülasyonlarda (10+ sezon) nasıl garanti edilecek?

## Ana Referans

Bu belge, `docs/01_GAME_DESIGN_DOCUMENT.md` içindeki Bölüm 10 (Olay, Bağlam ve Sonuç Sistemi) ve Bölüm 31 (Örnek Sistem Etkileşim Matrisi) esas alınarak hazırlanacaktır.
