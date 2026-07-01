# Karar Günlüğü

**Durum:** Yaşayan belge / güncellemeye açık

## Belgenin Amacı

Bu belge, proje boyunca alınan her önemli teknik veya tasarımsal kararı; gerekçesiyle, durumuyla ve etkilediği sistemlerle birlikte kalıcı olarak kayıt altına almak için kullanılır. Ana oyun tasarım belgesindeki Kural 10 gereği, hiçbir önemli karar sessizce varsayılmamalı; burada açıkça belgelenmelidir.

## Ana Referans

Bu belge, `docs/01_GAME_DESIGN_DOCUMENT.md` içindeki Bölüm 30 (Değişmez Proje Kuralları) ve Bölüm 37 (Belge Değişiklik Politikası) ile uyumlu şekilde işletilir.

## Karar Tablosu

| ID | Tarih | Karar | Durum | Gerekçe | Etkilenen Sistemler |
|---|---|---|---|---|---|
| D-001 | 2026-07-01 | Ana oyun tasarım belgesi (`01_GAME_DESIGN_DOCUMENT.md`) projenin temel referansıdır. | Kabul edildi | Tüm tasarım ve teknik kararların tutarlı bir başlangıç noktasına ihtiyacı vardır. | Tüm sistemler |
| D-002 | 2026-07-01 | İlk oynanabilir sürüm (MVP) teknik direktör kariyerine odaklanacaktır. | Kabul edildi | Ana belge Bölüm 27, MVP kapsamını teknik direktör kariyeriyle sınırlandırmaktadır; futbolcu kariyeri MVP dışıdır. | Teknik Direktör Kariyeri, Futbolcu Kariyeri, MVP Kapsamı |
| D-003 | 2026-07-01 | Henüz oyun motoru ve teknoloji (dil, framework, veritabanı) seçilmemiştir. | Kabul edildi | Teknoloji seçimi, domain modeli ve MVP kapsamı netleşmeden yapılmamalıdır (Bölüm 36 – Geliştirme Yaklaşımı). | Teknik Mimari |
| D-004 | 2026-07-01 | Henüz üretim kodu yazılmayacaktır. | Kabul edildi | Bu aşamanın amacı yalnızca anlama, dokümantasyon ve planlamadır. | Tüm sistemler |
| D-005 | 2026-07-01 | Sistemler arası etkiler tanımlanmadan özellik geliştirilmeyecektir. | Kabul edildi | Ana belge Kural 1 ve Kural 2 ile uyumlu; olaylar doğrudan tablo manipülasyonuyla değil, tanımlı olaylar ve kurallar üzerinden aktarılmalıdır. | Tüm sistemler |
