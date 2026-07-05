import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { EVENT_TYPE_LABELS, EventType } from '../../core/models';

@Component({
  selector: 'app-create-event',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-event.component.html',
  styleUrl: './create-event.component.scss'
})
export class CreateEventComponent {
  eventTypes = Object.entries(EVENT_TYPE_LABELS).map(([k, v]) => ({ value: +k, label: v }));

  form = {
    name: '',
    hostNames: '',
    eventType: EventType.Wedding,
    date: new Date().toISOString().split('T')[0],
    sizeSmallAvailable: true,
    sizeMediumAvailable: true,
    sizeLargeAvailable: true,
    maxCopies: 6,
    averagePrepTimeMinutes: 2,
    isActive: true,
    ordersOpen: true
  };

  loading = false;
  error = '';

  constructor(private api: ApiService, private router: Router) {}

  submit(): void {
    this.loading = true;
    this.error = '';
    this.api.createEvent(this.form).subscribe({
      next: (evt) => this.router.navigate(['/admin/events', evt.id]),
      error: (err) => {
        this.error = err.error?.message || 'שגיאה ביצירת אירוע';
        this.loading = false;
      },
      complete: () => (this.loading = false)
    });
  }
}
