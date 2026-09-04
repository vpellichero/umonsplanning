import { TestBed } from '@angular/core/testing';
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { LegalDisclaimerDialog } from './legal-disclaimer-dialog';

const STORAGE_KEY = 'umonsplanning.legal-disclaimer-accepted';

/** In-memory Storage stand-in: jsdom's own localStorage is shadowed by Node's native (and, in
 * this test runner, non-functional without extra flags) global of the same name. */
function createStorageStub(): Storage {
  const store = new Map<string, string>();
  return {
    getItem: (key: string) => store.get(key) ?? null,
    setItem: (key: string, value: string) => void store.set(key, value),
    removeItem: (key: string) => void store.delete(key),
    clear: () => store.clear(),
    key: (index: number) => [...store.keys()][index] ?? null,
    get length() {
      return store.size;
    },
  } as Storage;
}

describe('LegalDisclaimerDialog', () => {
  beforeAll(() => {
    // jsdom does not implement HTMLDialogElement.showModal()/close() (both are `undefined`) —
    // a minimal polyfill lets these tests exercise the component's actual open/close logic.
    if (typeof HTMLDialogElement.prototype.showModal !== 'function') {
      HTMLDialogElement.prototype.showModal = function (this: HTMLDialogElement) {
        this.setAttribute('open', '');
      };
    }

    if (typeof HTMLDialogElement.prototype.close !== 'function') {
      HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
        this.removeAttribute('open');
      };
    }
  });

  beforeEach(async () => {
    Object.defineProperty(window, 'localStorage', {
      value: createStorageStub(),
      configurable: true,
    });
    await TestBed.configureTestingModule({ imports: [LegalDisclaimerDialog] }).compileComponents();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('opens automatically when the disclaimer has not been accepted yet', async () => {
    const fixture = TestBed.createComponent(LegalDisclaimerDialog);
    fixture.detectChanges();
    await fixture.whenStable();

    const dialog = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    expect(dialog.open).toBe(true);
  });

  it('does not reopen once the disclaimer has already been accepted', async () => {
    window.localStorage.setItem(STORAGE_KEY, 'true');
    const fixture = TestBed.createComponent(LegalDisclaimerDialog);
    fixture.detectChanges();
    await fixture.whenStable();

    const dialog = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    expect(dialog.open).toBe(false);
  });

  it('persists acceptance to localStorage and closes the dialog', async () => {
    const fixture = TestBed.createComponent(LegalDisclaimerDialog);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const acceptButton = Array.from(compiled.querySelectorAll('button')).find((b) =>
      b.textContent?.includes("J'accepte"),
    )!;
    acceptButton.click();
    fixture.detectChanges();

    expect(window.localStorage.getItem(STORAGE_KEY)).toBe('true');
    expect((compiled.querySelector('dialog') as HTMLDialogElement).open).toBe(false);
  });

  it('navigates back in browser history when declined', async () => {
    const fixture = TestBed.createComponent(LegalDisclaimerDialog);
    fixture.detectChanges();
    await fixture.whenStable();

    const backSpy = vi.spyOn(window.history, 'back').mockImplementation(() => {});
    const compiled = fixture.nativeElement as HTMLElement;
    const declineButton = Array.from(compiled.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Refuser'),
    )!;
    declineButton.click();

    expect(backSpy).toHaveBeenCalledOnce();
    expect(window.localStorage.getItem(STORAGE_KEY)).toBeNull();
  });
});
