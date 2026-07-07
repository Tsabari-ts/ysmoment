import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { interval, Subscription, switchMap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import {
  EVENT_TYPE_LABELS,
  GuestEventResponse,
  MagnetSize,
  OrderStatus,
  PublicOrderView,
  SIZE_LABELS,
  STATUS_LABELS
} from '../../core/models';
import { ACCESSIBILITY_STATEMENT_HTML, PRIVACY_POLICY_HTML, TERMS_OF_USE_HTML } from '../../core/legal-content';
import { ModalComponent } from '../../shared/modal/modal.component';

const WHATSAPP_CONTACT_URL =
  'https://api.whatsapp.com/send?phone=972524225365&text=' +
  encodeURIComponent('היי, אשמח לשמוע פרטים נוספים על שירות הברקוד ולסגור אירוע');

function defaultForm() {
  return {
    customerName: '',
    phone: '',
    magnetSize: MagnetSize.Medium,
    quantity: 1,
    privacyAccepted: false
  };
}

@Component({
  selector: 'app-guest-order',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  templateUrl: './guest-order.component.html',
  styleUrl: './guest-order.component.scss'
})
export class GuestOrderComponent implements OnInit, OnDestroy {
  slug = '';
  event?: GuestEventResponse;
  sizes = SIZE_LABELS;
  eventTypeLabels = EVENT_TYPE_LABELS;
  whatsappContactUrl = WHATSAPP_CONTACT_URL;

  form = defaultForm();

  imageFile?: File;
  imagePreview?: string;
  loading = false;
  error = '';
  submitted = false;
  cancelled = false;
  order?: PublicOrderView;
  OrderStatus = OrderStatus;
  statusLabels = STATUS_LABELS;

  loadFailed = false;
  showFallback = false;
  diagnosticsSent = false;
  bannerDismissed = false;

  nameError = '';
  phoneError = '';
  quantityError = '';

  legalDocOpen: 'privacy' | 'terms' | 'accessibility' | null = null;
  privacyPolicyHtml = PRIVACY_POLICY_HTML;
  termsOfUseHtml = TERMS_OF_USE_HTML;
  accessibilityStatementHtml = ACCESSIBILITY_STATEMENT_HTML;

  private pollSub?: Subscription;
  private fallbackTimer?: ReturnType<typeof setTimeout>;

  constructor(private route: ActivatedRoute, private api: ApiService) {}

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.loadEvent();
  }

  private loadEvent(): void {
    this.event = undefined;
    this.loadFailed = false;
    this.showFallback = false;
    if (this.fallbackTimer) clearTimeout(this.fallbackTimer);

    this.api.getGuestEvent(this.slug).subscribe({
      next: (evt) => (this.event = evt),
      error: () => (this.loadFailed = true)
    });
    this.fallbackTimer = setTimeout(() => {
      if (!this.event && !this.loadFailed) this.showFallback = true;
    }, 8000);
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
    if (this.fallbackTimer) clearTimeout(this.fallbackTimer);
  }

  get availableSizes(): { value: MagnetSize; label: string }[] {
    if (!this.event) return [];
    const list: { value: MagnetSize; label: string }[] = [];
    if (this.event.sizeSmallAvailable) list.push({ value: MagnetSize.Small, label: SIZE_LABELS[MagnetSize.Small] });
    if (this.event.sizeMediumAvailable) list.push({ value: MagnetSize.Medium, label: SIZE_LABELS[MagnetSize.Medium] });
    if (this.event.sizeLargeAvailable) list.push({ value: MagnetSize.Large, label: SIZE_LABELS[MagnetSize.Large] });
    return list;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.imageFile = file;
    const reader = new FileReader();
    reader.onload = () => (this.imagePreview = reader.result as string);
    reader.readAsDataURL(file);
  }

  validateName(): boolean {
    if (!this.form.customerName.trim()) {
      this.nameError = 'נא להזין שם מלא';
      return false;
    }
    this.nameError = '';
    return true;
  }

  validatePhone(): boolean {
    if (!this.normalizeIsraeliPhone(this.form.phone)) {
      this.phoneError = 'נא להזין מספר טלפון תקין';
      return false;
    }
    this.phoneError = '';
    return true;
  }

  validateQuantity(): boolean {
    const max = this.event?.maxCopies ?? 1;
    if (!this.form.quantity || this.form.quantity < 1 || this.form.quantity > max) {
      this.quantityError = `ניתן לבחור עד ${max} מגנטים`;
      return false;
    }
    this.quantityError = '';
    return true;
  }

  private normalizeIsraeliPhone(phone: string): string | null {
    let digits = (phone || '').replace(/[^\d+]/g, '');
    if (digits.startsWith('+972')) digits = '0' + digits.slice(4);
    else if (digits.startsWith('972')) digits = '0' + digits.slice(3);
    return /^05\d{8}$/.test(digits) ? digits : null;
  }

  submit(): void {
    const nameOk = this.validateName();
    const phoneOk = this.validatePhone();
    const qtyOk = this.validateQuantity();
    if (!nameOk || !phoneOk || !qtyOk) return;

    if (!this.imageFile) {
      this.error = 'יש להעלות תמונה';
      return;
    }
    if (!this.form.privacyAccepted) {
      this.error = 'יש לאשר את מדיניות הפרטיות';
      return;
    }

    const fd = new FormData();
    fd.append('customerName', this.form.customerName);
    fd.append('phone', this.form.phone);
    fd.append('magnetSize', String(this.form.magnetSize));
    fd.append('quantity', String(this.form.quantity));
    fd.append('privacyAccepted', String(this.form.privacyAccepted));
    fd.append('image', this.imageFile);

    this.loading = true;
    this.error = '';
    this.api.createOrder(this.slug, fd).subscribe({
      next: (order) => {
        this.order = order;
        this.submitted = true;
        this.startPolling(order.publicToken);
      },
      error: (err) => {
        this.error = err.error?.message || 'שגיאה בשליחת ההזמנה';
        this.loading = false;
      },
      complete: () => (this.loading = false)
    });
  }

  cancelOrder(): void {
    if (!this.order || !confirm('לבטל את ההזמנה?')) return;
    this.api.cancelOrder(this.order.publicToken).subscribe({
      next: () => {
        this.pollSub?.unsubscribe();
        this.submitted = false;
        this.order = undefined;
        this.cancelled = true;
      },
      error: (err) => (this.error = err.error?.message || 'לא ניתן לבטל')
    });
  }

  startNewOrder(): void {
    this.cancelled = false;
    this.submitted = false;
    this.order = undefined;
    this.imageFile = undefined;
    this.imagePreview = undefined;
    this.form = defaultForm();
    this.nameError = '';
    this.phoneError = '';
    this.quantityError = '';
    this.error = '';
    // Re-check event/order availability from scratch instead of trusting the
    // event snapshot fetched when the guest first opened this page — it may
    // have since closed, paused, or ended while they were mid-order.
    this.loadEvent();
  }

  dismissBanner(): void {
    this.bannerDismissed = true;
  }

  openLegalDoc(doc: 'privacy' | 'terms' | 'accessibility'): void {
    this.legalDocOpen = doc;
  }

  closeLegalDoc(): void {
    this.legalDocOpen = null;
  }

  reportLoadIssue(): void {
    const payload = {
      slug: this.slug,
      url: window.location.href,
      userAgent: navigator.userAgent,
      online: navigator.onLine,
      timestamp: new Date().toISOString(),
      state: this.loadFailed ? 'load-failed' : this.event ? 'loaded' : 'still-loading'
    };
    this.api.reportClientIssue(payload).subscribe({
      next: () => (this.diagnosticsSent = true),
      error: () => (this.diagnosticsSent = true)
    });
  }

  get statusMessage(): string {
    if (!this.order) return '';
    switch (this.order.status) {
      case OrderStatus.New:
        return 'ההזמנה שלך בתור וממתינה לטיפול';
      case OrderStatus.InProgress:
        return 'המגנט שלך בטיפול כרגע! כמעט מוכן';
      case OrderStatus.Ready:
        return 'המגנט שלך מוכן ומחכה לך בלוח! 🎉';
      case OrderStatus.Cancelled:
        return 'ההזמנה בוטלה';
      default:
        return '';
    }
  }

  private startPolling(publicToken: string): void {
    this.pollSub = interval(5000)
      .pipe(switchMap(() => this.api.getOrderStatus(publicToken)))
      .subscribe((status) => {
        if (this.order) {
          this.order = {
            ...this.order,
            status: status.status,
            positionInQueue: status.positionInQueue,
            estimatedWaitMinutes: status.estimatedWaitMinutes
          };
        }
        if (status.status === OrderStatus.Ready || status.status === OrderStatus.Cancelled) {
          this.pollSub?.unsubscribe();
        }
      });
  }
}
