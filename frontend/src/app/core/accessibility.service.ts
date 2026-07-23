import { Injectable, signal } from '@angular/core';

export type AccessibilityColorMode = 'default' | 'dark' | 'light';

export interface AccessibilitySettings {
  textSizeStep: 0 | 1 | 2 | 3;
  readableFont: boolean;
  highContrast: boolean;
  colorMode: AccessibilityColorMode;
  invertColors: boolean;
  grayscale: boolean;
  highlightLinks: boolean;
  highlightButtons: boolean;
  textSpacing: boolean;
  disableAnimations: boolean;
  largeCursor: boolean;
  focusHighlight: boolean;
  hideImages: boolean;
  highlightHeadings: boolean;
  readingMask: boolean;
  readingGuide: boolean;
  magnifier: boolean;
}

export type AccessibilityToggleKey =
  | 'readableFont'
  | 'highContrast'
  | 'invertColors'
  | 'grayscale'
  | 'highlightLinks'
  | 'highlightButtons'
  | 'textSpacing'
  | 'disableAnimations'
  | 'largeCursor'
  | 'focusHighlight'
  | 'hideImages'
  | 'highlightHeadings'
  | 'readingMask'
  | 'readingGuide'
  | 'magnifier';

const STORAGE_KEY = 'ys-accessibility-settings';

const DEFAULT_SETTINGS: AccessibilitySettings = {
  textSizeStep: 0,
  readableFont: false,
  highContrast: false,
  colorMode: 'default',
  invertColors: false,
  grayscale: false,
  highlightLinks: false,
  highlightButtons: false,
  textSpacing: false,
  disableAnimations: false,
  largeCursor: false,
  focusHighlight: false,
  hideImages: false,
  highlightHeadings: false,
  readingMask: false,
  readingGuide: false,
  magnifier: false
};

const HEBREW_RANGE = /[֐-׿]/g;

@Injectable({ providedIn: 'root' })
export class AccessibilityService {
  private readonly _settings = signal<AccessibilitySettings>(this.loadSettings());
  readonly state = this._settings.asReadonly();

  readonly isReading = signal(false);
  readonly noVoiceForLang = signal(false);
  readonly mousePosition = signal({ x: 0, y: 0 });

  constructor() {
    this.applyToDom(this._settings());
  }

  toggle(key: AccessibilityToggleKey): void {
    this.mutate((s) => {
      const next = { ...s, [key]: !s[key] };
      if (key === 'highContrast' && next.highContrast) next.colorMode = 'default';
      return next;
    });
  }

  setColorMode(mode: AccessibilityColorMode): void {
    this.mutate((s) => ({
      ...s,
      colorMode: s.colorMode === mode ? 'default' : mode,
      highContrast: false
    }));
  }

  increaseTextSize(): void {
    this.mutate((s) => ({ ...s, textSizeStep: (Math.min(3, s.textSizeStep + 1) as 0 | 1 | 2 | 3) }));
  }

  restoreTextSize(): void {
    this.mutate((s) => ({ ...s, textSizeStep: 0 }));
  }

  reset(): void {
    this.stopReading();
    this._settings.set({ ...DEFAULT_SETTINGS });
    this.applyToDom(DEFAULT_SETTINGS);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* private-mode/quota — nothing we can do */
    }
  }

  readPage(): void {
    if (!('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    this.noVoiceForLang.set(false);

    const text = this.extractPageText();
    if (!text) return;

    const lang = this.detectLang(text);

    const voices = window.speechSynthesis.getVoices();
    if (voices.length === 0) {
      // Voice list loads asynchronously in most browsers on first use.
      window.speechSynthesis.addEventListener('voiceschanged', () => this.speak(text, lang), { once: true });
    } else {
      this.speak(text, lang);
    }
  }

  private speak(text: string, lang: string): void {
    const voice = this.findVoice(lang);

    // No installed voice actually speaks this language: most engines fall back to a default
    // English voice that silently drops every word it can't pronounce instead of erroring,
    // which sounds like broken noise rather than a clear failure. Surface it instead of
    // playing that back to the user — this is a missing OS voice pack, not a code bug.
    if (!voice) {
      this.noVoiceForLang.set(true);
      this.isReading.set(false);
      return;
    }

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = lang;
    utterance.voice = voice;
    utterance.onend = () => this.isReading.set(false);
    utterance.onerror = () => this.isReading.set(false);

    window.speechSynthesis.speak(utterance);
    this.isReading.set(true);
  }

  private findVoice(lang: string): SpeechSynthesisVoice | undefined {
    const voices = window.speechSynthesis.getVoices();
    const prefix = lang.split('-')[0];
    // "iw" is the deprecated-but-still-shipped legacy ISO code for Hebrew that some
    // OS voice packs still use instead of "he".
    const altPrefix = prefix === 'he' ? 'iw' : prefix;
    return (
      voices.find((v) => v.lang === lang) ??
      voices.find((v) => v.lang.startsWith(prefix)) ??
      voices.find((v) => v.lang.startsWith(altPrefix))
    );
  }

  stopReading(): void {
    if ('speechSynthesis' in window) window.speechSynthesis.cancel();
    this.isReading.set(false);
    this.noVoiceForLang.set(false);
  }

  setMousePosition(x: number, y: number): void {
    this.mousePosition.set({ x, y });
  }

  private mutate(fn: (s: AccessibilitySettings) => AccessibilitySettings): void {
    const next = fn(this._settings());
    this._settings.set(next);
    this.applyToDom(next);
    this.saveSettings(next);
  }

  private applyToDom(s: AccessibilitySettings): void {
    const root = document.documentElement;
    const classMap: Record<string, boolean> = {
      'accessibility-text-step-1': s.textSizeStep === 1,
      'accessibility-text-step-2': s.textSizeStep === 2,
      'accessibility-text-step-3': s.textSizeStep === 3,
      'accessibility-readable-font': s.readableFont,
      'accessibility-high-contrast': s.highContrast,
      'accessibility-dark-mode': s.colorMode === 'dark',
      'accessibility-light-mode': s.colorMode === 'light',
      'accessibility-highlight-links': s.highlightLinks,
      'accessibility-highlight-buttons': s.highlightButtons,
      'accessibility-text-spacing': s.textSpacing,
      'accessibility-disable-animations': s.disableAnimations,
      'accessibility-large-cursor': s.largeCursor,
      'accessibility-focus-highlight': s.focusHighlight,
      'accessibility-hide-images': s.hideImages,
      'accessibility-highlight-headings': s.highlightHeadings,
      'accessibility-reading-mask': s.readingMask,
      'accessibility-reading-guide': s.readingGuide,
      'accessibility-magnifier-active': s.magnifier
    };

    for (const [cls, active] of Object.entries(classMap)) {
      root.classList.toggle(cls, active);
    }

    const filters: string[] = [];
    if (s.invertColors) filters.push('invert(1) hue-rotate(180deg)');
    if (s.grayscale) filters.push('grayscale(1)');
    root.style.filter = filters.join(' ');
  }

  private loadSettings(): AccessibilitySettings {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return { ...DEFAULT_SETTINGS };
      return { ...DEFAULT_SETTINGS, ...JSON.parse(raw) };
    } catch {
      return { ...DEFAULT_SETTINGS };
    }
  }

  private saveSettings(s: AccessibilitySettings): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    } catch {
      /* private-mode/quota — nothing we can do */
    }
  }

  private detectLang(text: string): string {
    const docLang = document.documentElement.lang;
    if (docLang) return docLang.startsWith('he') ? 'he-IL' : docLang;
    const hebrewMatches = text.match(HEBREW_RANGE)?.length ?? 0;
    return hebrewMatches > text.length * 0.1 ? 'he-IL' : 'en-US';
  }

  private extractPageText(): string {
    const clone = document.body.cloneNode(true) as HTMLElement;
    clone
      .querySelectorAll('script, style, noscript, app-accessibility-widget, [aria-hidden="true"]')
      .forEach((el) => el.remove());
    return this.cleanTextForSpeech(clone.textContent || '');
  }

  // Strips emoji and decorative separator glyphs (·, •, arrows, ✕...) used throughout the
  // UI as visual dividers/icons — speech engines tend to read these out by name ("bullet",
  // "left arrow"), which drowns out the actual content and can also force an English voice
  // mid-sentence. Replacing them with a period keeps the natural pause without the noise.
  private cleanTextForSpeech(raw: string): string {
    return raw
      .replace(/\p{Extended_Pictographic}/gu, ' ')
      .replace(/[•·✕✖◦‣∙←→↑↓]/g, '.')
      .replace(/\.(\s*\.)+/g, '.')
      .replace(/\s+/g, ' ')
      .trim();
  }
}
