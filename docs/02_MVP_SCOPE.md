# MVP Kapsamı

**Belge:** `docs/02_MVP_SCOPE.md`
**Durum:** Kesinleşti
**Kapsam:** İlk oynanabilir sürüm — MVP
**Ana referans:** `docs/01_GAME_DESIGN_DOCUMENT.md`
**İlgili karar günlüğü:** `docs/15_DECISION_LOG.md`

---

# 1. Belgenin Amacı

Bu belge, Football Career Simulator projesinin ilk oynanabilir sürümünün kesin kapsamını, sınırlarını, geliştirme kilometre taşlarını ve ölçülebilir kabul kriterlerini tanımlar.

Belge aşağıdaki soruyu doğrulayacak en küçük fakat gerçek anlamda oynanabilir ürünü tarif eder:

> Transfer, taktik, maç, futbolcu ilişkileri, verilen sözler, hafıza ve dinamik olaylardan oluşan çekirdek oyun; en az 5–10 sezon boyunca anlamlı, açıklanabilir ve tekrar etmeyen kararlar üretebiliyor mu?

Bu belge:

* nihai oyun vizyonunu değiştirmez,
* MVP dışındaki özellikleri geliştirme kapsamına almaz,
* oyun motoru, programlama dili, framework veya veritabanı seçmez,
* alt sistemlerde henüz kararlaştırılmamış ayrıntılar hakkında sessiz varsayım yapmaz,
* `docs/01_GAME_DESIGN_DOCUMENT.md` içinde tanımlanan değişmez proje kurallarına tabidir.

---

# 2. Bağlayıcı Tasarım İlkeleri

MVP geliştirilirken aşağıdaki ilkeler bağlayıcıdır.

## 2.1. Olaylar üzerinden sistem etkileşimi

Bir sistem başka bir sistemin verisini doğrudan ve kontrolsüz biçimde değiştirmemelidir.

Sistemler arası etkiler mümkün olduğunca şu zincir üzerinden ilerlemelidir:

1. Kaynak sistem bir olay üretir.
2. Olay gerekli bağlam bilgilerini taşır.
3. İlgili sistemler kendi kurallarını değerlendirir.
4. Sonuçlar yeni olaylar veya tanımlı domain işlemleri olarak üretilir.
5. Sonuçların yalnızca bir kez uygulanması güvence altına alınır.

Örnek:

`TrainingFocusSelected`
→ Training System olayı değerlendirir
→ futbolcu yükü, gelişim ve sakatlık riski sonuçlarını hesaplar
→ ilgili sonuç olaylarını yayınlar.

## 2.2. Kullanıcı arayüzünün sınırı

Kullanıcı arayüzü iş kurallarının sahibi değildir.

Arayüz:

* domain sistemlerinden gelen bilgileri gösterir,
* oyuncu kararlarını toplar,
* komutları ilgili sisteme iletir,
* sonuçları ve bekleyen kararları sunar.

Arayüz doğrudan:

* moral,
* ilişki,
* kondisyon,
* hafıza,
* söz durumu,
* transfer durumu,
* yönetim güveni

gibi domain verilerini değiştiremez.

## 2.3. Uzun dönem bütünlüğü

Kısa vadeli geliştirme hızı uğruna:

* kayıt bütünlüğü,
* veri doğrulama,
* olayların tekil uygulanması,
* rastlantısallığın tekrar üretilebilirliği,
* en az 10 sezonluk simülasyon,
* eski kayıtların sürüm bilgisi

ihmal edilemez.

## 2.4. MVP kapsam disiplini

Her özellik şu kategorilerden birine açıkça atanmalıdır:

1. MVP sınırları içinde tam işlevli
2. MVP’de gerçek fakat sadeleştirilmiş
3. İlk dikey kesitte geçici, soyut veya özet temsil
4. MVP sonrasına ertelenmiş

Kategorisi belirlenmemiş özellik üretim kapsamına alınamaz.

---

# 3. MVP’nin Temel Oyuncu Deneyimi

## 3.1. Oyuncunun rolü

Oyuncu, bir profesyonel futbol kulübünün A takım teknik direktörüdür.

Oyuncu:

* kulüp sahibi değildir,
* kulüp başkanı değildir,
* tam yetkili genel menajer değildir,
* büyük finansal kararların nihai sahibi değildir.

Oyuncu öncelikle kulübün sportif performansından, A takım kadrosundan ve futbolcularla kurulan profesyonel ilişkilerden sorumludur.

## 3.2. Temel oyuncu fantezisi

> Yalnızca maç kazanan biri değil, sportif ve insani kararları yıllar boyunca hatırlanan bir teknik direktör olmak.

## 3.3. Ana eğlence kaynağı

MVP’nin ana eğlence kaynağı, birbirleriyle çatışan sportif ve insani kararların kısa ve uzun vadeli sonuçlarıdır.

Örnek karar çatışmaları:

* Formda futbolcuyu oynatmak veya söz verilen genç futbolcuya şans vermek
* Yıldız futbolcuyu memnun etmek veya takım içi adalet algısını korumak
* Kritik maç için yorgun kadroyu kullanmak veya sonraki haftaları düşünmek
* Kısa vadeli sonuç için yüksek riskli taktik kullanmak veya daha kontrollü oynamak
* Futbolcuyu basın önünde korumak veya disiplin mesajı vermek
* Yönetimin beklentisine uymak veya teknik direktörün sportif planını savunmak

## 3.4. Başarı tanımı

Başarı yalnızca kupa veya lig şampiyonluğu değildir.

Aşağıdakiler başarı göstergesi olabilir:

* Yönetim beklentilerini karşılamak veya aşmak
* Zor durumdaki bir kulübü geliştirmek
* Daha iyi iş teklifleri almak
* Futbolcuların güvenini kazanmak
* Genç futbolcular geliştirmek
* Mali ve sportif sınırlar içinde rekabet etmek
* Teknik direktör kimliğinin dünyada tanınması
* Geçmiş kararların yıllar sonra olumlu sonuç üretmesi
* Bir kulüpte sürdürülebilir sportif yapı oluşturmak
* İşten çıkarılma sonrasında kariyeri yeniden kurmak

## 3.5. Başarısızlık tanımı

Başarısızlık, kaydın silinmesi veya her olumsuz sonuçta anlık oyun sonu değildir.

Başarısızlık daha çok kariyer yönünün değişmesi şeklinde çalışır:

* yönetim güveninin azalması,
* futbolcu güveninin kaybedilmesi,
* sözlerin ihlal edilmesi,
* transfer hedeflerinin kaçırılması,
* sportif sonuçların kötüleşmesi,
* işten çıkarılma,
* daha düşük seviyeli bir kulüpte yeniden başlama,
* itibarın ve kariyer fırsatlarının azalması.

---

# 4. Teknik Direktörün Yetki Sınırları

## 4.1. Teknik direktörün doğrudan kontrol alanı

Oyuncu aşağıdaki alanlarda doğrudan karar verir:

* A takım maç kadrosu
* İlk 11
* Yedek futbolcular
* Temel taktik
* Maç planı
* Maç içi sınırlı müdahaleler
* Haftalık ana antrenman odağı
* Genel antrenman yoğunluğu
* Dinlenme yaklaşımı
* Kritik futbolcu görüşmeleri
* Futbolcuya söz verme
* Sözü reddetme veya erteleme
* Transfer ihtiyacını belirleme
* Transfer hedeflerini önceliklendirme
* Bir futbolcunun sportif uygunluğu hakkında karar verme
* Kritik basın cevapları
* Kritik yönetim cevapları

## 4.2. Kulüp yönetiminin kontrol alanı

Kulüp yönetimi aşağıdaki alanların nihai sınırlarını belirler:

* Transfer bütçesi
* Maaş bütçesi
* Sezon beklentileri
* Kulüp politikaları
* Büyük finansal kararlar
* Nihai mali onaylar
* Teknik direktörün iş güvenliği
* Teknik direktörün görev durumu

## 4.3. Açık bırakılan transfer yetkileri

Aşağıdaki konular bu belgede kesinleştirilmemiştir:

* Transfer müzakeresini hangi aktörün yürüttüğü
* Teknik direktörün nihai veto hakkı
* Yönetimin teknik direktör istemeden futbolcu transfer edip edemeyeceği
* Sözleşme ayrıntılarını hangi aktörün belirlediği
* Nihai finansal onayın hangi koşullarda reddedilebileceği

Bu ayrıntılar transfer sistemi tasarım belgesinde kararlaştırılacaktır.

---

# 5. Kariyer Başlangıcı, Süresi ve Bitişi

## 5.1. Başlangıç noktası

MVP kariyeri:

> Birinci sezonun sezon öncesi hazırlık döneminin ilk gününde başlar.

## 5.2. Normal kariyer süresi

MVP oynanış kapsamı:

> En fazla 10 tamamlanmış sezondur.

## 5.3. Normal bitiş noktası

MVP’nin normal bitişi:

> Onuncu tamamlanmış sezonun sonundaki kariyer değerlendirmesidir.

## 5.4. Kayıt dosyasının durumu

Onuncu sezonun tamamlanması:

* kayıt dosyasını silmez,
* kayıt dosyasını bozmaz,
* kayıt dosyasını kullanılamaz hâle getirmez.

Ancak MVP’nin doğrulanmış oynanış kapsamı onuncu sezon sonunda tamamlanmış sayılır.

Onuncu sezon sonrasında açık uçlu kariyer devamı MVP kapsamında değildir.

## 5.5. İşten çıkarılma

İşten çıkarılma doğrudan oyun sonu değildir.

Oyuncu:

* bir süre işsiz kalabilir,
* itibarına göre sınırlı iş teklifleri alabilir,
* daha düşük seviyeli bir kulüpte kariyerini yeniden kurabilir,
* eski kulüpleriyle yeniden karşılaşabilir,
* eski futbolcularıyla başka kulüplerde yeniden karşılaşabilir,
* geçmiş ilişkilerini ve kişisel hafızalarını korur.

## 5.6. MVP dışında kalan kariyer piyasası ayrıntıları

MVP’de bulunmayacaktır:

* ayrıntılı iş başvurusu sistemi,
* ayrıntılı iş görüşmeleri ve mülakatlar,
* teknik ekibin yeni kulübe taşınması,
* ayrıntılı sözleşme tazminatı,
* milli takım teklifleri,
* gelişmiş teknik direktör menajerlik piyasası.

## 5.7. Açık kariyer sonu konuları

Aşağıdakiler ilgili alt sistem belgesinde kararlaştırılacaktır:

* işsizliğin maksimum süresi,
* erken kariyer sonu koşulu,
* onuncu sezon öncesinde kalıcı kariyer sonunun hangi koşullarda oluşacağı.

---

# 6. Geliştirme Kilometre Taşları

MVP tek parçada geliştirilmeyecektir.

## 6.1. Kilometre Taşı 1 — Tek sezonluk dikey kesit

Amaç:

* haftalık karar döngüsünü,
* kadro yönetimini,
* minimum taktik sistemini,
* minimum maç simülasyonunu,
* haftalık antrenman odağını,
* yorgunluk ve sakatlık etkilerini,
* sınırlı ilişkileri,
* sınırlı söz türlerini,
* kayıt ve yüklemeyi

tek kulüpte ve tek sezonda doğrulamaktır.

Bu aşama nihai MVP değildir.

## 6.2. Kilometre Taşı 2 — Aynı kulüpte çok sezon

Amaç:

* sezon geçişini,
* futbolcu yaşlanmasını,
* futbolcu gelişimini ve düşüşünü,
* transfer dönemlerini,
* hafıza etkilerini,
* verilen sözlerin uzun vadeli sonuçlarını,
* futbolcu emekliliğini,
* yeni futbolcu üretimini,
* kayıt dosyasının büyümesini

doğrulamaktır.

## 6.3. Kilometre Taşı 3 — İşten çıkarılma ve sınırlı kulüp değiştirme

Amaç:

* teknik direktör itibarını,
* işten çıkarılmayı,
* işsizliği,
* sınırlı iş tekliflerini,
* kulüpler arasında taşınan kişisel hafızaları,
* teknik direktör kariyer geçmişini,
* eski kulüplerle yeniden karşılaşmayı,
* eski futbolcularla yeniden karşılaşmayı

doğrulamaktır.

## 6.4. Kilometre Taşı 4 — 10 sezonluk MVP kabul testi

Amaç:

* tam kariyer akışını,
* uzun dönem performansını,
* kayıt bütünlüğünü,
* olay hacmini,
* tekrar riskini,
* sistemler arası tutarlılığı,
* aktif futbolcu havuzunun devamlılığını,
* farklı rastlantı tohumlarıyla oluşan kariyer çeşitliliğini

test etmektir.

---

# 7. Oyun Haftası ve Zaman Akışı

## 7.1. Oyun haftasının tanımı

Oyun haftası her zaman pazartesi–pazar arasında ve tek maç içeren sabit bir yapı değildir.

Oyun haftası:

> Oyuncunun bir sonraki anlamlı planlama ve değerlendirme dönemidir.

Bir planlama döneminde:

* hiç maç olmayabilir,
* bir maç olabilir,
* iki maç olabilir,
* istisnai olarak daha fazla maç olabilir.

Takvim gerçek günler üzerinden ilerler. Haftalık kontrol merkezi bu günleri yönetilebilir bir planlama penceresinde toplar.

## 7.2. Haftalık kontrol merkezinin sorumluluğu

Haftalık kontrol merkezi bir domain sistemi değildir.

Görevleri:

* farklı sistemlerden gelen bilgileri toplamak,
* bilgileri oyuncu açısından önceliklendirmek,
* bekleyen zorunlu kararları göstermek,
* yaklaşan maçları göstermek,
* zamanı ilerletme sürecini koordine etmek,
* kritik kararların neden gerekli olduğunu açıklamak.

Haftalık kontrol merkezi doğrudan domain verisi değiştiremez.

---

# 8. Kesin Haftalık Oynanış Döngüsü

## 8.1. Hafta başlangıç özeti

Oyuncuya önceliklendirilmiş olarak gösterilir:

* yaklaşan maçlar,
* uygun olmayan futbolcular,
* sakatlıklar,
* cezalar,
* yorgunluk durumu,
* kritik futbolcu talepleri,
* süresi yaklaşan sözler,
* transfer gelişmeleri,
* sözleşme gelişmeleri,
* yönetimin önemli mesajları,
* kritik basın gelişmeleri.

Bu ekran bütün verilerin gösterildiği büyük ve kontrolsüz bir rapor ekranı olmamalıdır.

Yalnızca oyuncunun karar vermesi gereken veya yaklaşan kararları etkileyen bilgiler öne çıkarılmalıdır.

## 8.2. Haftalık sportif plan

Oyuncu aşağıdakileri belirler:

* haftalık ana antrenman odağı,
* genel antrenman yoğunluğu,
* dinlenme yaklaşımı,
* birden fazla maç varsa maçlar arasındaki öncelik.

Günlük antrenman oturumlarının ayrıntılı dağılımı antrenman sistemi veya soyutlanmış yardımcı personel tarafından uygulanır.

## 8.3. Kritik kararlar

Yalnızca gerektiğinde gösterilir:

* futbolcu görüşmesi,
* söz talebi,
* süresi dolmak üzere olan söz,
* disiplin sorunu,
* önemli transfer kararı,
* önemli sözleşme kararı,
* yönetimin zorunlu kararı,
* kritik basın sorusu,
* takım içi kriz.

Düşük önemdeki haberler ve rutin iletişim zaman akışını durdurmamalıdır.

## 8.4. Maç hazırlığı

Her maç için ayrı kontrol noktası bulunur.

Oyuncu aşağıdakileri onaylar:

* maç kadrosu,
* ilk 11,
* yedekler,
* temel taktik,
* maç planı.

Oyuncu önceki geçerli kadro ve taktiği tekrar kullanabilir.

Sistem her maçta bütün seçimlerin sıfırdan yapılmasını zorunlu tutmaz. Önceki seçim yeni koşullara göre yeniden doğrulanır.

## 8.5. Maç günü

Maç günü ayrı bir uygulama akışıdır.

MVP’de desteklenecek temel müdahaleler:

* oyuncu değişikliği,
* takım mentalitesi değişikliği,
* tempo veya risk yaklaşımı değişikliği,
* temel taktik değişikliği,
* sakatlık durumuna tepki,
* kart durumuna tepki,
* skor durumuna tepki.

Kesin müdahale parametreleri maç sistemi belgesinde kararlaştırılacaktır.

## 8.6. Maç sonrası sonuç

Aşağıdaki bilgiler öne çıkarılır:

* maç sonucu,
* önemli futbolcu performansları,
* sakatlıklar,
* yorgunluk sonuçları,
* söz ilerlemesi,
* sözün yerine getirilmesi,
* söz ihlali,
* ilişki veya hafıza oluşturan gelişmeler,
* yönetim tepkisi,
* basın tepkisi,
* oluşan yeni kritik kararlar.

Küçük sayısal değişikliklerin tamamı ayrı bildirim olarak gösterilmez.

## 8.7. Planlama döneminin tamamlanması

Zorunlu engel kalmadığında bir sonraki anlamlı planlama dönemine geçilir.

---

# 9. Zamanı İlerletmeyi Engelleyen Koşullar

Zaman ilerlemesini yalnızca aşağıdaki durumlar engelleyebilir:

1. Yaklaşan maç için zorunlu maç hazırlığı tamamlanmamışsa
2. Süresi dolacak kritik bir karar bekliyorsa
3. Oyuncunun manuel kontrolüne aldığı zorunlu görev tamamlanmamışsa

Bunların dışındaki düşük öncelikli işlemler:

* otomatik çözümlenir,
* personele devredilir,
* veya sonraki özette raporlanır.

---

# 10. Kritik Olay Kesintileri

MVP’de normal haftalık akışı kesebilecek olaylar sınırlıdır:

* ciddi sakatlık,
* son tarihi yaklaşan önemli söz,
* transfer döneminin kapanmasına bağlı acil karar,
* ciddi futbolcu krizi,
* ciddi takım içi kriz,
* yönetim ültimatomu,
* yönetimin acil talebi,
* teknik direktörün görev durumunu etkileyen gelişme.

Aşağıdakiler varsayılan olarak zamanı durdurmaz:

* rutin medya haberi,
* küçük futbolcu memnuniyetsizliği,
* düşük önemdeki transfer güncellemesi,
* rutin personel raporu,
* küçük istatistik değişikliği.

---

# 11. Özel Haftalık Akışlar

## 11.1. Çift maç haftası

Çift maç haftasında:

* tek genel haftalık sportif plan hazırlanır,
* her maç için ayrı maç hazırlığı yapılır,
* ilk maçın sonuçları ikinci maçı etkiler,
* ciddi sonuç yoksa ilk maç sonrasında bütün haftalık raporlar tekrar gösterilmez,
* sakatlık, ceza, yoğun yorgunluk veya kritik olay oluşursa ikinci maç öncesinde yeni karar noktası yaratılır.

## 11.2. Maç olmayan hafta

Maç olmayan haftada:

* antrenman ve dinlenme planı,
* transfer kararları,
* sözleşme kararları,
* futbolcu ilişkileri,
* aktif sözler,
* yönetim gelişmeleri,
* kritik basın gelişmeleri

işlenmeye devam eder.

Yapay bir maç hazırlığı aşaması gösterilmez.

---

# 12. Personel Yetkisi ve MVP Sınırı

## 12.1. Personelin yürütebileceği görevler

Personel:

* günlük antrenman programını uygular,
* kondisyon raporu hazırlar,
* sağlık raporu hazırlar,
* rakip analizi hazırlar,
* futbolcu performans raporu hazırlar,
* transfer adayları için ön araştırma yapar,
* düşük önem seviyeli rutin iletişimi yürütür.

## 12.2. Personelin kendiliğinden veremeyeceği kararlar

Personel:

* maç kadrosunu kesinleştiremez,
* ilk 11’i kesinleştiremez,
* önemli taktik değişikliği yapamaz,
* futbolcuya bağlayıcı söz veremez,
* ciddi disiplin cezasını kendiliğinden uygulayamaz,
* futbolcunun satışına nihai sportif onay veremez,
* gelen transfere nihai sportif onay veremez,
* kritik basın sorusunu cevaplayamaz,
* kritik yönetim sorusunu cevaplayamaz.

## 12.3. İlk dikey kesitte personel

İlk dikey kesitte personel:

* soyutlanmış yardımcı ekip,
* rapor üretici,
* rutin görev uygulayıcı

olarak temsil edilir.

İlk dikey kesitte zorunlu değildir:

* personel işe alma,
* personel sözleşmeleri,
* ayrıntılı personel özellikleri,
* personel gelişimi,
* personel ilişkileri,
* personelin kulüpler arasında taşınması,
* gelişmiş personel karar yapay zekâsı.

Ayrıntılı personel yönetimi nihai MVP için de zorunlu değildir.

---

# 13. Sistem Sınıflandırması

## 13.1. MVP sınırları içinde tam işlevli sistemler

“Tam işlevli” ifadesi nihai oyun vizyonundaki bütün ayrıntıları değil, onaylanan MVP sorumluluklarının eksiksiz çalışmasını ifade eder.

### Teknik direktör kariyeri

Zorunlu kapsam:

* kariyer başlangıcı,
* aktif kulüp kaydı,
* işsizlik durumu,
* teknik direktör itibarı,
* kulüp geçmişi,
* sezon geçmişi,
* işten çıkarılma,
* sınırlı iş teklifleri,
* kulüp değiştirme,
* kişisel hafızaların korunması,
* onuncu sezon sonu kariyer değerlendirmesi.

### Kadro yönetimi

Zorunlu kapsam:

* A takım kadrosu,
* maç kadrosu seçimi,
* ilk 11,
* yedekler,
* pozisyon uygunluğu,
* sakatlık ve ceza kontrolü,
* önceki geçerli kadroyu tekrar kullanma,
* kadro seçiminin sözler ve ilişkiler için olay üretmesi.

### Olay ve kural motoru

Zorunlu kapsam:

* olay üretimi,
* olay kaynağı,
* olay hedefi,
* olay tarihi,
* olay önemi,
* olay işlem durumu,
* bağlam taşıma,
* kural değerlendirmesi,
* sonuç üretimi,
* aynı sonucun iki kez uygulanmasının engellenmesi,
* uzun dönem olay hacminin kontrolü.

### Hafıza ve söz sistemi

Zorunlu kapsam:

* sınırlı sayıda gerçek söz türü,
* söz oluşturma,
* söz izleme,
* söz son tarihi,
* sözün yerine getirilmesi,
* söz ihlali,
* önemli olayların hafızaya kaydedilmesi,
* hafızaların ilişki, diyalog, transfer ve kariyer kararlarına etkisi,
* hafızaların zamanla etki kaybetmesi,
* benzer olayların hafızayı yeniden güçlendirmesi.

Bütün olayların hafızaya alınması zorunlu değildir.

### Kayıt ve yükleme

Zorunlu kapsam:

* planlama döneminin ortasında kayıt,
* bekleyen kararların korunması,
* tamamlanmış olayların tekrar çalıştırılmaması,
* devredilmiş görevlerin durumunun korunması,
* maç hazırlığı durumunun korunması,
* rastlantısallık durumunun korunması,
* en az 10 sezonluk kayıtların yüklenebilmesi,
* kayıt sürüm numarası,
* veri doğrulama,
* geçersiz veya eksik veri tespiti.

Kayıt ve yükleme kritik ve ertelenemezdir.

---

# 14. MVP’de Gerçek Fakat Sadeleştirilmiş Sistemler

Bu sistemler dekoratif veya geçici değildir. Gerçek domain kurallarına, olaylara ve oynanış sonuçlarına sahip olacaktır.

## 14.1. Dünya ve zaman simülasyonu

Minimum kapsam:

* gerçek gün bazlı takvim,
* sezon öncesi dönem,
* lig sezonu,
* sezon arası dönem,
* yaz transfer dönemi,
* kış transfer dönemi,
* fikstür,
* maç günleri,
* maçsız planlama dönemleri,
* çok maçlı planlama dönemleri,
* sezon geçişi,
* oyuncu dışındaki maçların sonuçlandırılması,
* en az 10 sezon ilerleme.

Ertelenen ayrıntılar:

* çok ülke,
* çok lig,
* yükselme ve düşme,
* kıtasal turnuvalar,
* milli takım takvimi,
* dinamik federasyon kuralları,
* bütün dünyanın eşit ayrıntıda simülasyonu.

## 14.2. Kulüp modeli

Minimum kapsam:

* kulüp kimliği,
* lig üyeliği,
* A takım kadrosu,
* temel sportif itibar veya güç,
* transfer bütçesi,
* maaş bütçesi,
* sezon beklentisi,
* yönetim güveni,
* sınırlı kulüp politikaları,
* aktif teknik direktör kaydı,
* kulüp geçmişi.

Ertelenen ayrıntılar:

* stadyum geliştirme,
* tesis yatırımları,
* ayrıntılı borç yapısı,
* başkanlık seçimleri,
* sahip değişimleri,
* ayrıntılı kulüp siyaseti,
* ayrıntılı altyapı akademisi.

## 14.3. Futbolcu modeli

Minimum kapsam:

* kimlik,
* yaş,
* kulüp,
* pozisyon,
* sadeleştirilmiş sportif yetenekler,
* mevcut seviye,
* sınırlı gelişim kapasitesi,
* kondisyon,
* yorgunluk,
* sakatlık,
* ceza durumu,
* temel kadro statüsü,
* sınırlı kişilik girdileri,
* sözleşme özeti,
* ilişki ve hafıza sistemlerine bağlanan aktör kimliği.

Ertelenen ayrıntılar:

* çok geniş yetenek listesi,
* ayrıntılı özel hayat,
* ayrıntılı menajer ağı,
* ayrıntılı liderlik grupları,
* futbolcu kariyer modu,
* ayrıntılı bireysel antrenman.

## 14.4. Antrenman

Minimum kapsam:

* haftalık ana odak,
* genel yoğunluk,
* dinlenme yaklaşımı,
* fikstür yoğunluğu etkisi,
* yorgunluk etkisi,
* sakatlık riski etkisi,
* sınırlı gelişim etkisi,
* sınırlı taktik hazırlık etkisi,
* açıklanabilir sonuç özeti.

Ertelenen ayrıntılar:

* günlük manuel antrenman planı,
* bireysel antrenman,
* pozisyon eğitimi,
* özel antrenör yönetimi,
* tesis kalitesi,
* mentorluk grupları.

## 14.5. Taktik

Minimum kapsam:

* formasyon veya saha dizilimi,
* oyuncu ve pozisyon eşleşmesi,
* takım mentalitesi,
* tempo veya risk yaklaşımı,
* sınırlı hücum ve savunma yaklaşımı,
* maç planı,
* önceki taktiği tekrar kullanma,
* sınırlı maç içi değişiklik,
* taktik ile kadro uyumsuzluğunun etkisi.

Ertelenen ayrıntılar:

* ayrıntılı bireysel roller,
* ayrıntılı bireysel talimatlar,
* duran top editörü,
* ayrıntılı pres bölgeleri,
* pas ağı editörü,
* fiziksel saha editörü.

## 14.6. Maç simülasyonu

Minimum kapsam:

* iki geçerli takım,
* maç kadroları,
* ilk 11 ve yedekler,
* futbolcu yetenekleri,
* taktik girdileri,
* kondisyon,
* yorgunluk,
* temel maç bağlamı,
* kontrollü rastlantısallık,
* skor üretimi,
* gol olayları,
* temel kartlar,
* maç içi sakatlık,
* oyuncu değişiklikleri,
* sınırlı taktik müdahaleler,
* önemli olay zaman çizelgesi,
* oyuncu performans özeti,
* tekrar üretilebilir maç bağlamı.

Ertelenen ayrıntılar:

* fiziksel 2D futbolcu hareketi,
* 3D maç sunumu,
* ayrıntılı top fiziği,
* tam gerçek zamanlı fiziksel simülasyon,
* çok geniş maç olayı havuzu,
* ayrıntılı hakem sistemi,
* hava durumu simülasyonu.

## 14.7. Sakatlık ve yorgunluk

Minimum kapsam:

* futbolcu yükü,
* maç yükü,
* antrenman yükü,
* dinlenme,
* toparlanma,
* sakatlık riski,
* sakatlık oluşumu,
* sınırlı sakatlık şiddeti,
* iyileşme süresi,
* maça uygunluk,
* maç içi sakatlık,
* sağlık raporu,
* ciddi sakatlık kesintisi.

Ertelenen ayrıntılar:

* ayrıntılı vücut bölgeleri,
* cerrahi seçenekleri,
* rehabilitasyon programları,
* yanlış teşhis,
* sağlık personeli yönetimi,
* kronik sakatlıkların ayrıntılı simülasyonu.

## 14.8. İlişki sistemi

Minimum kapsam:

* ilişkilerin tek genel puana indirgenmemesi,
* sınırlı sayıda ilişki boyutu,
* ilişkilerin açıklanabilir olaylardan etkilenmesi,
* ilişkilerin diyalog, transfer, söz ve kariyer kararlarına etkisi.

Başlangıç için değerlendirilebilecek boyutlar:

* güven,
* saygı,
* profesyonel uyum.

Kesin boyutlar ilişki sistemi belgesinde belirlenecektir.

Ertelenen ayrıntılar:

* gelişmiş arkadaşlık ağları,
* geniş sosyal gruplar,
* kapsamlı takım içi hizip simülasyonu,
* özel yaşam ilişkileri.

## 14.9. Kişilik ve motivasyon

Minimum kapsam:

* sınırlı sayıda anlamlı kişilik girdisi,
* kişiliğin karar olasılıklarını etkilemesi,
* motivasyonların transfer, oynama süresi, söz ve kariyer kararlarında kullanılması.

Değerlendirilebilecek başlangıç girdileri:

* profesyonellik,
* hırs,
* sabır,
* sadakat,
* para motivasyonu,
* oynama süresi motivasyonu.

Kesin kişilik boyutları ilgili sistem belgesinde belirlenecektir.

Ertelenen ayrıntılar:

* kapsamlı psikolojik profil,
* çok katmanlı kişilik testi,
* ayrıntılı ruh sağlığı simülasyonu.

## 14.10. Diyalog sistemi

Minimum kapsam:

* bağlama göre seçenekli görüşmeler,
* kişilik girdileri,
* ilişki durumu,
* aktif sözler,
* geçmiş hafızalar,
* diyalog sonucunda olay üretimi,
* diyalog sonucunda söz veya hafıza üretimi.

İlk görüşme kapsamı:

* forma süresi talebi,
* söz görüşmesi,
* disiplin görüşmesi,
* transfer isteği,
* kritik basın cevabı,
* kritik yönetim cevabı.

Ertelenen ayrıntılar:

* harici üretken yapay zekâya bağımlı serbest diyalog,
* sınırsız serbest metin,
* çok geniş sabit konuşma arşivi.

## 14.11. Transfer ve sözleşme sistemi

Minimum kapsam:

* transfer ihtiyacı belirleme,
* hedef önceliklendirme,
* sportif uygunluk değerlendirmesi,
* kulüp ihtiyacı,
* futbolcunun oynama ihtimali,
* teknik direktör ve kulüp itibarı,
* maaş beklentisi,
* futbolcunun kariyer hedefi,
* sınırlı kişilik ve motivasyon etkisi,
* sadeleştirilmiş müzakere,
* kulüp yönetiminin finansal sınırları,
* nihai mali onay,
* temel sözleşme süresi ve maliyeti.

Ertelenen ayrıntılar:

* ayrıntılı menajer ağı,
* karmaşık sözleşme maddeleri,
* uzun mülakat süreçleri,
* gelişmiş transfer taksitleri,
* çok katmanlı temsilci ilişkileri.

## 14.12. Yönetim sistemi

Minimum kapsam:

* sezon beklentileri,
* transfer ve maaş bütçeleri,
* yönetim güveni,
* kritik talepler,
* performans değerlendirmesi,
* işten çıkarılma kararı,
* finansal onay.

Ertelenen ayrıntılar:

* yönetim kurulu siyaseti,
* başkanlık seçimleri,
* sahip değişimleri,
* ayrıntılı yönetici kişilikleri,
* kulüp içi siyasi bloklar.

## 14.13. Basın sistemi

Minimum kapsam:

* yalnızca kritik olaylarda karar üretme,
* kritik basın soruları,
* cevapların itibar, ilişki veya yönetim sonucu üretmesi,
* önemli olayların kamuoyu anlatısına dönüşmesi.

Ertelenen ayrıntılar:

* her maç öncesi ve sonrası zorunlu toplantı,
* gelişmiş medya kuruluşu kişilikleri,
* kapsamlı gazeteci ilişki ağı,
* sürekli tekrar eden rutin sorular.

## 14.14. Taraftar sistemi

Minimum kapsam:

* kulüp bağlamına göre özet taraftar tepkisi,
* sportif sonuçların etkisi,
* eski futbolcu ve eski kulüp bağlamı,
* önemli açıklamaların etkisi,
* taraftar tepkisinin yönetim veya itibar girdisi olarak kullanılması.

Ertelenen ayrıntılar:

* doğrudan yönetilen taraftar grupları,
* ayrıntılı tribün siyaseti,
* taraftar liderleri,
* kapsamlı protesto organizasyonu.

## 14.15. Kulüp finansı

Minimum kapsam:

* transfer bütçesi,
* maaş bütçesi,
* temel sözleşme maliyetleri,
* yönetimin finansal onayı.

Ertelenen ayrıntılar:

* ayrıntılı muhasebe,
* sponsorluk yönetimi,
* stadyum ekonomisi,
* borç yönetimi,
* yatırım sistemi,
* kişisel ekonomi.

## 14.16. Kullanıcı arayüzü

Minimum kapsam:

* işlevsel, menü ve metin ağırlıklı sunum,
* haftalık kontrol merkezi,
* kadro ekranı,
* taktik ekranı,
* maç hazırlığı,
* maç ekranı,
* maç sonrası sonuç ekranı,
* transfer ekranı,
* kritik olay ve diyalog ekranları,
* kayıt ve yükleme ekranı.

Bağlayıcı sınırlar:

* iş kuralları kullanıcı arayüzünde bulunmaz,
* görsel kalite MVP kabul kriteri değildir,
* gelişmiş animasyonlar MVP kabul kriteri değildir,
* düşük önemdeki değişiklikler oyuncuyu sürekli bildirimle kesmez.

---

# 15. İlk Dikey Kesitte Geçici veya Özet Temsil

Aşağıdaki alanlar Kilometre Taşı 1 sırasında geçici, soyut veya özet verilerle temsil edilebilir:

* oyuncunun kulübü dışındaki kulüplerin ayrıntılı davranışları,
* iş piyasası,
* kulüp değiştirme,
* uzak maçların ayrıntılı istatistikleri,
* personel özellikleri,
* geniş içerik havuzu,
* dünya haberlerinin çeşitliliği,
* oyuncu dışındaki teknik direktörlerin ayrıntılı karar süreçleri.

Bu geçici temsiller nihai MVP’de gerekli gerçek sistemlerin yerine kalıcı olarak kullanılamaz.

---

# 16. Geçici veya Sahte Sonuçlarla Temsil Edilemeyecek Alanlar

Aşağıdaki alanlar ilk dikey kesitte dahi gerçek kurallara ve gerçek sonuçlara sahip olmalıdır:

* oyuncunun oynadığı maçın sonucu,
* kadro uygunluğu,
* taktik etkisi,
* antrenman sonucu,
* yorgunluk nedenleri,
* sakatlık nedenleri,
* zaman ve fikstür bütünlüğü,
* verilen kararların olay üretmesi,
* sözlerin yerine getirilmesi,
* sözlerin ihlal edilmesi,
* hafızaların ilerideki kararlara etkisi,
* tamamlanmış olayların yalnızca bir kez uygulanması,
* kayıt ve yükleme bütünlüğü.

---

# 17. Kesin Sayısal MVP Kapsamı

## 17.1. Dünya kapsamı

* 1 kurgusal ülke
* 1 profesyonel lig
* 20 kulüp
* Her kulüp için bir aktif teknik direktör kaydı
* Oyuncunun yönettiği teknik direktörün kulüpler arasında geçiş yapabilmesi

## 17.2. Lig yapısı

* Çift devreli lig sistemi
* Her kulüp için sezon başına 38 lig maçı
* Sezon öncesi hazırlık dönemi
* Lig sezonu
* Sezon arası dönem
* Yaz transfer dönemi
* Kış transfer dönemi

## 17.3. Futbolcu kapsamı

* Kulüp başına yaklaşık 23 A takım futbolcusu
* Yaklaşık 460 kulüplü aktif futbolcu
* Yaklaşık 40 serbest futbolcu
* Yaklaşık 500 aktif futbolcu

Bu sayılar başlangıç hedefidir. Uzun kariyer sırasında emeklilik ve yeni futbolcu üretimi nedeniyle toplam aktif sayı kontrollü bir aralıkta değişebilir.

## 17.4. Kariyer süresi

* En fazla 10 tamamlanmış sezon

## 17.5. MVP’de bulunmayacak turnuva yapıları

* ikinci profesyonel lig,
* yükselme,
* düşme,
* ulusal kupa,
* kıtasal turnuva,
* milli takım maçları.

Bu sınır takvim, turnuva, kadro kayıt kuralları ve dünya simülasyonu karmaşıklığını azaltmak için kabul edilmiştir.

---

# 18. Uzun Kariyer İçin Minimum Dünya Yenilenmesi

On sezon içinde aktif futbolcu havuzunun çökmesini engellemek için aşağıdaki sistemler zorunludur:

* basit yaşlanma,
* basit sportif gelişim,
* basit sportif düşüş,
* basit emeklilik,
* her sezon sınırlı sayıda yeni kurgusal futbolcu üretimi.

Yeni futbolcu üretimi:

* ayrıntılı altyapı akademisi değildir,
* genç takım yönetimi değildir,
* altyapı personeli simülasyonu değildir,
* yalnızca aktif futbolcu havuzunun uzun vadeli devamlılığını sağlar.

---

# 19. Maç Sunumu

MVP maç sunumu aşağıdaki bileşenlerden oluşur:

* olay zaman çizelgesi,
* skor,
* temel maç istatistikleri,
* önemli anlar,
* oyuncu değişiklikleri,
* sınırlı maç içi taktik müdahaleleri,
* maçı hızlandırma,
* maçı doğrudan sonuca götürme.

MVP dışında tutulur:

* fiziksel 2D futbolcu hareketleri,
* 3D maç motoru,
* ayrıntılı görsel top fiziği.

Maç sonucunu hesaplayan simülasyon çekirdeği ile maçın kullanıcıya sunulduğu görsel veya metinsel katman birbirinden ayrılmalıdır.

Maç sunumunun değiştirilmesi, maç sonucunu hesaplayan domain kurallarını değiştirmek zorunda bırakmamalıdır.

---

# 20. İlk Dikey Kesitin Kesin Sınırı

İlk dikey kesit nihai MVP değildir.

İlk dikey kesitte bulunmalıdır:

* oyuncunun yönettiği bir kulüp,
* sınırlı sayıda rakip kulüp verisi,
* tek sezon,
* tek maçlı standart planlama dönemleri,
* gerçek kadro seçimi,
* gerçek minimum taktik sistemi,
* gerçek minimum maç simülasyonu,
* gerçek haftalık antrenman odağı,
* gerçek yorgunluk etkileri,
* gerçek sakatlık etkileri,
* sınırlı futbolcu ilişkileri,
* sınırlı söz türleri,
* maç sonrası sonuçlar,
* olay üretimi,
* hafıza etkisinin minimum gerçek örneği,
* kayıt ve yükleme.

Kilometre Taşı 1’de zorunlu değildir:

* işten çıkarılma,
* işsizlik,
* kulüp değiştirme,
* on sezonluk dünya,
* çift maç haftasının eksiksiz uygulanması,
* gelişmiş delegasyon,
* ayrıntılı personel yönetimi,
* geniş dünya haberleri,
* geniş içerik havuzu.

Tasarım, bu özelliklerin sonraki kilometre taşlarında eklenebilmesini engellememelidir.

---

# 21. Kayıt, Yükleme ve Tekrar Üretilebilirlik

Planlama döneminin ortasında kayıt alınıp yüklenmesi durumunda:

* tamamlanmış olaylar tekrar çalıştırılmamalı,
* uygulanmış sonuçlar ikinci kez uygulanmamalı,
* bekleyen kararlar korunmalı,
* kararların son tarihleri korunmalı,
* devredilmiş görevlerin durumu korunmalı,
* maç hazırlığı durumu korunmalı,
* maçın mevcut durumu korunmalı veya kayıt kapsamına göre açıkça desteklenmemeli,
* rastlantısallık durumu korunmalı,
* aynı rastlantısal sonuç kontrolsüz biçimde yeniden üretilmemeli,
* kayıt sürüm bilgisi korunmalı,
* yükleme sırasında veri doğrulaması yapılmalıdır.

Kayıt formatının, sürüm geçişinin ve kurtarma stratejisinin ayrıntıları `docs/13_SAVE_SYSTEM.md` içinde belirlenecektir.

---

# 22. Kesin MVP Kabul Kriterleri

MVP aşağıdaki kriterlerin tamamı karşılanmadan tamamlanmış sayılmaz.

1. Oyun dünyası 10 sezon boyunca hata vermeden ilerleyebilmelidir.
2. Hiçbir fikstür veya maç iki kez işlenmemelidir.
3. Kayıt dosyası 10 sezon sonunda yüklenebilir kalmalıdır.
4. İşten çıkarılma kaydı otomatik olarak sonlandırmamalıdır.
5. Teknik direktör kulüp değiştirdiğinde kişisel kariyer geçmişini korumalıdır.
6. Futbolcular yaşlanmalı, gelişmeli, düşüşe geçmeli ve sınırlı biçimde emekli olmalıdır.
7. Aktif futbolcu havuzu yeni futbolcu üretimiyle devam edebilmelidir.
8. Transferler yalnızca en yüksek maaş veya genel güç değerine göre sonuçlanmamalıdır.
9. Kadro, taktik, yorgunluk ve kondisyon maç olasılıklarını anlamlı biçimde etkilemelidir.
10. Güçlü takım uzun örneklemde daha başarılı olmalı fakat bütün maçları kazanmamalıdır.
11. Verilen sözler yerine getirildiğinde veya ihlal edildiğinde gerçek sonuç üretmelidir.
12. Önemli hafızalar ilerideki diyalog veya kararları etkileyebilmelidir.
13. Haftalık akış düşük önemdeki işlemler yüzünden sürekli durmamalıdır.
14. Kritik kararlar neden durdurulduğunu açıklayabilmelidir.
15. Aynı olayın sonucu birden fazla kez uygulanmamalıdır.
16. En az binlerce otomatik maçta geçersiz kadro veya skor durumu oluşmamalıdır.
17. İlişki değişiklikleri açıklanabilir olaylara dayanmalıdır.
18. Aynı başlangıç durumu farklı rastlantı tohumlarıyla farklı fakat geçerli kariyerler üretebilmelidir.
19. Aynı kayıt ve rastlantı durumu kontrolsüz biçimde farklı sonuç üretmemelidir.
20. MVP dışındaki hiçbir sistem temel oynanış için zorunlu olmamalıdır.

---

# 23. MVP Sonrasına Ertelenen Kapsam

Aşağıdaki özellikler MVP geliştirme kapsamına dahil değildir:

* futbolcu kariyeri,
* fiziksel 2D maç gösterimi,
* 3D maç motoru,
* gerçek kulüp lisansları,
* gerçek futbolcu lisansları,
* çok oyunculu mod,
* milli takım kariyeri,
* kulüp sahipliği,
* sportif direktör kariyeri,
* gelişmiş saha dışı yaşam,
* kişisel ekonomi,
* ev satın alma,
* araç satın alma,
* sponsorluk yönetimi,
* ayrıntılı altyapı akademisi,
* ayrıntılı personel yönetimi,
* çok ülke,
* çok lig,
* kıtasal turnuvalar,
* ulusal kupa,
* yükselme ve düşme,
* gelişmiş taraftar grupları,
* harici üretken yapay zekâya bağımlı serbest diyalog,
* mod desteği,
* çevrim içi bulut özellikleri.

Bu sistemler için MVP sırasında zorunlu kullanıcı arayüzü, içerik veya dış servis bağımlılığı oluşturulmamalıdır.

---

# 24. Açık Kalan Tasarım Soruları

Aşağıdaki konular kesin MVP kapsamını engellemez ancak ilgili alt sistem belgelerinde kararlaştırılmalıdır:

* kesin futbolcu yetenek sayısı,
* kesin taktik parametreleri,
* kesin antrenman odağı sayısı,
* kesin söz türü sayısı,
* kesin ilişki boyutları,
* teknik direktör başlangıç geçmişi,
* oyuncunun başlangıç kulübünü seçme yöntemi,
* işsizliğin maksimum süresi,
* erken kariyer bitiş koşulu,
* transfer görüşmesindeki veto ayrıntıları,
* transfer müzakeresinin sorumlusu,
* sözleşme ayrıntılarını belirleyen aktör,
* sezon sonu kariyer değerlendirme puanı,
* maç simülasyonundaki kesin matematiksel model.

Bu konularda sessiz varsayım yapılamaz.

---

# 25. Kesinleşmiş, Ertelenmiş ve Açık Kararlar

## 25.1. Kesinleşmiş

* Teknik direktör kariyeri ana ve tek MVP kariyer modudur.
* Kariyer en fazla 10 tamamlanmış sezondur.
* İşten çıkarılma doğrudan oyun sonu değildir.
* Sadeleştirilmiş kulüp değiştirme bulunur.
* Haftalık kontrol merkezi temel oynanış akışıdır.
* Rutin günlük işlemler otomatik ilerler.
* Maç günü ayrı bir akıştır.
* Kritik olaylar zamanı sınırlı biçimde kesebilir.
* Kullanıcı arayüzü domain kurallarının sahibi değildir.
* 1 kurgusal ülke ve 1 profesyonel lig bulunur.
* Ligde 20 kulüp bulunur.
* Lig çift devrelidir ve kulüp başına 38 maç içerir.
* Yaklaşık 500 aktif futbolcu bulunur.
* İki transfer dönemi bulunur.
* Maç sunumu olay zaman çizelgesi ve temel istatistiklerden oluşur.
* Fiziksel 2D ve 3D maç sunumu bulunmaz.
* Olay/kural motoru, hafıza/söz sistemi ve kayıt/yükleme ertelenemezdir.
* Sportif çekirdekteki sistemler gerçek domain sonuçları üretir.
* İlk dikey kesit tek sezonluk doğrulama aşamasıdır.
* MVP, 20 kesin kabul kriteriyle doğrulanır.

## 25.2. Ertelenmiş

MVP sonrasına ertelenen bütün sistemler Bölüm 23’te listelenmiştir.

## 25.3. Açık

İlgili alt sistem belgelerine bırakılan kararlar Bölüm 24’te listelenmiştir.

---

# 26. GDD Uyumluluk Kontrolü

Bu MVP kapsamı aşağıdaki GDD hükümleriyle uyumludur:

* MVP yalnızca teknik direktör kariyerine odaklanır.
* Dünya kurgusal ve sınırlı kapsamdadır.
* Kulüp sayısı GDD’deki yaklaşık 20–40 kulüp sınırı içindedir.
* Temel kulüp ve futbolcu modelleri bulunur.
* Kadro, antrenman, taktik ve maç sistemleri bulunur.
* Transfer ve sözleşme sistemi bulunur.
* İlişki, söz ve hafıza sistemleri gerçek sonuç üretir.
* Sınırlı fakat sistemik olay motoru bulunur.
* Temel basın ve yönetim etkileşimleri bulunur.
* Kayıt ve yükleme zorunludur.
* En az 10 sezonluk uzun dönem testleri zorunludur.
* 3D maç motoru, futbolcu kariyeri, gerçek lisanslar, gelişmiş kişisel ekonomi ve bütün dünya ligleri MVP dışında tutulur.
* Sistemler arası etkiler olay ve kurallar üzerinden düşünülür.
* Kullanıcı arayüzü iş kurallarından ayrılır.
* Harici üretken yapay zekâ temel simülasyon için zorunlu değildir.

Bu belge hazırlanırken GDD ile doğrudan bir çelişki tespit edilmemiştir.

---

# 27. Sonraki Belgelendirme Adımı

Bu kapsam belgesi kesinleştikten sonra önerilen en küçük sıradaki tasarım çalışmaları:

1. `docs/03_DOMAIN_MODEL.md`
2. `docs/04_EVENT_RULE_ENGINE.md`
3. `docs/05_MEMORY_AND_PROMISE_SYSTEM.md`
4. `docs/06_RELATIONSHIP_SYSTEM.md`
5. `docs/12_WORLD_SIMULATION.md`
6. `docs/09_MATCH_SIMULATION.md`
7. `docs/08_TRANSFER_SYSTEM.md`
8. `docs/13_SAVE_SYSTEM.md`
9. `docs/14_TEST_STRATEGY.md`

Bu sıra yeni bir teknoloji veya üretim kodu kararı anlamına gelmez.
