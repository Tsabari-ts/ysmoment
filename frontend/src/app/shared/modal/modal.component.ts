import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FocusTrapDirective } from '../directives/focus-trap.directive';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, FocusTrapDirective],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent {
  @Input() open = false;
  @Input() label = '';
  @Output() closed = new EventEmitter<void>();

  close(): void {
    this.closed.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.close();
  }
}
