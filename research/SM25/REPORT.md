# SM25 Temiz Oda Sistem Araştırma Raporu

**Güncelleme tarihi**: 2026-11-09 (kod–rapor uygunluk denetimi; ayrıntı en altta)

**Milestone**: Oyuncu Sözleşmesi Görüşme Sistemi
**İlke**: Tescilli kod kopyalanmadı; salt okunur yerel ARM64 incelemesinden davranış öğrenilip bağımsız ve yapılandırılabilir bir model tasarlandı.

---

## Yönetici Özeti

Önceki milestone'larda piyasa değeri, temel ücret referansı ve oyuncu gelişimi için kesinleşen sonuçlar korunmuştur. Bu milestone, açık kalan haftalık ücret zincirini sözleşme üretiminden görüşme kabulüne kadar tamamlamıştır.

En önemli sonuç şudur: SM25'te tek bir “nihai ücret hesaplayıcı” bütün durumları temsil etmez.

- AI/otomatik sözleşmede `PlayerContract.Generate`, hesaplanan değeri personel çarpanından ve parasal yuvarlamadan geçirerek `PlayerContract.Wage +0x18` alanına yazar.
- Kullanıcı sözleşme görüşmesinde hesaplanan değer `DemandedWages +0x24` kabul eşiğidir; kabul edilen teklif `ContractInfo.Wage +0x18` alanından yeni sözleşmenin `Wage +0x18` alanına kopyalanır.
- Mevcut ücret getter'ı yalnızca saklı tamsayıyı döndürür; yeniden hesaplama yapmaz.

## Kesinleşen Yerel Bulgular

| Konu | Sonuç | Güven |
|---|---|---|
| Mevcut ücret | `PlayerContract.get_Wage`, VA `0x02FBFB18`, `ldr w0,[x0,#0x18]` | KESİN |
| Otomatik üst seviye üretici | `PlayerContract.Generate`, VA `0x02FBFB30` | KESİN |
| Kompleks ücret üretici | `PlayerContract.GenerateNewContractWages(PlayerData,Club,double)`, VA `0x02FBFCD4` | KESİN |
| Sözleşme süresi | `1 + (yıl-1)/40`; 1-5 yıl için `1,000–1,100` | KESİN |
| Transfer menajer özelliği | Başka kulüpten oyuncuda `TraitId 38` | KESİN |
| Yenileme menajer özelliği | Kullanıcı kulübünün oyuncusunda `TraitId 36` | KESİN |
| Özellik yüzdesi | `original × (1 + yüzde/100)`, veri alanı `+0x24` | KESİN |
| Personel etkisi | `ModifierType.WageNegotiator = 9`; veri çarpanı `+0x18` ile doğrudan çarpım | KESİN |
| Asgari ücret | Kompleks üreticide `Math.Max(wage,50)` | KESİN |
| Üst tavan | Native zincirde global ücret tavanı bulunmadı | KESİN |
| Yuvarlama | Büyüklüğe bağlı basamak ve bankacı yuvarlaması | KESİN |
| Yeni sözleşme talebi | Kabul edilebilir ücret × tohumlu deterministik rastgele `[1,00,1,10]`, sonra parasal yuvarlama | KESİN |
| Görüşme ekranı beklentisi | Kompleks taban × tohumlu deterministik rastgele `[1,10,1,25]`, `100` adımına yuvarlama | KESİN |
| Kullanıcı görüşmesindeki nihai ücret | Kabul edilen teklifin `PlayerContract.Wage` alanına kopyası | KESİN |

## Girdi ve Bağımlılık Denetimi

Doğrudan kullanılanlar:

- Güncel veya sabit ücret reytingi.
- Oyuncunun zirve durumu ve potansiyeli.
- Hedef kulübün ortalama reytingi.
- Hedef kulübün lig reytingi.
- Sözleşme süresi.
- Hedefin kullanıcı kulübü olup olmadığı ve oyuncunun mevcut kulübü.
- `TraitId 36` veya `TraitId 38` seviye yüzdesi.
- `WageNegotiator` kaynak veri çarpanı.

Doğrudan kullanılmayanlar:

- Yaş, form, moral, oyuncu itibarı, kadro rolü, hırs, sadakat, açgözlülük ve profesyonellik.
- `PlayerData.get_Value` piyasa değeri.

Yaş, yalnız zirve/potansiyel durumunun başka sistemlerde belirlenmesi yoluyla dolaylı sonuç yaratabilir. Sadakat, ayrı `PlayerConcernContractWage` memnuniyet eşiğinde kullanılır; nihai üreticinin girdisi değildir. Bu negatif çağrı grafiği sonucu `YÜKSEK GÜVEN` seviyesindedir.

## Nihai Ücret Formülü

```text
Base = GetPlayerBasicValue(Rating, IsPeaked, Potential)

ClubAdjusted = Base
  × (0,5 + 0,5 × Rating / ClubAverageRating)
  × (ClubAverageRating / 94)
  × (LeagueRating / 89)

LengthAdjusted  = ClubAdjusted × (1 + (ContractYears - 1) / 40)
IntegerAdjusted = Trunc(LengthAdjusted)

TraitAdjusted = hedef kullanıcı kulübüyse
  max(Trunc(IntegerAdjusted × (1 + TraitPercentage / 100)), 0)
  değilse IntegerAdjusted

Floored       = max(TraitAdjusted, 50)
StaffAdjusted = hedef kullanıcı kulübüyse Floored × WageNegotiatorMultiplier
                değilse Floored

AutomaticFinalWeeklyWage = RoundMonetaryValue(StaffAdjusted)
```

Formülün işlemleri `KESİN`; `TraitId 36` ve `TraitId 38` `%−5`, `WageNegotiator` veri çarpanı `0,90` olarak asset kaynaklarından doğrulanmıştır.

## Oyuncu Sözleşmesi Görüşme Milestone'u

Etkileşimli state machine `OfferContractModal.SubmitContractOffer -> TransfersController.ConfirmContractOffer -> TransferBid.TryAcceptContractOffer -> ProcessContractResult` zinciridir. Nihai evaluator tek puan yerine ücret, kadro rolü ve süre kapılarını bu sırada çalıştırır. Sabır her tekliften önce `Current - 1 - rol - süre - ücret` formülüyle düşer; `Insulted(7)` veya sabrın sıfıra inmesi görüşmeyi kapatır.

`ContractInfo` yalnız expiry, wage, contract length, signing fee, demanded wage, negotiation count, squad status ve rejection reason taşır. Native etkileşimli zincirde performans/gol/clean-sheet/sadakat bonusu, agent fee, release clause veya opsiyon alanı yoktur. `SigningFee` saklanıp kabulde ödenir fakat acceptance kapısı değildir.

İmza parası `PlayerData.Value` tabanlıdır. Yenilemede oyuncu/kulüp reytingine göre seeded `[0,025–0,05]` veya `[0,05–0,10]`, serbest oyuncuda yaşa göre `0,10/0,25`, diğer transferde `0,10` uygulanır. Genç serbest oyuncu tazminatı 23 yaş ve altında `Value × 0,20`dir. Kullanıcı kulübünde `TransferNegotiator=8` çarpanı `0,90`; `TraitId 33` seviyeleri `%−5/%−10`dur.

Ayrıntılı alan haritası, enum'lar, sabır matrisleri ve state machine için `CONTRACT_NEGOTIATION.md` esas belgedir. Bulguların güven düzeyi aksi belirtilmedikçe `KESİN`dir.

## İki Kulüp Overload'unun Yeri

`WageModifierForClub(Club)`, tekil sözleşme ve görüşme zincirinde oyuncu/kulüp oranını kullanır:

```text
0,5 + 0,5 × PlayerRating / ClubAverageRating
```

`WageModifierForClub(PlayerContractHelper)`, toplu lig/AI üretiminde ters oran yönünü ve önceden hesaplanmış bağlamı kullanır:

```text
0,5 + 0,5 × Helper.TeamAverageRating / PlayerRating
```

Overload'lar birbirlerini çağırmaz.

## Yuvarlama ve Sınırlar

| Değer aralığı | Adım |
|---|---:|
| `< 250` | 10 |
| `< 1.000` | 25 |
| `< 5.000` | 50 |
| `< 10.000` | 100 |
| `< 50.000` | 500 |
| `< 100.000` | 1.000 |
| `< 5.000.000` | 5.000 |
| `< 10.000.000` | 100.000 |
| `< 50.000.000` | 500.000 |
| `>= 50.000.000` | 1.000.000 |

`5.000.000` kontrolünden sonra gelen `1.000.000` karşılaştırması gölgelenmiştir; bu nedenle `10.000` adımı erişilebilir değildir. Yuvarlama yarım durumda çifte, yani bankacı yöntemine gider.

## Temiz Oda Uygulaması

`PlayerWageModel` durumsuz, deterministik, UI/persistence bağımsız ve `PlayerWageBalanceProfile` ile yapılandırılabilirdir. Native zincirden bağımsız denge tercihleri şunlardır:

- `650.000` referans skala, üs `10`, zirvede olmayan oyuncuda `%85` CA ve `%15` PA.
- Kulüp/lig çarpanlarında güvenli alt ve üst sınırlar.
- Ayrı menajer özelliği yüzdesi, personel yüzdesi, kadro rolü ve pazar durumu girdileri.
- Haftalık `100` alt tabanı ve `1.000.000` üst güvenlik tavanı.
- Ham, yuvarlanmış ve sınırlandırılmış değerleri ayrı döndüren denetlenebilir kırılım.

`ScoutTransferValuationModel` içindeki eski kuadratik ücret tahmini kaldırılmış; önerilen haftalık ücret bu modele bağlanmıştır. Piyasa değeri modeliyle matematiksel bağımlılık kurulmamıştır.

## Doğrulama Durumu

- Native doğrulayıcılar: `VERIFY_DEMANDED_WAGES=PASS`, `VERIFY_CONTRACT_NEGOTIATION=PASS`, `VERIFY_WAGE_CHAIN=PASS`, `VERIFY_GET_PLAYER_BASIC_VALUE=PASS`.
- Odaklı görüşme ve demanded wage testleri: 38/38; toplam unit testler: 1217/1217 başarılı (2026-09-15). 2026-11-09 denetim sonrası: 1247/1247 hızlı test + 10 sezon soak 1/1.
- Simülasyonlar: `demanded_wage_matrix.csv` (108 senaryo), 15.576 valuation, 2.340 wage ve 288 contract negotiation satırı üretildi.
- Production Presentation build: 0 hata, 0 uyarı.
- Godot console career smoke: `CAREER_UI_SMOKE_TEST_RESULT=PASS`.
- `git diff --check`: başarılı.

## Demanded Wages Milestone Sonucu (Özet)

- `ContractInfo.DemandedWages (+0x24)` alanına yazan tüm 6 yerel adres tespit edildi (`0x030278F4`, `0x03027B74`, `0x03027DB0`, `0x0303A964`, `0x0303B2D4`, `0x030E0B94`).
- İlk görüşme talebi (`GetNegotiationWageForUserManager`): `BaseContractWage × [1.10, 1.25]`, Seed `PlayerId + Game.Seed`, en yakın 100'e yuvarlama (`Round(..., 100)`).
- Kabul eşiği (`GetAcceptableNewContractWage`): `PlayerContract.Generate` taban ücreti; eğer yenilemede oyuncu kalmak istemiyorsa (`!WillPlayerRenewContract`) `%50 ceza` (`wage + wage/2`).
- Ret sonrası yeni talep (`GetRequestedNewContractWage`): `AcceptableWage × [1.00, 1.10]`, Seed `PlayerId + ClubId`, `MonetaryHelpers.RoundMonetaryValue`.
- Sözleşme süresi çarpanı (`LengthMultiplier`): `1.0 + (length - 1) / 40.0` (Her ilave yıl için +%2.5).
- Menajer özellikleri: Transferde `Trait 38 HardBargain (-5%)`, yenilemede `Trait 36 ToughNegotiator (-5%)`.
- Asgari ücret tabanı: `Math.Max(wage, 50)` (£50 kesin native floor).
- Rejection stabilitesi: Aynı düşük teklif tekrar verilirse Seed `PlayerId + ClubId` sabit olduğu için talep sabit kalır; sözleşme süresi uzatılırsa talep artar.

## Açık Noktalar

- SM25 `RNG.GetRand` algoritmasının bit düzeyinde clean-room PRNG parity'si çıkarılmadı (davranışsal parity sağlandı).
- `OtherOffer(8)` rakip teklif seçiminin tüm alt öncelikleri ve `TookTooLong(9)` takvim/saat sınırı tam ayrıştırılmadı (kapsam dışı).
- Ayrı `CalculateAgentFee()` formülü acceptance state machine'in parçası olmadığı için transfer ekonomisi çalışmasına bırakıldı (kapsam dışı).

## 2026-11-09 Kod–Rapor Uygunluk Denetimi

Bu raporun ve `DESIGN_TRANSLATION.md` / `CONTRACT_NEGOTIATION.md` belgelerinin üretim koduna yansıması denetlendi. Bulunan sapmalar düzeltildi (karar kayıtları D-416–D-421):

| Konu | Denetimde bulunan | Düzeltme |
|---|---|---|
| Kabul eşiği (`DemandedWages`) | `Generate()` eşiği `[1,00–1,10]` rastgele çarpanla şişiriyordu | Eşik artık rastgele çarpansız kabul edilebilir ücret; isteksiz yenileme +%50 ve £50 taban uygulanıyor |
| Tohumlar | Ekran beklentisi `seed ^ 0x9E3779B9`, ret sonrası talep `PlayerId+Game.Seed` ile üretiliyordu | Ekran `PlayerId+Game.Seed`, ret sonrası `PlayerId+ClubId` (`RequestedWeeklyWage`) |
| Menajer özelliği | Aynı alan bir yerde yüzde (negatif=indirim), bir yerde oran (pozitif=indirim) olarak okunuyordu; indirim native profilde de uygulanıyordu | Tek birim: yüzde, negatif = indirim, yalnız kullanıcı kulübü; `Trunc` yuvarlaması. Beceri→indirim eşlemesi (`%0,3/puan`, en çok %15) simülatör kuralı olarak `PlayerContractProposalService` içinde |
| Üretim adaptörü | Yalnız evaluator bağlıydı; scout önerisi ile kabul eşiği farklı kulüp gücü/rol/zirve girdileri kullanıyordu | Ortak `PlayerWageModel.EvaluateContractBasis`; scout önerisi bu tabanın altına inmez; teklif süresi talebe yansır |
| Sabır | Her teklif yeni sabırla değerlendiriliyordu | Sabır kayıtlı reddedilmiş tekliflerden yeniden kurulur; tükenince görüşme kapanır |

`ContractInfo` alan haritası, sabır matrisleri, imza parası ve yuvarlama tablosu kodla birebir uyumlu bulundu.
