import { AfterViewInit, Component, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  BUSINESS_PHONE_DISPLAY,
  BUSINESS_PHONE_TEL,
  DEVELOPER_CREDIT_URL,
  FACEBOOK_URL,
  INSTAGRAM_URL,
  TIKTOK_URL,
  WHATSAPP_CONTACT_URL
} from '../../core/business-info';
import { ACCESSIBILITY_STATEMENT_HTML, PRIVACY_POLICY_HTML } from '../../core/legal-content';
import { ModalComponent } from '../../shared/modal/modal.component';

interface Step {
  num: string;
  title: string;
  text: string;
}

interface GalleryPhoto {
  src: string;
  alt: string;
}

interface Review {
  name: string;
  eventType: string;
  quote: string;
}

interface Faq {
  question: string;
  answer: string;
}

const REVIEW_CARD_WIDTH = 320;
const REVIEW_CARD_GAP = 16;
const REVIEW_STEP = REVIEW_CARD_WIDTH + REVIEW_CARD_GAP;

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterLink, ModalComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent implements AfterViewInit, OnDestroy {
  whatsappUrl = WHATSAPP_CONTACT_URL;
  phoneDisplay = BUSINESS_PHONE_DISPLAY;
  phoneTel = BUSINESS_PHONE_TEL;
  instagramUrl = INSTAGRAM_URL;
  facebookUrl = FACEBOOK_URL;
  tiktokUrl = TIKTOK_URL;
  developerCreditUrl = DEVELOPER_CREDIT_URL;
  logoSrc = 'assets/logo-ys (1).png';

  privacyPolicyHtml = PRIVACY_POLICY_HTML;
  accessibilityStatementHtml = ACCESSIBILITY_STATEMENT_HTML;
  legalDocOpen: 'privacy' | 'accessibility' | null = null;

  steps: Step[] = [
    {
      num: '01',
      title: 'סרקו את הברקוד שעל השולחן',
      text: 'כל שולחן מקבל ברקוד אישי. סורקים עם מצלמת הטלפון — בלי להוריד שום אפליקציה.'
    },
    {
      num: '02',
      title: 'בחרו איזו תמונה להדפיס',
      text: 'מצלמים סלפי טרי או בוחרים תמונה אהובה מהגלריה — כל אורח בוחר בעצמו בדיוק את הרגע שיודפס.'
    },
    {
      num: '03',
      title: 'אספו את המגנט',
      text: 'המגנט מודפס תוך רגעים, והודעת איסוף נשלחת ברגע שהוא מוכן.'
    }
  ];

  galleryPhotos: GalleryPhoto[] = [
    { src: 'assets/gallery-1.webp', alt: 'רגע ריקודים' },
    { src: 'assets/gallery-2.webp', alt: 'זוג צוחק' },
    { src: 'assets/gallery-3.webp', alt: 'חברות' },
    { src: 'assets/gallery-4.webp', alt: 'משפחה' },
    { src: 'assets/gallery-5.webp', alt: 'ילדים' },
    { src: 'assets/gallery-6.webp', alt: 'קלוז-אפ' }
  ];

  aboutPhotoSrc = 'assets/me.webp';

  reviews: Review[] = [
    { name: 'שירן ואורי', eventType: 'חתונה', quote: 'יגל היה הצלם שלנו וגם הביא את שירות הברקודים — האורחים סרקו, בחרו תמונה, ולקחו מגנט הביתה. שילוב מנצח!' },
    { name: 'משפחת לוי', eventType: 'בר מצווה', quote: 'כל האורחים בלי יוצא מן הכלל יצאו מרוצים עם מגנט ביד. הילדים לא הפסיקו לסרוק ולהדפיס.' },
    { name: 'טלי כהן', eventType: 'אירוע חברה', quote: 'מיתגנו את המגנטים עם הלוגו של החברה. נראה יוקרתי, וכולם אהבו שהם בוחרים לבד את התמונה.' },
    { name: 'נועה ורן', eventType: 'חתונה', quote: 'הכי אהבנו שאין תורים ואין עמדה מסורבלת — כל אחד סורק מהטלפון שלו ובוחר בדיוק את התמונה שאהב.' },
    { name: 'דנה', eventType: 'יום הולדת 40', quote: 'שירות חלק ומהיר. תוך שניות מהצילום כבר היה מגנט מוכן לאיסוף. פשוט כיף.' },
    { name: 'אבי ומיכל', eventType: 'ברית', quote: 'לקחנו חבילה של צילום מקצועי יחד עם הברקודים. קיבלנו גם אלבום מהמם וגם מזכרת לכל אורח.' },
    { name: 'רועי', eventType: 'אירוע חברה', quote: 'פשוט, כיפי ובלי כאב ראש. האורחים בחרו תמונות והדפיסו בעצמם — הצלחה מוחלטת לכל הצוות.' },
    { name: 'ליאת', eventType: 'בת מצווה', quote: 'הבנות היו בהיסטריה. כל אחת בחרה סלפי אחר והדפיסה כמה מגנטים שרצתה.' },
    { name: 'משפחת פרץ', eventType: 'חינה', quote: 'הצבעוניות של האירוע קיבלה מגנטים תואמים. שירות אדיב ומקצועי מהרגע הראשון ועד הסוף.' },
    { name: 'יוסי ואורלי', eventType: 'חתונה', quote: 'בסוף הערב לכל אורח היה מגנט על המקרר. בדיוק המזכרת שרצינו שתישאר מהיום הכי חשוב שלנו.' }
  ];
  activeReviewIndex = 9;

  faqs: Faq[] = [
    { question: 'כמה זמן לוקח עד שהמגנט מוכן?', answer: 'משך ההמתנה משתנה בהתאם לעומס ההזמנות. ההזמנה שלך נכנסת לתור ההדפסה, וכשמגיע תורה המגנט מודפס. בסיום ההדפסה תקבל/י הודעה שהמגנט מוכן לאיסוף.' },
    { question: 'צריך להתקין אפליקציה?', answer: 'ממש לא. סורקים את הברקוד עם מצלמת הטלפון והכול עובד ישירות בדפדפן.' },
    { question: 'איך האורח בוחר את התמונה?', answer: 'אחרי הסריקה נפתח לאורח מסך שבו הוא מצלם סלפי או בוחר תמונה מהגלריה.' },
    { question: 'כמה מגנטים אפשר להפיק בערב?', answer: 'ללא הגבלה. השירות עובד ברצף לאורך כל האירוע וכל אורח יכול להפיק כמה שירצה.' },
    { question: 'אפשר להוסיף מיתוג אישי לאירוע?', answer: 'בטח. אפשר לעצב את המגנט עם שם, תאריך, לוגו או עיצוב שתואם לאווירת האירוע שלכם.' },
    { question: 'מתאים לכל סוג אירוע?', answer: 'בהחלט — חתונות, בר/בת מצווה, ימי הולדת, אירועי חברה וברים. מתאימים את המיתוג לאירוע שלכם.' },
    { question: 'מה צריך להכין מראש מבחינתי?', answer: 'כמעט כלום — רק חשמל ומעט מקום לנקודת האיסוף. את כל השאר אני מביא ומקים.' },
    { question: 'מציעים גם שירותי צילום?', answer: 'כן! אני צלם אירועים, ואפשר לשלב חבילת צילום מקצועי יחד עם שירות הברקודים והמגנטים.' },
    { question: 'באילו אזורים אתם פועלים?', answer: 'בכל הארץ. ספרו לי איפה ומתי האירוע ונתאם את כל הפרטים.' },
    { question: 'איך מזמינים ומה המחיר?', answer: 'שולחים לי הודעה עם התאריך וסוג האירוע, ואני חוזר אליכם עם הצעה מותאמת ובדיקת זמינות.' }
  ];
  openFaqIndex: number | null = null;

  private observer?: IntersectionObserver;

  constructor(private host: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) return;

    const targets = this.host.nativeElement.querySelectorAll('.reveal');
    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add('revealed');
            this.observer?.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.15 }
    );
    targets.forEach((el) => this.observer!.observe(el));
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  openLegalDoc(doc: 'privacy' | 'accessibility'): void {
    this.legalDocOpen = doc;
  }

  closeLegalDoc(): void {
    this.legalDocOpen = null;
  }

  get reviewTrackTransform(): string {
    return `translateX(${-this.activeReviewIndex * REVIEW_STEP}px)`;
  }

  nextReview(): void {
    this.activeReviewIndex = (this.activeReviewIndex + 1) % this.reviews.length;
  }

  prevReview(): void {
    this.activeReviewIndex = (this.activeReviewIndex - 1 + this.reviews.length) % this.reviews.length;
  }

  goToReview(i: number): void {
    this.activeReviewIndex = i;
  }

  toggleFaq(i: number): void {
    this.openFaqIndex = this.openFaqIndex === i ? null : i;
  }
}
