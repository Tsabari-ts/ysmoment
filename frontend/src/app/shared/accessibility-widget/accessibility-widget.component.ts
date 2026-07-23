import { Component, ElementRef, HostListener, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccessibilityService, AccessibilityToggleKey } from '../../core/accessibility.service';
import { FocusTrapDirective } from '../directives/focus-trap.directive';

interface ToggleFeature {
  key: AccessibilityToggleKey;
  label: string;
}

const READING_BAND_HALF_HEIGHT = 70;
const READING_GUIDE_HEIGHT = 42;

@Component({
  selector: 'app-accessibility-widget',
  standalone: true,
  imports: [CommonModule, FocusTrapDirective],
  templateUrl: './accessibility-widget.component.html',
  styleUrl: './accessibility-widget.component.scss'
})
export class AccessibilityWidgetComponent {
  readonly isOpen = signal(false);

  readonly displayToggles: ToggleFeature[] = [
    { key: 'readableFont', label: 'גופן קריא' },
    { key: 'invertColors', label: 'היפוך צבעים' },
    { key: 'grayscale', label: 'גווני אפור' }
  ];

  readonly emphasisToggles: ToggleFeature[] = [
    { key: 'highlightLinks', label: 'הדגשת קישורים' },
    { key: 'highlightButtons', label: 'הדגשת כפתורים' },
    { key: 'highlightHeadings', label: 'הדגשת כותרות' },
    { key: 'textSpacing', label: 'ריווח טקסט מוגדל' }
  ];

  readonly navToggles: ToggleFeature[] = [
    { key: 'disableAnimations', label: 'עצירת אנימציות ותנועה' },
    { key: 'largeCursor', label: 'סמן עכבר מוגדל' },
    { key: 'focusHighlight', label: 'הדגשת פוקוס מקלדת' },
    { key: 'hideImages', label: 'הסתרת תמונות' },
    { key: 'readingMask', label: 'מסכת קריאה' },
    { key: 'readingGuide', label: 'קו הכוונה לקריאה' },
    { key: 'magnifier', label: 'זכוכית מגדלת' }
  ];

  readonly maskTopHeight = computed(() => Math.max(0, this.a11y.mousePosition().y - READING_BAND_HALF_HEIGHT));
  readonly maskBottomTop = computed(() => this.a11y.mousePosition().y + READING_BAND_HALF_HEIGHT);
  readonly guideTop = computed(() => Math.max(0, this.a11y.mousePosition().y - READING_GUIDE_HEIGHT / 2));

  constructor(public a11y: AccessibilityService, private host: ElementRef<HTMLElement>) {}

  togglePanel(): void {
    this.isOpen.update((v) => !v);
  }

  closePanel(): void {
    this.isOpen.set(false);
  }

  toggle(key: AccessibilityToggleKey): void {
    this.a11y.toggle(key);
  }

  resetAll(): void {
    this.a11y.reset();
  }

  toggleReadPage(): void {
    if (this.a11y.isReading()) {
      this.a11y.stopReading();
    } else {
      this.a11y.readPage();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.isOpen()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.closePanel();
    }
  }

  @HostListener('document:mousemove', ['$event'])
  onDocumentMouseMove(event: MouseEvent): void {
    const s = this.a11y.state();
    if (!s.readingMask && !s.readingGuide && !s.magnifier) return;

    this.a11y.setMousePosition(event.clientX, event.clientY);

    if (s.magnifier) {
      const xPct = (event.clientX / window.innerWidth) * 100;
      const yPct = (event.clientY / window.innerHeight) * 100;
      document.documentElement.style.setProperty('--a11y-mx', `${xPct}%`);
      document.documentElement.style.setProperty('--a11y-my', `${yPct}%`);
    }
  }
}
