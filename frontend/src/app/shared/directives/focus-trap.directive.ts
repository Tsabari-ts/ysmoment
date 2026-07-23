import { AfterViewInit, Directive, ElementRef, EventEmitter, HostListener, OnDestroy, Output } from '@angular/core';

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Traps Tab focus inside the host element, focuses the first focusable child on attach,
 * restores focus to whatever was focused before on detach, and emits (escapePressed) on Escape.
 * Meant for elements that are added/removed from the DOM via @if (dialogs, popovers) so
 * ngAfterViewInit/ngOnDestroy line up with open/close.
 */
@Directive({
  selector: '[appFocusTrap]',
  standalone: true
})
export class FocusTrapDirective implements AfterViewInit, OnDestroy {
  @Output() escapePressed = new EventEmitter<void>();

  private previouslyFocused?: HTMLElement;

  constructor(private host: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    this.previouslyFocused = document.activeElement as HTMLElement;
    setTimeout(() => this.focusFirstElement());
  }

  ngOnDestroy(): void {
    this.previouslyFocused?.focus();
  }

  private focusFirstElement(): void {
    const container = this.host.nativeElement;
    const focusable = container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR);
    (focusable[0] ?? container).focus();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.escapePressed.emit();
      return;
    }

    if (event.key === 'Tab') {
      const container = this.host.nativeElement;
      const focusable = Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
      if (focusable.length === 0) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }
  }
}
