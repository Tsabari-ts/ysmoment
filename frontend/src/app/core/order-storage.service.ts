import { Injectable } from '@angular/core';

const PREFIX = 'ysm_orders_';

@Injectable({ providedIn: 'root' })
export class OrderStorageService {
  getTokens(eventSlug: string): string[] {
    try {
      const raw = localStorage.getItem(PREFIX + eventSlug);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as string[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  addToken(eventSlug: string, token: string): void {
    const tokens = this.getTokens(eventSlug).filter((t) => t !== token);
    tokens.unshift(token);
    localStorage.setItem(PREFIX + eventSlug, JSON.stringify(tokens.slice(0, 20)));
  }

  setTokens(eventSlug: string, tokens: string[]): void {
    localStorage.setItem(PREFIX + eventSlug, JSON.stringify(tokens.slice(0, 20)));
  }

  removeToken(eventSlug: string, token: string): void {
    const tokens = this.getTokens(eventSlug).filter((t) => t !== token);
    if (tokens.length) {
      localStorage.setItem(PREFIX + eventSlug, JSON.stringify(tokens));
    } else {
      localStorage.removeItem(PREFIX + eventSlug);
    }
  }
}
