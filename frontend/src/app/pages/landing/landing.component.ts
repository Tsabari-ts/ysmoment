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

interface WhyCard {
  icon: 'camera' | 'scan' | 'heart';
  title: string;
  text: string;
}

interface Step {
  num: string;
  text: string;
}

interface Testimonial {
  name: string;
  eventType: string;
  quote: string;
}

interface GalleryTile {
  rotate: number;
}

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

  privacyPolicyHtml = PRIVACY_POLICY_HTML;
  accessibilityStatementHtml = ACCESSIBILITY_STATEMENT_HTML;
  legalDocOpen: 'privacy' | 'accessibility' | null = null;

  whyCards: WhyCard[] = [
    {
      icon: 'camera',
      title: 'חוויה, לא רק תמונה',
      text: 'האורחים לא רק מצטלמים, הם יוצאים עם חפץ ביד. זה מה שהופך אתכם לאירוע שמדברים עליו.'
    },
    {
      icon: 'scan',
      title: 'אפס בלאגן טכני',
      text: 'בלי אפליקציות להורדה, בלי הסברים לאורחים. סורקים, מעלים, וזהו.'
    },
    {
      icon: 'heart',
      title: 'עמדה חיה באירוע',
      text: 'צוות מקצועי נמצא איתכם לאורך כל האירוע, לא עוזב לרגע.'
    }
  ];

  steps: Step[] = [
    { num: '01', text: 'סרקו את הברקוד שעל השולחן' },
    { num: '02', text: 'צלמו תמונה או העלו קיימת מהגלריה' },
    { num: '03', text: 'אספו את המגנט מעמדה ייעודית עם קבלת הודעת איסוף' }
  ];

  // TODO: replace with real event photos
  galleryTiles: GalleryTile[] = [-6, 3, -3, 8, -4, 5, -7, 4].map((rotate) => ({ rotate }));

  // TODO: placeholder testimonials — replace with real client quotes
  testimonials: Testimonial[] = [
    { name: 'מאיה ואיתי', eventType: 'חתונה', quote: 'האורחים פשוט השתגעו על זה — כולם יצאו עם מגנט ביד וזה היה הנושא של הערב!' },
    { name: 'רועי', eventType: 'בר מצווה', quote: 'הכל היה חלק ומקצועי, ילדים ומבוגרים נהנו באותה מידה. ממליץ בחום.' },
    { name: 'שירה', eventType: 'אירוע פרטי', quote: 'מזכרת אמיתית מהערב, לא סתם תמונה במצלמה. תוספת שעשתה את ההבדל.' }
  ];

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
}
