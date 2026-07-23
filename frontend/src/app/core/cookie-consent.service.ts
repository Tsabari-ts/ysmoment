import { Injectable, computed, signal } from '@angular/core';

export type CookieConsentChoice = 'accepted' | 'declined';

const STORAGE_KEY = 'cookieConsent';

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  private readonly _choice = signal<CookieConsentChoice | null>(this.loadChoice());
  readonly choice = this._choice.asReadonly();
  readonly showBanner = computed(() => this._choice() === null);

  accept(): void {
    this.setChoice('accepted');
  }

  decline(): void {
    this.setChoice('declined');
  }

  private setChoice(choice: CookieConsentChoice): void {
    this._choice.set(choice);
    try {
      localStorage.setItem(STORAGE_KEY, choice);
    } catch {
      /* private-mode/quota — nothing we can do */
    }
  }

  private loadChoice(): CookieConsentChoice | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw === 'accepted' || raw === 'declined' ? raw : null;
    } catch {
      return null;
    }
  }
}
