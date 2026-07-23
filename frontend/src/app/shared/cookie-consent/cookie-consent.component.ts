import { Component, ElementRef, OnDestroy, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CookieConsentService } from '../../core/cookie-consent.service';
import { AccessibilityService } from '../../core/accessibility.service';
import { PRIVACY_POLICY_HTML } from '../../core/legal-content';
import { ModalComponent } from '../modal/modal.component';

const DISMISS_ANIMATION_MS = 220;
const BANNER_OFFSET_VAR = '--cookie-banner-offset';

@Component({
  selector: 'app-cookie-consent',
  standalone: true,
  imports: [CommonModule, ModalComponent],
  templateUrl: './cookie-consent.component.html',
  styleUrl: './cookie-consent.component.scss'
})
export class CookieConsentComponent implements OnDestroy {
  readonly isLeaving = signal(false);
  readonly privacyOpen = signal(false);
  readonly privacyPolicyHtml = PRIVACY_POLICY_HTML;

  private resizeObserver?: ResizeObserver;

  constructor(public consent: CookieConsentService, private a11y: AccessibilityService) {}

  // Tracks the fixed-position banner element (present only while @if is true) so the
  // accessibility widget / floating WhatsApp button can be pushed clear of it — a
  // `position: fixed` element doesn't contribute to a normal parent's box, so this has to
  // observe the banner itself rather than the component host.
  @ViewChild('banner')
  set bannerRef(ref: ElementRef<HTMLElement> | undefined) {
    this.resizeObserver?.disconnect();
    if (ref) {
      this.resizeObserver = new ResizeObserver((entries) => {
        const height = entries[0]?.contentRect.height ?? 0;
        document.documentElement.style.setProperty(BANNER_OFFSET_VAR, `${height}px`);
      });
      this.resizeObserver.observe(ref.nativeElement);
    } else {
      document.documentElement.style.setProperty(BANNER_OFFSET_VAR, '0px');
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    document.documentElement.style.setProperty(BANNER_OFFSET_VAR, '0px');
  }

  accept(): void {
    this.dismiss(() => this.consent.accept());
  }

  decline(): void {
    this.dismiss(() => this.consent.decline());
  }

  openPrivacy(): void {
    this.privacyOpen.set(true);
  }

  closePrivacy(): void {
    this.privacyOpen.set(false);
  }

  private dismiss(action: () => void): void {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const skipAnimation = reduceMotion || this.a11y.state().disableAnimations;

    this.isLeaving.set(true);
    setTimeout(action, skipAnimation ? 0 : DISMISS_ANIMATION_MS);
  }
}
