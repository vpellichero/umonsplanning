import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HomePage } from './home-page';

describe('HomePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
      deferBlockBehavior: DeferBlockBehavior.Manual,
    }).compileComponents();
  });

  it('should render the page title', () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Votre horaire UMONS');
  });

  it('should render a button to open the calendar link dialog', () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(compiled.querySelectorAll('button'));
    expect(buttons.some((b) => b.textContent?.includes('Générer mon lien de calendrier'))).toBe(true);
  });

  it('should not render the calendar link dialog before the button is interacted with', () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-calendar-link-dialog')).toBeNull();
  });

  it('should render the calendar link dialog once its @defer block completes', async () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();

    const [deferBlock] = await fixture.getDeferBlocks();
    await deferBlock.render(DeferBlockState.Complete);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-calendar-link-dialog')).not.toBeNull();
  });
});
