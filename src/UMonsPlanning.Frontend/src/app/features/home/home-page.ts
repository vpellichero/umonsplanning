import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CalendarLinkDialog } from '../calendar-link/calendar-link-dialog';

@Component({
  selector: 'app-home-page',
  imports: [CalendarLinkDialog, RouterLink, NgOptimizedImage],
  templateUrl: './home-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly calendarLinkDialog = viewChild.required(CalendarLinkDialog);

  openCalendarLinkDialog(): void {
    this.calendarLinkDialog().open();
  }
}
