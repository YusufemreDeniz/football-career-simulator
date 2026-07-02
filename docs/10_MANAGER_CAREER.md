# Teknik Direktör Kariyeri ve İstihdam Sistemi

**Belge:** `docs/10_MANAGER_CAREER.md`
**Durum:** Kesinleşti
**Ana referans:** `docs/01_GAME_DESIGN_DOCUMENT.md`
**Kesin MVP sınırı:** `docs/02_MVP_SCOPE.md`
**Domain sınırları:** `docs/03_DOMAIN_MODEL.md`
**Olay ve kural sözleşmeleri:** `docs/04_EVENT_RULE_ENGINE.md`
**Hafıza ve söz sözleşmeleri:** `docs/05_MEMORY_AND_PROMISE_SYSTEM.md`
**İlişki sözleşmeleri:** `docs/06_RELATIONSHIP_SYSTEM.md`
**Diyalog ve karar sözleşmeleri:** `docs/07_DIALOGUE_SYSTEM.md`
**Transfer ve sözleşme sözleşmeleri:** `docs/08_TRANSFER_SYSTEM.md`
**Maç sözleşmeleri:** `docs/09_MATCH_SIMULATION.md`
**Mimari yön:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`
**İlgili karar günlüğü:** `docs/15_DECISION_LOG.md`

---

## 1. Belgenin Amacı

Bu belge, Football Career Simulator projesinde oyuncunun canlandırdığı teknik direktörün kariyer başlangıcını, kulüpler nezdindeki görev (employment) ilişkisini, iş piyasasını, yönetim kurulu (board) nezdindeki güvenilirliğini, itibarını, davranıştan doğan profilini, kariyer geçmişini ve kariyerin normal veya erken bitişini kesinleştirir.

Bu belge:

* `Manager Career & Employment` bounded context'inin (`docs/03_DOMAIN_MODEL.md` Bölüm 7.5) authoritative sorumluluklarını ayrıntılandırır,
* yeni bir bounded context oluşturmaz,
* üretim sınıfları, veritabanı tabloları veya ORM modelleri tanımlamaz,
* kesin sayısal formülleri, eşikleri veya dengeleme parametrelerini belirlemez,
* GDD, MVP kapsamı, Domain Model veya diğer kesinleşmiş ön koşul belgeleriyle çelişmez; yalnız onları teknik direktör kariyeri ve istihdam açısından detaylandırır.

---

## 2. Referanslar ve Kapsam

Ana referans:

`docs/01_GAME_DESIGN_DOCUMENT.md` — özellikle Bölüm 7.1 (Teknik Direktör Kariyeri) ve Bölüm 8.1 (Teknik Direktör Döngüsü).

Kesin MVP sınırı:

`docs/02_MVP_SCOPE.md` — özellikle Bölüm 5 (Kariyer Başlangıcı, Süresi ve Bitişi) ve Bölüm 13.1 (Teknik Direktör Kariyeri zorunlu kapsamı).

Domain sınırları:

`docs/03_DOMAIN_MODEL.md` — özellikle Bölüm 7.5 (`Manager Career & Employment`), Bölüm 8 (Aggregate Root'lar), Bölüm 11 (Veri Sahipliği Matrisi) ve Bölüm 16.1 (Manager Kulüp Değişimi).

Bu belge aşağıdaki ölçeği destekler:

* 20 kulüp, her kulüp için bir aktif teknik direktör kaydı,
* en fazla 10 tamamlanmış sezon,
* oyuncunun yönettiği teknik direktörün kariyeri boyunca birden fazla kulüpte görev alabilmesi,
* işten çıkarılma ve sınırlı kulüp değiştirmenin gerçek domain kurallarıyla desteklenmesi.

Bu belge yalnızca ilk dikey kesiti değil, kesinleşmiş 10 sezonluk MVP'yi destekler. İlk dikey kesitte zorunlu asgari kapsam Bölüm 36'da ayrıca tanımlanmıştır.

---

## 3. Uyumluluk ve Terminoloji Notu

Bu belge hazırlanmadan önce aşağıdaki kesinleşmiş belgeler baştan sona okunmuş ve ayrıntılı tutarlılık kontrolüne tabi tutulmuştur:

* `docs/01_GAME_DESIGN_DOCUMENT.md`
* `docs/02_MVP_SCOPE.md`
* `docs/03_DOMAIN_MODEL.md`
* `docs/04_EVENT_RULE_ENGINE.md`
* `docs/05_MEMORY_AND_PROMISE_SYSTEM.md`
* `docs/06_RELATIONSHIP_SYSTEM.md`
* `docs/07_DIALOGUE_SYSTEM.md`
* `docs/08_TRANSFER_SYSTEM.md`
* `docs/09_MATCH_SIMULATION.md`
* `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`

Bu inceleme sonucunda GDD, MVP kapsamı, Domain Model, Event/Rule Engine, Memory/Promise, Relationship, Dialogue, Transfer, Match ve Technology/Architecture belgeleri arasında bu belgenin kapsamını etkileyen gerçek bir çelişki tespit edilmemiştir.

Terminoloji netliği için:

* `docs/03_DOMAIN_MODEL.md` Bölüm 7.5 ve Bölüm 9.2'de value object adayı olarak geçen **`BoardTrust`**,
* `docs/02_MVP_SCOPE.md` Bölüm 14.12 ve `docs/06_RELATIONSHIP_SYSTEM.md` D-106'da geçen **"yönetim güveni" / "Board Confidence"**,
* `docs/08_TRANSFER_SYSTEM.md` Bölüm 1 ve D-131'de geçen **"Board Confidence"**

ifadelerinin tamamı **aynı authoritative kurumsal değerlendirme kavramına** karşılık gelir: kulübün teknik direktörün görevine devam etmesine yönelik kurumsal değerlendirmesi.

Bu belgede kanonik terim **`Board Confidence`** olarak kullanılır. `BoardTrust`, Domain Model'in henüz teknoloji bağımsız düzeyde bıraktığı bir value object adı önerisidir ve `Board Confidence` kavramının olası bir temsilidir; ikinci ve bağımsız bir authoritative confidence state'i oluşturulmamıştır. Gelecekteki teknik isimlendirme (`BoardTrust`, `BoardConfidence` vb.) bu kavramsal birliği bozamaz.

---

## 4. Bağlayıcı Tasarım İlkeleri

1. `Manager Career & Employment`, Manager identity, career history, reputation, active employment, club active-manager assignment, Job Offer, Season Expectation, Board Confidence, Board Assessment, employment risk, dismissal ve unemployment state'inin tek authoritative owner'ıdır.
2. `ManagerCareer`, `ClubEmployment` ve `JobOffer` ayrı aggregate root sınırlarıdır; birleştirilmez.
3. Başka hiçbir context bu verileri doğrudan değiştiremez; yalnız event, query veya command girdisi sağlayabilir.
4. Board Confidence, Relationship state'i değildir; Manager Reputation, Board Confidence değildir; Manager Profile, Reputation değildir; Season Expectation, Board Confidence değildir.
5. Club bütçesi ve politikaları `Club & Governance`'a, Match Result ve standings `Competition`'a, Relationship/Memory/Promise `Social Continuity`'ye, diyalog ve bekleyen kararlar `Interaction & Narrative`'e aittir; bu belge onları yeniden tanımlamaz.
6. Teknik direktörün aktif görevi ile kulübün aktif teknik direktörü aynı `ClubEmployment` kaydından türetilir.
7. UI hiçbir kariyer veya employment state'ini doğrudan değiştiremez.
8. Event & Rule Evaluation, manager career veya employment business state'inin sahibi değildir; yalnız consequence request üretir.
9. Çok context'li employment geçişleri (offer acceptance → activation, dismissal → unemployment, club değiştirme) Application katmanı tarafından orkestre edilir ve atomik/idempotent olmak zorundadır.
10. Snapshot ana runtime state kaynağıdır; tam event sourcing kullanılmaz.
11. Harici üretken yapay zekâ; iş teklifi, dismissal, Board Confidence, profile veya kariyer sonucu üretimi için zorunlu bağımlılık olamaz.
12. Yeni bounded context oluşturulmaz; Domain Model'deki 14 bounded context sınırı korunur.

---

## 5. Terminoloji

* **Manager:** Oyuncunun canlandırdığı veya dünya tarafından simüle edilen teknik direktörün kalıcı kariyer kimliği.
* **ManagerCareer:** Manager'ın kulüpten bağımsız kalıcı kariyer aggregate'ı; identity, career state, reputation, career history ve completion durumunu taşır.
* **ClubEmployment:** Manager ile Club arasındaki aktif veya geçmiş görev ilişkisinin authoritative kaydı; ayrı aggregate root'tur.
* **JobOffer:** Bir club'ın bir manager'a yönelik iş teklifinin ayrı yaşam döngüsüne sahip aggregate'ı.
* **Starting Background:** Kariyerin başlangıç bağlamını tanımlayan authored content kategorisi (ör. amatör takım teknik direktörü).
* **Season Expectation:** Bir ClubEmployment için geçerli olan, kulüp politikası ve hedeflerinden türeyen sezon beklentisi kaydı.
* **Board Confidence:** Kulübün, teknik direktörün görevine devam etmesine yönelik authoritative kurumsal değerlendirmesi.
* **Board Assessment:** Board Confidence'ı güncelleyen periyodik, milestone veya kritik değerlendirme olayı.
* **Employment Risk:** Board Confidence'tan türeyen, employment'ın devamlılığına ilişkin niteliksel bant (Secure/Stable/Under Review/Critical).
* **Dismissal:** Committed Board Assessment sonucunda ClubEmployment'ın kulüp tarafından sona erdirilmesi.
* **Unemployment:** Manager'ın aktif ClubEmployment'a sahip olmadığı career state.
* **Manager Reputation:** Manager'ın futbol dünyasındaki kurumsal ve sportif tanınırlığı.
* **Manager Profile:** Committed davranışlardan türeyen, çoklu ve genişleyebilir profile label/evidence modeli.
* **Career History:** Manager'ın kariyeri boyunca korunan önemli olay ve milestone kaydı.
* **Career Evaluation:** Kariyerin normal veya erken bitişinde üretilen çok boyutlu değerlendirme.

---

## 6. Authoritative Veri Sahipliği

`Manager Career & Employment` context'i aşağıdaki verilerin tek authoritative owner'ıdır:

* Manager identity
* Starting background
* Career state ve career history
* Manager reputation
* Manager profile evidence ve türetilmiş profile label'ları
* Active employment (ClubEmployment)
* Club active-manager assignment (ClubEmployment'tan türetilir)
* Job offers
* Season expectations
* Board Confidence
* Board assessments
* Employment risk state
* Dismissal records
* Unemployment state
* Career completion ve early career end kayıtları

Bu context'in sahip **olmadığı** veriler: Club bütçesi, squad, match result, standings, player contract, relationship, memory, promise ve diyalog metni. Bu veriler ilgili authoritative context'lere aittir ve bu belge tarafından yeniden tanımlanmaz.

| Veri alanı | Authoritative owner | Okuyabilen context'ler | Değiştirebilen context |
| --- | --- | --- | --- |
| Manager identity, career history | Manager Career & Employment | Tümü | Manager Career & Employment |
| Active employment / club active-manager | Manager Career & Employment | Club, Competition, Transfer, Team Preparation, UI | Manager Career & Employment |
| Board Confidence, Board Assessment, Employment Risk | Manager Career & Employment | Club, Interaction, UI | Manager Career & Employment |
| Manager Reputation, Manager Profile | Manager Career & Employment | Transfer, Interaction, UI | Manager Career & Employment |
| Season Expectation | Manager Career & Employment | Club, UI | Manager Career & Employment |
| Job Offer | Manager Career & Employment | UI, Interaction | Manager Career & Employment |
| Club politikaları ve bütçe | Club & Governance | Manager Career & Employment | Club & Governance |
| Match Result, standings | Match, Competition | Manager Career & Employment | Match, Competition |
| Relationship, Memory, Promise | Social Continuity | Manager Career & Employment | Social Continuity |
| Diyalog ve bekleyen karar | Interaction & Narrative | Manager Career & Employment | Interaction & Narrative |

---

## 7. Aggregate ve Yaşam Döngüsü Sınırları

### 7.1. ManagerCareer

Manager'ın kulüpten bağımsız kalıcı kariyer aggregate'ıdır.

Kavramsal lifecycle:

`Created → Awaiting Initial Employment → Employed ↔ Unemployed → MvpCompleted veya EndedEarly`

### 7.2. ClubEmployment

Manager ile Club arasındaki aktif görev ilişkisinin authoritative kaydıdır. Ayrıntılı yaşam döngüsü Bölüm 11'de tanımlanmıştır.

### 7.3. JobOffer

Bir club'ın bir manager'a yönelik iş teklifinin ayrı yaşam döngüsüne sahip aggregate'ıdır. Ayrıntılı yaşam döngüsü Bölüm 18'de tanımlanmıştır.

### 7.4. Ortak invariant'lar

1. `ManagerCareer`, `ClubEmployment` ve `JobOffer` ayrı aggregate root sınırlarıdır; biri diğerinin iç state'ini doğrudan değiştiremez.
2. Teknik direktörün aktif görevi ile kulübün aktif teknik direktörü aynı `ClubEmployment` kaydından türetilir; ikinci bir "aktif manager" kaydı tutulmaz.
3. Manager kimliği kulüp değişiminde veya işten çıkarılmada değişmez.
4. Bir manager aynı anda en fazla bir aktif `ClubEmployment` kaydına sahip olabilir.
5. Bir club aynı anda en fazla bir aktif manager'a sahip olabilir.
6. `JobOffer`, `ClubEmployment` değildir; offer acceptance doğrudan employment başlatmış sayılmaz.

---

## 8. Kariyer Başlangıcı ve Starting Background

### 8.1. Başlangıç noktası

MVP kariyeri, birinci sezonun sezon öncesi hazırlık döneminin ilk gününde, oyuncunun aktif bir A takım teknik direktörü olarak başlar (`docs/02_MVP_SCOPE.md` Bölüm 5.1 ile uyumlu).

Kariyer oluşturma akışı simülasyon zamanının başlamasından önce tamamlanır:

1. Kalıcı Manager identity oluşturulur.
2. Starting Background seçilir.
3. Starting Background ve kulüp giriş politikalarına göre sınırlı ilk Job Offer seti oluşturulur.
4. En az bir geçerli başlangıç teklifi garanti edilir.
5. Oyuncu bir teklifi kabul eder.
6. `ClubEmployment` aktive edilir.
7. Kulübün active-manager assignment'ı aynı kayıttan türetilir.
8. İlk Season Expectation ve başlangıç Board Confidence kaydı oluşturulur.
9. Birinci sezonun sezon öncesi ilk günü açılır.

Oyuncu, kariyer başlangıcında doğrudan ve kuralsız biçimde herhangi bir kulübü sahiplenemez. Başlangıç kulübü, uygun ve sınırlı Job Offer seti üzerinden seçilir.

### 8.2. Starting Background

GDD Bölüm 7.1'de tanımlanan aşağıdaki altı starting background MVP authored content setine dahil edilir:

* Amatör takım teknik direktörü
* Altyapı antrenörü
* Yardımcı antrenör
* Alt liglerde çalışan genç teknik direktör
* Futbolculuktan yeni emekli olmuş eski oyuncu
* Profesyonel futbol geçmişi olmayan taktik uzmanı

Her background en az şu başlangıç bağlamlarını etkileyebilmelidir:

* ilk reputation seviyesi veya bandı,
* uygun ilk kulüp aralığı,
* ilk iş teklifleri,
* medya ilgisi,
* başlangıç Board Confidence bağlamı,
* futbolcuların ilk yaklaşımına sunulan context,
* taktik veya gelişim odaklı başlangıç profile signal'ları,
* kulüp kültürü ve beklenti uyumu.

Starting Background:

* Manager Profile'ın kalıcı ve değişmez sonucu değildir,
* oyuncuya otomatik başarı sağlamaz,
* exact sayısal modifier'ları bu belgede belirlemez,
* kulüp değişiminde değişmez,
* save/load sırasında korunur.

---

## 9. İlk İş Teklifleri ve Kariyer Aktivasyonu

İlk Job Offer seti Starting Background, authored kulüp giriş politikaları ve dünya başlangıç state'ine göre üretilir.

Kurallar:

* En az bir geçerli başlangıç teklifi garanti edilir; boş bir başlangıç teklif seti geçerli bir kariyer başlangıcı sayılmaz.
* İlk teklif seti sınırlıdır; oyuncunun bütün 20 kulübe kuralsız erişimi yoktur.
* Oyuncu teklifi kabul ettiğinde Application, Bölüm 11.6'da tanımlanan aktivasyon sürecini yürütür.
* Aktivasyon başarısız olursa kısmi ClubEmployment veya club assignment bırakılmaz; kariyer başlangıcı yeniden değerlendirilir.

Bu akış simülasyon zamanı başlamadan tamamlanır; birinci sezonun sezon öncesi ilk günü ancak aktivasyon tamamlandıktan sonra açılır.

---

## 10. Manager Career Yaşam Döngüsü

Kavramsal lifecycle:

`Created → Awaiting Initial Employment → Employed ↔ Unemployed → MvpCompleted veya EndedEarly`

Kurallar:

1. Manager Career, ClubEmployment sona erdiğinde silinmez.
2. Manager identity ve career history tüm kulüp geçişlerinde korunur.
3. Manager aynı anda `Employed` ve `Unemployed` olamaz.
4. Career tamamlandıktan sonra (`MvpCompleted` veya `EndedEarly`) normal simulation time tekrar ilerletilemez.
5. Tamamlanmış kariyer save dosyası yüklenebilir ve kariyer değerlendirmesi görüntülenebilir.
6. MVP'de post-career rol geçişleri bu lifecycle'ın parçası değildir.

Geçersiz geçişler (`docs/03_DOMAIN_MODEL.md` Bölüm 12.2 ile uyumlu):

* aktif employment kapanmadan ikinci employment başlatmak,
* completed career için yeni employment,
* aynı club'da iki active manager.

---

## 11. Club Employment Yaşam Döngüsü

### 11.1. Kapsam

`ClubEmployment`, manager ile club arasındaki aktif görev ilişkisinin authoritative kaydıdır.

En az şu bilgileri kavramsal olarak taşır:

* Employment identity
* ManagerId
* ClubId
* Başlangıç oyun tarihi
* Bitiş oyun tarihi
* Employment status
* Başlangıç nedeni
* Bitiş nedeni
* Aktif Season Expectation referansı
* Başlangıç ve güncel employment risk bağlamı
* Son Board Assessment referansı
* Correlation/idempotency bilgisi

### 11.2. Lifecycle

`Proposed veya Pending Activation → Active → Ending → Ended`

Terminal bitiş nedenleri en az şunları ayırır:

* Dismissed
* Voluntary Departure
* Career Completed
* Early Career End
* Invalidated before activation

### 11.3. Invariant'lar

* Bir manager aynı anda en fazla bir `Active` ClubEmployment kaydına sahip olabilir.
* Bir club aynı anda en fazla bir `Active` ClubEmployment üzerinden manager'a sahip olabilir.
* Terminal state'teki ClubEmployment normal command ile yeniden `Active` yapılamaz.

### 11.4. MVP dışı ayrıntılar

Detaylı manager maaşı, teknik direktör sözleşme maddeleri, tazminat ve fesih bedeli MVP kapsamında değildir (Bölüm 39 ile uyumlu).

### 11.5. Kulüp active-manager assignment

Club'ın aktif teknik direktörü ayrı bir kayıt olarak tutulmaz; her zaman ilgili club için `Active` durumdaki `ClubEmployment` kaydından türetilir. İkinci ve bağımsız bir "aktif manager" alanı oluşturulmaz.

### 11.6. Aktivasyon süreci (genel çerçeve)

Bir ClubEmployment aktivasyonu (ilk kariyer başlangıcı, unemployed manager'ın işe alınması veya employed manager'ın kulüp değiştirmesi fark etmeksizin) Application tarafından şu genel adımlarla yürütülür:

1. İlgili JobOffer'ın geçerliliği doğrulanır.
2. Hedef club'da geçerli vacancy doğrulanır.
3. Manager'ın mevcut employment durumu doğrulanır.
4. Varsa mevcut employment ilgili nedenle sona erdirilir.
5. Yeni `ClubEmployment` `Active` olarak oluşturulur.
6. Club active-manager assignment aynı kayıttan türetilir.
7. İlk/yeni Season Expectation ve gerekli Board Confidence bağlamı oluşturulur.
8. JobOffer `Completed` olur; diğer açık offer'lar açık policy ile değerlendirilir.

Bu süreç kısmi geçerli state bırakmaz; herhangi bir adım başarısız olursa önceki geçerli state korunur ve aktivasyon gerçekleşmemiş sayılır.

---

## 12. Season Expectations

Season Expectation:

* `Manager Career & Employment` context'inin authoritative kaydıdır,
* Club & Governance tarafından sağlanan kulüp politikası ve hedef bağlamından oluşturulur,
* oyuncuya açık ve anlaşılır biçimde sunulur,
* sezon hedefi, beklenen lig seviyesi ve ilgili önemli sportif öncelikleri temsil eder,
* Board Assessment için ana karşılaştırma girdilerinden biridir.

Kurallar:

* Geriye dönük sessizce değiştirilemez.
* Yalnız açık bir board/governance kararı ve committed event ile revize edilebilir.
* Revize edildiğinde eski ve yeni beklenti ile gerekçe izlenebilir olmalıdır.
* Tek bir Match Result değildir.
* Board Confidence ile aynı state değildir.

Kesin beklenti puanlama formülü ve hedef eşikleri açık bırakılır (Bölüm 40).

---

## 13. Board Confidence

Kanonik terim `Board Confidence`'tır (Bölüm 3).

Board Confidence:

* kulübün teknik direktörün görevine devam etmesine yönelik kurumsal değerlendirmesidir,
* Relationship Record değildir,
* manager reputation değildir,
* taraftar desteği değildir,
* kişisel bir yönetici–teknik direktör ilişkisi değildir,
* tek bir Match Result'ın doğrudan kopyası değildir.

### 13.1. Girdiler

Board Confidence en az şu girdiler üzerinden değerlendirilebilir:

* Season Expectation'a göre mevcut performans,
* lig sıralaması ve trend,
* son dönem sonuçları,
* önemli maç ve sezon sonuçları,
* kulüp politikalarına uyum,
* transferde Sporting Approval ve kadro planının kulüp yönüyle uyumu,
* kriz yönetimi,
* kritik public narrative sonuçları,
* tekrarlanan disiplin, Promise veya insan yönetimi örüntüleri,
* önemli olumlu veya olumsuz kariyer olayları.

### 13.2. Sahiplik kuralı

Başka context'ler Board Confidence'ı doğrudan değiştiremez. Yalnız committed fact, Integration Event, query veya değerlendirme girdisi sağlar. Nihai değişikliği yalnız `Manager Career & Employment` owner'ı uygular.

### 13.3. Sunum

Board Confidence'ın exact sayısal ölçeği açık bırakılabilir; ancak oyuncuya ve debug araçlarına en az niteliksel band, ana nedenler ve son değişim yönü sunulmalıdır.

---

## 14. Board Assessment ve Employment Risk

Board Confidence her event handler'ın rastgele sırasına göre değiştirilmez (`docs/04_EVENT_RULE_ENGINE.md` Bölüm 27 ile uyumlu — event motoru yalnız evaluation input'larını owner'a iletir).

### 14.1. Değerlendirme türleri

* **Periodic Assessment:** Oyun zamanı üzerinden planlanan düzenli değerlendirme.
* **Milestone Assessment:** Season başlangıcı, season ortası, season sonu veya önemli hedef sonucu.
* **Critical Assessment:** Ağır sonuç serisi, ciddi kriz veya açık yönetim tetikleyicisi.

### 14.2. Kurallar

Exact periyodik gün sayısı veya katsayı bu belgede belirlenmeyebilir; ancak:

* her frame veya UI açılışında değerlendirme yapılamaz,
* aynı değerlendirme aynı ProcessingKey ile ikinci kez uygulanamaz,
* değerlendirme girdilerinin hangi oyun zamanı aralığını kapsadığı belli olmalıdır,
* sonuç ana olumlu ve olumsuz faktörlerle açıklanmalıdır,
* normal dismissal kararı committed Board Assessment olmadan üretilemez.

### 14.3. Employment Risk bantları

Employment risk için exact enum zorunlu değildir; fakat en az şu semantik bantlar desteklenir:

* Secure
* Stable
* Under Review
* Critical

Bu bantlar Board Confidence'ın oyuncuya ve employment süreçlerine sunulan türetilmiş yorumudur; ikinci authoritative confidence state'i değildir.

---

## 15. Dismissal

İşten çıkarılma:

* doğrudan oyun sonu değildir (`docs/02_MVP_SCOPE.md` Bölüm 5.5 ile uyumlu),
* Match context tarafından uygulanamaz,
* Dialogue sistemi tarafından uygulanamaz,
* tek bir notification ile gerçekleşmiş sayılmaz,
* committed Board Assessment ve açık Dismissal Decision sonucunda yürütülür.

### 15.1. Akış

1. Board Assessment employment'ın devamını kritik bulur.
2. Manager Career & Employment owner'ı dismissal kararını doğrular.
3. Dismissal sonucu committed Domain Event olur.
4. Application, aktif ClubEmployment'ı güvenli biçimde sona erdirir.
5. Club active-manager assignment boşalır.
6. Manager `Unemployed` state'ine geçer.
7. Unemployment başlangıç tarihi ve son değerlendirme tarihi kaydedilir.
8. Aktif Job Offer ve market evaluation süreçleri başlatılabilir.
9. Social Continuity, Transfer, Team Preparation ve Interaction sistemleri committed departure event'ini kendi kurallarıyla değerlendirir.

### 15.2. Kurallar

* Manager dismissal aynı causation ile ikinci kez uygulanamaz.
* Dismissal nedeni, ana Board Assessment faktörleri ve employment kapanış bilgisi kariyer geçmişinde korunur.

---

## 16. İşsizlik

İşsizlik statüsü MVP'de gerçek fakat sadeleştirilmiş bir domain state'idir.

İşsiz manager:

* dünya zamanını ilerletebilir,
* sınırlı Job Offer alabilir,
* aktif kulüp kadrosu, taktiği, antrenmanı veya transferi üzerinde yetkiye sahip değildir,
* kişisel Memory, Relationship, Reputation, Profile ve Career History kayıtlarını korur,
* eski kulüp ve futbolcularla yeniden karşılaşabilir.

### 16.1. 365 günlük sınır

Kesin karar:

> MVP'de kesintisiz işsizliğin maksimum süresi 365 oyun günüdür.

Kurallar:

* 365 günlük süre oyun zamanı üzerinden hesaplanır.
* Duvar saati kullanılmaz.
* Aynı işsizlik döneminde sayaç save/load sonrasında korunur.
* Yeni bir employment aktive olduğunda işsizlik dönemi sona erer.
* Kabul edilmiş ve güvenli activation sürecinde bulunan geçerli bir Job Offer varsa 365. gün kontrolü süreci yarıda kesmez; activation sonucu beklenir.
* 365 oyun günü sonunda aktif employment veya kabul edilmiş geçerli activation süreci yoksa kariyer `EndedEarly` durumuna geçer.
* Early end nedeni `Prolonged Unemployment` olarak kaydedilir.
* Early end save dosyasını silmez; kariyer değerlendirmesi ve geçmiş görüntülenebilir.
* İşten çıkarılma anı doğrudan oyun sonu değildir; oyuncuya bir tam iş piyasası döngüsü tanınır.

MVP'de bunun dışında zorunlu erken kariyer sonu koşulu oluşturulmaz.

---

## 17. İş Piyasası ve Job Offer Üretimi

### 17.1. Kapsam

MVP iş piyasası şunları içerir:

* kulüp vacancy oluşması,
* unemployed manager değerlendirmesi,
* uygun employed manager'a sınırlı dış teklif,
* Job Offer oluşturulması,
* teklifin kabul, ret, expiry, withdrawal veya invalidation sonucu,
* yeni employment aktivasyonu.

MVP iş piyasası şunları içermez:

* serbest iş başvurusu ekranı,
* ayrıntılı iş mülakatları,
* menajer/temsilci ağı,
* maaş ve tazminat pazarlığı,
* teknik ekibin yeni kulübe taşınması,
* milli takım teklifleri,
* bütün kulüplere açık kuralsız başvuru,
* gelişmiş transfer benzeri manager negotiation turları.

### 17.2. Tetikleyiciler

Job market evaluation en az şu tetikleyicilerde çalışabilir:

* manager işsiz kaldığında,
* bir club vacancy oluştuğunda,
* season sonu veya season başlangıcı gibi kariyer dönüm noktalarında,
* işsizlik sırasında planlanmış market review zamanı geldiğinde.

Job Offer seti sınırlı olmalı ve süresiz büyüyememelidir. Exact aktif teklif limiti ve review aralığı dengeleme verisi olarak açık bırakılır.

### 17.3. Eligibility

Bir Job Offer'ın oluşturulması en az şu girdileri değerlendirir:

* club vacancy veya açıkça planlanmış vacancy,
* manager'ın employment durumu,
* manager reputation,
* son kariyer performansı,
* manager profile ile club policy/culture uyumu,
* kulübün sportif seviyesi ve beklentisi,
* geçmiş dismissal ve departure kayıtları,
* eski club veya aktörlerle önemli history,
* manager'ın aynı club'daki geçmiş employment'ı,
* manager'ın teklif almaya uygun olup olmadığı,
* teklifin oyun zamanı bağlamı.

Kesin offer puanlama formülü belirlenmez (Bölüm 40).

Aynı state, aynı içerik sürümü ve aynı seed ile aynı market evaluation aynı sonucu üretmelidir. Rastlantısal tie-break gerekiyorsa açık, seeded ve sürümlenmiş Random Context kullanılır (`docs/04_EVENT_RULE_ENGINE.md` Bölüm 10 ile uyumlu).

Bir club aynı manager için aynı anda birden fazla aktif Job Offer oluşturamaz.

---

## 18. Job Offer Yaşam Döngüsü

`JobOffer`, `ClubEmployment` değildir.

### 18.1. Lifecycle

`Prepared → Offered → Accepted / Rejected / Expired / Withdrawn / Invalidated → Completed veya Archived`

### 18.2. Kurallar

* Offer benzersiz kimliğe sahip olmalıdır.
* Teklifin source club'ı, target manager'ı ve deadline'ı açık olmalıdır.
* Deadline oyun zamanı üzerinden çalışır.
* Expired veya withdrawn offer kabul edilemez.
* Aynı offer iki kez kabul edilemez.
* Offer acceptance doğrudan employment başlatmış sayılmaz.
* Acceptance sonrasında Application-owned activation süreci (Bölüm 11.6) çalışır.
* Activation başarısız olursa kısmi ClubEmployment veya club assignment bırakılamaz.
* Yeni employment başarıyla aktive olduğunda offer `Completed` olur.
* Başka açık teklifler açık policy ile invalidated veya retained olur; sessizce belirsiz state'te bırakılamaz.

---

## 19. Gönüllü Kulüp Değiştirme

Kulüp değiştirme yalnız dismissal sonrasında gerçekleşmek zorunda değildir.

Employed manager uygun bir dış Job Offer alabilir. Teklif kabul edildiğinde Application şu kritik geçişi güvenli ve tekil biçimde orkestre eder:

1. Offer geçerliliği yeniden doğrulanır.
2. Yeni club'da geçerli vacancy veya planlanmış assignment doğrulanır.
3. Manager'ın mevcut active employment'ı doğrulanır.
4. Mevcut employment `Voluntary Departure` nedeniyle sona erdirilir.
5. Eski club active-manager assignment'ı boşaltılır.
6. Yeni ClubEmployment aktive edilir.
7. Yeni club active-manager assignment'ı aynı kayıttan türetilir.
8. Manager Career `Employed` kalır; geçici çift employment oluşmaz.
9. Career History ve Reputation değerlendirme girdileri kaydedilir.
10. Diğer offer'lar açık kuralla invalidated veya yeniden değerlendirilir.

Bu kritik geçiş:

* kısmi geçerli state bırakamaz,
* aynı offer için ikinci kez uygulanamaz,
* bir manager'ı aynı anda iki club'a bağlayamaz,
* bir club'a aynı anda iki active manager atayamaz.

Detaylı resignation compensation veya manager contract feshi MVP kapsamında değildir.

---

## 20. Manager Reputation

Manager Reputation:

* manager'ın futbol dünyasındaki kurumsal ve sportif tanınırlığıdır,
* Board Confidence değildir,
* belirli bir Player → Manager Relationship değildir,
* yalnız son maçın sonucu değildir,
* Starting Background ile başlatılabilir fakat zamanla kariyer kanıtlarıyla değişir.

Reputation en az şu committed kariyer sonuçlarından etkilenebilir:

* beklentiye göre season performansı,
* önemli sportif başarı veya başarısızlık,
* kulüp seviyesini geliştirme,
* güçlü veya zayıf dismissal geçmişi,
* yeni kulüpte başarılı yeniden başlangıç,
* sürekli kısa employment dönemleri,
* önemli public narrative ve kriz sonuçları,
* kalıcı manager profile kanıtları.

Her küçük Match Result ayrı kalıcı reputation history kaydı üretmemelidir. Küçük etkiler dönemsel değerlendirmede birleştirilebilir.

Exact reputation ölçeği, tier sayısı ve delta formülü açık bırakılır (Bölüm 40).

---

## 21. Davranıştan Oluşan Manager Profile

Manager Profile başlangıç ekranında seçilen sabit sınıf değildir.

Profile:

* tekrar eden committed davranışlardan,
* uzun dönem sonuçlardan,
* employment ve career milestone'larından

türetilir.

### 21.1. Profile evidence modeli

Aşağıdaki GDD profile kategorilerini destekleyecek genişletilebilir bir profile evidence modeli tanımlanır:

* Genç oyuncu geliştiricisi
* Sert ve otoriter
* Oyuncu dostu
* Taktik uzmanı
* Savunma uzmanı
* Hücum futbolu savunucusu
* Kriz yöneticisi
* Yıldız futbolcularla sorun yaşayan
* Yönetimle sık çatışan
* Sadık
* Kariyer odaklı ve sık kulüp değiştiren
* Medyada tartışmalı
* Büyük maç uzmanı

### 21.2. Kurallar

* Aynı manager aynı anda birden fazla profile label taşıyabilir.
* Bazı profile signal'ları birbirini zayıflatabilir fakat yalnız handler sırasına göre birbirini silemez.
* Label'lar yalnız kozmetik değildir; Job Offer fit, medya yaklaşımı, oyuncuların ilk bağlamı veya board beklentisi için query/read model girdisi olabilir.
* Profile label başka context'in state'ini doğrudan değiştiremez.
* Starting Background yalnız başlangıç signal'ı sağlayabilir; kalıcı profile garantisi vermez.
* Profile eşikleri ve exact evidence ağırlıkları açık bırakılır.
* Label metinleri authoritative state yerine semantic profile kimliklerinden türetilmelidir.

---

## 22. Kariyer Geçmişi ve Milestone'lar

Career History en az şunları korur:

* Starting Background
* employment dönemleri
* club değişimleri
* dismissal kayıtları
* voluntary departure kayıtları
* season expectation sonuçları
* önemli sportif başarılar
* önemli başarısızlıklar
* reputation milestone'ları
* profile milestone'ları
* işsizlik dönemleri
* accepted employment offer'ları
* kariyerin normal veya erken bitişi

Her küçük Board Confidence veya Reputation değişimini ayrı sonsuz history kaydına dönüştürmek zorunlu değildir. Tamamlanmış dönemler ve önemli milestone'lar korunur; düşük önem hareketleri özetleme/compaction politikasına tabidir (Bölüm 33 ile uyumlu).

---

## 23. Haftalık Oynanış Döngüsüyle Entegrasyon

`docs/02_MVP_SCOPE.md` Bölüm 7-11'de tanımlanan haftalık kontrol merkezi ve planlama döngüsüyle entegrasyon:

* Manager Career & Employment, hafta başı özetine aktif employment durumu, Season Expectation özeti, Board Confidence bandı ve aktif Job Offer'ları read model olarak sağlar.
* Kritik kararlar listesine yalnız gerektiğinde (Job Offer deadline'ı, kritik Board warning, dismissal bildirimi) katkı sağlar.
* Haftalık kontrol merkezi bir domain sistemi değildir; bu context'in state'ini doğrudan değiştiremez (`docs/02_MVP_SCOPE.md` Bölüm 7.2 ile uyumlu).
* Zamanı yalnız Bölüm 9'da (MVP Scope) tanımlanan koşullar (örn. süresi dolacak kritik Job Offer kararı) durdurabilir.

---

## 24. Match ve Competition Entegrasyonu

* Match committed result ve performance fact üretir; bu context bu girdileri doğrudan tüketmez, ancak Board Assessment ve career evaluation'a girdi olarak kullanır.
* Competition accepted result, standings ve season sonuçlarını üretir.
* Manager Career & Employment bu girdileri Board Assessment ve career evaluation içinde değerlendirir.
* Match veya Competition Board Confidence'ı doğrudan değiştiremez.
* Manager Career Match Result'ı veya standings'i yeniden hesaplayamaz.

Yasak doğrudan mutation: Match/Competition → Board Confidence, Season Expectation veya Career History alanlarına doğrudan yazma.

---

## 25. Club & Governance Entegrasyonu

* Club politikaları, budget boundaries ve kurumsal hedef bağlamı Club & Governance'a aittir.
* Season Expectation kaydı Manager Career & Employment alanında, Club & Governance'ın sağladığı politika/hedef bağlamından oluşturulur.
* Club & Governance ClubEmployment state'ini doğrudan değiştiremez.
* Manager Career club bütçesini veya finansal state'i değiştiremez.

Yasak doğrudan mutation: Club & Governance → ClubEmployment veya Board Confidence alanlarına doğrudan yazma; Manager Career & Employment → Club budget/policy alanlarına doğrudan yazma.

---

## 26. Transfer ve Team Preparation Entegrasyonu

### 26.1. Transfer

* Teknik direktörün Sporting Approval kararı Transfer sisteminin tanımladığı sınırlar içinde kullanılır (`docs/08_TRANSFER_SYSTEM.md` D-131, D-133, D-134 ile uyumlu).
* Manager departure açık Transfer integration event'i üretir.
* Manager Career aktif Transfer Process'i doğrudan iptal edemez.
* Transfer owner, departure sonrasında açık süreçlerin devam, yeniden onay, invalidation veya cancellation ihtiyacını kendi kurallarıyla değerlendirir.
* Yeni teknik direktörün eski Sporting Approval kararlarını otomatik sahiplenmesi varsayılmaz.
* Kesin re-approval politikası Transfer sisteminin mevcut sınırlarıyla çelişmeyecek şekilde açık ve izlenebilir olmalıdır; exact politika Bölüm 40'ta açık bırakılır.

### 26.2. Team Preparation

* Squad, MatchSelection ve TacticPlan Team Preparation'a aittir.
* Aktif employment, manager'ın club üzerindeki sportif command yetkisinin ön koşuludur.
* Employment bittiğinde manager yeni squad veya tactic command gönderemez.
* Manager Career squad veya tactic state'ini doğrudan değiştiremez.

Yasak doğrudan mutation: Manager Career & Employment → TransferProcess, ClubSquad, MatchSelection veya TacticPlan alanlarına doğrudan yazma.

---

## 27. Relationship, Memory ve Promise Entegrasyonu

### 27.1. Relationship

* MVP'nin zorunlu ana ilişki yönü Player → Manager'dır (`docs/06_RELATIONSHIP_SYSTEM.md` D-093 ile uyumlu).
* Manager kulüpten ayrıldığında ilgili ilişkiler silinmez.
* Social Continuity kendi kurallarıyla ilişkileri `Dormant` hâle getirebilir.
* Manager ve player yeniden aynı profesyonel bağlama geldiğinde mevcut ilişki yeniden etkinleşebilir.
* Board Confidence, Player → Manager Relationship değildir.

### 27.2. Memory

* Dismissal, voluntary departure, önemli başarı, eski kulüple yeniden karşılaşma ve kariyer milestone'ları Memory candidate olabilir.
* Manager Career doğrudan MemoryRecord oluşturamaz veya değiştiremez.
* Memory authority committed event'i kendi kurallarıyla değerlendirir.
* Manager'ın kişisel career history kaydı ile aktörlerin öznel MemoryRecord'ları aynı kavram değildir.

### 27.3. Promise

* Manager dismissal veya voluntary departure aktif Promise'ları sessizce silmez.
* Manager Career Promise'ı doğrudan fulfilled, broken, invalidated veya cancelled yapamaz.
* Social Continuity, employment departure event'ini Promise condition ve context'e göre değerlendirir.
* Bazı Promise'lar invalidated, bazıları broken, bazıları başka bağlamda devam edebilir; nihai karar Promise owner'a aittir.

Yasak doğrudan mutation: Manager Career & Employment → Relationship, MemoryRecord veya Promise alanlarına doğrudan yazma.

---

## 28. Dialogue, Board ve Public Narrative Entegrasyonu

### 28.1. Dialogue ve Interaction

Aşağıdaki durumlar Decision Request veya Dialogue bağlamı üretebilir:

* Job Offer kabul/ret
* kritik Board warning
* dismissal bildirimi
* önemli Season Expectation revizyonu
* voluntary club change
* kariyer değerlendirmesi

Dialogue:

* offer state'ini doğrudan değiştiremez,
* employment başlatamaz veya bitiremez,
* Board Confidence değiştiremez,
* dismissal kararı veremez.

Oyuncu seçimi Application üzerinden owner-specific Command'a dönüştürülür (`docs/07_DIALOGUE_SYSTEM.md` D-116 ile uyumlu).

### 28.2. Public Narrative

Public Narrative:

* board assessment için girdi olabilir,
* reputation evaluation için girdi olabilir,
* doğrudan Board Confidence veya Reputation mutasyonu yapamaz.

Yasak doğrudan mutation: Dialogue veya Interaction & Narrative → Job Offer, ClubEmployment, Board Confidence veya Reputation alanlarına doğrudan yazma.

---

## 29. Command Kategorileri

Kesin üretim sınıfları veya enum'ları tanımlanmaz. Aşağıdakiler kavramsal sözleşme örnekleridir.

En az şunlar kapsanır:

* Start Manager Career
* Select Starting Background
* Generate Initial Job Offers
* Accept Job Offer
* Reject Job Offer
* Withdraw veya Invalidate Job Offer
* Activate Employment
* End Employment
* Request Voluntary Departure
* Set Season Expectation
* Revise Season Expectation
* Evaluate Board Confidence
* Record Board Assessment
* Record Dismissal Decision
* Begin Unemployment
* Run Job Market Evaluation
* Complete MVP Career
* End Career Due to Prolonged Unemployment

---

## 30. Domain Event ve Integration Event Kategorileri

En az şunlar kapsanır:

* Manager Career Started
* Starting Background Selected
* Job Offer Created
* Job Offer Accepted
* Job Offer Rejected
* Job Offer Expired
* Job Offer Withdrawn
* Job Offer Invalidated
* Employment Activated
* Season Expectation Set
* Season Expectation Revised
* Board Assessment Completed
* Board Confidence Changed
* Employment Risk Changed
* Manager Dismissed
* Employment Ended
* Manager Became Unemployed
* Manager Employment Changed
* Reputation Milestone Reached
* Profile Evidence Recorded
* Manager Profile Changed
* Career Evaluation Completed
* Manager Career Completed
* Manager Career Ended Early

Her event (`docs/03_DOMAIN_MODEL.md` Bölüm 14.2 ile uyumlu):

* committed domain gerçeği olmalı,
* geçmiş zamanlı olmalı,
* GameTime taşımalı,
* EventId ve schema version bilgisine sahip olmalı,
* gerekli causation/correlation bilgisini taşımalı,
* başka context'in state'ini doğrudan değiştirme talimatı olmamalıdır.

---

## 31. Application Orkestrasyon Akışları

### 31.1. Kariyer başlangıcı ve ilk employment aktivasyonu

* **Authoritative owner'lar:** Manager Career & Employment (career, employment, offer, expectation, board confidence), Club & Governance (policy/hedef bağlamı, salt okunur).
* **Transaction/checkpoint sınırı:** Simülasyon zamanı başlamadan önce tek bir Application-owned checkpoint; kariyer başlangıcı ya tamamen tamamlanır ya da hiç başlamamış sayılır.
* **Duplicate koruması:** Aynı career creation isteği ikinci kez işlenemez; ProcessingKey ile korunur.
* **Başarısızlıkta bırakılacak state:** Hiçbir kısmi ManagerCareer, ClubEmployment veya Season Expectation kaydı bırakılmaz.
* **Üretilen event'ler:** Manager Career Started, Starting Background Selected, Job Offer Created (çoğul), Job Offer Accepted, Employment Activated, Season Expectation Set, Board Confidence başlangıç kaydı.

### 31.2. Board Assessment ve Board Confidence güncellemesi

* **Authoritative owner:** Manager Career & Employment.
* **Girdi sağlayıcılar:** Match/Competition (sonuç ve standings fact'leri), Club & Governance (politika uyumu), Interaction & Narrative (public narrative fact'leri).
* **Transaction sınırı:** Tek bir Board Assessment değerlendirmesi tek bir ProcessingKey ile atomik uygulanır.
* **Duplicate koruması:** Aynı değerlendirme penceresi ve tetikleyici için aynı ProcessingKey ikinci kez sonuç üretmez.
* **Başarısızlıkta bırakılacak state:** Önceki Board Confidence ve Employment Risk değeri korunur; kısmi güncelleme uygulanmaz.
* **Üretilen event'ler:** Board Assessment Completed, gerekiyorsa Board Confidence Changed ve Employment Risk Changed.

### 31.3. Dismissal ve unemployment başlangıcı

* **Authoritative owner:** Manager Career & Employment.
* **Ön koşul:** Committed Board Assessment ve açık Dismissal Decision.
* **Transaction sınırı:** ClubEmployment kapanışı, club assignment boşaltma ve Manager Career state geçişi tek bir Application-owned adımda atomik yürütülür.
* **Duplicate koruması:** Aynı dismissal causation'ı ikinci kez uygulanamaz.
* **Başarısızlıkta bırakılacak state:** ClubEmployment `Active` kalır; kısmi `Ending` state bırakılmaz.
* **Üretilen event'ler:** Manager Dismissed, Employment Ended, Manager Became Unemployed.

### 31.4. Unemployed manager için offer oluşturulması

* **Authoritative owner:** Manager Career & Employment.
* **Girdi sağlayıcılar:** Club & Governance (vacancy bağlamı).
* **Transaction sınırı:** Her Job Market Evaluation çalıştırması kendi ProcessingKey'i ile atomik sonuç üretir.
* **Duplicate koruması:** Aynı tetikleyici ve zaman penceresi için aynı evaluation ikinci kez offer üretmez.
* **Başarısızlıkta bırakılacak state:** Kısmi veya tutarsız Job Offer kaydı bırakılmaz.
* **Üretilen event'ler:** Job Offer Created (sıfır veya daha fazla).

### 31.5. Job Offer acceptance ve employment activation

* **Authoritative owner:** Manager Career & Employment.
* **Transaction sınırı:** Bölüm 11.6'daki aktivasyon süreci tek bir Application-owned Unit of Work'te tamamlanır.
* **Duplicate koruması:** Aynı offer iki kez kabul edilemez; aynı activation iki kez tamamlanamaz.
* **Başarısızlıkta bırakılacak state:** Offer `Offered` state'inde kalır veya açık policy ile invalidate edilir; kısmi ClubEmployment oluşturulmaz.
* **Üretilen event'ler:** Job Offer Accepted, Employment Activated, Season Expectation Set.

### 31.6. Employed manager'ın başka kulübe geçmesi

* **Authoritative owner:** Manager Career & Employment.
* **Transaction sınırı:** Bölüm 19'daki 10 adımlık geçiş tek bir Application-owned Unit of Work'te atomik yürütülür.
* **Duplicate koruması:** Aynı offer için ikinci kez employment değişimi tetiklenemez.
* **Başarısızlıkta bırakılacak state:** Mevcut employment `Active` kalır; yeni employment oluşturulmaz.
* **Üretilen event'ler:** Job Offer Accepted, Employment Ended (Voluntary Departure), Manager Employment Changed, Employment Activated, Season Expectation Set.

### 31.7. 365 günlük unemployment sınırı ve early career end

* **Authoritative owner:** Manager Career & Employment.
* **Transaction sınırı:** Her unemployment deadline kontrolü kendi ProcessingKey'i ile atomik değerlendirilir.
* **Duplicate koruması:** Aynı unemployment dönemi için 365. gün kontrolü ikinci kez `EndedEarly` üretmez.
* **Özel kural:** Kabul edilmiş ve güvenli activation sürecinde bulunan geçerli bir Job Offer varsa 365. gün kontrolü süreci yarıda kesmez; activation sonucu beklenir.
* **Başarısızlıkta bırakılacak state:** Career `Unemployed` state'inde kalır.
* **Üretilen event'ler:** Manager Career Ended Early (`Prolonged Unemployment` nedeniyle).

### 31.8. Onuncu sezon sonu career completion ve evaluation

* **Authoritative owner:** Manager Career & Employment.
* **Girdi sağlayıcılar:** Competition (season completion fact'i), World & Calendar (season boundary).
* **Transaction sınırı:** Açık simulation step güvenli checkpoint'e getirildikten sonra tek bir Application-owned adımda career completion ve evaluation üretilir.
* **Duplicate koruması:** Aynı career için completion ikinci kez uygulanamaz.
* **Başarısızlıkta bırakılacak state:** Career `Employed`/`Unemployed` state'inde kalır; yarım Career Evaluation bırakılmaz.
* **Üretilen event'ler:** Career Evaluation Completed, Manager Career Completed.

---

## 32. Determinizm, Idempotency ve Conflict Resolution

Bağlayıcı kurallar (`docs/04_EVENT_RULE_ENGINE.md` Bölüm 10-11 ile uyumlu):

* Oyun zamanı için duvar saati kullanılmaz.
* Job market evaluation gizli global RNG kullanamaz; yalnız açık seeded Random Context kullanılabilir.
* Aynı state, content version, simulation version ve seed aynı offer sonuçlarını üretmelidir.
* Aynı Match Result veya Competition result aynı Board Assessment etkisini iki kez uygulayamaz.
* Aynı Job Offer iki kez kabul edilemez.
* Aynı dismissal iki kez uygulanamaz.
* Aynı employment transition iki kez tamamlanamaz.
* Handler sırası conflict çözüm kuralı olamaz.
* Aynı simulation step içinde dismissal ve accepted offer çakışırsa açık conflict policy kullanılır.
* Terminal state'teki Career, Employment veya JobOffer normal command ile yeniden açılamaz.
* Application process manager tamamlanan adımları ve business completion identity'yi korumalıdır.
* Exactly-once transport varsayılmaz; mantıksal tekil etki idempotency ile sağlanır.

### Conflict policy öncelikleri

* Commit edilmiş employment ending kararı, sonradan gelen eski employment command'larını geçersiz kılar.
* Expired offer acceptance reddedilir.
* Aynı gün offer acceptance ve expiry varsa canonical simulation ordering kullanılır.
* Club başka bir manager employment'ını önce aktive etmişse ikinci activation reddedilir.
* Manager başka bir employment'ı önce aktive etmişse ikinci activation reddedilir.
* Kariyer completion sınırına ulaşılmışsa yeni employment activation başlamaz.

---

## 33. Save/Load ve Veri Bütünlüğü

Save dosyası en az şunları korur:

* Manager identity
* Starting Background
* Career state
* Career başlangıç tarihi
* World season/career boundary bilgisi
* Manager Reputation
* Profile evidence ve current profile projection için gerekli state
* Career History ve önemli milestone'lar
* Active ClubEmployment
* Employment history
* Active Job Offer'lar
* Offer deadline ve lifecycle state'leri
* Season Expectations
* Board Confidence
* Son Board Assessment
* Employment risk
* Dismissal kayıtları
* Unemployment başlangıç tarihi
* 365 günlük unemployment deadline
* Aktif employment/offer process manager state'i
* Idempotency ve business completion kayıtları
* Career Evaluation ve completion state'i

### Kurallar

* Save/load ManagerId veya EmploymentId değiştiremez.
* Load sırasında iki active employment bulunursa save invalid kabul edilmelidir; sessiz seçim yapılamaz.
* Aynı club için iki active manager assignment bulunursa save invalid kabul edilmelidir.
* Accepted offer ile activation process state'i tutarlı olmalıdır.
* Unemployment deadline load sonrasında yeniden başlatılamaz.
* Career completed save yüklenebilir, ancak normal MVP time progression yeniden açılamaz.
* Persistence şeması veya SQLite tablo tasarımı bu belgede belirlenmez (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` ile uyumlu).

---

## 34. Veri Büyümesi ve Retention

### Kalıcı korunması gerekenler

* Manager identity
* employment dönemleri
* dismissal ve voluntary departure kayıtları
* önemli season sonuçları
* önemli career milestone'ları
* normal veya early career end
* güncel Reputation ve Profile için gerekli authoritative state
* aktif offer ve employment süreçleri
* güncel Board Confidence ve expectation'lar

### Özetlenebilecek veya retention politikasına tabi tutulabilecekler

* her küçük Board Confidence delta'sı,
* her rutin periodic assessment'ın tam ayrıntısı,
* reddedilen düşük önem Job Offer geçmişi,
* düşük önem profile evidence girdileri,
* her maç için ayrı reputation mikro etkisi,
* eski debug/audit kayıtları.

### Compaction kuralları

Compaction:

* authoritative current state'i değiştiremez,
* önemli dismissal veya employment history'yi silemez,
* current profile/reputation sonucunun yeniden açıklanmasını imkânsız hâle getiremez,
* aktif process/idempotency kayıtlarını kaldıramaz.

---

## 35. Presentation ve Read Model Sınırı

Haftalık kontrol merkezi bir domain sistemi değildir (`docs/02_MVP_SCOPE.md` Bölüm 7.2 ile uyumlu).

Manager Career read model en az şunları sunabilmelidir:

* aktif club ve employment başlangıç tarihi,
* Season Expectation özeti,
* Board Confidence bandı,
* son değişim yönü,
* Board Confidence'ın ana olumlu ve olumsuz nedenleri,
* employment risk bandı,
* aktif Job Offer'lar ve deadline'ları,
* unemployment başlangıcı ve kalan maksimum süre,
* Manager Reputation özeti,
* mevcut profile label'ları ve ana kanıt kategorileri,
* önemli Career History ve milestone'lar,
* onuncu sezon sınırına kalan süre,
* completed career evaluation.

UI:

* offer kabul veya ret command'ını Application'a gönderir,
* resignation/club change kararını Application'a gönderir,
* Board Confidence veya Career state'ini doğrudan yazamaz,
* employment bitiremez,
* offer deadline değiştiremez,
* profile label atayamaz.

---

## 36. Sınır Durumları

1. **Starting Background seçildikten sonra başlangıç teklifi üretilememesi:** Kariyer başlangıcı tamamlanmaz; Application en az bir geçerli teklif garantisi sağlanana kadar başlangıç akışını tamamlamaz.
2. **Başlangıç teklifinin kabul anında invalid hâle gelmesi:** Acceptance reddedilir; oyuncuya güncel geçerli teklif seti yeniden sunulur.
3. **Manager zaten aktif employment sahibiyken ikinci activation:** İkinci activation reddedilir; mevcut employment korunur.
4. **Club zaten aktif manager sahibiyken activation:** İkinci activation reddedilir.
5. **Aynı offer'ın iki kez kabul edilmesi:** İkinci kabul reddedilir; yalnız ilk sonuç geçerli sayılır.
6. **Offer'ın acceptance ile aynı simulation step'te expire olması:** Canonical simulation ordering kullanılır (Bölüm 32).
7. **Dismissal ile dış offer acceptance'ın aynı simulation step'e düşmesi:** Commit edilmiş employment ending kararı önceliklidir (Bölüm 32).
8. **Season boundary gününde dismissal:** Dismissal işlenir; season transition ile aynı checkpoint sırasında çakışma açık conflict policy ile çözülür.
9. **Onuncu sezon sonunda açık Job Offer:** Yeni employment activation başlamaz; offer açık policy ile invalidate veya archive edilir.
10. **Onuncu sezon sonunda active employment transition:** Career completion sınırına ulaşıldığından yeni transition başlamaz.
11. **Unemployment'ın 365. gününde kabul edilmiş pending offer:** Activation süreci beklenir; 365. gün kontrolü süreci yarıda kesmez.
12. **365. gün herhangi bir geçerli offer olmaması:** Kariyer `EndedEarly` (`Prolonged Unemployment`) olur.
13. **Save/load sırasında accepted offer fakat eksik process manager:** Save invalid kabul edilir; sessizce tamamlanmış varsayılmaz.
14. **Save/load sırasında iki active employment:** Save invalid kabul edilir.
15. **Manager ayrılırken açık Promise:** Promise sessizce silinmez; Social Continuity kendi kurallarıyla değerlendirir (Bölüm 27.3).
16. **Manager ayrılırken açık Transfer Process:** Transfer owner kendi kurallarıyla değerlendirir (Bölüm 26.1).
17. **Manager ayrıldıktan sonra Player → Manager Relationship'in korunması:** İlişki silinmez; Dormant olabilir (Bölüm 27.1).
18. **Eski club'a yeni manager olarak dönüş:** Geçmiş employment history korunur; yeni ClubEmployment ayrı kayıt olarak açılır.
19. **Eski futbolcuyla başka club'da yeniden karşılaşma:** Kişisel Memory ve Relationship kayıtları korunduğundan yeniden etkinleşebilir.
20. **Aynı club tarafından tekrar işe alınma:** Yeni Job Offer ve yeni ClubEmployment kaydı gerektirir; eski kayıt tarihsel olarak korunur.
21. **Çok kısa employment dönemlerinin Reputation/Profile etkisi:** Tekrarlanan kısa dönemler profile evidence ve reputation değerlendirmesine girdi olabilir (Bölüm 20, 21).
22. **Career completed save'in yeniden yüklenmesi:** Save yüklenebilir; normal time progression yeniden açılamaz.
23. **Düşük Board Confidence fakat henüz committed dismissal olmaması:** Employment `Active` kalır; yalnız Employment Risk bandı düşük gösterilir.
24. **Critical assessment'ın duplicate işlenmesi:** ProcessingKey ile engellenir; ikinci işlem etkisiz kalır.
25. **Bir club vacancy event'inin iki offer üretim zincirini tetiklemesi:** Aynı vacancy için duplicate offer üretimi ProcessingKey ile engellenir.

---

## 37. Otomatik Test Stratejisi

Bu bölüm üretim test kodu içermez; teknoloji bağımsız test matrisini tanımlar (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 13 ile uyumlu).

### 37.1. Unit test aileleri

* Manager Career lifecycle
* ClubEmployment lifecycle
* JobOffer lifecycle
* Starting Background validation
* Season Expectation validation
* Board Assessment
* Board Confidence band projection
* Employment risk projection
* Reputation evaluation
* Profile evidence aggregation
* 365 günlük unemployment deadline
* Career Evaluation

### 37.2. Invariant testleri

* Manager başına en fazla bir active employment
* Club başına en fazla bir active manager
* Active employment ile unemployment'ın aynı anda bulunmaması
* Terminal offer'ın tekrar kabul edilememesi
* Terminal employment'ın tekrar aktive edilememesi
* Career completed state'in yeniden açılamaması
* Dismissal'ın ikinci kez uygulanamaması
* Employment history'nin current employment ile tutarlı olması

### 37.3. Integration testleri

* Match Result → Board Assessment
* Competition standings → expectation evaluation
* Dismissal → unemployment
* Dismissal → Promise/Memory/Relationship integration events
* Departure → Transfer re-evaluation
* Job Offer acceptance → employment activation
* Employed manager club change
* Club vacancy → job market evaluation
* Dialogue choice → owner-specific command
* Onuncu sezon → career evaluation

### 37.4. Process manager testleri

* Başlangıç employment aktivasyonu
* Dismissal transition
* Employed manager club change
* Accepted offer activation
* Kısmi başarısızlık ve rollback
* Save/load ortasında process devamı
* Duplicate event sonrası yalnız bir final completion

### 37.5. Determinism ve idempotency testleri

* Aynı job market state ve seed ile aynı offer seti
* Aynı career events ile aynı Reputation/Profile sonucu
* Aynı Board Assessment girdileriyle aynı sonuç
* Aynı EventId'nin ikinci kez uygulanmaması
* Aynı offer acceptance'ın ikinci kez completion üretmemesi

### 37.6. Property ve uzun dönem testleri

En az şu property'ler kontrol edilir:

* Hiçbir simülasyon tarihinde bir manager iki club'da aktif değildir.
* Hiçbir club aynı anda iki active manager taşımaz.
* Career History kronolojik olarak geriye gitmez.
* Employment bitiş tarihi başlangıç tarihinden önce olamaz.
* Offer deadline offer creation tarihinden önce olamaz.
* Unemployment deadline save/load ile değişmez.
* Completed career yeniden active hâle gelemez.

### 37.7. 10 sezonluk soak test

Birden fazla seed ile en az 10 sezon simüle edilir.

Raporlanması gereken ölçümler:

* dismissal sayısı ve sezonlara dağılımı,
* voluntary club change sayısı,
* işsizlik süreleri,
* 365 günlük early end sayısı,
* oluşturulan, kabul edilen, reddedilen ve expired offer sayıları,
* club başına manager turnover,
* manager başına employment dönemi sayısı,
* duplicate veya invalid employment denemeleri,
* Board Assessment hacmi,
* Reputation/Profile evidence veri büyümesi,
* save dosyası ve snapshot büyümesi,
* aynı seed determinism sonucu,
* invalid manager/club assignment sayısı,
* kariyerlerin kaçının onuncu sezon değerlendirmesine ulaştığı.

Kesin denge hedefleri bu belgede belirlenmez; ancak aşırı dismissal, hiç dismissal olmaması, teklif açlığı, sürekli aynı club'lar arası geçiş ve kontrolsüz veri büyümesi raporlanmalıdır.

---

## 38. İlk Dikey Kesit Sınırı

İlk dikey kesit için zorunlu minimum:

* Manager identity
* Starting Background
* En az bir geçerli başlangıç Job Offer
* Offer kabul/ret
* Employment activation
* Club active-manager assignment invariant'ı
* İlk Season Expectation
* Başlangıç Board Confidence
* Match/Competition sonucundan en az bir gerçek Board Assessment
* Board Confidence değişiminin açıklaması
* En az bir employment risk bandı değişimi
* Career History milestone
* Save/load
* Duplicate offer acceptance koruması
* Duplicate employment activation koruması
* Headless test edilebilirlik

İlk dikey kesitte dismissal, unemployment ve kulüp değiştirme tam oynanabilir olmak zorunda değildir; ancak aggregate sınırları, event sözleşmeleri ve save state'i daha sonraki kilometre taşını engellemeyecek biçimde gerçek olmalıdır.

MVP Kilometre Taşı 3'te (`docs/02_MVP_SCOPE.md` Bölüm 6.3 ile uyumlu) dismissal, unemployment, limited Job Offer ve club change gerçek domain kurallarıyla tamamlanır.

---

## 39. MVP Dışı Kapsam

Aşağıdakiler açıkça MVP dışında bırakılır:

* Futbolcu kariyeri
* Milli takım teknik direktörlüğü
* Ayrıntılı manager sözleşmesi
* Manager maaşı ve kişisel ekonomi
* Fesih tazminatı
* Manager agent veya temsilci sistemi
* Ayrıntılı iş başvurusu
* Ayrıntılı mülakatlar
* Çok turlu manager contract negotiation
* Teknik ekibin club'lar arasında taşınması
* Personel işe alma ve ekip kurma ayrıntıları
* Post-career sportif direktörlük
* Kulüp sahipliği
* Akademi sahipliği
* Emeklilik sonrası yatırım veya medya kariyeri
* Gelişmiş kişisel board-member ilişkileri
* Tam küresel manager pazarı
* Harici generative AI ile iş teklifi veya kariyer sonucu üretimi

Bu özelliklerin gelecekte eklenmesini engelleyen bir domain kısıtı oluşturulmamıştır; yalnız MVP kapsamı dışında tutulmuşlardır.

---

## 40. Açık Bırakılan Implementasyon Ayrıntıları

Aşağıdaki konular bu belgede sessiz varsayımla kesinleştirilmemiş, açık bırakılmıştır:

* Exact Board Confidence puanlama formülü ve sayısal ölçeği.
* Exact Manager Reputation puanlama formülü, tier sayısı ve delta formülü.
* Exact Manager Profile eşikleri ve evidence ağırlıkları.
* Exact Job Offer puanlama formülü.
* Exact periyodik Board Assessment gün sayısı veya katsayısı.
* Exact aktif Job Offer limiti ve market review aralığı.
* Manager maaşı, sözleşme maddeleri, tazminat ve fesih bedeli ayrıntıları.
* Ayrıntılı iş başvurusu, mülakat ve manager agent sistemi ayrıntıları.
* Teknik ekibin club'lar arasında taşınması ayrıntıları.
* Milli takım teklifleri.
* Post-career rol geçişleri.
* Persistence şeması veya SQLite tablo tasarımı.
* UI ekran tasarımı ve ayrıntılı sunum kararları.
* Transfer re-approval politikasının exact kuralları (yalnız Transfer sisteminin mevcut sınırlarıyla çelişmemesi gerektiği belirtilmiştir).

Bu kararlar ilgili teknik spike'lar veya gelecekteki alt sistem/implementasyon belgeleri olmadan sessizce kesinleştirilemez.

---

## 41. Tutarlılık Kontrol Listesi

Bu belgenin aşağıdaki kesinleşmiş belgelerle tutarlılığı doğrulanmıştır:

* [x] `docs/01_GAME_DESIGN_DOCUMENT.md` Bölüm 7.1 ve 8.1 ile uyumlu (starting background, profil kategorileri, sorumluluklar).
* [x] `docs/02_MVP_SCOPE.md` Bölüm 4, 5, 6.3, 13.1 ile uyumlu (yetki sınırları, kariyer başlangıcı/bitişi, Kilometre Taşı 3, zorunlu kapsam).
* [x] `docs/03_DOMAIN_MODEL.md` Bölüm 7.5, 8, 11, 12.2, 13, 16.1 ile uyumlu (context sorumluluğu, aggregate root'lar, veri sahipliği, lifecycle, invariant'lar, kulüp değişimi).
* [x] `docs/04_EVENT_RULE_ENGINE.md` Bölüm 16.4, 16.5, 27, 31 ile uyumlu (dismissal/club join process, veri sahipliği matrisi, test matrisi).
* [x] `docs/05_MEMORY_AND_PROMISE_SYSTEM.md` D-084, D-085 ile uyumlu (kulüp değişiminde Memory korunması, departure'da Promise değerlendirmesi).
* [x] `docs/06_RELATIONSHIP_SYSTEM.md` D-093, D-103, D-106 ile uyumlu (Player → Manager yönü, ilişki korunması, Board Confidence/Relationship ayrımı).
* [x] `docs/07_DIALOGUE_SYSTEM.md` D-112, D-116, D-125 ile uyumlu (Dialogue'un authoritative owner olmaması, command üretimi).
* [x] `docs/08_TRANSFER_SYSTEM.md` D-131, D-133, D-134, D-145 ile uyumlu (Sporting/Financial Approval ayrımı, Board Confidence terimi).
* [x] `docs/09_MATCH_SIMULATION.md` D-160, D-165 ile uyumlu (Match'in foreign context state'ini doğrudan değiştirememesi).
* [x] `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` ile uyumlu (persistence, determinizm ve test altyapısı yönü).
* [x] Yeni bounded context oluşturulmamıştır; mevcut 14 bounded context sınırı korunmuştur.
* [x] `BoardTrust`, `Board Confidence` ve "yönetim güveni" aynı kavram olarak ele alınmış, ikinci bir state oluşturulmamıştır.
* [x] Bir manager veya club için ikinci active employment/assignment state'i tanımlanmamıştır.
* [x] Dismissal doğrudan oyun sonu olarak tanımlanmamıştır.
* [x] 365 günlük unemployment sınırı ve onuncu sezon sonu career evaluation kuralı belgelenmiştir.
* [x] Kod, test kodu, proje dosyası veya yapılandırma üretilmemiştir.
