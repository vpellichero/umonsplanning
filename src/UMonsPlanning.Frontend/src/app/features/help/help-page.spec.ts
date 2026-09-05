import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HelpPage } from './help-page';

describe('HelpPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpPage],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should render a help entry for each supported calendar application', () => {
    const fixture = TestBed.createComponent(HelpPage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('li').length).toBeGreaterThanOrEqual(4);
  });

  it('should render only https official-guide links opened in a new tab', () => {
    const fixture = TestBed.createComponent(HelpPage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const anchors = Array.from(
      compiled.querySelectorAll<HTMLAnchorElement>('#calendar-apps a[target="_blank"]'),
    );
    expect(anchors.length).toBeGreaterThan(0);
    for (const anchor of anchors) {
      expect(anchor.href.startsWith('https://')).toBe(true);
      expect(anchor.target).toBe('_blank');
      expect(anchor.rel).toContain('noopener');
    }
  });

  it('should render an internal detailed-guide link for each entry', () => {
    const fixture = TestBed.createComponent(HelpPage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const guideLinks = Array.from(compiled.querySelectorAll<HTMLAnchorElement>('#calendar-apps a'))
      .filter((anchor) => anchor.target !== '_blank');
    expect(guideLinks.length).toBeGreaterThanOrEqual(5);
    for (const link of guideLinks) {
      expect(link.getAttribute('href')).toMatch(/^\/horaire-umons-/);
    }
  });
});
