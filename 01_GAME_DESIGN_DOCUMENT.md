# FUTBOL KARİYERİ VE YAŞAM SİMÜLASYONU
## Ana Oyun Tasarım Dokümanı

**Belge kodu:** GDD-001  
**Sürüm:** 0.1 – İlk Vizyon  
**Durum:** Yaşayan belge / geliştirmeye açık  
**Dil:** Türkçe  
**Önerilen dosya yolu:** `docs/01_GAME_DESIGN_DOCUMENT.md`

---

# 1. Belgenin Amacı

Bu belge, uzun soluklu bir futbol kariyeri ve yaşam simülasyonu oyununun temel vizyonunu, tasarım ilkelerini, ana sistemlerini ve sistemler arasındaki ilişkileri tanımlar.

Belgenin amacı doğrudan kod üretmek değildir. Bu doküman;

- oyunun ne olduğunu,
- ne olmadığını,
- oyuncuya hangi deneyimi sunacağını,
- hangi sistemlerin birbiriyle nasıl ilişki kuracağını,
- ilk oynanabilir sürümün sınırlarını,
- uzun vadeli hedefleri,
- geliştirme sırasında korunması gereken değişmez kuralları

tanımlayan ana referanstır.

Bu belge, oyun geliştirme süreci boyunca güncellenecek bir “yaşayan doküman” olarak kullanılacaktır. Yapılan her önemli tasarım kararı bu belgeye veya ilgili alt tasarım dokümanına işlenmelidir.

---

# 2. Oyun Vizyonu

Oyun; futbol dünyasını yalnızca maç, transfer ve taktik döngüsünden ibaret görmeyen, saha içi ve saha dışı kararların uzun yıllar boyunca sonuç ürettiği, yaşayan ve oyuncuyu hatırlayan bir futbol kariyeri simülasyonudur.

Oyuncu kariyerine temel olarak iki farklı yoldan başlayabilecektir:

1. Futbolcu kariyeri
2. Teknik direktör kariyeri

Bu iki kariyer seçeneği yalnızca farklı başlangıç ekranları olmayacaktır. Her kariyer türü;

- farklı sorumluluklara,
- farklı karar mekanizmalarına,
- farklı ilişki ağlarına,
- farklı risklere,
- farklı başarı ölçütlerine,
- farklı yaşam deneyimlerine

sahip olacaktır.

Oyunun nihai hedefi, oyuncuya bir futbol oyununu “bitirdiği” hissini vermek değil; yıllar geçtikçe değişen, yeni hikâyeler oluşturan ve önceki kararları yeniden karşısına çıkaran bir futbol yaşamı sunmaktır.

---

# 3. Çözülmek İstenen Temel Problem

Mevcut futbol menajerlik ve kariyer oyunlarının önemli bir kısmı birkaç sezon sonra tekrar eden bir döngüye girer:

1. Oyuncu transfer yapar.
2. Taktik kurar.
3. Maç oynar veya simüle eder.
4. Para ve itibar kazanır.
5. Daha güçlü oyuncular transfer eder.
6. Aynı döngüyü daha yüksek sayılarla tekrarlar.

Oyuncu birkaç sezon içinde sistemleri çözer. Belirsizlik azalır, dünya tahmin edilebilir hâle gelir ve kariyer kişisel bir hikâye olmaktan çıkar.

Bu oyunun çözmek istediği temel problem şudur:

> Futbol dünyası oyuncunun kararlarını, ilişkilerini, sözlerini, krizlerini ve geçmişini hatırlamalı; bu geçmiş ilerleyen yıllarda yeni sonuçlar üretmelidir.

Oyunun farklılığı yalnızca daha fazla lig, oyuncu, menü veya istatistik sunmak değildir. Farklılık, sistemlerin birbirleriyle anlamlı biçimde etkileşim kurmasından doğacaktır.

---

# 4. Oyunun Tek Cümlelik Tanımı

> Oyuncunun kararlarını yıllarca hatırlayan; futbolcu veya teknik direktör olarak saha içi ve saha dışı gerçek bir futbol kariyeri yaşatan, dinamik ve uzun vadeli bir futbol dünyası simülasyonu.

---

# 5. Temel Tasarım Sütunları

## 5.1. Yaşayan Dünya

Oyun dünyası yalnızca oyuncunun eylemlerini beklememelidir. Kulüpler, futbolcular, teknik direktörler, menajerler, yöneticiler, medya ve diğer aktörler kendi hedefleri doğrultusunda hareket etmelidir.

Dünya;

- yönetim değişiklikleri,
- ekonomik krizler,
- yeni yatırımcılar,
- beklenmedik futbolcu gelişimleri,
- teknik direktör değişimleri,
- taktik akımlar,
- taraftar protestoları,
- kulüp kültürü değişimleri,
- kişisel rekabetler

üretebilmelidir.

## 5.2. Uzun Vadeli Sonuçlar

Kararlar yalnızca anlık sayı değişiklikleri üretmemelidir.

Bir basın açıklaması, tutulmayan söz, oyuncuya verilen destek, tartışmalı transfer veya kulüpten ayrılma biçimi yıllar sonra yeniden anlam kazanabilmelidir.

## 5.3. Dünya Hafızası

Oyun önemli olayları yalnızca geçmiş ekranında göstermemelidir. Aktörler bu olayları hatırlamalı ve gelecekteki kararlarında kullanmalıdır.

Örnek hafıza kayıtları:

- “Gençken bana forma şansı verdi.”
- “Transfer sözü verdi ancak tutmadı.”
- “Beni basın önünde suçladı.”
- “Sakatlığım sırasında beni destekledi.”
- “Eski kulübüme saygısızlık yaptı.”
- “Maaş indirimi yaparak kulübe yardımcı oldum.”
- “Kritik finalde takımı yalnız bıraktı.”

## 5.4. Sistemik Oynanış

Her özellik, mümkün olduğunca birden fazla sistemi etkilemelidir.

Örneğin gece hayatı yalnızca bir animasyon veya tüketim ekranı değildir. Şunları etkileyebilir:

- yorgunluk,
- antrenman performansı,
- teknik direktör ilişkisi,
- takım arkadaşlarının algısı,
- medya ilgisi,
- taraftar itibarı,
- sponsor riski,
- transfer ihtimali,
- mali durum,
- kişilik gelişimi.

## 5.5. Belirsizlik ve Eksik Bilgi

Oyuncu, futbol dünyasındaki bütün bilgileri kesin olarak bilmemelidir.

Tahmini veya gizli olabilecek bilgiler:

- gerçek potansiyel,
- profesyonellik,
- baskıya dayanıklılık,
- sakatlığa yatkınlık,
- sadakat,
- menajerin gerçek niyeti,
- takım içindeki gizli huzursuzluk,
- kulüp yönetiminin sabır seviyesi.

Bilgiler gözlem, ilişki, deneyim, raporlar ve zaman aracılığıyla daha güvenilir hâle gelmelidir.

## 5.6. Kariyerin Kişiselleşmesi

Aynı başlangıç koşullarına sahip iki kariyer, birkaç sezon sonra farklı dünyalara dönüşebilmelidir.

Bunun için oyun;

- kontrollü rastlantısallık,
- kişilik tabanlı kararlar,
- olay zincirleri,
- kalıcı ilişkiler,
- değişen kulüp kültürleri,
- dinamik kariyer yolları

kullanmalıdır.

## 5.7. Yavaş Açılan Derinlik

Oyunun tüm sistemleri ilk birkaç sezonda tüketilmemelidir.

Kariyer ilerledikçe;

- kaptanlık,
- liderlik,
- mentorluk,
- teknik direktörlük,
- sportif direktörlük,
- futbol akademisi,
- menajerlik şirketi,
- yatırım,
- kulüp sahipliği,
- emeklilik sonrası roller

gibi yeni sorumluluklar ve seçenekler anlam kazanmalıdır.

---

# 6. Hedef Oyuncu Deneyimi

Oyuncu aşağıdaki hisleri yaşamalıdır:

- “Bu kariyer yalnızca bana ait.”
- “Dünya verdiğim kararları hatırlıyor.”
- “Her kararın açık veya gizli bir bedeli olabilir.”
- “İnsanları yalnızca puan olarak değil, karakter olarak yönetiyorum.”
- “Kulüplerin ve futbolcuların kendilerine ait çıkarları var.”
- “Güçlü takım her zaman kazanmaz ama sonuçlar anlamsız biçimde rastgele değildir.”
- “Yıllar geçtikçe dünya gerçekten değişiyor.”
- “Yeni bir kariyer başlattığımda aynı olayların kopyasını yaşamıyorum.”
- “Başarılı olmak yalnızca daha yüksek genel güce sahip oyuncuları toplamak değildir.”
- “Futbol dışındaki hayat, futbol kariyerimin doğal bir parçasıdır.”

---

# 7. Kariyer Modları

## 7.1. Teknik Direktör Kariyeri

Teknik direktör kariyeri, ilk oynanabilir sürüm için ana kariyer modu olarak düşünülmektedir.

Oyuncu şu geçmişlerden biriyle başlayabilir:

- amatör takım teknik direktörü,
- altyapı antrenörü,
- yardımcı antrenör,
- alt liglerde çalışan genç teknik direktör,
- futbolculuktan yeni emekli olmuş eski oyuncu,
- profesyonel futbol geçmişi olmayan taktik uzmanı.

Başlangıç geçmişi şunları etkileyebilir:

- ilk iş teklifleri,
- medya ilgisi,
- oyuncuların ilk yaklaşımı,
- kulüp yönetimlerinin güveni,
- taraftar beklentisi,
- başlangıç itibarı,
- taktik bilgisi,
- ilişki ağı.

Ünlü eski futbolcu daha kolay fırsat bulabilir ancak beklenti seviyesi yüksek olabilir. Futbol geçmişi olmayan teknik direktör daha düşük itibardan başlayabilir ancak farklı gelişim avantajlarına sahip olabilir.

### Teknik direktörün sorumlulukları

- taktik sistem kurmak,
- kadro seçmek,
- antrenman yaklaşımını belirlemek,
- oyuncularla bireysel görüşmeler yapmak,
- takım içi disiplini yönetmek,
- teknik ekip kurmak,
- transfer hedefleri belirlemek,
- oyuncu menajerleriyle görüşmek,
- yönetimle iletişim kurmak,
- basın toplantılarına katılmak,
- taraftar beklentisini yönetmek,
- krizleri çözmek,
- altyapı stratejisi belirlemek,
- maç içi kararlar almak,
- verilen sözleri takip etmek.

### Teknik direktör profili

Teknik direktörün kimliği yalnızca başlangıçta seçilen özelliklerden oluşmamalıdır. Davranışları zamanla bir profil yaratmalıdır.

Oluşabilecek profiller:

- genç oyuncu geliştiricisi,
- sert ve otoriter,
- oyuncu dostu,
- taktik uzmanı,
- savunma uzmanı,
- hücum futbolu savunucusu,
- kriz yöneticisi,
- yıldız futbolcularla sorun yaşayan,
- yönetimle sık çatışan,
- sadık,
- kariyer odaklı ve sık kulüp değiştiren,
- medyada tartışmalı,
- büyük maç uzmanı.

Bu etiketler yalnızca kozmetik olmamalı; iş tekliflerini, transferleri, oyuncu ilişkilerini ve medya yaklaşımını etkilemelidir.

---

## 7.2. Futbolcu Kariyeri

Futbolcu kariyeri, teknik direktör kariyerinden sonra geliştirilecek geniş kapsamlı ikinci ana moddur.

Muhtemel başlangıçlar:

- mahalle veya okul takımı,
- yerel amatör kulüp,
- futbol akademisi,
- profesyonel kulüp altyapısı,
- alt lig kulübü,
- menajersiz genç oyuncu,
- geç keşfedilen futbolcu.

Başlangıç koşulları şunlardan etkilenebilir:

- doğduğu bölge,
- aile ekonomisi,
- eğitim durumu,
- altyapı imkânları,
- fiziksel gelişim,
- karakter,
- sosyal çevre,
- tesadüfi keşif fırsatları,
- gözlemci ağı.

Yetenek tek başına başarı garantisi olmamalıdır. Bir futbolcunun yükselişi;

- doğru zamanda iyi performans,
- düzenli çalışma,
- uygun teknik direktör,
- doğru kulüp seçimi,
- sakatlıklardan korunma,
- sosyal çevre,
- menajer tercihi,
- psikolojik dayanıklılık,
- profesyonellik

gibi birçok etkene bağlı olmalıdır.

### Futbolcu kariyerinin aşamaları

1. Keşfedilme
2. Altyapı veya amatör dönem
3. Profesyonel sözleşme
4. İlk takım mücadelesi
5. Forma rekabeti
6. Çıkış dönemi
7. Kariyer zirvesi
8. Liderlik veya yıldız statüsü
9. Fiziksel düşüş
10. Kariyer sonu planlaması
11. Emeklilik sonrası rol

---

# 8. Ana Oynanış Döngüsü

## 8.1. Teknik Direktör Döngüsü

1. Dünya ve kulüp durumunu incele.
2. Kadro, moral, yorgunluk ve ilişkileri değerlendir.
3. Antrenman ve taktik kararları al.
4. Oyuncular, yönetim, basın veya menajerlerle görüş.
5. Maça hazırlan.
6. Maç sırasında kararlar al.
7. Sonuçları değerlendir.
8. Olaylar, tepkiler ve yeni sorunlarla karşılaş.
9. Kısa ve uzun vadeli planları güncelle.
10. Dünya zamanının ilerlemesiyle yeni gelişmeler yaşa.

## 8.2. Futbolcu Döngüsü

1. Fiziksel ve psikolojik durumu değerlendir.
2. Antrenman ve kişisel gelişim kararları al.
3. Teknik direktör, takım arkadaşları ve menajerle iletişim kur.
4. Kadro ve forma rekabetini takip et.
5. Maçta performans göster.
6. Saha dışı yaşam kararları al.
7. Basın, taraftar ve sponsor tepkilerini yönet.
8. Kariyer tekliflerini değerlendir.
9. Finansal ve kişisel planlama yap.
10. Uzun vadeli kariyer kimliğini şekillendir.

---

# 9. Dünya Simülasyonu

Dünya simülasyonu, oyunun ana taşıyıcı sistemidir. Oyuncu hiçbir işlem yapmasa bile dünya belirli sınırlar içinde ilerlemeli ve değişmelidir.

## 9.1. Dünya Aktörleri

- futbolcular,
- teknik direktörler,
- yardımcı antrenörler,
- kulüp başkanları,
- sportif direktörler,
- gözlemciler,
- oyuncu menajerleri,
- medya kuruluşları,
- gazeteciler,
- taraftar grupları,
- sponsorlar,
- yatırımcılar,
- federasyonlar,
- hakemler,
- aile üyeleri ve yakın çevre.

Her aktörün en azından şu bileşenlere sahip olması hedeflenir:

- kimlik,
- rol,
- hedefler,
- kişilik,
- motivasyonlar,
- ilişkiler,
- hafıza,
- itibar,
- karar verme eğilimleri,
- mevcut durum.

## 9.2. Kulüp Yaşam Döngüsü

Kulüpler zaman içinde;

- yönetim değiştirebilir,
- ekonomik krize girebilir,
- yatırım alabilir,
- borçlanabilir,
- altyapıya yatırım yapabilir,
- stadyum yenileyebilir,
- taraftar desteğini kaybedebilir,
- transfer politikasını değiştirebilir,
- sportif hedeflerini yükseltebilir veya düşürebilir,
- kulüp kültürünü dönüştürebilir.

## 9.3. Futbol Dünyasının Dönüşümü

Uzun kariyerlerde;

- yeni taktik trendler doğabilir,
- bazı ligler ekonomik olarak güçlenebilir,
- bazı kulüpler gerileyebilir,
- yeni yıldızlar ortaya çıkabilir,
- eski futbolcular yeni rollere geçebilir,
- menajerlik ağları güçlenebilir,
- futbol kuralları veya turnuva yapıları değişebilir,
- taraftar ve medya davranışları dönüşebilir.

Dünya 15 yıl sonra başlangıçtaki durumun yalnızca yaşlandırılmış kopyası olmamalıdır.

---

# 10. Olay, Bağlam ve Sonuç Sistemi

Bu oyun, doğrudan tablo güncellemelerine dayanan kırılgan bir yapı yerine olay tabanlı bir simülasyon yaklaşımı kullanmalıdır.

Her önemli gelişme üç ana parçadan oluşur:

1. Olay
2. Bağlam
3. Sonuçlar

## 10.1. Olay

Olay, dünyada gerçekleşen anlamlı bir değişimdir.

Örnekler:

- futbolcu gece kulübünde görüntülendi,
- teknik direktör forma sözü verdi,
- oyuncu antrenmana geç kaldı,
- kulüp yönetimi transfer bütçesini azalttı,
- menajer basına açıklama yaptı,
- takım kaptanı teknik direktörü eleştirdi,
- sponsor sözleşmeyi gözden geçirmeye başladı,
- genç oyuncu kritik maçta gol attı.

## 10.2. Bağlam

Aynı olay her durumda aynı sonucu üretmemelidir.

Bağlam değişkenleri:

- olayın ilk kez veya tekrar gerçekleşmesi,
- oyuncunun itibarı,
- kulübün kültürü,
- teknik direktörün kişiliği,
- takımın sportif durumu,
- olayın zamanı,
- yaklaşan maçın önemi,
- aktörlerin ilişkileri,
- medyanın ilgisi,
- ülke veya lig kültürü,
- sözleşme durumu,
- futbolcunun takım içindeki statüsü.

## 10.3. Sonuçlar

Bir olayın doğrudan ve gecikmeli sonuçları olabilir.

Örnek olay:

> Futbolcu final maçından iki gün önce gece kulübünde görüntülendi.

Muhtemel sonuçlar:

- yorgunluk artışı,
- antrenman performansı düşüşü,
- teknik direktör güven kaybı,
- medya haberi,
- taraftar tepkisi,
- sponsor memnuniyetsizliği,
- takım arkadaşlarında huzursuzluk,
- kadro dışı bırakılma ihtimali,
- futbolcunun savunma amaçlı açıklama yapması,
- ileride “disiplinsiz” itibar etiketi oluşması.

Sonuçların şiddeti bağlama göre belirlenmelidir.

## 10.4. Olay Zincirleri

Olaylar tek başına sona ermek zorunda değildir.

Örnek zincir:

1. Futbolcu forma süresi ister.
2. Teknik direktör üç maç içinde şans vereceğine söz verir.
3. Futbolcu üç maç boyunca yedek kalır.
4. Tutulmayan söz hafızaya kaydedilir.
5. Futbolcunun güveni düşer.
6. Menajeri teknik direktörle görüşür.
7. Görüşme başarısız olur.
8. Menajer basına bilgi sızdırır.
9. Takım içinde görüş ayrılığı oluşur.
10. Başka bir kulüp futbolcuyla ilgilenir.
11. Yönetim kriz raporu ister.
12. Transfer veya barışma süreci başlar.

Her aşama otomatik olmak zorunda değildir. Aktörlerin kişiliği ve bağlamı zincirin yönünü değiştirebilmelidir.

---

# 11. Hafıza Sistemi

Hafıza sistemi, oyunun tekrar hissini kıran en önemli sistemlerden biridir.

## 11.1. Hafıza Türleri

- kişisel hafıza,
- ilişkisel hafıza,
- kulüp hafızası,
- medya hafızası,
- taraftar hafızası,
- kariyer hafızası,
- rekabet hafızası.

## 11.2. Hafıza Kaydının Özellikleri

Bir hafıza kaydı şu bilgileri içerebilir:

- olay türü,
- olay tarihi,
- ilgili aktörler,
- olayın önemi,
- olumlu veya olumsuz yönü,
- güvenilirlik,
- kamuya açık olup olmadığı,
- zamanla azalma oranı,
- yeniden tetiklenme koşulları,
- olayın özeti,
- duygusal etkisi.

## 11.3. Unutma ve Yeniden Hatırlama

Bütün olaylar sonsuza kadar eşit ağırlıkta kalmamalıdır.

- küçük olaylar zamanla etkisini kaybedebilir,
- travmatik veya önemli olaylar uzun süre kalabilir,
- benzer yeni bir olay eski hafızayı yeniden güçlendirebilir,
- medya eski bir olayı yeniden gündeme taşıyabilir,
- yıldönümleri veya eski kulüple karşılaşmalar geçmişi tetikleyebilir.

## 11.4. Verilen Sözler

Söz sistemi hafıza sisteminin özel bir alt alanıdır.

Söz örnekleri:

- daha fazla forma süresi,
- belirli mevkide oynatma,
- transfer izni,
- kaptanlık,
- yeni sözleşme,
- maaş artışı,
- belirli oyuncuyu transfer etme,
- takım hedefi,
- izin veya dinlenme.

Her söz;

- veren taraf,
- alan taraf,
- tarih,
- son tarih,
- koşullar,
- gerçekleşme durumu,
- kısmi gerçekleşme,
- ihlal nedeni,
- algılanan adalet

bilgilerine sahip olmalıdır.

---

# 12. İlişki Sistemi

İlişkiler tek bir -100 / +100 puanından ibaret olmamalıdır.

Bir ilişkinin farklı boyutları olabilir:

- güven,
- saygı,
- yakınlık,
- korku,
- sadakat,
- profesyonel uyum,
- kişisel uyum,
- kıskançlık,
- rekabet,
- borçluluk hissi,
- kırgınlık.

İki kişi birbirine saygı duyabilir ancak kişisel olarak anlaşamayabilir. Bir oyuncu teknik direktörden korkabilir fakat ona güvenmeyebilir.

## 12.1. İlişki Kaynakları

- ortak geçmiş,
- verilen destek,
- tutulmuş veya tutulmamış sözler,
- maç süresi,
- sözleşme görüşmeleri,
- basın açıklamaları,
- sosyal etkinlikler,
- krizler,
- başarılar,
- ortak rakipler,
- kültürel yakınlık,
- takım içi gruplar.

## 12.2. Takım İçi Sosyal Yapı

Takım içerisinde;

- arkadaşlık grupları,
- milliyet veya dil grupları,
- yaş grupları,
- liderlik çevreleri,
- yıldız oyuncu etrafında oluşan gruplar,
- genç oyuncular,
- dışlanan futbolcular,
- forma rekabetleri

oluşabilir.

Takım uyumu, yalnızca bütün oyuncuların ortalama mutluluk puanı olmamalıdır.

---

# 13. Kişilik ve Motivasyon Sistemi

Her aktörün davranışlarını belirleyen sabit ve değişken özellikleri olmalıdır.

## 13.1. Kişilik Boyutları

Örnek kişilik boyutları:

- profesyonellik,
- hırs,
- sadakat,
- dürüstlük,
- sabır,
- liderlik,
- disiplin,
- risk alma,
- ego,
- duygusal kontrol,
- sosyallik,
- uyumluluk,
- baskıya dayanıklılık,
- para odaklılık,
- şöhret isteği.

## 13.2. Motivasyonlar

Aktörlerin öncelikleri farklı olabilir:

- para,
- başarı,
- düzenli oynama,
- aile,
- şehir yaşamı,
- ülkeye yakınlık,
- şöhret,
- sadakat,
- güvenlik,
- liderlik,
- gelişim,
- intikam,
- eski kulübe dönme,
- belirli teknik direktörle çalışma.

Aynı transfer teklifi iki futbolcu için tamamen farklı anlam taşımalıdır.

## 13.3. Kişilik Gelişimi

Kişilik bütünüyle sabit olmamalıdır.

- genç oyuncular takım liderlerinden etkilenebilir,
- başarı ego seviyesini artırabilir,
- ağır sakatlık risk yaklaşımını değiştirebilir,
- kötü bir transfer sadakat veya güven duygusunu azaltabilir,
- iyi mentorluk profesyonelliği artırabilir,
- tekrar eden disiplin sorunları kalıcı itibar oluşturabilir.

---

# 14. Kulüp Kültürü

Her kulüp yalnızca bütçe, kadro ve tesislerden oluşmamalıdır.

Kulüp kültürü şu alanlarda tanımlanabilir:

- altyapıya verilen önem,
- yıldız transfer beklentisi,
- yerel oyuncu tercihi,
- sabır seviyesi,
- oyun tarzı geleneği,
- taraftar baskısı,
- yönetim müdahalesi,
- finansal risk eğilimi,
- kulüp efsanelerine bağlılık,
- disiplin anlayışı,
- ticari başarı önceliği,
- sportif başarı önceliği.

Teknik direktörün kararları kulüp kültürüyle uyumlu veya çatışmalı olabilir.

Örnek:

Altyapısıyla tanınan bir kulüpte genç oyunculara hiç şans vermemek;

- yönetim güvenini,
- taraftar desteğini,
- kulüp kimliğini,
- basın yorumlarını

olumsuz etkileyebilir.

---

# 15. Diyalog Sistemi

Diyaloglar oyunun ana ayırt edici özelliklerinden biridir.

## 15.1. Temel Amaç

Diyalog sistemi yalnızca doğru seçeneği bulmaya dayanan bir mini oyun olmamalıdır. Aynı cümle;

- konuşan kişinin karakterine,
- geçmiş ilişkiye,
- mevcut ruh hâline,
- takım durumuna,
- önceki sözlere,
- kamuoyu baskısına,
- konuşmanın özel veya kamuya açık olmasına

göre farklı sonuçlar üretebilmelidir.

## 15.2. Diyalog Alanları

- futbolcu ile teknik direktör görüşmeleri,
- takım toplantıları,
- soyunma odası konuşmaları,
- transfer görüşmeleri,
- sözleşme pazarlıkları,
- oyuncu menajeri görüşmeleri,
- yönetim toplantıları,
- basın toplantıları,
- rakip teknik direktör açıklamaları,
- taraftar temsilcileriyle görüşmeler,
- aile ve özel hayat konuşmaları,
- kriz görüşmeleri,
- sponsor görüşmeleri.

## 15.3. Diyalog Sonuçları

Diyaloglar şunları etkileyebilir:

- güven,
- saygı,
- motivasyon,
- medya anlatısı,
- itibar,
- sözler,
- ilişki hafızası,
- takım içi gruplar,
- transfer kararı,
- yönetim desteği,
- gelecekteki konuşma seçenekleri.

## 15.4. Tekrarı Önleme

Diyalog çeşitliliği yalnızca yüzlerce cümle yazmakla sağlanmamalıdır.

Sistem;

- bağlam şablonları,
- kişilik filtreleri,
- geçmiş olay referansları,
- farklı tonlar,
- hedef ve motivasyonlar,
- olay zinciri aşamaları

kullanarak anlamlı varyasyon üretmelidir.

Temel oyun kararları, dış bir yapay zekâ servisine zorunlu bağımlı olmamalıdır. İleride çevrim içi üretken yapay zekâ destekli ek diyaloglar değerlendirilebilir; ancak oyunun kayıt bütünlüğü, sonuç hesaplaması ve ana simülasyonu deterministik kurallarla yönetilmelidir.

---

# 16. Transfer ve Sözleşme Sistemi

Transferler yalnızca kulüp bütçesi, oyuncu değeri ve maaş üzerinden sonuçlanmamalıdır.

## 16.1. Transfer Kararını Etkileyen Unsurlar

- kulübün itibarı,
- ligin kalitesi,
- teknik direktörün itibarı,
- forma ihtimali,
- taktik uygunluk,
- şehir ve yaşam koşulları,
- aile tercihleri,
- maaş,
- bonuslar,
- sözleşme süresi,
- serbest kalma maddeleri,
- menajer komisyonu,
- kulübün sportif hedefi,
- Avrupa kupaları,
- takım arkadaşları,
- eski ilişkiler,
- taraftar baskısı,
- rakip teklifler,
- futbolcunun kariyer aşaması,
- oyuncunun kişisel motivasyonları.

## 16.2. Oyuncu Menajerleri

Menajerler bağımsız aktörler olmalıdır.

Menajer profilleri:

- yüksek komisyon isteyen,
- oyuncu kariyerine öncelik veren,
- sürekli transfer yaptırmaya çalışan,
- medyayı baskı aracı olarak kullanan,
- belirli kulüplerle yakın ilişkili,
- güvenilir,
- fırsatçı,
- yıldız oyuncu portföyüne sahip,
- genç yetenek uzmanı.

Bir menajerle kurulan ilişki, temsil ettiği diğer oyuncularla yapılacak görüşmeleri etkileyebilir.

## 16.3. Pazarlık

Pazarlık yalnızca teklif miktarını artırma süreci olmamalıdır.

Taraflar;

- taleplerini öncelik sırasına koymalı,
- bazı taleplerini gizleyebilmeli,
- taviz verebilmeli,
- blöf yapabilmeli,
- süre baskısını kullanabilmeli,
- başka teklifler üzerinden avantaj kurabilmeli,
- önceki davranışları hatırlamalıdır.

---

# 17. Maç ve Taktik Sistemi

İlk aşamada üç boyutlu maç motoru zorunlu değildir.

İlk sürüm için uygun sunum seçenekleri:

- iki boyutlu saha görünümü,
- metin tabanlı maç anlatımı,
- basit konum animasyonları,
- olay zaman çizelgesi,
- ayrıntılı istatistik ekranı.

## 17.1. Maç Sonucunu Etkileyen Unsurlar

- oyuncu kalitesi,
- pozisyon uyumu,
- taktik uyumu,
- takım uyumu,
- oyuncu formu,
- moral,
- yorgunluk,
- sakatlık,
- rakip taktik,
- teknik direktör kararları,
- ev sahibi avantajı,
- hava ve saha koşulları,
- maç önemi,
- baskı,
- liderlik,
- oyuncu kişilikleri,
- kontrollü rastlantısallık.

## 17.2. Tasarım İlkesi

Güçlü takımın her zaman kazanmadığı ancak sonuçların tamamen rastgele de olmadığı bir sistem kurulmalıdır.

Oyuncu, maç sonrasında;

- neden kazandığını,
- neden kaybettiğini,
- hangi faktörlerin etkili olduğunu

yaklaşık olarak anlayabilmelidir. Bununla birlikte bütün hesaplamaların tam formülü açık edilmemelidir.

## 17.3. Taktik Kimlik

Taktikler yalnızca diziliş seçimi değildir.

- oyun temposu,
- pres yaklaşımı,
- savunma çizgisi,
- geçiş oyunu,
- genişlik,
- risk seviyesi,
- rol dağılımı,
- oyuncu özgürlüğü,
- maç içi uyarlamalar

birlikte takım kimliğini oluşturmalıdır.

Takımın yeni taktiğe alışması zaman almalıdır.

---

# 18. Futbolcu Gelişimi

Gelişim, yalnızca maç ve antrenman puanı biriktirmeye bağlı olmamalıdır.

## 18.1. Gelişimi Etkileyen Unsurlar

- antrenman kalitesi,
- antrenman disiplini,
- maç süresi,
- yaş,
- fiziksel gelişim,
- teknik ekip kalitesi,
- mentorlar,
- takım seviyesi,
- taktik rol,
- özgüven,
- psikolojik durum,
- yaşam düzeni,
- sakatlık geçmişi,
- profesyonellik,
- rekabet seviyesi.

## 18.2. Potansiyel

Potansiyel kesin bir hedef sayı olarak görülmemelidir.

Potansiyel;

- çevre,
- fırsatlar,
- sakatlıklar,
- kişilik,
- antrenör kalitesi,
- doğru pozisyon,
- kariyer kararları

tarafından şekillenen bir aralık olmalıdır.

## 18.3. Düşüş ve Yaşlanma

Fiziksel düşüş bütün oyuncularda aynı yaşta başlamamalıdır.

- yaşam tarzı,
- sakatlıklar,
- fiziksel yapı,
- pozisyon,
- profesyonellik,
- maç yoğunluğu

kariyer süresini etkilemelidir.

---

# 19. Saha Dışı Yaşam

Saha dışı yaşam, oyunun futbol simülasyonuyla bağlantılı bir sistem olmalıdır.

## 19.1. Futbolcu Yaşam Seçenekleri

- ev satın alma veya kiralama,
- araç satın alma,
- aileye destek,
- yatırım,
- iş kurma,
- tatil,
- gece hayatı,
- sosyal etkinlikler,
- kişisel antrenör,
- psikolojik danışman,
- marka anlaşmaları,
- sosyal medya,
- yardım faaliyetleri,
- menajer değiştirme.

## 19.2. Teknik Direktör Yaşam Seçenekleri

- aile ve şehir tercihleri,
- basın ilişkileri,
- sosyal medya,
- yatırımlar,
- eğitim ve lisanslar,
- yardımcı ekip ağı,
- eski kulüplerle ilişki,
- futbol dışı kamuoyu imajı.

## 19.3. Tasarım Kuralı

Bir yaşam özelliği eklenmeden önce şu sorular cevaplanmalıdır:

- Finansal etkisi var mı?
- Kariyer performansını etkiliyor mu?
- İlişkilere etkisi var mı?
- Medya veya taraftar tepkisi oluşturuyor mu?
- İtibar veya kişilik gelişimini etkiliyor mu?
- Yeni olaylar üretiyor mu?

Hiçbir anlamlı etkisi olmayan özellikler öncelikli geliştirme kapsamına alınmamalıdır.

---

# 20. Finans ve Kişisel Ekonomi

Futbolcu veya teknik direktörün kazandığı para yalnızca ekranda büyüyen bir sayı olmamalıdır.

Kişisel ekonomi alanları:

- maaş,
- prim,
- sponsorluk,
- menajer komisyonu,
- vergi,
- yaşam giderleri,
- yatırımlar,
- gayrimenkul,
- iş girişimleri,
- aile desteği,
- mali danışman,
- borç ve kötü yatırım riski.

Ekonomik kararlar;

- yaşam standardını,
- kamuoyu imajını,
- aile ilişkilerini,
- emeklilik güvenliğini,
- sponsorlukları,
- psikolojik durumu

etkileyebilir.

İlk MVP’de ayrıntılı kişisel ekonomi sistemi bulunmayabilir; ancak veri modeli gelecekte bu sisteme izin verecek şekilde tasarlanmalıdır.

---

# 21. Medya, Taraftar ve İtibar

## 21.1. İtibar Türleri

Tek bir genel itibar değeri yerine farklı itibar alanları kullanılabilir:

- sportif itibar,
- profesyonellik itibarı,
- medya itibarı,
- taraftar itibarı,
- oyuncular arasındaki itibar,
- yönetimler arasındaki itibar,
- ulusal itibar,
- uluslararası itibar.

## 21.2. Medya Anlatıları

Medya yalnızca olayları duyurmamalı; anlatı oluşturmalıdır.

Örnek anlatılar:

- “Gençlere güvenmeyen teknik direktör”
- “Büyük maçların hocası”
- “Disiplinsiz yıldız”
- “Kulübüne sadık kaptan”
- “Parayı tercih eden futbolcu”
- “Yönetimle savaşan teknik direktör”

Anlatılar gerçek olaylardan beslenmeli ancak medya kuruluşunun karakterine göre abartılabilmelidir.

## 21.3. Taraftar Davranışları

Taraftar tepkileri;

- kulüp kültürü,
- sportif başarı,
- oyuncunun geçmişi,
- derbi performansı,
- sadakat,
- basın açıklamaları,
- transfer tercihleri

üzerinden oluşmalıdır.

---

# 22. Rekabet ve Husumet Sistemi

Rekabet yalnızca önceden tanımlanmış derbilerden ibaret olmamalıdır.

Dinamik rekabet kaynakları:

- şampiyonluk yarışları,
- tekrarlanan kritik maçlar,
- tartışmalı transfer,
- basın açıklamaları,
- eski kulübe dönüş,
- iki teknik direktör arasındaki çatışma,
- iki futbolcunun forma rekabeti,
- hakem kararları,
- final veya küme düşme mücadeleleri.

Rekabet;

- medya ilgisini,
- taraftar tepkisini,
- oyuncu baskısını,
- maç atmosferini,
- transfer ilişkilerini

etkileyebilir.

---

# 23. Kariyer Sonrası Yaşam

Futbolcu kariyeri emeklilik ekranında sona ermemelidir.

Muhtemel roller:

- teknik direktör,
- yardımcı antrenör,
- altyapı antrenörü,
- gözlemci,
- sportif direktör,
- oyuncu menajeri,
- kulüp yöneticisi,
- kulüp sahibi,
- yorumcu,
- akademi kurucusu.

Eski futbolcular geçmiş ilişkilerini ve itibarlarını yeni rollerine taşımalıdır.

Örnek:

Gençken forma şansı verilen bir futbolcu, yıllar sonra teknik direktör veya sportif direktör olduğunda eski hocasına karşı olumlu davranabilir.

---

# 24. Uzun Vadeli İçerik ve Şaşırtıcılık

Oyunun ilk üç sezonunda bütün olaylar görülmemelidir.

İçerik katmanları kariyer aşamasına göre açılabilir:

- başlangıç ve kendini kanıtlama,
- istikrar,
- liderlik,
- şöhret,
- kriz,
- kariyer zirvesi,
- düşüş,
- miras bırakma,
- emeklilik,
- yeni rol.

Şaşırtıcılık yalnızca rastgele olaylardan gelmemelidir. Şaşırtıcı gelişmeler;

- geçmiş kararların gecikmeli sonuçları,
- aktörlerin gizli hedefleri,
- değişen ilişkiler,
- dünya ekonomisi,
- yeni kariyer rolleri,
- beklenmedik gelişim veya düşüşler

üzerinden doğmalıdır.

---

# 25. Rastlantısallık ve Adalet

Oyunda rastlantısallık bulunmalıdır ancak sonuçlar anlamsız olmamalıdır.

Temel prensipler:

- rastlantı, olasılık aralıkları içinde çalışmalı,
- kişilik ve bağlam olasılıkları değiştirmeli,
- kritik sonuçlar mümkünse birden fazla faktöre dayanmalı,
- oyuncu her şeyi önceden bilememeli,
- sonuç sonrası açıklanabilir ipuçları bulunmalı,
- kayıt yükleyerek sonucu değiştirme davranışını azaltmak için bazı olaylarda kontrollü tohumlama değerlendirilebilir.

---

# 26. Kayıt ve Dünya Bütünlüğü

Uzun kariyerler oyunun temel vaadi olduğu için kayıt sistemi kritik önemdedir.

Gereksinimler:

- güvenilir kayıt ve yükleme,
- sürüm geçişi,
- veri doğrulama,
- bozuk kayıt kurtarma,
- otomatik yedek,
- olay günlüğü,
- kayıt sürüm numarası,
- deterministik veya yeniden üretilebilir kritik simülasyonlar,
- uzun süreli performans testleri.

Bir oyuncunun 15 yıllık kariyer kaydının güncelleme sonrasında kullanılamaz hâle gelmesi kabul edilemez bir risk olarak değerlendirilmelidir.

---

# 27. İlk Oynanabilir Sürüm – MVP

İlk sürüm, nihai oyunun küçük ama gerçek çalışan çekirdeği olmalıdır.

## 27.1. MVP’nin Ana Amacı

Şu soruyu test etmek:

> Transfer, taktik, maç, ilişkiler, sözler, hafıza ve dinamik olaylardan oluşan çekirdek oyun en az 5–10 sezon boyunca anlamlı kararlar üretebiliyor mu?

## 27.2. MVP Kapsamı

- yalnızca teknik direktör kariyeri,
- kurgusal tek ülke,
- sınırlı sayıda lig,
- yaklaşık 20–40 kulüp,
- yönetilebilir sayıda futbolcu,
- sezon ve takvim sistemi,
- temel kulüp modeli,
- temel futbolcu modeli,
- teknik direktör profili,
- basit antrenman ve kadro yönetimi,
- temel taktik sistemi,
- iki boyutlu veya metin tabanlı maç simülasyonu,
- temel transfer ve sözleşme sistemi,
- oyuncu ilişkileri,
- söz verme sistemi,
- hafıza kayıtları,
- sınırlı ancak sistemik olay motoru,
- temel basın ve yönetim görüşmeleri,
- kayıt ve yükleme,
- uzun sezon simülasyon testleri.

## 27.3. MVP Dışında Tutulacaklar

- üç boyutlu maç motoru,
- çevrim içi çok oyunculu mod,
- gerçek lisanslı kulüpler ve futbolcular,
- ayrıntılı futbolcu yaşam simülasyonu,
- ayrıntılı kişisel ekonomi,
- futbolcu kariyeri,
- kulüp sahipliği,
- gelişmiş sponsor sistemi,
- bütün dünya ligleri,
- gerçek zamanlı üretken yapay zekâya zorunlu bağımlılık.

Bu alanlar gelecekte eklenebilir ancak ilk çekirdeğin doğrulanmasını geciktirmemelidir.

---

# 28. Genişletilmiş Sürüm Hedefleri

MVP doğrulandıktan sonra değerlendirilebilecek sistemler:

- futbolcu kariyeri,
- ayrıntılı altyapı ve keşif,
- gelişmiş medya anlatıları,
- futbolcu özel hayatı,
- kişisel ekonomi,
- sponsorluk,
- farklı ülkeler ve lig kültürleri,
- dinamik federasyon kararları,
- sportif direktör kariyeri,
- gelişmiş 2D maç sunumu,
- mod desteği,
- bulut kayıt,
- isteğe bağlı yapay zekâ destekli diyalog zenginleştirme.

---

# 29. Nihai Vizyon

Uzun vadeli nihai sürüm şu özelliklere ulaşabilir:

- onlarca sezon süren kalıcı dünya,
- futbolcu, teknik direktör ve yönetici kariyerleri,
- emeklilik sonrası devam eden yaşam,
- değişen lig ve kulüp güçleri,
- gelişmiş saha dışı yaşam,
- çok katmanlı sosyal ilişkiler,
- dinamik futbol tarihi,
- gelişmiş 2D veya 3D maç motoru,
- kullanıcı tarafından oluşturulan içerik,
- modüler lig paketleri,
- isteğe bağlı çevrim içi özellikler.

Nihai vizyon, MVP’nin geliştirilme kapsamı olarak değerlendirilmemelidir.

---

# 30. Değişmez Proje Kuralları

## Kural 1

Bir özellik, hangi sistemleri etkilediği ve hangi sistemlerden etkilendiği yazılmadan kodlanmayacaktır.

## Kural 2

Önemli oyun sonuçları doğrudan bağımsız tablo güncellemeleriyle değil, mümkün olduğunca tanımlı olaylar ve kurallar üzerinden üretilecektir.

## Kural 3

Bir sistem tamamlanmadan çok sayıda yeni sistem paralel biçimde açılmayacaktır.

## Kural 4

Her ana sistem için otomatik testler ve uzun dönem simülasyon testleri planlanacaktır.

## Kural 5

Kullanıcı arayüzü, iş kurallarının bulunduğu yer olmayacaktır.

## Kural 6

Oyun motoru, veritabanı veya arayüz teknolojisi değişse bile temel alan modeli korunabilecek şekilde modüler tasarım hedeflenecektir.

## Kural 7

Oyunun ana simülasyonu internet bağlantısına veya harici bir üretken yapay zekâ servisine zorunlu olarak bağımlı olmayacaktır.

## Kural 8

Her özellik MVP, genişletilmiş sürüm veya nihai vizyon kategorilerinden birine atanacaktır.

## Kural 9

Yeni özellik eklemekten önce mevcut sistemlerin uzun sezonlarda tekrar üretip üretmediği test edilecektir.

## Kural 10

Kısa vadeli hız uğruna kayıt bütünlüğü, test edilebilirlik ve sistem açıklığı feda edilmeyecektir.

---

# 31. Örnek Sistem Etkileşim Matrisi

| Kaynak sistem | Olay | Etkilenen sistemler |
|---|---|---|
| Yaşam tarzı | Futbolcu gece kulübünde görüntülendi | Yorgunluk, medya, itibar, ilişki, sponsor, maç performansı |
| Diyalog | Teknik direktör forma sözü verdi | Sözler, hafıza, güven, kadro planı |
| Kadro yönetimi | Söz verilen oyuncu oynatılmadı | Güven, moral, menajer, medya, transfer isteği |
| Transfer | Rakip kulüp yıldız oyuncuyu aldı | Taraftar, rekabet, yönetim güveni, medya |
| Maç | Genç oyuncu finalde gol attı | İtibar, gelişim, özgüven, transfer değeri, hafıza |
| Basın | Teknik direktör oyuncuyu suçladı | İlişki, takım grupları, medya anlatısı, yönetim |
| Ekonomi | Kulüp maaş ödemesini geciktirdi | Moral, transfer isteği, yönetim itibarı, medya |
| Sakatlık | Kaptan uzun süre sakatlandı | Liderlik, taktik, takım uyumu, transfer ihtiyacı |

---

# 32. Örnek Ayrıntılı Olay Senaryosu

## Başlangıç Durumu

- 21 yaşındaki genç kanat oyuncusu son beş maçta yedek kalmıştır.
- Oyuncunun hırsı ve ego seviyesi yüksektir.
- Teknik direktöre olan saygısı yüksek, güveni orta seviyededir.
- Menajeri fırsatçı ve medyayı kullanmaya yatkındır.
- Takım şampiyonluk yarışındadır.

## Olay Zinciri

1. Oyuncu bireysel görüşme talep eder.
2. Daha fazla forma süresi ister.
3. Teknik direktör üç maç içinde şans vereceğini söyler.
4. Söz hafıza ve söz sistemine kaydedilir.
5. İlk maçta oyuncu yedek kalır ancak takım kazanır.
6. İkinci maçta oyuncu yine yedek kalır.
7. Güven azalır fakat sportif başarı nedeniyle tepki sınırlı kalır.
8. Üçüncü maç öncesi oyuncu antrenmanda iyi performans gösterir.
9. Teknik direktör yine oynatmazsa söz ihlal edilir.
10. Oyuncunun menajeri görüşme talep eder.
11. Teknik direktör savunmacı veya uzlaşmacı cevap verebilir.
12. Görüşme kötü geçerse menajer basına bilgi sızdırabilir.
13. Medya “Genç oyuncuya verilen söz tutulmadı” anlatısını oluşturabilir.
14. Takım kaptanı teknik direktörü destekleyebilir veya oyuncunun yanında durabilir.
15. Başka bir kulüp durumu fırsat olarak görebilir.
16. Yönetim krizin şampiyonluk yarışını etkilemesinden endişe edebilir.
17. Oyuncu transfer isteyebilir, özür kabul edebilir veya antrenman disiplinini düşürebilir.

Bu örnekte sonuç, yalnızca “moral -10” değildir. Olay birden fazla sistemi etkiler ve farklı yönlere ilerleyebilir.

---

# 33. Alt Tasarım Dokümanları

Bu ana doküman ileride aşağıdaki belgelerle desteklenmelidir:

- `00_PROJECT_INDEX.md`
- `02_MVP_SCOPE.md`
- `03_DOMAIN_MODEL.md`
- `04_EVENT_RULE_ENGINE.md`
- `05_MEMORY_AND_PROMISE_SYSTEM.md`
- `06_RELATIONSHIP_SYSTEM.md`
- `07_DIALOGUE_SYSTEM.md`
- `08_TRANSFER_SYSTEM.md`
- `09_MATCH_SIMULATION.md`
- `10_MANAGER_CAREER.md`
- `11_PLAYER_CAREER.md`
- `12_WORLD_SIMULATION.md`
- `13_SAVE_SYSTEM.md`
- `14_TEST_STRATEGY.md`
- `15_DECISION_LOG.md`

Her belge yalnızca gerektiği aşamada oluşturulmalıdır.

---

# 34. Açık Tasarım Soruları

Aşağıdaki konular daha sonra karara bağlanmalıdır:

1. Oyun dünyası gerçek kulüpler yerine tamamen kurgusal mı olacak?
2. İlk sürümde kaç lig ve takım bulunacak?
3. Bir oyun günü hangi adımlarla ilerleyecek?
4. Maçlar gerçek zamanlı mı, hızlandırılmış mı, tamamen simülasyon mu olacak?
5. Oyuncu bütün teknik direktör görevlerini mi yapacak, bazı görevleri ekibine devredebilecek mi?
6. Diyaloglarda serbest metin girişi olacak mı?
7. Oyuncu özellikleri sayısal olarak ne kadar görünür olacak?
8. Gizli bilgiler nasıl keşfedilecek?
9. Dünya simülasyonunda performans için hangi ayrıntı seviyeleri kullanılacak?
10. Oyun yalnızca masaüstüne mi geliştirilecek?
11. Mod desteği hangi aşamada planlanacak?
12. Gerçek futbol verileri veya lisanslar gelecekte değerlendirilecek mi?
13. Futbolcu kariyerinde maç kontrolü nasıl sunulacak?
14. Oyun içi bir sezonun hedeflenen gerçek oynama süresi ne olacak?
15. Oyuncunun başarısız olması ve işsiz kalması nasıl eğlenceli tutulacak?

Bu soruların tamamı ilk aşamada cevaplanmak zorunda değildir. Ancak teknik mimariyi doğrudan etkileyen sorular geliştirme başlamadan önce çözülmelidir.

---

# 35. Başarı Ölçütleri

İlk çekirdek sürüm başarılı kabul edilebilmek için:

- en az 10 sezon boyunca hata vermeden simüle edilebilmeli,
- kulüp ve oyuncu davranışları tamamen anlamsız görünmemeli,
- aynı başlangıçtan farklı kariyer sonuçları üretilebilmeli,
- önemli kararlar geçmiş olaylarla bağlantı kurabilmeli,
- transferler yalnızca para karşılaştırması olmamalı,
- maç sonuçları güç farkıyla ilişkili fakat tamamen belirlenmiş olmamalı,
- söz ve ilişki sistemleri gerçek oynanış sonuçları üretmeli,
- oyuncu birkaç sezon içinde bütün olay kalıplarını tüketmemeli,
- kayıt dosyası uzun kariyerlerde güvenilir kalmalıdır.

---

# 36. Geliştirme Yaklaşımı

Her ana sistem için şu sıra izlenmelidir:

1. Sistem amacı yazılır.
2. Kullanılan veriler tanımlanır.
3. Etkilendiği sistemler tanımlanır.
4. Etkilediği sistemler tanımlanır.
5. Ürettiği olaylar yazılır.
6. Tepki verdiği olaylar yazılır.
7. Örnek senaryolar hazırlanır.
8. Veri modeli oluşturulur.
9. İş kuralları kodlanır.
10. Otomatik testler yazılır.
11. Uzun dönem simülasyon testi yapılır.
12. Kullanıcı arayüzü bağlanır.

Bu sıra korunarak aceleci ve kontrolsüz geliştirme önlenmelidir.

---

# 37. Belge Değişiklik Politikası

Bu belgede yapılan önemli değişiklikler aşağıdaki biçimde kaydedilmelidir:

| Sürüm | Tarih | Değişiklik | Gerekçe |
|---|---|---|---|
| 0.1 | İlk oluşturma | Ana vizyon ve sistemler tanımlandı | Proje başlangıç referansı |

Yeni fikirler doğrudan ana kapsama eklenmeden önce şu sorularla değerlendirilmelidir:

- Oyunun temel vaadine hizmet ediyor mu?
- Başka hangi sistemleri etkiliyor?
- MVP için gerekli mi?
- Tekrar hissini azaltıyor mu?
- Test edilebilir mi?
- Teknik maliyeti sağladığı değere uygun mu?

---

# 38. Sonuç

Bu proje, yalnızca çok sayıda özellik eklenerek başarılı olamaz.

Oyunun temel gücü;

- yaşayan dünya,
- olay bağlamı,
- kalıcı hafıza,
- karakter motivasyonları,
- sistemler arası sonuçlar,
- uzun vadeli kariyer değişimi

üzerine kurulmalıdır.

Geliştirme sürecinde hızdan önce tutarlılık, özellik sayısından önce sistem derinliği ve görsel gösterişten önce oynanış sonuçları önceliklendirilmelidir.

Bu belge, bütün teknik ve tasarımsal kararların başlangıç noktasıdır.
