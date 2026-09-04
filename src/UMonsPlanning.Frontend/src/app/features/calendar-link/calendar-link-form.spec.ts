import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CalendarLinkForm } from './calendar-link-form';

describe('CalendarLinkForm', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CalendarLinkForm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should render the formation select without requiring any interaction first', () => {
    const fixture = TestBed.createComponent(CalendarLinkForm);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('select#formation')).not.toBeNull();
  });

  it('should not show a generated link before a formation is chosen', () => {
    const fixture = TestBed.createComponent(CalendarLinkForm);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#generated-url')).toBeNull();
  });
});
