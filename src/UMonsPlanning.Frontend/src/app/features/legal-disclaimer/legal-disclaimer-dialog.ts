import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  viewChild,
} from '@angular/core';

/** localStorage key recording that the visitor has already accepted the disclaimer below. */
const ACCEPTED_STORAGE_KEY = 'umonsplanning.legal-disclaimer-accepted';

/**
 * Modal shown on first visit (any page): UMonsPlanning has no affiliation with the
 * Université de Mons or PRONOTE, and is offered as-is with no liability for calendar errors.
 * Declining navigates back in browser history ; accepting persists the choice in localStorage so
 * it is not asked again — see the two buttons below.
 */
@Component({
  selector: 'app-legal-disclaimer-dialog',
  templateUrl: './legal-disclaimer-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegalDisclaimerDialog {
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    afterNextRender(() => {
      if (window.localStorage.getItem(ACCEPTED_STORAGE_KEY) !== 'true') {
        this.dialog().nativeElement.showModal();
      }
    });
  }

  /** Blocks the Escape key from dismissing the dialog without an explicit accept/decline choice. */
  protected preventDismiss(event: Event): void {
    event.preventDefault();
  }

  protected decline(): void {
    window.history.back();
  }

  protected accept(): void {
    window.localStorage.setItem(ACCEPTED_STORAGE_KEY, 'true');
    this.dialog().nativeElement.close();
  }
}
