namespace AvaloniaCalculator.Localization;

// Port-specific controls not present in Uno's resource dictionary (Cut; Manual rate).
internal static class ExtraStrings
{
    private static readonly Dictionary<string,string[]> Values = """
af-ZA|Knip|Handmatige koers
am-ET|ቁረጥ|በእጅ የሚገባ የምንዛሬ ተመን
ar-SA|قص|سعر صرف يدوي
az-Latn-AZ|Kəs|Əl ilə məzənnə
be-BY|Выразаць|Курс уручную
bg-BG|Изрязване|Ръчен курс
bn-BD|কাটুন|ম্যানুয়াল বিনিময় হার
ca-ES|Retalla|Tipus de canvi manual
cs-CZ|Vyjmout|Ruční kurz
da-DK|Klip|Manuel kurs
de-DE|Ausschneiden|Manueller Wechselkurs
el-GR|Αποκοπή|Χειροκίνητη ισοτιμία
en-GB|Cut|Manual rate
en-US|Cut|Manual rate
es-ES|Cortar|Tipo de cambio manual
es-MX|Cortar|Tipo de cambio manual
et-EE|Lõika|Käsitsi sisestatud kurss
eu-ES|Ebaki|Eskuzko truke-tasa
fa-IR|برش|نرخ دستی
fi-FI|Leikkaa|Manuaalinen kurssi
fil-PH|Gupitin|Manwal na palitan
fr-CA|Couper|Taux manuel
fr-FR|Couper|Taux manuel
gl-ES|Cortar|Tipo de cambio manual
ha-Latn-NG|Yanke|Farashin musaya da hannu
he-IL|גזור|שער חליפין ידני
hi-IN|काटें|मैन्युअल विनिमय दर
hr-HR|Izreži|Ručni tečaj
hu-HU|Kivágás|Kézi árfolyam
id-ID|Potong|Kurs manual
is-IS|Klippa|Handvirkt gengi
it-IT|Taglia|Tasso manuale
ja-JP|切り取り|手動の為替レート
kk-KZ|Қиып алу|Қолмен енгізілген бағам
km-KH|កាត់|អត្រាប្តូរប្រាក់ដោយដៃ
kn-IN|ಕತ್ತರಿಸಿ|ಹಸ್ತಚಾಲಿತ ವಿನಿಮಯ ದರ
ko-KR|잘라내기|수동 환율
lo-LA|ຕັດ|ອັດຕາແລກປ່ຽນດ້ວຍຕົນເອງ
lt-LT|Iškirpti|Rankinis kursas
lv-LV|Izgriezt|Manuāls kurss
mk-MK|Исечи|Рачен курс
ml-IN|മുറിക്കുക|സ്വമേധയാ നൽകുന്ന വിനിമയ നിരക്ക്
ms-MY|Potong|Kadar manual
nb-NO|Klipp ut|Manuell kurs
nl-NL|Knippen|Handmatige koers
pl-PL|Wytnij|Kurs ręczny
pt-BR|Recortar|Taxa manual
pt-PT|Cortar|Taxa manual
ro-RO|Decupare|Curs manual
ru-RU|Вырезать|Курс вручную
sk-SK|Vystrihnúť|Ručný kurz
sl-SI|Izreži|Ročni tečaj
sq-AL|Prit|Kurs manual
sr-Latn-RS|Iseci|Ručni kurs
sv-SE|Klipp ut|Manuell kurs
sw-KE|Kata|Kiwango cha kubadilisha kwa mkono
ta-IN|வெட்டு|கைமுறை மாற்று விகிதம்
te-IN|కత్తిరించు|మాన్యువల్ మార్పిడి రేటు
th-TH|ตัด|อัตราแลกเปลี่ยนที่กำหนดเอง
tr-TR|Kes|Manuel kur
uk-UA|Вирізати|Курс вручну
uz-Latn-UZ|Kesish|Qo‘lda kiritilgan kurs
vi-VN|Cắt|Tỷ giá thủ công
zh-CN|剪切|手动汇率
zh-TW|剪下|手動匯率
""".Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(line=>line.Trim().Split('|')).ToDictionary(p=>p[0],p=>p[1..]);
    public static string Get(string language,string key) => Values[language][key switch { "Cut"=>0,"ManualRate"=>1,_=>throw new KeyNotFoundException(key) }];
}
