# SM25 Oyuncu Sözleşmesi Görüşme Sistemi

Bu belge, mevcut ücret milestone'unu tekrar çözmeden SM25'in oyuncu sözleşmesi görüşmesini girişten kabul veya kesin ret sonucuna kadar birleştirir. Native bulgular ile Football Career Simulator denge kararları ayrı tutulmuştur.

## 1. Sonuç Özeti

- Etkileşimli görüşme tek bir puan hesabı değildir. Sıralı boolean/enum kapıları `Wage -> SquadStatus -> ContractLength` biçimindedir. Güven: **KESİN**.
- Native `ContractInfo` içinde performans bonusu, gol bonusu, clean-sheet bonusu, sadakat bonusu, agent fee, release clause veya opsiyon alanı yoktur. Güven: **KESİN**.
- `SigningFee` teklifte saklanır ve kabulde ödenir; ancak `TryAcceptContractOffer` içinde kabul kapısı değildir. Güven: **KESİN**.
- Sabır teklif gönderilmeden hemen önce düşürülür. Hakaret sonucu veya `Current <= 0` görüşmeyi kapatır. Güven: **KESİN**.
- Ret sonrasında native bir counter-offer nesnesi oluşturulmaz. Talep yeniden hesaplanır, aynı modal açık kalır ve yeni teklifi kullanıcı verir. Güven: **YÜKSEK GÜVEN**.
- Rastgele sonuçlar sabit değildir. Tohum `PlayerId + Game.Seed` ile kurulduğu için aynı girdi ve aynı oyun tohumu altında **seeded deterministic random** davranıştır. Güven: **KESİN**.

## 2. Native Giriş Noktaları

| Metot | Native VA | Temel görev | Güven |
|---|---:|---|---|
| `ContractInfo.GetDefaultNewContractDemands(PlayerData)` | `0x0302792C` | Varsayılan süre, ücret eşiği ve beklenen rolü üretir. | KESİN |
| `OfferContractModal.SubmitContractOffer()` | `0x030E0C5C` | Sabır hesabı, `Negotiation` snapshot'ı ve teklif gönderimini başlatır. | KESİN |
| `Negotiation..ctor(PlayerData,ContractInfo,double)` | `0x03032E74` | Ekrandaki teklifin bağımsız snapshot'ını oluşturur. | KESİN |
| `TransfersController.ConfirmContractOffer(Negotiation)` | `0x03048E60` | `TransferBid`/`ContractInfo` bulur veya oluşturur ve snapshot'ı kopyalar. | KESİN |
| `ContractInfo.AddNegotiation(Negotiation)` | `0x03027CE8` | Tur sayısını artırır ve teklif alanlarını `ContractInfo`'ya taşır. | KESİN |
| `TransferBid.TryAcceptContractOffer()` | `0x0303B218` | Etkileşimli nihai karar kapılarını sırasıyla çalıştırır. | KESİN |
| `OfferContractModal.ProcessContractResult(...)` | `0x030E0F48` | Kabul, kesin ret veya modalda devam kararını verir. | KESİN |
| `TransferBid.RejectContractOffer(...)` | `0x0303A940` | Ret durumunu ve yeniden üretilen talebi kaydeder. | KESİN |
| `TransferBid.AcceptContractOffer()` | `0x0303AAA4` | Transfer kabul nesnesini kurar. | YÜKSEK GÜVEN |
| `PlayerData.AcceptContractOffer(TransferBid)` | `0x02FC2C70` | Yenilemede mevcut kontratı günceller ve imza parasını öder. | KESİN |
| `PlayerContract.CreateNew(ContractInfo)` | `0x02FC01EC` | Transferde kabul edilen expiry/wage alanlarını gerçek kontrata kopyalar. | KESİN |

`TransfersController.ConfirmContractOffer` için Cpp2IL metot başlangıcı `0x03048E60` olarak doğrulandı; daha önce yalnız gövde içindeki `0x03048F10` adresini giriş gibi kullanmak yanlıştı.

## 3. ContractInfo ve Negotiation Yerleşimi

### 3.1 ContractInfo

`ContractInfo : PlayerContract`:

| Alan | Tür | Ofset | Yazan | Okuyan | Güven |
|---|---|---:|---|---|---|
| `ExpiryDate` | `DateTime` | `+0x10` | `AddNegotiation` | kabul/kontrat üretimi | KESİN |
| `Wage` | `int` | `+0x18` | `AddNegotiation` | `WillAcceptWage`, `CreateNew` | KESİN |
| `ContractLength` | `byte` | `+0x1C` | varsayılan üretim, `AddNegotiation` | ücret/süre değerlendirmesi | KESİN |
| `SigningFee` | `int` | `+0x20` | modal, `AddNegotiation` | kabul sonrası ödeme/analitik | KESİN |
| `DemandedWages` | `int` | `+0x24` | `RecalculateContract` (`0x03027B74`), `GetNegotiationWageForUserManager` (`0x03027DB0`), `RejectContractOffer` (`0x0303A964`), `TryAcceptContractOffer` (`0x0303B2D4`) | `WillAcceptWage` (`0x03027CA8`) | KESİN |
| `NegotiationCount` | `short` | `+0x28` | `AddNegotiation`, ret yolu | unmanaged görüşme sınırı | KESİN |
| `SquadStatus` | `ePlayerSquadStatus` | `+0x2C` | varsayılan üretim, `AddNegotiation` | rol kabulü | KESİN |
| `RejectionReason` | `eContractRejectReason` | `+0x30` | karar zinciri | UI/transfer akışı | KESİN |

Doğrudan accessor VA'ları sırasıyla `0x030278CC..0x03027924` aralığındadır.

### 3.2 Negotiation snapshot

| Alan | Tür | Ofset |
|---|---|---:|
| `PlayerID` | `int` | `+0x10` |
| `SquadStatus` | `ePlayerSquadStatus` | `+0x14` |
| `WagesPerWeek` | `int` | `+0x18` |
| `SigningFee` | `int` | `+0x1C` |
| `ContractLength` | `byte/int kaynaklı` | `+0x20` |
| `ContractExpiry` | `DateTime` | `+0x28` |

`AddNegotiation`, önce `ContractInfo.NegotiationCount` değerini artırır; ardından wage, signing fee, length, expiry ve squad status değerlerini aynı nesneye kopyalar. `TryAcceptContractOffer` normal/hakaret/süre/rol ret dalında bu sayacı bir kez daha artırır. Bu nedenle kabul edilen gönderim sayaçta `+1`, reddedilen gönderim `+2` üretir. Ayrı `TransferBid.NegotiationCount +0x54` alanı bulunmakla birlikte bu etkileşimli artışın hedefi `ContractInfo +0x28`dir. Native yapıda ayrıca bonus alanı yoktur. Güven: **KESİN**.

## 4. Gerçek Durum Makinesi

```text
OfferContractModal.Populate
  -> GetDefaultNewContractDemands
  -> CalculateTotalSigningFee
  -> new PlayerPatience

SubmitContractOffer
  -> PlayerPatience.CalculatePatience         [tekliften önce]
  -> new Negotiation                          [snapshot]
  -> TransfersController.ConfirmContractOffer
       -> mevcut TransferBid/ContractInfo'yu bul veya oluştur
       -> ContractInfo.AddNegotiation
  -> TransferBid.TryAcceptContractOffer
       -> free-agent rakip teklif kontrolü -> OtherOffer(8)
       -> DemandedWages = GetAcceptableNewContractWage(...)
       -> WillAcceptWage
       -> WillAcceptSquadStatus
       -> WillAcceptContractLength
       -> hepsi kabul: ManagedClubContractAccepted
       -> aksi halde NegotiationCount++
       -> GetRequestedNewContractWage(..., true)
       -> RejectContractOffer
  -> ProcessContractResult
       -> Accepted(10): AcceptContract
       -> Insulted(7) veya CurrentPatience <= 0: RefuseContract
       -> diğer ret: modal açık, kullanıcı yeni teklif verebilir

AcceptContract
  -> yenileme: PlayerData.AcceptContractOffer
       -> ExpiryDate ve Wage mevcut PlayerContract'a kopyalanır
       -> SigningFee ödenir
  -> transfer: TransferBid.AcceptContractOffer -> TransferPlayer
       -> Club.SignedPlayer -> PlayerContract.CreateNew(ContractInfo)
```

Ücretsiz oyuncu, transfer ve yenileme aynı teklif kapılarını kullanır; ayrım demand/signing-fee üretiminde, rakip teklif kontrolünde ve kabul sonrası kontrata dönüşüm yolundadır.

## 5. Acceptance ve Ret Nedenleri

### 5.1 Wage kapısı

```text
if OfferedWage <= DemandedWages / 2:
    Insulted(7)
else:
    RequiredWage = !WillPlayerRenewContract ? 2 * DemandedWages : DemandedWages
    if OfferedWage < RequiredWage:
        Wages(2)
    else:
        Accepted(10)
```

`isTransferWanted` parametresi, kullanıcı kulübündeki oyuncu `WillPlayerRenewContract()` sonucu false ise etkinleşir; oyuncuyu tutmak için iki kat eşik gerekir. Tam yarı ücret `FCMP/B.GE` dalı nedeniyle hakaret sınırına dahildir. Güven: **KESİN**.

Unmanaged `ContractInfo.ConsiderWage()` yolu farklıdır: talep üstü kabul, yarı veya altı hakaret, dördüncü ve sonraki uygun olmayan turda `Negotiations(6)`, diğer durumda `Wages(2)`.

### 5.2 Squad status kapısı

Enum:

| Değer | Ad |
|---:|---|
| 0 | `None` |
| 1 | `KeyPlayer` |
| 2 | `FirstTeam` |
| 3 | `Rotation` |
| 4 | `Backup` |
| 5 | `HotProspect` |
| 6 | `Youngster` |
| 7 | `NotNeeded` |

Teklif edilen rol beklenen role eşit veya daha iyi ise kabul edilir. Tam bir kademe düşük rolde `IsPlayerHappyWithReducedSquadStatus` özel kontrolü kabulü kurtarabilir. Daha düşük sonuç `SquadStatus(4)` üretir. Rol, wage üreticisinin girdisi değildir; acceptance ve patience sistemlerinin ayrı girdisidir. Güven: **KESİN**.

### 5.3 Contract length kapısı

```text
if OfferedYears < MinimumContractOfferLength: ContractTooShort(1)
else if OfferedYears > (Age < 32 ? 5 : 3): ContractTooLong(0)
else Accepted(10)
```

### 5.4 Ret enum'u

| Değer | Sonuç | Etkileşimli evaluator kullanımı |
|---:|---|---|
| 0 | `ContractTooLong` | Evet |
| 1 | `ContractTooShort` | Evet |
| 2 | `Wages` | Evet |
| 3 | `Bonuses` | Hayır |
| 4 | `SquadStatus` | Evet |
| 5 | `SigningFee` | Hayır |
| 6 | `Negotiations` | Yalnız unmanaged `ConsiderWage` |
| 7 | `Insulted` | Evet, kesin kapatma |
| 8 | `OtherOffer` | Evet, free-agent rakip teklif dalı |
| 9 | `TookTooLong` | Ayrı timeout akışı |
| 10 | `Accepted` | Evet |

## 6. Sabır ve Tur Sistemi

`PlayerPatience` alanları:

| Alan | Ofset |
|---|---:|
| `Start` | `+0x10` |
| `Current` | `+0x18` |
| `SquadStatusData` | `+0x20` |
| `ContractLengthData` | `+0x28` |
| `DesiredStatus` | `+0x30` |
| rol cezası | `+0x38` |
| istenen süre | `+0x40` |
| süre cezası | `+0x48` |
| istenen ücret | `+0x50` |
| ücret cezası | `+0x58` |

Başlangıç:

```text
oyuncu ayrılmak istiyorsa 0
aktif concern varsa         2
aksi halde                  5
```

Her gönderimde sabır için tek tur ilerler; native `ContractInfo.NegotiationCount` ise kabulde `+1`, rette `+2` ilerler:

```text
Current = Current - 1 - SquadStatusPenalty - ContractLengthPenalty - WagePenalty
```

Ücret cezası:

| Offered / Demanded | Ceza |
|---:|---:|
| `>= 1,00` | `0` |
| `>= 0,75` | `1` |
| `>= 0,50` | `1,5` |
| `< 0,50` | `3` |

Süre matrisi satırda istenen 1..5, sütunda teklif 1..5 olacak biçimde:

```text
[0,   0.5, 1,   1.5, 1.5]
[0,   0,   0.5, 1,   1.5]
[1,   0.5, 0,   0.5, 1  ]
[1.5, 1,   0.5, 0,   0.5]
[1.5, 1.5, 1,   0.5, 0  ]
```

İkinci satır/birinci sütunun asset'teki metni `OneYears: 0.5`, sınıf alanı ise `OneYear`dır. JSON eşleşmediğinden native deserialization sonucu varsayılan `0` kullanılır. Bu davranış temiz oda native-parity profilinde korunmuştur.

Rol matrisi satır/sütun sırası `KeyPlayer, FirstTeam, Rotation, Backup, HotProspect, Youngster`:

```text
[0, 1, 1.5, 5,   5,   5  ]
[0, 0, 1,   1.5, 5,   5  ]
[0, 0, 0,   1,   1.5, 5  ]
[0, 0, 0,   0,   1,   1.5]
[0, 0, 0,   0,   0,   1  ]
[0, 0, 0,   0,   0,   0  ]
```

Sabır düşüşünde RNG, personality veya agent etkisi bulunmadı. Güven: **KESİN**.

## 7. Signing-on Fee

Kaynak: `PlayerData.CalculatePlayerSigningFee(short)` VA `0x02FC3438`.

```text
Value = PlayerData.Value

if renewal:
    factor = PlayerRating > ClubAverageRating
        ? SeededRand(0.05, 0.10)
        : SeededRand(0.025, 0.05)
else if free agent:
    factor = Age < 23 ? 0.10 : 0.25
else:
    factor = 0.10

PlayerFee = RoundMonetaryValue(Value * factor)

if target is current user club:
    PlayerFee = RoundMonetaryValue(PlayerFee * TransferNegotiator[8])
```

`TransferNegotiator[8]` asset değeri `0,90`dır.

Genç oyuncu tazminatı, yalnız mevcut kulüp kimliği `-1` olan free agent ve `Age <= 23` iken:

```text
CompensationFee = RoundMonetaryValue(Value * 0.20)
```

Burada bilinçli/native bir sınır farkı vardır: player fee için `Age < 23`, compensation için `Age <= 23`.

Toplam:

```text
Total = PlayerFee + CompensationFee
if target is current user club and TraitId 33 is active:
    Total = max(Total * (1 + TraitPercentage / 100), 0)
SigningFee = FCVTZS(Total)  // modal ContractInfo int alanına aktarırken sıfıra doğru kesme
```

`TraitId 33 EagerToPlay/SigningBonus`: seviye 1 `%−5`, seviye 2 `%−10`. İmza parası acceptance kapısı değil, kabul sonrası ödeme girdisidir. Global imza parası tavanı bulunmadı. Güven: **KESİN**.

## 8. Bonus ve Agent Fee Sınırı

Native `ContractInfo`, `Negotiation`, `AddNegotiation` ve `TryAcceptContractOffer` zincirinde appearance, goal, clean-sheet, loyalty veya agent bonusu alanı/kapısı yoktur. `eContractRejectReason.Bonuses` değeri mevcut olsa da bu etkileşimli akış tarafından üretilmez. Pozisyona bağlı bonus branch'i de bu state machine'de yoktur. Güven: **KESİN**.

`CalculateAgentFee()` ayrı transfer/UI/analitik hesabıdır. Son teklif snapshot'ına, `ContractInfo`'ya ve kabul edilen `PlayerContract`'a kopyalanmaz; dolayısıyla bu milestone'un kontrat kabul formülüne eklenmemiştir. Güven: **YÜKSEK GÜVEN**.

## 9. Counter-offer Davranışı

Ret halinde `TransferBid.NegotiationCount` artar ve `GetRequestedNewContractWage(club,length,true)` yeniden çalışır. Sonuç ret mesajı/demand alanına yazılır; yeni `Negotiation` veya ayrı bir player counter-offer nesnesi üretilmez. Normal rette modal açık kalır. Kullanıcının sonraki gönderimi yeni snapshot olarak aynı `ContractInfo` üzerine yazılır.

Aynı oyuncu, aynı `Game.Seed` ve aynı diğer girdilerle yeniden hesap aynı sonucu verir; bu **seeded deterministic random** davranıştır. Oyun tohumu değişirse aralık içinde farklı sonuç oluşabilir. “Constant” değildir.

## 10. RNG, Trait ve Staff Verisi

- RNG tohum bileşeni `Game +0x48`, metadata ile `Game.<Seed>k__BackingField` olarak kesinleşti. Formül `PlayerId + Game.Seed`dir.
- Trait 36 `ToughNegotiator`: tek seviye, `%−5`.
- Trait 38 `HardBargain`: tek seviye, `%−5`.
- Trait 33 `EagerToPlay/SigningBonus`: `%−5`, `%−10`.
- `ModifierType.WageNegotiator = 9`: `ValueModifiersJson` indeks 9, doğrudan çarpan `0,90`.
- `ModifierType.TransferNegotiator = 8`: indeks 8, doğrudan çarpan `0,90`.

Asset kaynakları ve SHA-256 kilitleri `verify_contract_negotiation.py` içinde doğrulanmaktadır. Güven: **KESİN**.

## 11. Football Career Simulator Tasarım Uyarlaması

### SM25 Native Davranış

- `ContractOfferEvaluator` varsayılan `Sm25NativeParity` profilinde wage, role ve length kapılarını uygular.
- `ContractNegotiationState` sabrı teklif öncesi native sırayla düşürür.
- `PlayerContractDemandModel`, aynı player/game seed ile tekrar üretilebilir talep ve imza parası kırılımı verir.
- Native'de bulunmayan bonus/signing-fee acceptance kapıları varsayılanda kapalıdır.

### Football Career Simulator Tasarım Uyarlaması

- `SimulatorBalance` profili imza parası ve açıkça `SimulatorBonus` adı verilen opsiyonel bonus eşiğini etkinleştirir.
- Bu iki sonuç `SigningFee(5)` ve `Bonuses(3)` enum değerlerini kullanır; native parity iddiası taşımaz.
- Mevcut `PlayerContractProposalService` persistence/UI akışı değiştirilmedi. Yeni evaluator bağımsız application modeli olarak eklendi; ileride adaptörle bu servise bağlanabilir.
- `PlayerWageBalanceProfile` içindeki haftalık `100` alt tabanı ve `1.000.000` üst tavan simülatör denge kuralıdır. Native minimum `50`, native global maksimum ise yoktur.

## 12. Doğrulama Varlıkları

- Native verifier: `research/SM25/scripts/verify_contract_negotiation.py`
- Unit/invariant testleri: `tests/FootballCareerSimulator.Tests/Transfer/ContractNegotiationModelTests.cs`
- Kontrollü grid: `research/SM25/simulations/contract_negotiation_matrix.csv` (`288` satır)
- Clean-room model: `src/FootballCareerSimulator.Application/Transfer/Services/ContractNegotiationModel.cs`

## 13. Açık Noktalar

- Clean-room RNG, seeded deterministic sözleşmeyi sağlar; SM25 `RNG.GetRand` bit-akışının birebir algoritmik klonu değildir. Exact PRNG parity **AÇIK**.
- `OtherOffer(8)` dalındaki rakip teklif seçiminin tüm öncelik/karşılaştırma alt formülü bu milestone'da tam ayrıştırılmadı. **AÇIK**.
- `TookTooLong(9)` ayrı timeout akışının tam takvim/saat sınırı çıkarılmadı. **AÇIK**.
- `CalculateAgentFee()` formülü bu state machine'e gate olmadığı için ayrı transfer ekonomisi milestone'una bırakıldı. **AÇIK**.
