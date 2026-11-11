# Açık Sorular

## Oyuncu Sözleşmesi Görüşme Milestone'undan Kalanlar

- SM25 `RNG.GetRand` bit-akışının clean-room exact parity algoritması nedir?
- Free-agent `OtherOffer(8)` kararında tüm rakip teklif öncelikleri nasıl sıralanır?
- `TookTooLong(9)` için kesin takvim/saat sınırı nedir?
- State machine'e gate olmayan ayrı `CalculateAgentFee()` formülünün tam transfer ekonomisi zinciri nedir?

Bu maddeler `AÇIK`tır; doğrulanan görüşme state machine'i ve clean-room model için engel değildir.

## Çözülen ve Doğrulanan Ücret Konuları

- `PlayerContract.Wage +0x18` mevcut saklı ücrettir; getter yeniden hesaplama yapmaz.
- Otomatik nihai üretici `PlayerContract.Generate`, VA `0x02FBFB30` olarak doğrulandı.
- Tekil sözleşme zincirinde `WageModifierForClub(Club)`, toplu lig/AI üretiminde `WageModifierForClub(PlayerContractHelper)` kullanılır; oran yönleri farklıdır.
- Sözleşme süresi çarpanı `1 + (yıl-1)/40` formülüdür.
- Transferde `TraitId 38`, yenilemede `TraitId 36` kullanılır; ikisi aynı hesapta birlikte uygulanmaz.
- Menajer özelliği `original × (1 + yüzde/100)` şeklinde uygulanır.
- `WageNegotiator = 9` kaynak veri çarpanını doğrudan ücretle çarpar.
- `TraitId 36` ve `TraitId 38` tek seviyede `%−5`; `WageNegotiator = 9` çarpanı `0,90`dır.
- RNG tohumu `PlayerId + Game.Seed`dir; `Game.Seed` alanı `+0x48` ofsetindedir.
- Kompleks üreticide haftalık `£50` tabanı vardır; native global üst tavan bulunmaz.
- Parasal yuvarlama büyüklüğe bağlı basamaklarla bankacı yöntemidir.
- Yeni sözleşme talebi seeded deterministic random `[1,00,1,10]`; görüşme ekranı beklentisi seeded deterministic random `[1,10,1,25]` aralığındadır; ikisi constant değildir.
- Kullanıcı görüşmesindeki final haftalık ücret, kabul edilen `ContractInfo.Wage` teklifinin `PlayerContract.Wage` alanına kopyasıdır.
- Oyuncu kişiliği, form, moral, yaş, itibar ve kadro rolü final üreticide doğrudan okunmaz. Sadakat yalnız ayrı ücret memnuniyeti/endişesi hesabında kullanılır.

## Sonraki Araştırma Adayları

- Satıcı kulübün bonservis talep zinciri: sözleşme süresi, transfer listesi ve transfer talebi çarpanları.
- Agent fee, transfer compensation ve rakip free-agent tekliflerinin tam ekonomi zinciri.
- `ClubTransferAI` teklif üst sınırı, bütçe ve mevki önceliği.
- Yetiştirme tazminatı ve kiralık satın alma opsiyonu formülleri.
