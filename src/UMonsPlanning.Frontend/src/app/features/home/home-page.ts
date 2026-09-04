import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CURRENT_ACADEMIC_YEAR } from '../../core/academic-year';
import { CalendarLinkForm } from '../calendar-link/calendar-link-form';
import { CalendarLinkCounter } from './calendar-link-counter';

@Component({
  selector: 'app-home-page',
  imports: [CalendarLinkForm, RouterLink, NgOptimizedImage, CalendarLinkCounter],
  templateUrl: './home-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  protected readonly academicYear = CURRENT_ACADEMIC_YEAR;
}
