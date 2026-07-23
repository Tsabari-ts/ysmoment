import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AccessibilityWidgetComponent } from './shared/accessibility-widget/accessibility-widget.component';
import { CookieConsentComponent } from './shared/cookie-consent/cookie-consent.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, AccessibilityWidgetComponent, CookieConsentComponent],
  templateUrl: './app.component.html',
  styles: [':host { display: block; min-height: 100vh; }']
})
export class AppComponent {}
