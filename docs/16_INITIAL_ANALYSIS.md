# Başlangıç Analizi

**Durum:** Tamamlandı (ilk sürüm) — güncellemeye açık

## Belgenin Amacı

Bu belge, `01_GAME_DESIGN_DOCUMENT.md` ana oyun tasarım belgesinin baştan sona okunup analiz edilmesi sonucunda hazırlanmış, projeyi kodlamaya başlamadan önce anlamak ve sonraki planlama aşamalarına hazırlamak amacıyla oluşturulmuş kapsamlı bir başlangıç değerlendirmesidir. Burada yer alan hiçbir madde, ana belgede açıkça yazılmayan bir tasarım kararını kesinleşmiş gibi sunmaz; belirsiz noktalar "açık soru" veya "risk" olarak işaretlenmiştir.

---

## 1. Projenin Özeti

Proje; futbolcu veya teknik direktör olarak başlanabilen, saha içi ve saha dışı kararların yıllarca sonuç ürettiği, dünyanın oyuncunun eylemlerinden bağımsız olarak da ilerlediği, uzun vadeli bir futbol kariyeri ve yaşam simülasyonudur. Oyunun çözmeye çalıştığı temel problem, mevcut futbol menajerlik/kariyer oyunlarının birkaç sezon sonra öngörülebilir ve tekrar eden bir döngüye dönüşmesidir. Bu oyun; dünyanın oyuncunun kararlarını, ilişkilerini, sözlerini ve geçmişini hatırlayıp bunları gelecekte yeniden anlamlı sonuçlara dönüştürmesini merkezi vaat olarak konumlandırır.

İlk oynanabilir sürüm (MVP) yalnızca teknik direktör kariyerine odaklanacak; futbolcu kariyeri, gelişmiş kişisel ekonomi, ayrıntılı saha dışı yaşam ve üç boyutlu maç motoru gibi geniş kapsamlı sistemler sonraki aşamalara bırakılacaktır.

## 2. Oyunun Temel Farklılaştırıcıları

- **Dünya hafızası:** Olaylar yalnızca bir geçmiş kaydı değil, aktörlerin gelecekteki kararlarını etkileyen aktif girdilerdir.
- **Söz sistemi:** Verilen sözlerin tutulup tutulmaması, hafızaya kalıcı olarak işlenen ve ilişkileri/itibarı etkileyen bir mekanizmadır.
- **Çok boyutlu ilişkiler:** İlişkiler tek bir skaler puan değil; güven, saygı, yakınlık, korku, sadakat gibi birbirinden bağımsız boyutlardan oluşur.
- **Kontrollü rastlantısallık:** Sonuçlar tamamen deterministik değil, ama tamamen rastgele de değildir; kişilik ve bağlam olasılıkları yönlendirir.
- **Yavaş açılan derinlik:** Sistemlerin tamamı ilk sezonlarda tüketilmez; kariyer ilerledikçe yeni sorumluluk ve içerik katmanları açılır.
- **Sistemik oynanış:** Hemen her özellik (örn. gece hayatı) birden fazla sistemi (yorgunluk, ilişki, medya, transfer) aynı anda etkiler; izole "kozmetik" özellikler hedeflenmez.
- **Belirsizlik ve eksik bilgi:** Oyuncu, potansiyel, sadakat, menajer niyeti gibi bazı bilgilere doğrudan değil, zaman ve gözlem yoluyla erişir.

## 3. Ana Sistemler

Ana belgede tanımlanan başlıca sistemler:

1. Dünya simülasyonu (aktörler, kulüp yaşam döngüsü, dünyanın uzun vadeli dönüşümü)
2. Olay, bağlam ve sonuç sistemi (olay zincirleri dahil)
3. Hafıza sistemi ve söz sistemi
4. İlişki sistemi ve kişilik/motivasyon sistemi
5. Kulüp kültürü
6. Diyalog sistemi
7. Transfer ve sözleşme sistemi (oyuncu menajerleri, pazarlık)
8. Maç ve taktik sistemi
9. Futbolcu gelişimi (potansiyel, yaşlanma/düşüş)
10. Saha dışı yaşam
11. Finans ve kişisel ekonomi
12. Medya, taraftar ve itibar sistemi
13. Rekabet ve husumet sistemi
14. Kariyer sonrası yaşam
15. Kayıt ve dünya bütünlüğü sistemi

## 4. Sistem Bağımlılıkları

Ana belge, sistemlerin izole özellikler değil birbirini etkileyen bir ağ olduğunu açıkça vurgular (Bölüm 5.4, Bölüm 31 — Örnek Sistem Etkileşim Matrisi). Tespit edilen başlıca bağımlılıklar:

- **Olay/Kural Motoru**, neredeyse tüm diğer sistemlerin (hafıza, ilişki, medya, transfer, moral, maç performansı) ortak entegrasyon noktasıdır; diğer sistemler sonuçlarını bu motor üzerinden üretir.
- **Hafıza ve Söz Sistemi**, ilişki sisteminin girdisidir (güven/saygı değişimleri geçmiş olaylara dayanır) ve diyalog sisteminin bağlamını besler.
- **İlişki ve Kişilik Sistemi**, diyalog sonuçlarını, transfer kararlarını, takım içi grupları ve maç performansını (takım uyumu) etkiler.
- **Kulüp Kültürü**, teknik direktör kararlarının (örn. genç oyuncuya şans verme) yönetim güveni, taraftar desteği ve medya yorumları üzerindeki etkisini belirler.
- **Diyalog Sistemi**, söz sistemini tetikler, ilişkiyi değiştirir ve medya anlatısına girdi sağlar.
- **Transfer Sistemi**, taraftar/rekabet/medya sistemlerini besler; menajer ilişkisi diğer oyuncu görüşmelerini de etkiler.
- **Maç Sistemi**, moral, itibar, gelişim, hafıza ve transfer değerini aynı anda etkiler.
- **Medya ve Taraftar Sistemi**, hafızayı yeniden tetikleyebilir (eski bir olayı gündeme taşıma) ve itibarı değiştirir.
- **Kayıt Sistemi**, tüm yukarıdaki sistemlerin ürettiği büyüyen veri hacmini (özellikle hafıza ve ilişki verisi) uzun vadede taşımak zorundadır; bu nedenle diğer tüm sistemlerin veri modeliyle dolaylı olarak bağımlıdır.

Bu bağımlılık ağı, hiçbir sistemin diğerlerinden tamamen izole tasarlanamayacağı anlamına gelir; bu da Kural 1 ve Kural 3'ün (etkiler tanımlanmadan kodlama yapılmayacak, çok sayıda sistem paralel açılmayacak) doğrudan gerekçesidir.

## 5. Kritik Olay Zincirleri

Ana belgede örneklenen olay zinciri (Bölüm 10.4 ve Bölüm 32), sistemler arası etkileşimi somutlaştırır:

> Forma süresi talebi → söz verilmesi → sözün tutulmaması → hafızaya kayıt → güven kaybı → menajer müdahalesi → medya sızıntısı → takım içi görüş ayrılığı → rakip kulüp ilgisi → yönetim krizi → transfer veya barışma.

Bu zincirin önemi, hiçbir aşamanın otomatik/zorunlu olmaması ve aktörlerin kişiliği ile bağlamın zincirin yönünü değiştirebilmesidir. Bu, olay motorunun yalnızca doğrusal bir "event log" değil, dallanabilen bir durum makinesi veya kural tabanlı bir çıkarım sistemi olarak tasarlanması gerektiğine işaret eder.

## 6. MVP İçin Zorunlu Sistemler

Ana belge Bölüm 27.2'ye göre MVP kapsamı:

- Teknik direktör kariyeri (tek kariyer türü)
- Kurgusal tek ülke, sınırlı sayıda lig, ~20-40 kulüp
- Sezon ve takvim sistemi
- Temel kulüp modeli ve temel futbolcu modeli
- Teknik direktör profili
- Basit antrenman ve kadro yönetimi
- Temel taktik sistemi
- 2D veya metin tabanlı maç simülasyonu
- Temel transfer ve sözleşme sistemi
- Oyuncu ilişkileri
- Söz verme sistemi
- Hafıza kayıtları
- Sınırlı ama sistemik olay motoru
- Temel basın ve yönetim görüşmeleri
- Kayıt ve yükleme
- Uzun sezon simülasyon testleri (5-10 sezon)

## 7. MVP Dışında Tutulması Gereken Sistemler

Ana belge Bölüm 27.3'e göre MVP dışında kalması gerekenler:

- Üç boyutlu maç motoru
- Çevrim içi çok oyunculu mod
- Gerçek lisanslı kulüpler/futbolcular
- Ayrıntılı futbolcu yaşam simülasyonu
- Ayrıntılı kişisel ekonomi
- Futbolcu kariyeri (bütünüyle)
- Kulüp sahipliği
- Gelişmiş sponsor sistemi
- Tüm dünya ligleri
- Gerçek zamanlı üretken yapay zekâya zorunlu bağımlılık

Kural 8 gereği, geliştirilecek her özellik MVP, genişletilmiş sürüm veya nihai vizyon kategorilerinden birine açıkça atanmalıdır; kategorisiz özellik geliştirmeye başlanmamalıdır.

## 8. Teknik Riskler

- **Performans:** Çok aktörlü (yüzlerce futbolcu, teknik direktör, menajer, kulüp) bir dünyanın her birim zamanda (gün/hafta/sezon) simüle edilmesi, özellikle uzun kariyerlerde (10-15+ sezon) performans sorunlarına yol açabilir.
- **Veri hacmi büyümesi:** Hafıza, ilişki ve olay günlüğü kayıtları zamanla katlanarak büyüyebilir; unutma/özetleme mekanizması olmadan kayıt dosyaları yönetilemez hâle gelebilir.
- **Determinizm/yeniden üretilebilirlik:** Ana belge kritik simülasyonların "deterministik veya yeniden üretilebilir" olmasını istiyor; bu, rastlantısallık kullanan sistemlerle (maç sonucu, olay tetikleme) dikkatli bir tohumlama/durum yönetimi gerektirir.
- **Kayıt sürüm geçişi:** Oyun geliştikçe veri modeli değişecektir; eski kayıtların yeni sürümlerle uyumsuz hâle gelmemesi (Bölüm 26'da "kabul edilemez risk" olarak tanımlanmıştır) ciddi bir mühendislik yükü taşır.
- **Modülerlik gereksinimi:** Kural 6, motor/veritabanı/arayüz teknolojisi değişse bile alan modelinin korunmasını istiyor; bu, katmanlar arası sıkı bağımlılık kurulmamasını gerektirir.
- **Dış yapay zekâ bağımsızlığı:** Ana simülasyonun harici AI servislerine zorunlu bağımlı olmaması gerekiyor (Kural 7); bu, diyalog ve olay üretiminin kural tabanlı/deterministik bir çekirdek üzerine kurulmasını zorunlu kılar.

## 9. Tasarımsal Riskler

- **Tekrar hissi:** Ana belgenin çözmeye çalıştığı temel problem tam olarak budur; olay/bağlam çeşitliliği yeterince zengin kurulmazsa oyun birkaç sezon içinde öngörülebilir hâle gelebilir (Bölüm 3, Bölüm 24).
- **Diyalog çeşitliliği tuzağı:** Bölüm 15.4 açıkça uyarıyor — çeşitlilik yüzlerce sabit cümle yazarak değil, bağlam şablonları ve kişilik filtreleriyle sağlanmalı; aksi hâlde içerik üretimi ölçeklenemez.
- **Adalet algısı:** Kontrollü rastlantısallık ile "sonuçların anlamsız görünmemesi" arasındaki denge (Bölüm 25, Bölüm 35) zor bir tasarım problemidir; oyuncu kaybettiğinde bile nedenini anlayabilmelidir.
- **Kişiselleşme vs. karmaşıklık:** Aynı başlangıcın farklı sonuçlar üretmesi hedefi (Bölüm 5.6) ile sistemin oyuncu tarafından anlaşılabilir kalması arasında denge kurulmalıdır.
- **Yavaş açılan derinlik ile MVP kapsamının çelişmesi riski:** MVP'nin küçük tutulması gerekirken, "derinlik" hissi vermesi de bekleniyor; bu iki hedefin MVP ölçeğinde nasıl bir arada sağlanacağı henüz netleşmemiştir.

## 10. Veri Bütünlüğü Riskleri

- 15 yıllık bir kariyer kaydının bir güncelleme sonrası kullanılamaz hâle gelmesi, ana belgede açıkça "kabul edilemez" olarak tanımlanmıştır (Bölüm 26).
- Söz, hafıza ve ilişki gibi karşılıklı referanslı verilerin (örn. bir sözün hem veren hem alan tarafı, hem de tetiklediği olayları referans etmesi) tutarlılığının korunması, özellikle kısmi kayıt bozulmalarında risk taşır.
- Otomatik yedekleme ve bozuk kayıt kurtarma mekanizmaları olmadan, uzun vadeli oynanışın temel vaadi (kalıcı, hatırlayan dünya) tehlikeye girer.
- Olay günlüğünün (event log) büyüklüğü ile "geriye dönük deterministik yeniden hesaplama" ihtiyacı arasında bir tasarım gerilimi vardır.

## 11. Performans ve Uzun Dönem Simülasyon Riskleri

- Başarı ölçütlerinden biri, sistemin "en az 10 sezon boyunca hata vermeden simüle edilebilmesi"dir (Bölüm 35). Bu, erken aşamadan itibaren uzun dönem simülasyon testlerinin (Kural 4, Kural 9) planlanmasını gerektirir.
- Oyuncunun doğrudan gözlemlemediği kulüp/lig/oyuncuların ne kadar ayrıntılı simüle edileceği (tam simülasyon mu, özetlenmiş simülasyon mu) henüz karara bağlanmamış olup doğrudan performansı etkileyecek bir tasarım sorusudur.
- Sistemlerin "birkaç sezon içinde bütün olay kalıplarını tükettirmeme" gereksinimi (Bölüm 35), zamanla büyüyen bir içerik/kural havuzu gerektirir; bu da uzun vadede bakım yükünü artırabilir.

## 12. Henüz Cevaplanmamış Sorular

Ana belge Bölüm 34'te 15 açık tasarım sorusu listeler; bunların hiçbiri bu analizde varsayılarak cevaplanmamıştır. Öne çıkanlar:

1. Oyun dünyası tamamen kurgusal mı olacak?
2. İlk sürümde kaç lig ve takım olacak (kesin sayı)?
3. Bir oyun günü hangi somut adımlarla ilerleyecek?
4. Maçlar gerçek zamanlı mı, hızlandırılmış mı, tamamen simülasyon mu olacak?
5. Teknik direktör tüm görevleri mi yapacak, ekibine devredebilecek mi?
6. Diyaloglarda serbest metin girişi olacak mı?
7. Oyuncu özellikleri sayısal olarak ne kadar açık gösterilecek?
8. Gizli bilgiler (potansiyel, sadakat vb.) nasıl keşfedilecek?
9. Dünya simülasyonu performans için hangi ayrıntı seviyelerini kullanacak?
10. Platform yalnızca masaüstü mü olacak?
11. Mod desteği hangi aşamada planlanacak?
12. Gerçek futbol verileri/lisansları ileride değerlendirilecek mi?
13. Futbolcu kariyerinde maç kontrolü nasıl sunulacak?
14. Bir sezonun hedeflenen gerçek oynama süresi ne olacak?
15. Oyuncunun işsiz kalması nasıl eğlenceli tutulacak?

Ana belge bu soruların tamamının ilk aşamada cevaplanmasını zorunlu kılmıyor, ancak **teknik mimariyi doğrudan etkileyen sorular** (özellikle 3, 4, 9, 10) geliştirme başlamadan önce çözülmelidir.

## 13. Kodlamadan Önce Tamamlanması Gereken Belgeler

Ana belge Bölüm 36 (Geliştirme Yaklaşımı) ve önerilen çalışma sırası göz önüne alındığında, kodlamadan önce en az şu belgelerin olgunlaştırılması gerekir:

1. `02_MVP_SCOPE.md` — kesin MVP kapsamı ve kabul kriterleri
2. `03_DOMAIN_MODEL.md` — aktörler ve veri modeli
3. `04_EVENT_RULE_ENGINE.md` — olay/kural motorunun çalışma prensibi
4. `05_MEMORY_AND_PROMISE_SYSTEM.md` — hafıza ve söz sisteminin veri yapısı
5. `06_RELATIONSHIP_SYSTEM.md` — ilişki/kişilik modelinin boyutları
6. `13_SAVE_SYSTEM.md` — kayıt formatı ve bütünlük stratejisi (kritik gereksinim)
7. `14_TEST_STRATEGY.md` — uzun dönem simülasyon test yaklaşımı
8. Teknoloji/mimari seçim kararı (henüz ayrı bir belgesi yok; `15_DECISION_LOG.md` üzerinden kayıt altına alınmalı)

## 14. Önerilen Bir Sonraki En Küçük Adım

Ana belgeyi ve mevcut dokümantasyon iskeletini değiştirmeden, bir sonraki en küçük ve güvenli adım:

> `02_MVP_SCOPE.md` belgesini, ana belgenin Bölüm 27'sine dayanarak; kesin lig/kulüp/futbolcu sayıları, bir "oyun günü/haftası"nın somut adımları ve MVP kabul kriterlerinin ölçülebilir test senaryoları ile doldurmak.

Bu adım küçük, test edilebilir ve tek bir sisteme (MVP kapsam tanımı) odaklıdır; herhangi bir kod veya teknoloji kararı gerektirmez ve Kural 3'e (paralel çok sistem açmama) uygundur.

---

## Merkezi İlke: Olaylar Üzerinden Aktarım

Bu analiz boyunca vurgulanması gereken en kritik mimari ilke şudur:

**Bir sistemde gerçekleşen bir olay, başka sistemlere doğrudan tablo manipülasyonuyla değil; tanımlı olaylar, bağlam kuralları ve sonuç mekanizmaları üzerinden aktarılmalıdır.**

Bu ilke ana belgede Kural 2 olarak zaten açıkça yer almaktadır ve bu analizin 4. ve 5. bölümlerinde gösterilen tüm sistem bağımlılıkları ve olay zincirleri, bu ilkenin neden teknik olarak zorunlu olduğunu doğrulamaktadır: sistemler birbirine doğrudan değil, olay/bağlam/sonuç motoru üzerinden bağlanmalıdır. Aksi hâlde sistemler arası sıkı bağımlılıklar (tight coupling) oluşur, bu da hem test edilebilirliği hem de gelecekteki teknoloji/mimari değişikliklerine dayanıklılığı (Kural 6) tehlikeye atar.
